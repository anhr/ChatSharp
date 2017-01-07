using System;

namespace ChatSharp.Events
{
    /// <summary>
    /// Raised when a IRC server's 321 RPL_LISTSTART "Channel :Users  Name" response to a LIST command. See https://tools.ietf.org/html/rfc1459#section-4.2.6
    /// </summary>
    public class ListStartEventArgs : EventArgs
    {
        /// <summary>
        /// The IRC server's 321 RPL_LISTSTART "Channel :Users  Name" reply that has occured.
        /// </summary>
        public IrcMessage Message { get; set; }
        internal ListStartEventArgs(IrcMessage message)
        {
            Message = message;
        }
    }
}
