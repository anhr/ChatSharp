using System;
using System.Linq;

namespace ChatSharp.Handlers
{
    internal static class UserHandlers
    {
        public static void HandleWhoIsUser(IrcClient client, IrcMessage message)
        {
            if (message.Parameters != null && message.Parameters.Length >= 6)
            {
                var whois = (WhoIs)client.RequestManager.PeekOperation("WHOIS " + message.Parameters[1]).State;
                whois.User.Nick = message.Parameters[1];
                whois.User.User = message.Parameters[2];
                whois.User.Hostname = message.Parameters[3];
                whois.User.RealName = message.Parameters[5];
                if (client.Users.Contains(whois.User.Nick))
                {
                    var user = client.Users[whois.User.Nick];
                    user.User = whois.User.User;
                    user.Hostname = whois.User.Hostname;
                    user.RealName = whois.User.RealName;
                    whois.User = user;
                }
            }
        }

        private static WhoIs PeekWhoIsOperation(IrcClient client, IrcMessage message)
        {
            RequestOperation requestOperation = client.RequestManager.PeekOperation("WHOIS " + message.Parameters[1]);
            if (requestOperation == null)
                return null;
            return (WhoIs)requestOperation.State;
        }

        public static void HandleWhoIsLoggedInAs(IrcClient client, IrcMessage message)
        {
            var whois = PeekWhoIsOperation(client, message);
            if (whois == null)
                return;
            whois.LoggedInAs = message.Parameters[2];
        }

        public static void HandleWhoIsServer(IrcClient client, IrcMessage message)
        {
            var whois = PeekWhoIsOperation(client, message);
            if (whois == null)
                return;
            whois.Server = message.Parameters[2];
            whois.ServerInfo = message.Parameters[3];
        }

        public static void HandleWhoIsOperator(IrcClient client, IrcMessage message)
        {
            var whois = PeekWhoIsOperation(client, message);
            if (whois == null)
                return;
            whois.IrcOp = true;
        }

        public static void HandleWhoIsIdle(IrcClient client, IrcMessage message)
        {
            var whois = PeekWhoIsOperation(client, message);
            if (whois == null)
                return;
            whois.SecondsIdle = int.Parse(message.Parameters[2]);
        }

        public static void HandleWhoIsChannels(IrcClient client, IrcMessage message)
        {
            var whois = PeekWhoIsOperation(client, message);
            if (whois == null)
                return;
            var channels = message.Parameters[2].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < channels.Length; i++)
                if (!channels[i].StartsWith("#"))
                    channels[i] = channels[i].Substring(1);
            whois.Channels = whois.Channels.Concat(channels).ToArray();
        }

        public static void HandleWhoIsEnd(IrcClient client, IrcMessage message)
        {
            var request = client.RequestManager.DequeueOperation("WHOIS " + message.Parameters[1]);
            var whois = (WhoIs)request.State;
            if (!client.Users.Contains(whois.User.Nick))
                client.Users.Add(whois.User);
            if (request.Callback != null)
                request.Callback(request);
            client.OnWhoIsReceived(new Events.WhoIsReceivedEventArgs(whois));
            if (!string.IsNullOrEmpty(client.User.NSPassword))
            {
                string arguments = "IDENTIFY " + client.User.NSPassword;
                client.RequestManager.QueueOperation("NickServ", new RequestOperation(new ChatSharp.Handlers.UserHandlers.NickServState(arguments), ro =>{}));
                client.SendRawMessage("NickServ {0}", arguments);
            }
        }
        /// <summary>
        /// Raised when a IRC Error reply occurs. See rfc1459 6.1 for details.
        /// </summary>
        public class NickServState
        {
            /// <summary>
            /// The IRC error reply that has occured.
            /// </summary>
            public string Arguments { get; set; }

            internal NickServState(string arguments)
            {
                Arguments = arguments;
            }
        }
    }
}
