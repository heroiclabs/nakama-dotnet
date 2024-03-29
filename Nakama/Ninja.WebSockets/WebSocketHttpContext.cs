﻿using System.Collections.Generic;

namespace Nakama.Ninja.WebSockets
{
    /// <summary>
    /// The WebSocket HTTP Context used to initiate a WebSocket handshake
    /// </summary>
    public class WebSocketHttpContext
    {
        /// <summary>
        /// True if this is a valid WebSocket request
        /// </summary>
        public bool IsWebSocketRequest { get; private set; }

        public IList<string> WebSocketRequestedProtocols { get; private set; }

        /// <summary>
        /// The raw http header extracted from the stream
        /// </summary>
        public string HttpHeader { get; private set; }

        /// <summary>
        /// The Path extracted from the http header
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// The stream AFTER the header has already been read
        /// </summary>
        public System.IO.Stream Stream { get; private set; }

        /// <summary>
        /// Initialises a new instance of the WebSocketHttpContext class
        /// </summary>
        /// <param name="isWebSocketRequest">True if this is a valid WebSocket request</param>
        /// <param name="httpHeader">The raw http header extracted from the stream</param>
        /// <param name="path">The Path extracted from the http header</param>
        /// <param name="stream">The stream AFTER the header has already been read</param>
        public WebSocketHttpContext(bool isWebSocketRequest, IList<string> webSocketRequestedProtocols,
            string httpHeader, string path, System.IO.Stream stream)
        {
            IsWebSocketRequest = isWebSocketRequest;
            WebSocketRequestedProtocols = webSocketRequestedProtocols;
            HttpHeader = httpHeader;
            Path = path;
            Stream = stream;
        }
    }
}
