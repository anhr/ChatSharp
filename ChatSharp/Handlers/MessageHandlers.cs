using ChatSharp.Events;
using System.Linq;
using System;

namespace ChatSharp.Handlers
{
    internal static class MessageHandlers
    {
        public static void RegisterDefaultHandlers(IrcClient client)
        {
            //All IRC replies https://www.alien.net.au/irc/irc2numerics.html
            //rfc1459 https://tools.ietf.org/html/rfc1459 https://www.rfc-editor.org/rfc/rfc1459.txt
            //rfc2812 https://tools.ietf.org/html/rfc2812 https://www.rfc-editor.org/rfc/rfc2812.txt

            // General
            client.SetHandler("PING", HandlePing);
            client.SetHandler("PONG", HandlePong);
            client.SetHandler("NOTICE", HandleNotice);
            client.SetHandler("PRIVMSG", HandlePrivmsg);
            client.SetHandler("MODE", HandleMode);
            client.SetHandler("324", HandleMode);
            client.SetHandler("NICK", HandleNick);
            client.SetHandler("QUIT", HandleQuit);
            client.SetHandler("ERROR", ErrorHandlers.HandleFatalError);
            client.SetHandler("431", HandleErronousNick);//ERR_NONICKNAMEGIVEN ":No nickname given" - Returned when a nickname parameter expected for a command and isn't found.
            client.SetHandler("432", HandleErronousNick);//ERR_ERRONEUSNICKNAME "<nick> :Erroneus nickname" - Returned after receiving a NICK message which contains characters which do not fall in the defined set.
            client.SetHandler("433", HandleErronousNick);//ERR_NICKNAMEINUSE "<nick> :Nickname is already in use" - Returned when a NICK message is processed that results in an attempt to change to a currently existing nickname.
            client.SetHandler("436", HandleErronousNick);//ERR_NICKCOLLISION "<nick> :Nickname collision KILL" - Returned by a server to a client when it detects a nickname collision (registered of a NICK that already exists by another server).

            // MOTD Handlers
            client.SetHandler("375", MOTDHandlers.HandleMOTDStart);
            client.SetHandler("372", MOTDHandlers.HandleMOTD);
            client.SetHandler("376", MOTDHandlers.HandleEndOfMOTD);
            client.SetHandler("422", MOTDHandlers.HandleMOTDNotFound);

            // Channel handlers
            client.SetHandler("JOIN", ChannelHandlers.HandleJoin);
            client.SetHandler("PART", ChannelHandlers.HandlePart);
            client.SetHandler("331", ChannelHandlers.HandleGetEmptyTopic);
            client.SetHandler("332", ChannelHandlers.HandleGetTopic);
            client.SetHandler("333", ChannelHandlers.HandleTopicWhoTime);//RPL_TOPICWHOTIME http://www.tracyexpressbaseball.com/mishbox/reference/rawhelp3.htm#raw333 example: 333 blink #kif psycon!~psy@comm.wurbz.de 1500138059
            client.SetHandler("353", ChannelHandlers.HandleUserListPart);
            client.SetHandler("366", ChannelHandlers.HandleUserListEnd);
            client.SetHandler("KICK", ChannelHandlers.HandleKick);
            client.SetHandler("TOPIC", ChannelHandlers.HandleTopic);

            // User handlers
            client.SetHandler("301", UserHandlers.HandleWhoIsOther);//RPL_AWAY	RFC1459	<nick> :<message>	Used in reply to a command directed at a user who is marked as away
            client.SetHandler("307", UserHandlers.HandleWhoIsRegNick);//RPL_WHOISREGNICK example: :java.webchat.org 307 bonalink aro 179751 :is using a registered nickname.
            client.SetHandler("309", UserHandlers.HandleWhoIsNickTrace);//RPL_NICKTRACE example: :java.webchat.org 309 bonalink aro en :Preferred language: English
            client.SetHandler("311", UserHandlers.HandleWhoIsUser);
            client.SetHandler("312", UserHandlers.HandleWhoIsServer);
            client.SetHandler("313", UserHandlers.HandleWhoIsOperator);
            client.SetHandler("317", UserHandlers.HandleWhoIsIdle);
            client.SetHandler("318", UserHandlers.HandleWhoIsEnd);
            client.SetHandler("319", UserHandlers.HandleWhoIsChannels);
            client.SetHandler("330", UserHandlers.HandleWhoIsLoggedInAs);
            client.SetHandler("334", UserHandlers.HandleWhoIsLocation);//Users location
            client.SetHandler("338", UserHandlers.HandleWhoIsActually);//RPL_WHOISACTUALLY <source> 338 <target> <nick> <user>@<host> <ip> :Actual user@host, Actual IP
                                                                       //example: ":NuclearFallout.WA.US.GameSurge.net 338 blink2 blink2 ~blink2@95.188.70.66 95.188.70.66 :Actual user@host, Actual IP"
            client.SetHandler("378", UserHandlers.HandleWhoIsOther);//RPL_WHOISHOST Unreal
                                                                    //RPL_BANEXPIRED	aircd
                                                                    //RPL_MOTD AustHex     Used by AustHex to 'force' the display of the MOTD, however is considered obsolete due to client/ script awareness & ability to Also see #372.
            client.SetHandler("671", UserHandlers.HandleWhoIsOther);//RPL_WHOISSECURE	KineIRCd	<nick> <type> [:<info>]	Reply to WHOIS command - Returned if the target is connected securely, eg. type may be TLSv1, or SSLv2 etc. If the type is unknown, a '*' may be used.
                                                                        //example: 671 blink2 dvim :is using a secure connection

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
            client.SetHandler("004", ServerHandlers.HandleMyInfo);//RPL_MYINFO RFC2812 "<servername> <version> <available user modes> < available channel modes> "
            client.SetHandler("005", ServerHandlers.HandleISupport);//RPL_BOUNCE RFC2812 :Try server <server_name>, port <port_number>	Sent by the server to a user to suggest an alternative server, sometimes used when the connection is refused because the server is already full. Also known as RPL_SLINE (AustHex), and RPL_REDIR.

            // Error replies rfc1459 6.1
            client.SetHandler("401", ErrorHandlers.HandleError);//ERR_NOSUCHNICK "<nickname> :No such nick/channel"
            client.SetHandler("402", ErrorHandlers.HandleError);//ERR_NOSUCHSERVER "<server name> :No such server"
            client.SetHandler("403", ErrorHandlers.HandleError);//ERR_NOSUCHCHANNEL "<channel name> :No such channel"
            client.SetHandler("404", ErrorHandlers.HandleError);//ERR_CANNOTSENDTOCHAN "<channel name> :Cannot send to channel"
            client.SetHandler("405", ErrorHandlers.HandleError);//ERR_TOOMANYCHANNELS "<channel name> :You have joined too many \ channels"
            client.SetHandler("406", ErrorHandlers.HandleError);//ERR_WASNOSUCHNICK "<nickname> :There was no such nickname"
            client.SetHandler("407", ErrorHandlers.HandleError);//ERR_TOOMANYTARGETS "<target> :Duplicate recipients. No message \
            client.SetHandler("421", ErrorHandlers.HandleError);//ERR_UNKNOWNCOMMAND "<command> :Unknown command" - Returned to a registered client to indicate that the command sent is unknown by the server.
            client.SetHandler("437", ErrorHandlers.HandleError);//ERR_UNAVAILRESOURCE	RFC2812	<nick/channel/service> :<reason>	Return when the target is unable to be reached temporarily, eg. a delay mechanism in play, or a service being offline
            client.SetHandler("441", ErrorHandlers.HandleError);//ERR_USERNOTINCHANNEL	RFC1459	<nick> <channel> :<reason>	Returned by the server to indicate that the target user of the command is not on the given channel
            client.SetHandler("442", ErrorHandlers.HandleError);//ERR_NOTONCHANNEL "<channel> :You're not on that channel"
            client.SetHandler("451", ErrorHandlers.HandleError);//ERR_NOTREGISTERED ":You have not registered" - Returned by the server to indicate that the client must be registered before the server will allow it to be parsed in detail.
            client.SetHandler("455", ErrorHandlers.HandleError);//ERR_HOSTILENAME
            client.SetHandler("461", ErrorHandlers.HandleError);//ERR_NEEDMOREPARAMS "<command> :Not enough parameters" - Returned by the server by numerous commands to indicate to the client that it didn't supply enough parameters.
            client.SetHandler("465", ErrorHandlers.HandleError);//ERR_YOUREBANNEDCREEP ":You are banned from this server"
            client.SetHandler("470", ErrorHandlers.HandleError);//ERR_LINKCHANNEL automatically redirected to another channel. or ERR_KICKEDFROMCHAN
            client.SetHandler("471", ErrorHandlers.HandleError);//ERR_CHANNELISFULL "<channel> :Cannot join channel (+l)"
            client.SetHandler("472", ErrorHandlers.HandleError);//ERR_UNKNOWNMODE "<char> :is unknown mode char to me"
            client.SetHandler("473", ErrorHandlers.HandleError);//ERR_INVITEONLYCHAN "<channel> :Cannot join channel (+i)"
            client.SetHandler("474", ErrorHandlers.HandleError);//ERR_BANNEDFROMCHAN "<channel> :Cannot join channel (+b)"
            client.SetHandler("475", ErrorHandlers.HandleError);//ERR_BADCHANNELKEY "<channel> :Cannot join channel (+k)"
            client.SetHandler("477", ErrorHandlers.HandleError);//"<channel> :Cannot join channel (+r) - you need to be identified with services"
            client.SetHandler("479", ErrorHandlers.HandleError);//"<channel> :Illegal channel name
            client.SetHandler("482", ErrorHandlers.HandleError);//ERR_CHANOPRIVSNEEDED "<channel> :You're not channel operator" - Any command requiring 'chanop' privileges(such as MODE messages) must return this error if the client making the attempt is not a chanop on the specified channel.
            client.SetHandler("485", ErrorHandlers.HandleError);//ERR_UNIQOPRIVSNEEDED	RFC2812	:<reason>	Any mode requiring 'channel creator' privileges returns this error if the client is attempting to use it while not a channel creator on the given channel
            client.SetHandler("486", ErrorHandlers.HandleError);//ERR_NONONREG You must log in with services to message this user
            client.SetHandler("493", ErrorHandlers.HandleError);//ERR_NOFEATURE The user does not wish to receive that type of message.
            client.SetHandler("494", ErrorHandlers.HandleError);//ERR_BADFEATURE Your own modes prohibit you from sending that type of message.
            client.SetHandler("513", ErrorHandlers.HandleError);//ERR_BADPING Also known as ERR_NEEDPONG (Unreal/Ultimate) for use during registration, however it's not used in Unreal (and might not be used in Ultimate either).
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

            //help replies
            //RatBox: http://www.ratbox.org/
            client.SetHandler("704", HandleHelp);//RPL_HELPSTART	RatBox	<command> :<text>	Start of HELP command output
            client.SetHandler("705", HandleHelp);//RPL_HELPTXT	RatBox	<command> :<text>	Output from HELP command
            client.SetHandler("706", HandleHelp);//RPL_ENDOFHELP	RatBox	<command> :<text>	End of HELP command output
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
                client.OnUserQuit(new UserEventArgs(user, message.Parameters[0]));
            }
            else
                client.Disconnect();
        }

