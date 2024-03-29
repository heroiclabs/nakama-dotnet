﻿// ---------------------------------------------------------------------
// Copyright 2018 David Haig
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// ---------------------------------------------------------------------

using System;
using System.IO;
using System.IO.Compression;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if RELEASESIGNED
[assembly: InternalsVisibleTo("Ninja.WebSockets.UnitTests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100b1707056f4761b7846ed503642fcde97fc350c939f78026211304a56ba51e094c9cefde77fadce5b83c0a621c17f032c37c520b6d9ab2da8291a21472175d9caad55bf67bab4bffb46a96f864ea441cf695edc854296e02a44062245a4e09ccd9a77ef6146ecf941ce1d9da078add54bc2d4008decdac2fa2b388e17794ee6a6")]
#else
[assembly: InternalsVisibleTo("Ninja.WebSockets.UnitTests")]
#endif

namespace Nakama.Ninja.WebSockets.Internal
{
    /// <summary>
    /// Main implementation of the WebSocket abstract class
    /// </summary>
    internal class WebSocketImplementation : WebSocket
    {
        private readonly Guid _guid;
        private readonly Func<MemoryStream> _recycledStreamFactory;
        private readonly System.IO.Stream _stream;
        private readonly bool _includeExceptionInCloseResponse;
        private readonly bool _isClient;
        private readonly string _subProtocol;
        private CancellationTokenSource _internalReadCts;
        private WebSocketState _state;
        private readonly IPingPongManager _pingPongManager;
        private bool _isContinuationFrame;
        private WebSocketMessageType _continuationFrameMessageType = WebSocketMessageType.Binary;
        private WebSocketReadCursor _readCursor;
        private readonly bool _usePerMessageDeflate = false;
        private bool _tryGetBufferFailureLogged = false;
        const int MAX_PING_PONG_PAYLOAD_LEN = 125;
        private WebSocketCloseStatus? _closeStatus;
        private string _closeStatusDescription;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public event EventHandler<PongEventArgs> Pong;

        internal WebSocketImplementation(Guid guid, Func<MemoryStream> recycledStreamFactory, System.IO.Stream stream,
            TimeSpan keepAliveInterval, string secWebSocketExtensions, bool includeExceptionInCloseResponse,
            bool isClient, string subProtocol)
        {
            _guid = guid;
            _recycledStreamFactory = recycledStreamFactory;
            _stream = stream;
            _isClient = isClient;
            _subProtocol = subProtocol;
            _internalReadCts = new CancellationTokenSource();
            _state = WebSocketState.Open;
            _readCursor = new WebSocketReadCursor(null, 0, 0);

            if (secWebSocketExtensions?.IndexOf("permessage-deflate") >= 0)
            {
                _usePerMessageDeflate = true;
            }

            KeepAliveInterval = keepAliveInterval;
            _includeExceptionInCloseResponse = includeExceptionInCloseResponse;
            if (keepAliveInterval.Ticks < 0)
            {
                throw new InvalidOperationException("KeepAliveInterval must be Zero or positive");
            }

            if (keepAliveInterval != TimeSpan.Zero)
            {
                _pingPongManager = new PingPongManager(guid, this, keepAliveInterval, _internalReadCts.Token);
            }
        }

        public override WebSocketCloseStatus? CloseStatus => _closeStatus;

        public override string CloseStatusDescription => _closeStatusDescription;

        public override WebSocketState State { get { return _state; } }

        public override string SubProtocol => _subProtocol;

        public TimeSpan KeepAliveInterval { get; private set; }

