using System;

namespace ChatSharp.Events
{
    /// <summary>
    /// Describes the features the server supports.
    /// </summary>
    public class SupportsEventArgs : EventArgs
    {
        /// <summary>
        /// The server's supported featureset.
        /// </summary>
        public ServerInfo ServerInfo { get; set; }
        /// <summary>
        /// RPL_BOUNCE RFC2812 :Try server &lt;server_name&gt;, port &lt;port_number&gt;
        /// <para>Sent by the server to a user to suggest an alternative server, sometimes used when the connection is refused because the server is already full. Also known as RPL_SLINE (AustHex), and RPL_REDIR.</para>
        /// </summary>
        public IrcMessage Message;
        internal SupportsEventArgs(ServerInfo serverInfo, IrcMessage message)
        {
            ServerInfo = serverInfo;
            Message = message;
        }
    }
}
