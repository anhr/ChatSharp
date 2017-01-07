using System;

namespace ChatSharp.Events
{
    /// <summary>
    /// Raised when a IRC server's 323 RPL_LISTEND ":End of /LIST" response to a LIST command. See https://tools.ietf.org/html/rfc1459#section-4.2.6
    /// </summary>
    public class ListEndEventArgs : EventArgs
    {
        /// <summary>
        /// The IRC server's 321 RPL_LISTEND ":End of /LIST" reply that has occured.
        /// </summary>
        public IrcMessage Message { get; set; }
        internal ListEndEventArgs(IrcMessage message)
        {
            Message = message;
        }
    }
}