        /// <summary>
        /// Receive web socket result
        /// </summary>
        /// <param name="buffer">The buffer to copy data into</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The web socket result details</returns>
        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            try
            {
                // we may receive control frames so reading needs to happen in an infinite loop
                while (true)
                {
                    // allow this operation to be cancelled from iniside OR outside this instance
                    using (CancellationTokenSource linkedCts =
                           CancellationTokenSource.CreateLinkedTokenSource(_internalReadCts.Token, cancellationToken))
                    {
                        WebSocketFrame frame;

                        try
                        {
                            if (_readCursor.NumBytesLeftToRead > 0)
                            {
                                // If the buffer used to read the frame was too small to fit the whole frame then we need to "remember" this frame
                                // and return what we have. Subsequent calls to the read function will simply continue reading off the stream without 
                                // decoding the first few bytes as a websocket header.
                                _readCursor =
                                    await WebSocketFrameReader.ReadFromCursorAsync(_stream, buffer, _readCursor,
                                        linkedCts.Token);
                                frame = _readCursor.WebSocketFrame;
                            }
                            else
                            {
                                _readCursor = await WebSocketFrameReader.ReadAsync(_stream, buffer, linkedCts.Token);
                                frame = _readCursor.WebSocketFrame;
                            }
                        }
                        catch (InternalBufferOverflowException ex)
                        {
                            await CloseOutputAutoTimeoutAsync(WebSocketCloseStatus.MessageTooBig,
                                "Frame too large to fit in buffer. Use message fragmentation", ex);
                            throw;
                        }
                        catch (ArgumentOutOfRangeException ex)
                        {
                            await CloseOutputAutoTimeoutAsync(WebSocketCloseStatus.ProtocolError,
                                "Payload length out of range", ex);
                            throw;
                        }
                        catch (EndOfStreamException ex)
                        {
                            await CloseOutputAutoTimeoutAsync(WebSocketCloseStatus.InvalidPayloadData,
                                "Unexpected end of stream encountered", ex);
                            throw;
                        }
                        catch (OperationCanceledException ex)
                        {
                            await CloseOutputAutoTimeoutAsync(WebSocketCloseStatus.EndpointUnavailable,
                                "Operation cancelled", ex);
                            throw;
                        }
                        catch (Exception ex)
                        {
                            await CloseOutputAutoTimeoutAsync(WebSocketCloseStatus.InternalServerError,
                                "Error reading WebSocket frame", ex);
                            throw;
                        }

                        var endOfMessage = frame.IsFinBitSet && _readCursor.NumBytesLeftToRead == 0;
                        switch (frame.OpCode)
                        {
                            case WebSocketOpCode.ConnectionClose:
                                return await RespondToCloseFrame(frame, buffer, linkedCts.Token);
                            case WebSocketOpCode.Ping:
                                ArraySegment<byte> pingPayload = new ArraySegment<byte>(buffer.Array, buffer.Offset,
                                    _readCursor.NumBytesRead);
                                await SendPongAsync(pingPayload, linkedCts.Token);
                                break;
                            case WebSocketOpCode.Pong:
                                ArraySegment<byte> pongBuffer = new ArraySegment<byte>(buffer.Array,
                                    _readCursor.NumBytesRead, buffer.Offset);
                                Pong?.Invoke(this, new PongEventArgs(pongBuffer));
                                break;
                            case WebSocketOpCode.TextFrame:
                                if (!frame.IsFinBitSet)
                                {
                                    // continuation frames will follow, record the message type Text
                                    _continuationFrameMessageType = WebSocketMessageType.Text;
                                }

                                return new WebSocketReceiveResult(_readCursor.NumBytesRead, WebSocketMessageType.Text,
                                    endOfMessage);
                            case WebSocketOpCode.BinaryFrame:
                                if (!frame.IsFinBitSet)
                                {
                                    // continuation frames will follow, record the message type Binary
                                    _continuationFrameMessageType = WebSocketMessageType.Binary;
                                }

                                return new WebSocketReceiveResult(_readCursor.NumBytesRead, WebSocketMessageType.Binary,
                                    endOfMessage);
                            case WebSocketOpCode.ContinuationFrame:
                                return new WebSocketReceiveResult(_readCursor.NumBytesRead,
                                    _continuationFrameMessageType, endOfMessage);
                            default:
                                Exception ex = new NotSupportedException($"Unknown WebSocket opcode {frame.OpCode}");
                                await CloseOutputAutoTimeoutAsync(WebSocketCloseStatus.ProtocolError, ex.Message, ex);
                                throw ex;
                        }
                    }
                }
            }
            catch (Exception catchAll)
            {
                // Most exceptions will be caught closer to their source to send an appropriate close message (and set the WebSocketState)
                // However, if an unhandled exception is encountered and a close message not sent then send one here
                if (_state == WebSocketState.Open)
                {
                    await CloseOutputAutoTimeoutAsync(WebSocketCloseStatus.InternalServerError,
                        "Unexpected error reading from WebSocket", catchAll);
                }

                throw;
            }
        }

