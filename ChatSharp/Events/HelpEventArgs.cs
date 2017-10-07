using System;

namespace ChatSharp.Events
{
    /// <summary>
    /// Event describing an help.
    /// </summary>
    public class HelpEventArgs : EventArgs
    {
        /// <summary>
        /// The IRC message that describes this help reply.
        /// </summary>
        /// <value>The message.</value>
        public IrcMessage Message { get; set; }
        /// <summary>
        /// The text of the help reply.
        /// </summary>
        public string Help { get { return Message.Parameters[2]; } }

        internal HelpEventArgs(IrcMessage message)
        {
            Message = message;
        }
    }
}
