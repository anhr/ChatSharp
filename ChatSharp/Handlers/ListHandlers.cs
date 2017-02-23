
using System;

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
            try
            {
                var request = client.RequestManager.PeekOperation("LIST");
                ListState listState = (ListState)request.State;
                listState.Channel = new IrcChannel(client, message);
                if (client.Channels.Contains(listState.Channel))
                    return;
                client.Channels.Add(listState.Channel);
                client.OnListPartRecieved(new Events.ListEventArgs(listState));
            }
            catch (InvalidOperationException e)
            {
                client.OnListError(new Events.ErrorEventArgs(e));
            }
            catch (Exception e)
            {
                client.OnListError(new Events.ErrorEventArgs(e));
            }
        }
        /// <summary>
        /// 323 RPL_LISTEND ":End of /LIST"
        /// </summary>
        public static void HandleListEnd(IrcClient client, IrcMessage message)
        {
            var request = client.RequestManager.PeekOperation("LIST");
            ((ListState)request.State).Message = message;
            if (request.Callback != null)
                request.Callback(request);
        }
    }
}
