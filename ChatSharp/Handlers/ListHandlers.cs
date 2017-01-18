
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
            if (client.ChannelsList == null)
                client.ChannelsList = new ChannelCollection();
            else
                client.ChannelsList.RemoveAll();//The LIST mesage was sent to IRC server not once 
            ListState listState = (ListState)(client.RequestManager.PeekOperation("LIST")).State;
            listState.Message = message;
            if (listState.CallbackStart != null)
                listState.CallbackStart(listState);
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
                if (client.ChannelsList.Contains(listState.Channel))
                    return;
                client.ChannelsList.Add(listState.Channel);
                if (request.Callback != null)
                    request.Callback(request);
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
            ListState listState = (ListState)(client.RequestManager.DequeueOperation("LIST")).State;
            listState.Message = message;
            if (listState.CallbackEnd != null)
                listState.CallbackEnd(listState);
        }
    }
}
