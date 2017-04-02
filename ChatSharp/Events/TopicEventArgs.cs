using System;

namespace ChatSharp
{
    /// <summary>
    /// Raised when the topic of a channel has changed.
    /// </summary>
    public class TopicEventArgs : EventArgs
    {
        internal TopicEventArgs(IrcMessage message)
        {
            Message = message;
        }

        /// <summary>
        /// The IRC server's TOPIC reply that has occured.
        /// </summary>
        public IrcMessage Message { get; set; }
    }
}

