using System;

namespace Nakama
{
    /// <summary>
    /// A delegate used to determine whether or not a network exception is
    /// due to a temporary bad state on the server. For example, timeouts can be transient in cases where
    /// the server is experiencing temporarily high load.
    /// </summary>
    public delegate bool TransientExceptionDelegate(Exception e);
}