using ChatSharp.Events;
using System.Linq;
using System;

namespace ChatSharp.Handlers
{
    internal static class MessageHandlers
    {
        public static void RegisterDefaultHandlers(IrcClient client)
        {
            // General
            client.SetHandler("PING", HandlePing);
            client.SetHandler("NOTICE", HandleNotice);
            client.SetHandler("PRIVMSG", HandlePrivmsg);
            client.SetHandler("MODE", HandleMode);
            client.SetHandler("324", HandleMode);
            client.SetHandler("NICK", HandleNick);
            client.SetHandler("QUIT", HandleQuit);
            client.SetHandler("ERROR", ErrorHandlers.HandleFatalError);
            client.SetHandler("431", HandleErronousNick);
            client.SetHandler("432", HandleErronousNick);
            client.SetHandler("433", HandleErronousNick);
            client.SetHandler("436", HandleErronousNick);

            // MOTD Handlers
            client.SetHandler("375", MOTDHandlers.HandleMOTDStart);
            client.SetHandler("372", MOTDHandlers.HandleMOTD);
            client.SetHandler("376", MOTDHandlers.HandleEndOfMOTD);
            client.SetHandler("422", MOTDHandlers.HandleMOTDNotFound);

            // Channel handlers
            client.SetHandler("JOIN", ChannelHandlers.HandleJoin);
            client.SetHandler("PART", ChannelHandlers.HandlePart);
            client.SetHandler("332", ChannelHandlers.HandleGetTopic);
            client.SetHandler("331", ChannelHandlers.HandleGetEmptyTopic);
            client.SetHandler("353", ChannelHandlers.HandleUserListPart);
            client.SetHandler("366", ChannelHandlers.HandleUserListEnd);
            client.SetHandler("KICK", ChannelHandlers.HandleKick);

            // User handlers
            client.SetHandler("311", UserHandlers.HandleWhoIsUser);
            client.SetHandler("312", UserHandlers.HandleWhoIsServer);
            client.SetHandler("313", UserHandlers.HandleWhoIsOperator);
            client.SetHandler("317", UserHandlers.HandleWhoIsIdle);
            client.SetHandler("318", UserHandlers.HandleWhoIsEnd);
            client.SetHandler("319", UserHandlers.HandleWhoIsChannels);
            client.SetHandler("330", UserHandlers.HandleWhoIsLoggedInAs);

            // Listing handlers
            client.SetHandler("367", ListingHandlers.HandleBanListPart);
            client.SetHandler("368", ListingHandlers.HandleBanListEnd);
            client.SetHandler("348", ListingHandlers.HandleExceptionListPart);
            client.SetHandler("349", ListingHandlers.HandleExceptionListEnd);
            client.SetHandler("346", ListingHandlers.HandleInviteListPart);
            client.SetHandler("347", ListingHandlers.HandleInviteListEnd);
            client.SetHandler("728", ListingHandlers.HandleQuietListPart);
            client.SetHandler("729", ListingHandlers.HandleQuietListEnd);

            // Server handlers
            client.SetHandler("004", ServerHandlers.HandleMyInfo);
            client.SetHandler("005", ServerHandlers.HandleISupport);

            // Error replies rfc1459 6.1
            client.SetHandler("401", ErrorHandlers.HandleError);//ERR_NOSUCHNICK "<nickname> :No such nick/channel"
            client.SetHandler("402", ErrorHandlers.HandleError);//ERR_NOSUCHSERVER "<server name> :No such server"
            client.SetHandler("403", ErrorHandlers.HandleError);//ERR_NOSUCHCHANNEL "<channel name> :No such channel"
            client.SetHandler("404", ErrorHandlers.HandleError);//ERR_CANNOTSENDTOCHAN "<channel name> :Cannot send to channel"
            client.SetHandler("405", ErrorHandlers.HandleError);//ERR_TOOMANYCHANNELS "<channel name> :You have joined too many \ channels"
            client.SetHandler("406", ErrorHandlers.HandleError);//ERR_WASNOSUCHNICK "<nickname> :There was no such nickname"
            client.SetHandler("407", ErrorHandlers.HandleError);//ERR_TOOMANYTARGETS "<target> :Duplicate recipients. No message \
            client.SetHandler("465", ErrorHandlers.HandleError);//ERR_YOUREBANNEDCREEP ":You are banned from this server"
            client.SetHandler("471", ErrorHandlers.HandleError);//ERR_CHANNELISFULL "<channel> :Cannot join channel (+l)"
            client.SetHandler("472", ErrorHandlers.HandleError);//ERR_UNKNOWNMODE "<char> :is unknown mode char to me"
            client.SetHandler("473", ErrorHandlers.HandleError);//ERR_INVITEONLYCHAN "<channel> :Cannot join channel (+i)"
            client.SetHandler("474", ErrorHandlers.HandleError);//ERR_BANNEDFROMCHAN "<channel> :Cannot join channel (+b)"
            client.SetHandler("475", ErrorHandlers.HandleError);//ERR_BADCHANNELKEY "<channel> :Cannot join channel (+k)"
            client.SetHandler("477", ErrorHandlers.HandleError);//"<channel> :Cannot join channel (+r) - you need to be identified with services"
            client.SetHandler("479", ErrorHandlers.HandleError);//"<channel> :Illegal channel name
            client.SetHandler("538", ErrorHandlers.HandleError);//"<channel1> is linked to <channel2> but <channel2> is not accepting links from <channel1>." reply from irc.swiftirc.net IRC server

            //Replies RPL_LISTSTART, RPL_LIST, RPL_LISTEND mark
            //      the start, actual replies with data and end of the
            //      server's response to a LIST command.  If there are
            //      no channels available to return, only the start
            //      and end reply must be sent.
            //See https://tools.ietf.org/html/rfc1459#section-4.2.6 for details
            client.SetHandler("321", ListHandlers.HandleListStart);//RPL_LISTSTART "Channel :Users  Name"
            client.SetHandler("322", ListHandlers.HandleList);//RPL_LIST "<channel> <# visible> :<topic>"
            client.SetHandler("323", ListHandlers.HandleListEnd);//RPL_LISTEND ":End of /LIST"
        }

