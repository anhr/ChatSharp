using System;
using System.Linq;

namespace ChatSharp
{
    public partial class IrcClient
    {
        /// <summary>
        /// Changes your nick.
        /// </summary>
        public void Nick(string newNick)
        {
            SendRawMessage("NICK {0}", newNick);
            User.Nick = newNick;
        }

        /// <summary>
        /// Sends a message to one or more destinations (channels or users).
        /// </summary>
        public void SendMessage(string message, params string[] destinations)
        {
            const string illegalCharacters = "\r\n\0";
            if (destinations == null || !destinations.Any()) throw new InvalidOperationException("Message must have at least one target.");
            if (illegalCharacters.Any(message.Contains)) throw new ArgumentException("Illegal characters are present in message.", "message");
            string to = string.Join(",", destinations);
            SendRawMessage("PRIVMSG {0} :{1}{2}", to, PrivmsgPrefix, message);
        }

        /// <summary>
        /// Sends a CTCP action (i.e. "* SirCmpwn waves hello") to one or more destinations.
        /// </summary>
        public void SendAction(string message, params string[] destinations)
        {
            const string illegalCharacters = "\r\n\0";
            if (destinations == null || !destinations.Any()) throw new InvalidOperationException("Message must have at least one target.");
            if (illegalCharacters.Any(message.Contains)) throw new ArgumentException("Illegal characters are present in message.", "message");
            string to = string.Join(",", destinations);
            SendRawMessage("PRIVMSG {0} :\x0001ACTION {1}{2}\x0001", to, PrivmsgPrefix, message);
        }

        /// <summary>
        /// Sends a NOTICE to one or more destinations (channels or users).
        /// </summary>
        public void SendNotice(string message, params string[] destinations)
        {
            const string illegalCharacters = "\r\n\0";
            if (destinations == null || !destinations.Any()) throw new InvalidOperationException("Message must have at least one target.");
            if (illegalCharacters.Any(message.Contains)) throw new ArgumentException("Illegal characters are present in message.", "message");
            string to = string.Join(",", destinations);
            SendRawMessage("NOTICE {0} :{1}{2}", to, PrivmsgPrefix, message);
        }

        /// <summary>
        /// Leaves the specified channel.
        /// </summary>
        public void PartChannel(string channel)
        {
            if (!Channels.Contains(channel))
                throw new InvalidOperationException("Client is not present in channel.");
            SendRawMessage("PART {0}", channel);
        }

        /// <summary>
        /// Leaves the specified channel, giving a reason for your departure.
        /// </summary>
        public void PartChannel(string channel, string reason)
        {
            if (!Channels.Contains(channel))
                throw new InvalidOperationException("Client is not present in channel.");
            SendRawMessage("PART {0} :{1}", channel, reason);
        }

        /// <summary>
        /// Joins the specified channel.
        /// </summary>
        public void JoinChannel(string channel)
        {
            if (Channels.Contains(channel, this.User.Nick))
                throw new InvalidOperationException("Client is already present in channel.");
            SendRawMessage("JOIN {0}", channel);
        }

        /// <summary>
        /// Sets the topic for the specified channel.
        /// </summary>
        public void SetTopic(string channel, string topic)
        {
            if (!Channels.Contains(channel))
                throw new InvalidOperationException("Client is not present in channel.");
            SendRawMessage("TOPIC {0} :{1}", channel, topic);
        }

        /// <summary>
        /// Retrieves the topic for the specified channel.
        /// </summary>
        public void GetTopic(string channel)
        {
            SendRawMessage("TOPIC {0}", channel);
        }

        /// <summary>
        /// Kicks the specified user from the specified channel.
        /// </summary>
        public void KickUser(string channel, string user)
        {
            SendRawMessage("KICK {0} {1} :{1}", channel, user);
        }

        /// <summary>
        /// Kicks the specified user from the specified channel.
        /// </summary>
        public void KickUser(string channel, string user, string reason)
        {
            SendRawMessage("KICK {0} {1} :{2}", channel, user, reason);
        }

        /// <summary>
        /// Invites the specified user to the specified channel.
        /// </summary>
        public void InviteUser(string channel, string user)
        {
            SendRawMessage("INVITE {1} {0}", channel, user);
        }

        /// <summary>
        /// Sends a WHOIS query asking for information on the given nick.
        /// </summary>
        public void WhoIs(string nick)
        {
            WhoIs(nick, null);
        }