        /// <summary>
        /// Send data to the web socket
        /// </summary>
        /// <param name="buffer">the buffer containing data to send</param>
        /// <param name="messageType">The message type. Can be Text or Binary</param>
        /// <param name="endOfMessage">True if this message is a standalone message (this is the norm)
        /// If it is a multi-part message then false (and true for the last message)</param>
        /// <param name="cancellationToken">the cancellation token</param>
        public override async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType,
            bool endOfMessage, CancellationToken cancellationToken)
        {
            using (MemoryStream stream = _recycledStreamFactory())
            {
                WebSocketOpCode opCode = GetOppCode(messageType);

                if (_usePerMessageDeflate)
                {
                    // NOTE: Compression is currently work in progress and should NOT be used in this library.
                    // The code below is very inefficient for small messages. Ideally we would like to have some sort of moving window
                    // of data to get the best compression. And we don't want to create new buffers which is bad for GC.
                    using (MemoryStream temp = new MemoryStream())
                    {
                        DeflateStream deflateStream = new DeflateStream(temp, CompressionMode.Compress);
                        deflateStream.Write(buffer.Array, buffer.Offset, buffer.Count);
                        deflateStream.Flush();
                        var compressedBuffer = new ArraySegment<byte>(temp.ToArray());
                        WebSocketFrameWriter.Write(opCode, compressedBuffer, stream, endOfMessage, _isClient);
                    }
                }
                else
                {
                    WebSocketFrameWriter.Write(opCode, buffer, stream, endOfMessage, _isClient);
                }

                await WriteStreamToNetwork(stream, cancellationToken);
                _isContinuationFrame = !endOfMessage; // TODO: is this correct??
            }
        }

        /// <summary>
        /// Call this automatically from server side each keepAliveInterval period
        /// NOTE: ping payload must be 125 bytes or less
        /// </summary>
        public async Task SendPingAsync(ArraySegment<byte> payload, CancellationToken cancellationToken)
        {
            if (payload.Count > MAX_PING_PONG_PAYLOAD_LEN)
            {
                throw new InvalidOperationException(
                    $"Cannot send Ping: Max ping message size {MAX_PING_PONG_PAYLOAD_LEN} exceeded: {payload.Count}");
            }

            if (_state == WebSocketState.Open)
            {
                using (MemoryStream stream = _recycledStreamFactory())
                {
                    WebSocketFrameWriter.Write(WebSocketOpCode.Ping, payload, stream, true, _isClient);
                    await WriteStreamToNetwork(stream, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Aborts the WebSocket without sending a Close frame
        /// </summary>
        public override void Abort()
        {
            _state = WebSocketState.Aborted;
            _internalReadCts.Cancel();
        }

        /// <summary>
        /// Polite close (use the close handshake)
        /// </summary>
        public override async Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription,
            CancellationToken cancellationToken)
        {
            if (_state == WebSocketState.Open)
            {
                using (MemoryStream stream = _recycledStreamFactory())
                {
                    ArraySegment<byte> buffer = BuildClosePayload(closeStatus, statusDescription);
                    WebSocketFrameWriter.Write(WebSocketOpCode.ConnectionClose, buffer, stream, true, _isClient);
                    await WriteStreamToNetwork(stream, cancellationToken);
                    _state = WebSocketState.CloseSent;
                }
            }
        }

        /// <summary>
        /// Fire and forget close
        /// </summary>
        public override async Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription,
            CancellationToken cancellationToken)
        {
            if (_state == WebSocketState.Open)
            {
                _state = WebSocketState.Closed; // set this before we write to the network because the write may fail

                using (MemoryStream stream = _recycledStreamFactory())
                {
                    ArraySegment<byte> buffer = BuildClosePayload(closeStatus, statusDescription);
                    WebSocketFrameWriter.Write(WebSocketOpCode.ConnectionClose, buffer, stream, true, _isClient);
                    await WriteStreamToNetwork(stream, cancellationToken).ConfigureAwait(false);
                }
            }

            // cancel pending reads
            _internalReadCts.Cancel();
        }

        /// <summary>
        /// Dispose will send a close frame if the connection is still open
        /// </summary>
        public override void Dispose()
        {
            try
            {
                if (_state == WebSocketState.Open)
                {
                    CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    try
                    {
                        CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, "Service is Disposed", cts.Token)
                            .Wait();
                    }
                    catch (OperationCanceledException) {}
                }

                // cancel pending reads - usually does nothing
                _internalReadCts.Cancel();
                _stream.Close();
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        /// Called when a Pong frame is received
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnPong(PongEventArgs e)
        {
            Pong?.Invoke(this, e);
        }

        /// <summary>
        /// As per the spec, write the close status followed by the close reason
        /// </summary>
        /// <param name="closeStatus">The close status</param>
        /// <param name="statusDescription">Optional extra close details</param>
        /// <returns>The payload to sent in the close frame</returns>
        private ArraySegment<byte> BuildClosePayload(WebSocketCloseStatus closeStatus, string statusDescription)
        {
            byte[] statusBuffer = BitConverter.GetBytes((ushort)closeStatus);
            Array.Reverse(statusBuffer); // network byte order (big endian)

            if (statusDescription == null)
            {
                return new ArraySegment<byte>(statusBuffer);
            }
            else
            {
                byte[] descBuffer = Encoding.UTF8.GetBytes(statusDescription);
                byte[] payload = new byte[statusBuffer.Length + descBuffer.Length];
                Buffer.BlockCopy(statusBuffer, 0, payload, 0, statusBuffer.Length);
                Buffer.BlockCopy(descBuffer, 0, payload, statusBuffer.Length, descBuffer.Length);
                return new ArraySegment<byte>(payload);
            }
        }

        /// NOTE: pong payload must be 125 bytes or less
        /// Pong should contain the same payload as the ping
        private async Task SendPongAsync(ArraySegment<byte> payload, CancellationToken cancellationToken)
        {
            // as per websocket spec
            if (payload.Count > MAX_PING_PONG_PAYLOAD_LEN)
            {
                Exception ex =
                    new InvalidOperationException(
                        $"Max ping message size {MAX_PING_PONG_PAYLOAD_LEN} exceeded: {payload.Count}");
                await CloseOutputAutoTimeoutAsync(WebSocketCloseStatus.ProtocolError, ex.Message, ex);
                throw ex;
            }

            try
            {
                if (_state == WebSocketState.Open)
                {
                    using (MemoryStream stream = _recycledStreamFactory())
                    {
                        WebSocketFrameWriter.Write(WebSocketOpCode.Pong, payload, stream, true, _isClient);
                        await WriteStreamToNetwork(stream, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                await CloseOutputAutoTimeoutAsync(WebSocketCloseStatus.EndpointUnavailable,
                    "Unable to send Pong response", ex);
                throw;
            }
        }

        /// <summary>
        /// Called when a Close frame is received
        /// Send a response close frame if applicable
        /// </summary>
        private async Task<WebSocketReceiveResult> RespondToCloseFrame(WebSocketFrame frame, ArraySegment<byte> buffer,
            CancellationToken token)
        {
            _closeStatus = frame.CloseStatus;
            _closeStatusDescription = frame.CloseStatusDescription;

            if (_state == WebSocketState.CloseSent)
            {
                // this is a response to close handshake initiated by this instance
                _state = WebSocketState.Closed;
            }
            else if (_state == WebSocketState.Open)
            {
                // do not echo the close payload back to the client, there is no requirement for it in the spec.
                // However, the same CloseStatus as recieved should be sent back.
                ArraySegment<byte> closePayload = new ArraySegment<byte>(new byte[0], 0, 0);
                _state = WebSocketState.CloseReceived;

                using (MemoryStream stream = _recycledStreamFactory())
                {
                    WebSocketFrameWriter.Write(WebSocketOpCode.ConnectionClose, closePayload, stream, true, _isClient);
                    await WriteStreamToNetwork(stream, token);
                }
            }

            return new WebSocketReceiveResult(frame.Count, WebSocketMessageType.Close, frame.IsFinBitSet,
                frame.CloseStatus, frame.CloseStatusDescription);
        }

        /// <summary>
        /// Note that the way in which the stream buffer is accessed can lead to significant performance problems
        /// You want to avoid a call to stream.ToArray to avoid extra memory allocation
        /// MemoryStream can be configured to have its internal buffer accessible.
        /// </summary>
        private ArraySegment<byte> GetBuffer(MemoryStream stream)
        {
#if NET45
            // NET45 does not have a TryGetBuffer function on Stream
            if (_tryGetBufferFailureLogged)
            {
                return new ArraySegment<byte>(stream.ToArray(), 0, (int)stream.Position);
            }

            // note that a MemoryStream will throw an UnuthorizedAccessException if the internal buffer is not public. Set publiclyVisible = true
            try
            {
                return new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Position);
            }
            catch (UnauthorizedAccessException)
            {
                _tryGetBufferFailureLogged = true;
                return new ArraySegment<byte>(stream.ToArray(), 0, (int)stream.Position);
            }
#else
            // Avoid calling ToArray on the MemoryStream because it allocates a new byte array on tha heap
            // We avoid this by attempting to access the internal memory stream buffer
            // This works with supported streams like the recyclable memory stream and writable memory streams
            ArraySegment<byte> buffer;
            if (!stream.TryGetBuffer(out buffer))
            {
                if (!_tryGetBufferFailureLogged)
                {
                    _tryGetBufferFailureLogged = true;
                }

                // internal buffer not suppoted, fall back to ToArray()
                byte[] array = stream.ToArray();
                buffer = new ArraySegment<byte>(array, 0, array.Length);
            }

            return new ArraySegment<byte>(buffer.Array, buffer.Offset, (int)stream.Position);
#endif
        }

        /// <summary>
        /// Puts data on the wire
        /// </summary>
        /// <param name="stream">The stream to read data from</param>
        private async Task WriteStreamToNetwork(MemoryStream stream, CancellationToken cancellationToken)
        {
            ArraySegment<byte> buffer = GetBuffer(stream);
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _stream.WriteAsync(buffer.Array, buffer.Offset, buffer.Count, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Turns a spec websocket frame opcode into a WebSocketMessageType
        /// </summary>
        private WebSocketOpCode GetOppCode(WebSocketMessageType messageType)
        {
            if (_isContinuationFrame)
            {
                return WebSocketOpCode.ContinuationFrame;
            }
            else
            {
                switch (messageType)
                {
                    case WebSocketMessageType.Binary:
                        return WebSocketOpCode.BinaryFrame;
                    case WebSocketMessageType.Text:
                        return WebSocketOpCode.TextFrame;
                    case WebSocketMessageType.Close:
                        throw new NotSupportedException(
                            "Cannot use Send function to send a close frame. Use Close function.");
                    default:
                        throw new NotSupportedException($"MessageType {messageType} not supported");
                }
            }
        }

        /// <summary>
        /// Automatic WebSocket close in response to some invalid data from the remote websocket host
        /// </summary>
        /// <param name="closeStatus">The close status to use</param>
        /// <param name="statusDescription">A description of why we are closing</param>
        /// <param name="ex">The exception (for logging)</param>
        private async Task CloseOutputAutoTimeoutAsync(WebSocketCloseStatus closeStatus, string statusDescription,
            Exception ex)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(5);

            try
            {
                // we may not want to send sensitive information to the client / server
                if (_includeExceptionInCloseResponse)
                {
                    statusDescription = statusDescription + "\r\n\r\n" + ex.ToString();
                }

                var autoCancel = new CancellationTokenSource(timeSpan);
                await CloseOutputAsync(closeStatus, statusDescription, autoCancel.Token);
            }
            catch (OperationCanceledException)
            {
                // do not throw an exception because that will mask the original exception
            }
            catch
            {
                // do not throw an exception because that will mask the original exception
            }
        }
    }
}
