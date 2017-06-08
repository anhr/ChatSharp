using System;

namespace ChatSharp.Events
{
    /// <summary>
    /// Generic event args that represent an event regarding a user.
    /// </summary>
    public class UserEventArgs : EventArgs
    {
        /// <summary>
        /// The user this regards.
        /// </summary>
        public IrcUser User { get; set; }

        /// <summary>
        /// Reason for the end of the client session
        /// </summary>
        public string Reason { get; set; }

        internal UserEventArgs(IrcUser user, string reason)
        {
            User = user;
            Reason = reason;
        }
    }
}

