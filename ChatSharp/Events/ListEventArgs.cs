using System;

namespace ChatSharp.Events
{
    /// <summary>
    /// Raised when a IRC server's 322 RPL_LIST "channel # visible :topic". See https://tools.ietf.org/html/rfc1459#section-4.2.6
    /// </summary>
    public class ListEventArgs : EventArgs
    {
        /// <summary>
        /// The results of an IRC LIST query.
        /// </summary>
        public ListState ListState { get; private set; }
        internal ListEventArgs(ListState listState)
        {
            ListState = listState;
        }
    }
}