        public static void HandleNick(IrcClient client, IrcMessage message)
        {
            var user = client.Users.Get(message.Prefix);
            var oldNick = user.Nick;
            user.Nick = message.Parameters[0];

            client.OnNickChanged(new NickChangedEventArgs
            {
                User = user,
                OldNick = oldNick,
                NewNick = message.Parameters[0]
            });
        }

        public static void HandleQuit(IrcClient client, IrcMessage message)
        {
            var user = new IrcUser(message.Prefix);
            if (client.User.Nick != user.Nick)
            {
                client.Users.Remove(user.Nick);
                client.OnUserQuit(new UserEventArgs(user));
            }
            else
                client.Disconnect();
        }

        public static void HandlePing(IrcClient client, IrcMessage message)
        {
            client.ServerNameFromPing = message.Parameters[0];
            client.SendRawMessage("PONG :{0}", message.Parameters[0]);
        }

        public static void HandleNotice(IrcClient client, IrcMessage message)
        {
            client.OnNoticeRecieved(new IrcNoticeEventArgs(message));
        }

        public static void HandlePrivmsg(IrcClient client, IrcMessage message)
        {
            var eventArgs = new PrivateMessageEventArgs(client, message, client.ServerInfo);
            client.OnPrivateMessageRecieved(eventArgs);
            if (eventArgs.PrivateMessage.IsChannelMessage)
                client.OnChannelMessageRecieved(eventArgs);
            else
                client.OnUserMessageRecieved(eventArgs);
        }

