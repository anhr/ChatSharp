using System;

namespace ChatSharp.Events
{
    /// <summary>
    /// Raised when a IRC server's 353 RPL_NAMREPLY "&lt;channel&gt; :[[@|+]&lt;nick&lt; [[@|+]&lt;nick&lt; [...]]]". NAMES message reply. https://tools.ietf.org/html/rfc1459#section-4.2.5
    /// </summary>
    public class UserListEventArgs : EventArgs
    {
        /// <summary>
        /// The results of an IRC NAMES 353 RPL_NAMREPLY reply.
        /// </summary>
        internal UserListEventArgs(NamesState namesState)
        {
            NamesState = namesState;
        }
    }
}
