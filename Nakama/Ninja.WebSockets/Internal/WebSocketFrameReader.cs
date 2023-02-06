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
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nakama.Ninja.WebSockets.Internal
{
    /// <summary>
    /// Reads a WebSocket frame
    /// see http://tools.ietf.org/html/rfc6455 for specification
    /// </summary>
    internal static class WebSocketFrameReader
    {
        private static int CalculateNumBytesToRead(int numBytesLetfToRead, int bufferSize)
        {
            if (bufferSize < numBytesLetfToRead)
            {
                // the count needs to be a multiple of the mask key
                return bufferSize - bufferSize % 4;
            }
            else
            {
                return numBytesLetfToRead;
            }
        }

        /// <summary>
        /// The last read could not be completed because the read buffer was too small. 
        /// We need to continue reading bytes off the stream.
        /// Not to be confused with a continuation frame
        /// </summary>
        /// <param name="fromStream">The stream to read from</param>
        /// <param name="intoBuffer">The buffer to read into</param>
        /// <param name="readCursor">The previous partial websocket frame read plus cursor information</param>
        /// <param name="cancellationToken">the cancellation token</param>
        /// <returns>A websocket frame</returns>
        public static async Task<WebSocketReadCursor> ReadFromCursorAsync(System.IO.Stream fromStream,
            ArraySegment<byte> intoBuffer, WebSocketReadCursor readCursor, CancellationToken cancellationToken)
        {
            var remainingFrame = readCursor.WebSocketFrame;
            var minCount = CalculateNumBytesToRead(readCursor.NumBytesLeftToRead, intoBuffer.Count);
            await BinaryReaderWriter.ReadExactly(minCount, fromStream, intoBuffer, cancellationToken);
            if (remainingFrame.MaskKey.Count > 0)
            {
                ArraySegment<byte> payloadToMask =
                    new ArraySegment<byte>(intoBuffer.Array, intoBuffer.Offset, minCount);
                WebSocketFrameCommon.ToggleMask(remainingFrame.MaskKey, payloadToMask);
            }

            return new WebSocketReadCursor(remainingFrame, minCount, readCursor.NumBytesLeftToRead - minCount);
        }

        /// <summary>
        /// Read a WebSocket frame from the stream
        /// </summary>
        /// <param name="fromStream">The stream to read from</param>
        /// <param name="intoBuffer">The buffer to read into</param>
        /// <param name="cancellationToken">the cancellation token</param>
        /// <returns>A websocket frame</returns>
        public static async Task<WebSocketReadCursor> ReadAsync(System.IO.Stream fromStream, ArraySegment<byte> intoBuffer,
            CancellationToken cancellationToken)
        {
            // allocate a small buffer to read small chunks of data from the stream
            var smallBuffer = new ArraySegment<byte>(new byte[8]);

            await BinaryReaderWriter.ReadExactly(2, fromStream, smallBuffer, cancellationToken);
            byte byte1 = smallBuffer.Array[0];
            byte byte2 = smallBuffer.Array[1];

            // process first byte
            byte finBitFlag = 0x80;
            byte opCodeFlag = 0x0F;
            bool isFinBitSet = (byte1 & finBitFlag) == finBitFlag;
            WebSocketOpCode opCode = (WebSocketOpCode)(byte1 & opCodeFlag);

            // read and process second byte
            byte maskFlag = 0x80;
            bool isMaskBitSet = (byte2 & maskFlag) == maskFlag;
            uint len = await ReadLength(byte2, smallBuffer, fromStream, cancellationToken);
            int count = (int)len;
            var minCount = CalculateNumBytesToRead(count, intoBuffer.Count);
            ArraySegment<byte> maskKey = new ArraySegment<byte>();

            try
            {
                // use the masking key to decode the data if needed
                if (isMaskBitSet)
                {
                    maskKey = new ArraySegment<byte>(smallBuffer.Array, 0, WebSocketFrameCommon.MaskKeyLength);
                    await BinaryReaderWriter.ReadExactly(maskKey.Count, fromStream, maskKey, cancellationToken);
                    await BinaryReaderWriter.ReadExactly(minCount, fromStream, intoBuffer, cancellationToken);
                    ArraySegment<byte> payloadToMask =
                        new ArraySegment<byte>(intoBuffer.Array, intoBuffer.Offset, minCount);
                    WebSocketFrameCommon.ToggleMask(maskKey, payloadToMask);
                }
                else
                {
                    await BinaryReaderWriter.ReadExactly(minCount, fromStream, intoBuffer, cancellationToken);
                }
            }
            catch (InternalBufferOverflowException e)
            {
                throw new InternalBufferOverflowException(
                    $"Supplied buffer too small to read {0} bytes from {Enum.GetName(typeof(WebSocketOpCode), opCode)} frame",
                    e);
            }

            WebSocketFrame frame;
            if (opCode == WebSocketOpCode.ConnectionClose)
            {
                frame = DecodeCloseFrame(isFinBitSet, opCode, count, intoBuffer, maskKey);
            }
            else
            {
                // note that by this point the payload will be populated
                frame = new WebSocketFrame(isFinBitSet, opCode, count, maskKey);
            }

            return new WebSocketReadCursor(frame, minCount, count - minCount);
        }

        /// <summary>
        /// Extracts close status and close description information from the web socket frame
        /// </summary>
        private static WebSocketFrame DecodeCloseFrame(bool isFinBitSet, WebSocketOpCode opCode, int count,
            ArraySegment<byte> buffer, ArraySegment<byte> maskKey)
        {
            WebSocketCloseStatus closeStatus;
            string closeStatusDescription;

            if (count >= 2)
            {
                Array.Reverse(buffer.Array, buffer.Offset, 2); // network byte order
                int closeStatusCode = (int)BitConverter.ToUInt16(buffer.Array, buffer.Offset);
                if (Enum.IsDefined(typeof(WebSocketCloseStatus), closeStatusCode))
                {
                    closeStatus = (WebSocketCloseStatus)closeStatusCode;
                }
                else
                {
                    closeStatus = WebSocketCloseStatus.Empty;
                }

                int offset = buffer.Offset + 2;
                int descCount = count - 2;

                if (descCount > 0)
                {
                    closeStatusDescription = Encoding.UTF8.GetString(buffer.Array, offset, descCount);
                }
                else
                {
                    closeStatusDescription = null;
                }
            }
            else
            {
                closeStatus = WebSocketCloseStatus.Empty;
                closeStatusDescription = null;
            }

            return new WebSocketFrame(isFinBitSet, opCode, count, closeStatus, closeStatusDescription, maskKey);
        }

        /// <summary>
        /// Reads the length of the payload according to the contents of byte2
        /// </summary>
        private static async Task<uint> ReadLength(byte byte2, ArraySegment<byte> smallBuffer, System.IO.Stream fromStream,
            CancellationToken cancellationToken)
        {
            byte payloadLenFlag = 0x7F;
            uint len = (uint)(byte2 & payloadLenFlag);

            // read a short length or a long length depending on the value of len
            if (len == 126)
            {
                len = await BinaryReaderWriter.ReadUShortExactly(fromStream, false, smallBuffer, cancellationToken);
            }
            else if (len == 127)
            {
                len = (uint)await BinaryReaderWriter.ReadULongExactly(fromStream, false, smallBuffer,
                    cancellationToken);
                const uint
                    maxLen = 2147483648; // 2GB - not part of the spec but just a precaution. Send large volumes of data in smaller frames.

                // protect ourselves against bad data
                if (len > maxLen || len < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        $"Payload length out of range. Min 0 max 2GB. Actual {len:#,##0} bytes.");
                }
            }

            return len;
        }
    }
}