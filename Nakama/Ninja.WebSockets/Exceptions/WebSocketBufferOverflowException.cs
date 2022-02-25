using System;

namespace Nakama.Ninja.WebSockets.Exceptions
{
    [Serializable]
    public class WebSocketBufferOverflowException : Exception
    {
        public WebSocketBufferOverflowException() : base()
        {
        }

        public WebSocketBufferOverflowException(string message) : base(message)
        {
        }

        public WebSocketBufferOverflowException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}