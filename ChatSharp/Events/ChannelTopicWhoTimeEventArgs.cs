using System;

namespace ChatSharp.Events
{
    /// <summary>
    /// Details of a channel topic who time event.
    /// </summary>
    public class ChannelTopicWhoTimeEventArgs : EventArgs
    {
        /// <summary>
        /// The channel to which the user set the topic.
        /// </summary>
        public IrcChannel Channel { get; set; }
        /// <summary>
        /// User who last set the channel topic.
        /// </summary>
        public string User { get; set; }
        /// <summary>
        /// The time the user set the current channel topic.
        /// </summary>
        public string Time { get; set; }
        
        internal ChannelTopicWhoTimeEventArgs(IrcChannel channel, string user, string time)
        {
            Channel = channel;
            User = user;
            Time = time;
        }
    }
}