        public static void HandlePing(IrcClient client, IrcMessage message)
        {
            client.ServerNameFromPing = message.Parameters[0];
            client.SendRawMessage("PONG :{0}", message.Parameters[0]);
        }

        /// <summary>
        /// Exclude unhandled message event
        /// </summary>
        public static void HandlePong(IrcClient client, IrcMessage message)
        {
        }

        public static void HandleNotice(IrcClient client, IrcMessage message)
        {
            client.OnNoticeRecieved(new IrcNoticeEventArgs(message));
        }

        public static void HandleHelp(IrcClient client, IrcMessage message)
        {
            client.OnHelpRecieved(new HelpEventArgs(message));
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
            var eventArgs = new NickInUseEventArgs(message.Parameters[1]);
            if (message.Command == "433") // Nick in use
                client.OnNickInUse(eventArgs);
            if (!eventArgs.DoNotHandle && client.Settings.GenerateRandomNickIfRefused)
                client.Nick(eventArgs.NewNick);
            else if (message.Command != "433") // Nick is not use
                client.OnErronousNick(new ErronousNickEventArgs(message));
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
            IrcUser userPrefix = new IrcUser(message.Prefix);
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
                        client.OnModeChanged(new ModeChangeEventArgs(channel.Name, userPrefix, 
                            (add ? "+" : "-") + c + " " + message.Parameters[i++]));
                    }
                    else if (client.ServerInfo.SupportedChannelModes.ChannelLists.Contains(c))
                    {
                        if (message.Parameters.Count() > i)
                        {
                            try
                            {
                                IrcUser.ChannelsModes ChannelModes = channel.Users[message.Parameters[i]].ChannelModes;
                                if (ChannelModes.ContainsKey(channel))
                                {
                                    IrcUser.Modes modes = ChannelModes[channel];
                                    if (add)
                                        modes.Add(c);
                                    else modes.Remove(c);
                                }
                                else if (add)
                                    ChannelModes.Add(channel, (char?)c);
                            }
                            catch (System.Collections.Generic.KeyNotFoundException) { }//User not found if you baning a user
                        }
                        client.OnModeChanged(new ModeChangeEventArgs(channel.Name, userPrefix, 
                            (add ? "+" : "-") + c + " " + message.Parameters[i++]));
                    }
                    else if (client.ServerInfo.SupportedChannelModes.ChannelUserModes.Contains(c))
                    {
                        if (message.Parameters.Count() > i)
                        {
                            IrcUser.ChannelsModes ChannelModes = channel.Users[message.Parameters[i]].ChannelModes;
                            if (ChannelModes.ContainsKey(channel))
                            {
                                IrcUser.Modes modes = ChannelModes[channel];
                                if (add)
                                    modes.Add(c);
                                else modes.Remove(c);
                            }
                            else if (add)
                                ChannelModes.Add(channel, (char?)c);
                            client.OnModeChanged(new ModeChangeEventArgs(channel.Name, userPrefix, 
                                (add ? "+" : "-") + c + " " + message.Parameters[i]));
                        }
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
                        client.OnModeChanged(new ModeChangeEventArgs(channel.Name, userPrefix, (add ? "+" : "-") + c));
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
                    switch (c) {
                        case '+':
                            add = true;
                            if (!client.User.Mode.Contains(c))
                                client.User.Mode += c;
                            break;
                        case '-': add = false; break;
                        default:
                            if (add)
                            {
                                if (!client.User.Mode.Contains(c))
                                    client.User.Mode += c;
                            }
                            else
                                client.User.Mode = client.User.Mode.Replace(c.ToString(), string.Empty);
                            break;
                    }
                }
                client.OnModeChanged(new ModeChangeEventArgs(client.User.Nick, userPrefix, mode));
            }
        }
    }
}
