using System;

namespace ChatSharp.Events
{
    /// <summary>
    /// Event describing an IRC CTCP notice.
    /// </summary>
    /// <remarks>
    /// See https://en.wikipedia.org/wiki/Client-to-client_protocol for details
    /// </remarks>
    public class IrcNoticeEventCTCPArgs : EventArgs
    {
        /// <summary>
        /// CTCP command
        /// </summary>
        public string Command;
        /// <summary>
        /// CTCP reply
        /// </summary>
        public string Reply;
        /// <summary>
        /// The source of the CTCP notice.
        /// </summary>
        public string Source;
        /// <summary>
        /// Nick of the source user
        /// </summary>
        public string Nick { get { return new System.Text.RegularExpressions.Regex(@"(.*)!(.*)").Split(this.Source)[1]; } }
        internal IrcNoticeEventCTCPArgs(string command, string reply, string source)
        {
            this.Command = command;
            this.Reply = reply;
            this.Source = source;
        }
    }
}
