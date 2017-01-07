using System;

namespace ChatSharp.Events
{
    /// <summary>
    /// Raised when a IRC server's 322 RPL_LIST "channel # visible :topic". See https://tools.ietf.org/html/rfc1459#section-4.2.6
    /// </summary>
    public class ListEventArgs : EventArgs
    {
        /// <summary>
        /// The IRC server's 322 RPL_LIST reply that has occured.
        /// </summary>
        public IrcMessage Message { get; set; }

        internal ListEventArgs(IrcMessage message)
        {
            Message = message;
        }
    }
}
