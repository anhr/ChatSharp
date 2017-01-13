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
            Channels.Remove(Channels[channel]);
        }

        /// <summary>
        /// Leaves the specified channel, giving a reason for your departure.
        /// </summary>
        public void PartChannel(string channel, string reason)
        {
            if (!Channels.Contains(channel))
                throw new InvalidOperationException("Client is not present in channel.");
            SendRawMessage("PART {0} :{1}", channel, reason);
            Channels.Remove(Channels[channel]);
        }

        /// <summary>
        /// Joins the specified channel.
        /// </summary>
        public void JoinChannel(string channel)
        {
            if (Channels.Contains(channel))
                throw new InvalidOperationException("Client is not already present in channel.");
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
            var whois = new WhoIs();
            RequestManager.QueueOperation("WHOIS " + nick, new RequestOperation(whois, ro =>
                {
                    if (callback != null)
                        callback((WhoIs)ro.State);
                }));
            SendRawMessage("WHOIS {0}", nick);
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
        /// <para><param name="callbackStart">callbackStart: Called when a IRC server's 321 RPL_LISTSTART "Channel :Users  Name" response to a LIST message.</param></para>
        /// <para><param name="callback">callback: Called when a IRC server's 322 RPL_LIST &lt;channel&gt; # &lt;visible&gt; :&lt;topic&gt;" response to a LIST message.</param></para>
        /// <para><param name="callbackEnd">callbackEnd: Called when a IRC server's 323 RPL_LISTEND ":End of /LIST" response to a LIST message.</param></para>
        /// <para><param name="channels">channels: Comma separated channels list. If  the channels  parameter  is  used,  only the  status of  that channel is displayed.</param></para>
        /// <para><param name="server">server: server's address</param></para>
        /// </summary>
        public void List(
            Action<List> callbackStart = null
            , Action<List> callback = null
            , Action<List> callbackEnd = null
            , string channels = null
            , string server = null)
        {
            var list = new ChatSharp.List(callbackStart, callbackEnd);
            RequestManager.QueueOperation("LIST", new RequestOperation(list, ro =>
            {
                if (callback != null)
                    callback((List)ro.State);
            }));
            SendRawMessage("LIST {0} {1}", channels, server);
        }
        /// <summary>
        /// Names message. https://tools.ietf.org/html/rfc1459#section-4.2.5
        /// <para><param name="channels">channels: Comma separated channels list. If  the channels  parameter  is  used, specifies which channel(s) to return information about if valid.</param></para>
        /// <para><param name="callback">callback: Called when a IRC server's 353 RPL_NAMREPLY "&lt;channel&gt; :[[@|+]&lt;nick&gt; [[@|+]&lt;nick&gt; [...]]]" response to a NAMES message.</param></para>
        /// <para><param name="callbackEnd">callbackEnd: Called when a IRC server's 366 RPL_ENDOFNAMES "&lt;channel&gt; :End of /NAMES list" response to a NAMES message.</param></para>
        /// </summary>
        public void Names(
            string channels = null
            , Action<Names> callback = null
            , Action<Names> callbackEnd = null
            )
        {
            if (this.ChannelsList == null)
                this.ChannelsList = new ChannelCollection();
            var names = new ChatSharp.Names(callbackEnd);
            RequestManager.QueueOperation("NAMES" + (channels == null ? "" : " " + channels), new RequestOperation(names, ro =>
            {
                if (callback != null)
                    callback((Names)ro.State);
            }));
            SendRawMessage("NAMES {0}", channels);
        }
    }
}