        public static void HandleErronousNick(IrcClient client, IrcMessage message)
        {
            var eventArgs = new ErronousNickEventArgs(client.User.Nick);
            if (message.Command == "433") // Nick in use
                client.OnNickInUse(eventArgs);
            // else ... TODO
            if (!eventArgs.DoNotHandle && client.Settings.GenerateRandomNickIfRefused)
                client.Nick(eventArgs.NewNick);
        }

        public static void HandleMode(IrcClient client, IrcMessage message)
        {
            string target, mode = null;
            int i = 2;
            if (message.Command == "MODE")
            {
                target = message.Parameters[0];
                mode = message.Parameters[1];
            }
            else
            {
                target = message.Parameters[1];
                mode = message.Parameters[2];
                i++;
            }
            // Handle change
            bool add = true;
            if (target.StartsWith("#"))
            {
                var channel = client.Channels[target];
                foreach (char c in mode)
                {
                    if (c == '+')
                    {
                        add = true;
                        continue;
                    }
                    if (c == '-')
                    {
                        add = false;
                        continue;
                    }
                    if (channel.Mode == null)
                        channel.Mode = string.Empty;
                    // TODO: Support the ones here that aren't done properly
                    if (client.ServerInfo.SupportedChannelModes.ParameterizedSettings.Contains(c))
                    {
                        client.OnModeChanged(new ModeChangeEventArgs(channel.Name, new IrcUser(message.Prefix), 
                            (add ? "+" : "-") + c + " " + message.Parameters[i++]));
                    }
                    else if (client.ServerInfo.SupportedChannelModes.ChannelLists.Contains(c))
                    {
                        client.OnModeChanged(new ModeChangeEventArgs(channel.Name, new IrcUser(message.Prefix), 
                            (add ? "+" : "-") + c + " " + message.Parameters[i++]));
                    }
                    else if (client.ServerInfo.SupportedChannelModes.ChannelUserModes.Contains(c))
                    {
                        if (channel.UsersByMode == null)
                            channel.UsersByMode = new System.Collections.Generic.Dictionary<char?, UserPoolView>();
                        if (!channel.UsersByMode.ContainsKey(c))
                        {
                            channel.UsersByMode.Add(c,
                                new UserPoolView(channel.Users.Where(u =>
                                {
                                    if (!u.ChannelModes.ContainsKey(channel))
                                        u.ChannelModes.Add(channel, null);
                                    return u.ChannelModes[channel] == c;
                                })));
                        }
                        var user = new IrcUser(message.Parameters[0]);
                        if (add)
                        {
                            if (!channel.UsersByMode[c].Contains(user.Nick))
                                user.ChannelModes[channel] = c;
                        }
                        else
                        {
                            if (channel.UsersByMode[c].Contains(user.Nick))
                                user.ChannelModes[channel] = null;
                        }
                        client.OnModeChanged(new ModeChangeEventArgs(channel.Name, user, (add ? "+" : "-") + c));
                    }
                    if (client.ServerInfo.SupportedChannelModes.Settings.Contains(c))
                    {
                        if (add)
                        {
                            if (!channel.Mode.Contains(c))
                                channel.Mode += c.ToString();
                        }
                        else
                            channel.Mode = channel.Mode.Replace(c.ToString(), string.Empty);
                        client.OnModeChanged(new ModeChangeEventArgs(channel.Name, new IrcUser(message.Prefix), 
                            (add ? "+" : "-") + c));
                    }
                }
                if (message.Command == "324")
                {
                    var operation = client.RequestManager.DequeueOperation("MODE " + channel.Name);
                    operation.Callback(operation);
                }
            }
            else
            {
                // TODO: Handle user modes other than ourselves?
                foreach (char c in mode)
                {
                    if (add)
                    {
                        if (!client.User.Mode.Contains(c))
                            client.User.Mode += c;
                    }
                    else
                        client.User.Mode = client.User.Mode.Replace(c.ToString(), string.Empty);
                }
            }
        }
    }
}