        /// <summary>
        /// Sends a WHOIS query asking for information on the given nick, and a callback
        /// to run when we have received the response.
        /// </summary>
        public void WhoIs(string nick, Action<WhoIs> callback)
        {
            this.WhoIs(this.Users[nick], callback);
        }

        /// <summary>
        /// Sends a WHOIS query asking for information on the given user, and a callback
        /// to run when we have received the response.
        /// </summary>
        public void WhoIs(ChatSharp.IrcUser user, Action<WhoIs> callback = null)
        {
            user.WhoIs = new WhoIs();
            RequestManager.QueueOperation("WHOIS " + user.Nick, new RequestOperation(user.WhoIs, ro =>
            {
                WhoIs whoIs = (WhoIs)ro.State;
                if (callback != null)
                    callback(whoIs);
            }));
            SendRawMessage("WHOIS {0}", user.Nick);
        }

        /// <summary>
        /// Requests the mode of a channel from the server.
        /// </summary>
        public void GetMode(string channel)
        {
            GetMode(channel, null);
        }

        /// <summary>
        /// Requests the mode of a channel from the server, and passes it to a callback later.
        /// </summary>
        public void GetMode(string channel, Action<IrcChannel> callback)
        {
            RequestManager.QueueOperation("MODE " + channel, new RequestOperation(channel, ro =>
                {
                    var c = Channels[(string)ro.State];
                    if (callback != null)
                        callback(c);
                }));
            SendRawMessage("MODE {0}", channel);
        }

        /// <summary>
        /// Sets the mode of a target.
        /// </summary>
        public void ChangeMode(string target, string change)
        {
            SendRawMessage("MODE {0} {1}", target, change);
        }

        /// <summary>
        /// Gets a collection of masks from a channel by a mode. This can be used, for example,
        /// to get a list of bans.
        /// </summary>
        public void GetModeList(string channel, char mode, Action<MaskCollection> callback)
        {
            RequestManager.QueueOperation("GETMODE " + mode + " " + channel, new RequestOperation(new MaskCollection(), ro =>
                {
                    var c = (MaskCollection)ro.State;
                    if (callback != null)
                        callback(c);
                }));
            SendRawMessage("MODE {0} {1}", channel, mode);
        }
        /// <summary>
        /// List message. https://tools.ietf.org/html/rfc1459#section-4.2.6
        /// </summary>
        /// <param name="callback">Called when a IRC server's 322 RPL_LIST &lt;channel&gt; # &lt;visible&gt; :&lt;topic&gt;" response to a LIST message.</param>
        /// <param name="channels">Comma separated channels list. If  the channels  parameter  is  used,  only the  status of  that channel is displayed.</param>
        /// <param name="server">server's address</param>
        public void List(Action<ListState> callback = null,
            string channels = null,
            string server = null)
        {
            RequestManager.QueueOperation("LIST", new RequestOperation(new ChatSharp.ListState(this.Channels), ro =>
            {
                if (callback != null)
                    callback((ListState)ro.State);
            }));
            SendRawMessage("LIST {0} {1}", channels, server);
        }
        /// <summary>
        /// NickServ Registering Nicknames. http://wiki.foonetic.net/wiki/Nickserv_Commands#Registering_Nicknames
        /// </summary>
        /// <param name="pass">NickServ password</param>
        /// <param name="email">An email containing an authentication code will be sent to the specified email address.</param>
        public void NSRegister(string pass, string email)
        {
            SendRawMessage("NickServ {0}", "REGISTER " + pass + " " + email);
        }
        /// <summary>
        /// Names message. https://tools.ietf.org/html/rfc1459#section-4.2.5
        /// </summary>
        /// <param name="channels">Comma separated channels list. If  the channels  parameter  is  used, specifies which channel(s) to return information about if valid.</param>
        /// <param name="callback">Called when a IRC server's 353 RPL_NAMREPLY "&lt;channel&gt; :[[@|+]&lt;nick&gt; [[@|+]&lt;nick&gt; [...]]]" response to a NAMES message.</param>
        public void Names(string channels = null,
            Action<NamesState> callback = null)
        {
            RequestManager.QueueOperation("NAMES" + (channels == null ? "" : " " + channels),
                new RequestOperation(new ChatSharp.NamesState(this.Users), ro =>
            {
                if (callback != null)
                    callback((NamesState)ro.State);
            }));
            SendRawMessage("NAMES {0}", channels);
        }
    }
}
