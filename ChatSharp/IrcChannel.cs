using System.Collections.Generic;
using System.Linq;

namespace ChatSharp
{
    /// <summary>
    /// An IRC channel.
    /// </summary>
    public class IrcChannel
    {
        private IrcClient Client { get; set; }

        internal string _Topic;
        /// <summary>
        /// The channel topic. Will send a TOPIC command if set.
        /// </summary>
        public string Topic 
        {
            get
            {
                return _Topic;
            }
            set
            {
                Client.SetTopic(Name, value);
                _Topic = value;
            }
        }

        /// <summary>
        /// The name, including the prefix (i.e. #), of this channel.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; internal set; }
        /// <summary>
        /// The channel mode. May be null if we have not received the mode yet.
        /// </summary>
        public string Mode { get; internal set; }
        /// <summary>
        /// The users in this channel.
        /// </summary>
        public UserPoolView Users { get; private set; }
        /// <summary>
        /// The users count in this channel. It is # visible parameter of the IRC server's reply 322 RPL_LIST "channel # visible :topic". See rfc1459#section-6.2  Command responses.
        /// </summary>
        public uint UsersCount { get; private set; }
        /// <summary>
        /// Users in this channel, grouped by mode. Users with no special mode are grouped under null.
        /// </summary>
        public Dictionary<char?, UserPoolView> UsersByMode { get; set; }

        internal IrcChannel(IrcClient client, string name)
        {
            Client = client;
            Name = name;
            Users = new UserPoolView(client.Users.Where(u => u.Channels.Contains(this)));
        }

        /// <summary>
        /// Create channel from IRC server's reply 322 RPL_LIST "channel # visible :topic". See rfc1459#section-6.2  Command responses.
        /// </summary>
        public IrcChannel(IrcClient client, IrcMessage message)
        {
            Client = client;
            Name = message.Parameters[1];//Channel
            UsersCount = uint.Parse(message.Parameters[2]);//# visible
            _Topic = message.Parameters[3];
        }

        /// <summary>
        /// Invites a user to this channel.
        /// </summary>
        public void Invite(string nick)
        {
            Client.InviteUser(Name, nick);
        }

        /// <summary>
        /// Kicks a user from this channel.
        /// </summary>
        public void Kick(string nick)
        {
            Client.KickUser(Name, nick);
        }

        /// <summary>
        /// Kicks a user from this channel, giving a reason for the kick.
        /// </summary>
        public void Kick(string nick, string reason)
        {
            Client.KickUser(Name, nick, reason);
        }

        /// <summary>
        /// Parts this channel.
        /// </summary>
        public void Part()
        {
            Client.PartChannel(Name);
        }

        /// <summary>
        /// Parts this channel, giving a reason for your departure.
        /// </summary>
        public void Part(string reason)
        {
            Client.PartChannel(Name); // TODO
        }

        /// <summary>
        /// Sends a PRIVMSG to this channel.
        /// </summary>
        public void SendMessage(string message)
        {
            Client.SendMessage(message, Name);
        }

        /// <summary>
        /// Set the channel mode.
        /// </summary>
        public void ChangeMode(string change)
        {
            Client.ChangeMode(Name, change);
        }
    }
}
