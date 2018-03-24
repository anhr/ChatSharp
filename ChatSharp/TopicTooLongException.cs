using System;

namespace ChatSharp
{
    /// <summary>
    /// Raised when the topic is too long.
    /// </summary>
    public class TopicTooLongException : Exception
    {
        internal TopicTooLongException(uint maxTopicLength)
        {
            MaxTopicLength = maxTopicLength;
        }
        /// <summary>
        /// Set to maximum topic length for this server
        /// </summary>
        public uint MaxTopicLength { get; private set; }
    }
}
