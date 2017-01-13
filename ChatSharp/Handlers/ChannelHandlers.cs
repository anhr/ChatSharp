using ChatSharp.Events;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChatSharp.Handlers
{
    internal static class ChannelHandlers
    {
        public static void HandleJoin(IrcClient client, IrcMessage message)
        {
            var channel = client.Channels.GetOrAdd(message.Parameters[0]);
            var user = client.Users.GetOrAdd(message.Prefix);
            user.Channels.Add(channel);
            if (channel != null)
                client.OnUserJoinedChannel(new ChannelUserEventArgs(channel, new IrcUser(message.Prefix)));
        }

        public static void HandleGetTopic(IrcClient client, IrcMessage message)
        {
            var channel = client.Channels.GetOrAdd(message.Parameters[1]);
            var old = channel._Topic;
            channel._Topic = message.Parameters[2];
            client.OnChannelTopicReceived(new ChannelTopicEventArgs(channel, old, channel._Topic));
        }

        public static void HandleGetEmptyTopic(IrcClient client, IrcMessage message)
        {
            var channel = client.Channels.GetOrAdd(message.Parameters[1]);
            var old = channel._Topic;
            channel._Topic = message.Parameters[2];
            client.OnChannelTopicReceived(new ChannelTopicEventArgs(channel, old, channel._Topic));
        }

        public static void HandlePart(IrcClient client, IrcMessage message)
        {
            if (!client.Channels.Contains(message.Parameters[0]))
                return; // we aren't in this channel, ignore

            var user = client.Users.Get(message.Prefix);
            var channel = client.Channels[message.Parameters[0]];

            if (user.Channels.Contains(channel))
                user.Channels.Remove(channel);
            client.OnUserPartedChannel(new ChannelUserEventArgs(client.Channels[message.Parameters[0]],
                new IrcUser(message.Prefix)));
        }

        private static void GetOrAddUser(IrcClient client, IrcChannel channel, IrcMessage message, string nick, char? mode)
        {
            var user = client.Users.GetOrAdd(nick);
            if(!user.Channels.IsChannelExists(channel))
                user.Channels.Add(channel);
            if (!user.ChannelModes.ContainsKey(channel))
                user.ChannelModes.Add(channel, mode);
            else
                user.ChannelModes[channel] = mode;
            var request = client.RequestManager.PeekOperation("NAMES");
            if (request == null)
                request = client.RequestManager.PeekOperation("NAMES " + message.Parameters[2]);
            if (request == null)
                return;//IRC server sent 353 RPL_NAMREPLY reply automatically
            Names names = ((Names)request.State);
            names.User = user;
            names.Channel = channel;
            if (request.Callback != null)
                request.Callback(request);
        }

        private static void UserListPart(IrcClient client, IrcChannel channel, IrcMessage message, string[] users)
        {
            if (channel == null)
                return;
            foreach (var nick in users)
            {
                if (string.IsNullOrWhiteSpace(nick))
                    continue;
                var mode = client.ServerInfo.GetModeForPrefix(nick[0]);
                if (mode == null)
                    GetOrAddUser(client, channel, message, nick, null);
                else GetOrAddUser(client, channel, message, nick.Substring(1), mode.Value);
            }
        }

        public static void HandleUserListPart(IrcClient client, IrcMessage message)
        {
            var users = message.Parameters[3].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string channelName = message.Parameters[2];
            var channel = client.Channels.GetChannel(channelName);
            if (channel != null)
                UserListPart(client, channel, message, users);//Add new user into channel where user has joined to.
            if (client.ChannelsList != null)
            {
                channel = client.ChannelsList.GetChannel(channelName);
                if (channel == null)
                    client.ChannelsList.Add(new IrcChannel(client, channelName));
                UserListPart(client, client.ChannelsList[channelName], message, users);//Add new user into channel from collection of all channels as reply of the LIST message.
            }
        }

        public static void HandleUserListEnd(IrcClient client, IrcMessage message)
        {
            var channel = client.Channels.GetChannel(message.Parameters[1]);
            if (channel != null)
            {
                client.OnChannelListRecieved(new ChannelEventArgs(channel));
                if (client.Settings.ModeOnJoin)
                {
                    try
                    {
                        client.GetMode(channel.Name, c => { /* no-op */ });
                    }
                    catch { /* who cares */ }
                }
                if (client.Settings.WhoIsOnJoin)
                {
                    Task.Factory.StartNew(() => WhoIsChannel(channel, client, 0));
                }
            }
            var request = client.RequestManager.DequeueOperation("NAMES" + (message.Parameters[1] == "*" ? "" : " " + message.Parameters[1]));
            if (request == null)
                return;//IRC server sent 366 RPL_ENDOFNAMES reply automatically
            Names names = (Names)request.State;
            names.Message = message;
            names.Channel = channel;
            if (names.CallbackEnd != null)
                names.CallbackEnd(names);
        }

        private static void WhoIsChannel(IrcChannel channel, IrcClient client, int index)
        {
            // Note: joins and parts that happen during this will cause strange behavior here
            Thread.Sleep(client.Settings.JoinWhoIsDelay * 1000);
            var user = channel.Users[index];
            client.WhoIs(user.Nick, (whois) =>
                {
                    user.User = whois.User.User;
                    user.Hostname = whois.User.Hostname;
                    user.RealName = whois.User.RealName;
                    Task.Factory.StartNew(() => WhoIsChannel(channel, client, index + 1));
                });
        }

        public static void HandleKick(IrcClient client, IrcMessage message)
        {
            var channel = client.Channels[message.Parameters[0]];
            var kicked = channel.Users[message.Parameters[1]];
            if (kicked.Channels.Contains(channel))
                kicked.Channels.Remove(channel);
            client.OnUserKicked(new KickEventArgs(channel, new IrcUser(message.Prefix),
                kicked, message.Parameters[2]));
        }
    }
}
