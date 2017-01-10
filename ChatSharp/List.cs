using System;

namespace ChatSharp
{
    /// <summary>
    /// The results of an IRC LIST query.  See https://tools.ietf.org/html/rfc1459#section-4.2.6 for details
    /// </summary>
    public class List
    {
        /// <summary>
        /// Channel, created from 322 RPL_LIST "channel # visible :topic" LIST message reply.
        /// </summary>
        public IrcChannel Channel { get; set; }
        /// <summary>
        /// Called when a IRC server's 321 RPL_LISTSTART "Channel :Users  Name" response to a LIST message.
        /// </summary>
        public Action<List> CallbackStart { get; set; }
        /// <summary>
        /// Called when a IRC server's 323 RPL_LISTEND ":End of /LIST" response to a LIST message.
        /// </summary>
        public Action<List> CallbackEnd { get; set; }
        /// <summary>
        /// IRC server's 323 RPL_LISTEND ":End of /LIST" reply.
        /// </summary>
        public IrcMessage Message { get; set; }
        internal List(Action<List> callbackStart, Action<List> callbackEnd)
        {
            CallbackStart = callbackStart;
            CallbackEnd = callbackEnd;
        }
    }
}
