
namespace ChatSharp.Handlers
{
    /// <summary>
    /// IRC server's response to a LIST command. See https://tools.ietf.org/html/rfc1459#section-4.2.6
    /// </summary>
    internal static class ListHandlers
    {
        /// <summary>
        /// 321 RPL_LISTSTART "Channel :Users  Name"
        /// </summary>
        public static void HandleListStart(IrcClient client, IrcMessage message)
        {
            client.OnListStart(new Events.ListStartEventArgs(message));
        }
        /// <summary>
        /// 322 RPL_LIST "channel # visible :topic"
        /// </summary>
        public static void HandleList(IrcClient client, IrcMessage message)
        {
            client.OnList(new Events.ListEventArgs(message));
        }
        /// <summary>
        /// 323 RPL_LISTEND ":End of /LIST"
        /// </summary>
        public static void HandleListEnd(IrcClient client, IrcMessage message)
        {
            client.OnListEnd(new Events.ListEndEventArgs());
        }
    }
}
