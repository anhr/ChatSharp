namespace ChatSharp.Handlers
{
    /// <summary>
    /// IRC error replies handler. See rfc1459 6.1.
    /// </summary>
    internal static class ErrorHandlers
    {
        /// <summary>
        /// IRC Error replies handler. See rfc1459 6.1.
        /// </summary>
        public static void HandleError(IrcClient client, IrcMessage message)
        {
            if (message.Command == "401")
            {
                RequestOperation requestOperation = client.RequestManager.PeekOperation("WHOIS " + message.Parameters[1]);
                if(requestOperation != null)
                    ((WhoIs)requestOperation.State).error = message.Command;
            }
            client.OnErrorReply(new Events.ErrorReplyEventArgs(message));
        }
        /// <summary>
        /// IRC fatal error handler.
        /// </summary>
        public static void HandleFatalError(IrcClient client, IrcMessage message)
        {
            client.Disconnect(message);
        }
    }
}
