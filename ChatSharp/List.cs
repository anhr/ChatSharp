using System;

namespace ChatSharp
{
    /// <summary>
    /// The results of an IRC LIST query.  See https://tools.ietf.org/html/rfc1459#section-4.2.6 for details
    /// </summary>
    public class ListState
    {
        /// <summary>
        /// Channel, created from 322 RPL_LIST "channel # visible :topic" LIST message reply.
        /// </summary>
        public IrcChannel Channel { get; set; }
        /// <summary>
        /// IRC server's 323 RPL_LISTEND ":End of /LIST" reply.
        /// </summary>
        public IrcMessage Message { get; set; }
        /// <summary>
        /// Collection of all channels as reply of the LIST message. See https://tools.ietf.org/html/rfc1459#section-4.2.6
        /// </summary>
        public ChannelCollection Channels { get; private set; }
        /// <summary>
        /// Constructor
        /// </summary>
        public ListState(ChannelCollection channels)
        {
            Channels = channels;
        }
    }
}
