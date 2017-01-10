
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
            else client.ChannelsList.RemoveAll();//The LIST mesage was sent to IRC server not once 
            List list = (List)(client.RequestManager.PeekOperation("LIST")).State;
            list.Message = message;
            if(list.CallbackStart != null)
                list.CallbackStart(list);
        }
        /// <summary>
        /// 322 RPL_LIST "channel # visible :topic"
        /// </summary>
        public static void HandleList(IrcClient client, IrcMessage message)
        {
            string strError = "";
            try
            {
                var request = client.RequestManager.PeekOperation("LIST");
                List list = (List)request.State;
                list.Channel = new IrcChannel(client, message);
                client.ChannelsList.Add(list.Channel);
                if (request.Callback != null)
                    request.Callback(request);
            }
            catch (InvalidOperationException exception)
            {
                strError = exception.ToString();
            }
            catch (Exception exception)
            {
                strError = exception.ToString();
            }
            if (strError != "")
            {
                client.OnListError(new Events.ListErrorEventArgs(strError));
                return;
            }
        }
        /// <summary>
        /// 323 RPL_LISTEND ":End of /LIST"
        /// </summary>
        public static void HandleListEnd(IrcClient client, IrcMessage message)
        {
            List list = (List)(client.RequestManager.DequeueOperation("LIST")).State;
            list.Message = message;
            if(list.CallbackEnd != null)
                list.CallbackEnd(list);
        }
    }
}
