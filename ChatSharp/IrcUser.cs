using System;
using System.Linq;
using System.Collections.Generic;

namespace ChatSharp
{
    /// <summary>
    /// A user connected to IRC.
    /// </summary>
    public class IrcUser : IEquatable<IrcUser>
    {
        internal IrcUser()
        {
            Channels = new ChannelCollection();
            ChannelModes = new ChannelsModes();
            this.ctcp = new CTCP();
        }

        /// <summary>
        /// Constructs an IrcUser given a hostmask or nick.
        /// </summary>
        public IrcUser(string host) : this()
        {
            if (!host.Contains("@") && !host.Contains("!"))
                Nick = host;
            else
            {
                string[] mask = host.Split('@', '!');
                Nick = mask[0];
                User = mask[1];
                if (mask.Length <= 2)
                {
                    Hostname = "";
                }
                else
                {
                    Hostname = mask[2];
                }
            }
        }

        /// <summary>
        /// Constructs an IrcUser given a nick and user.
        /// </summary>
        public IrcUser(string nick, string user) : this()
        {
            Nick = nick;
            User = user;
            RealName = User;
            Mode = string.Empty;
        }

        /// <summary>
        /// Constructs an IRC user given a nick, user, and password.
        /// </summary>
        public IrcUser(string nick, string user, string password) : this(nick, user)
        {
            Password = password;
        }

        /// <summary>
        /// Constructs an IRC user given a nick, user, password, and real name.
        /// </summary>
        public IrcUser(string nick, string user, string password, string realName) : this(nick, user, password)
        {
            RealName = realName;
        }

        /// <summary>
        /// Constructs an IRC user given a nick, user, password, real name and Nickserv's password.
        /// </summary>
        public IrcUser(string nick, string user, string password, string realName, string NSPassword) : this(nick, user, password, realName)
        {
            this.NSPassword = NSPassword;
        }

        /// <summary>
        /// The user's nick.
        /// </summary>
        public string Nick { get; internal set; }
        /// <summary>
        /// The user's user (an IRC construct, a string that identifies your username).
        /// </summary>
        public string User { get; internal set; }
        /// <summary>
        /// The user's password. Will not be set on anyone but your own user.
        /// See https://tools.ietf.org/html/rfc1459#section-4.1.1
        /// </summary>
        public string Password { get; internal set; }
        /// <summary>
        /// The user's password for identify in Nickserv. Will not be set on anyone but your own user.
        /// See http://wiki.foonetic.net/wiki/Nickserv_Commands
        /// </summary>
        public string NSPassword { get; internal set; }
        /// <summary>
        /// The user's mode.
        /// </summary>
        /// <value>The mode.</value>
        public string Mode { get; internal set; }
        /// <summary>
        /// The user's real name.
        /// </summary>
        /// <value>The name of the real.</value>
        public string RealName { get; internal set; }
        /// <summary>
        /// The user's hostname.
        /// </summary>
        public string Hostname { get; internal set; }
        /// <summary>
        /// Channels this user is present in. Note that this only includes channels you are
        /// also present in, even after a successful WHOIS.
        /// </summary>
        /// <value>The channels.</value>
        public ChannelCollection Channels { get; set; }

        /// <summary>
        /// The channel modes class.
        /// </summary>
        public class Modes : List<char?>
        {
            new void Add(char? c)
            {
                if (!base.Contains(c))
                    base.Add(c);
            }

            /// <summary>
            /// Get user's channel mode.
            /// </summary>
            public char? Mode
            {
                get
                {
                    if (this.Contains('o'))
                        return 'o';
                    if (this.Count == 0)
                        return null;
                    return this[0];
                }
            }
        }
        /// <summary>
        /// The user's channel modes class.
        /// </summary>
        public class ChannelsModes : Dictionary<IrcChannel, Modes>
        {
            /// <summary>
            /// Add user's channel mode.
            /// </summary>
            public void Add(IrcChannel IrcChannel, char? c)
            {
                if (!this.ContainsKey(IrcChannel))
                    base.Add(IrcChannel, new Modes());
                Modes modes = base[IrcChannel];
                modes.Add(c);
            }
        }

        /// <summary>
        /// The user's channel modes.
        /// </summary>
        public ChannelsModes ChannelModes { get; internal set; }

        /// <summary>
        /// This user's hostmask (nick!user@host).
        /// </summary>
        public string Hostmask
        {
            get
            {
                return Nick + "!" + User + "@" + Hostname;
            }
        }

        /// <summary>
        /// The results of an IRC WHOIS query. Depending on the capabilities of the server you're connected to,
        /// some of these fields may be null.
        /// </summary>
        public WhoIs WhoIs { get; internal set; }

        /// <summary>
        /// Returns true if the user matches the given mask. Can be used to check if a ban applies
        /// to this user, for example.
        /// </summary>
        public bool Match(string mask)
        {
            if (mask.Contains("!") && mask.Contains("@"))
            {
                if (mask.Contains('$'))
                    mask = mask.Remove(mask.IndexOf('$')); // Extra fluff on some networks
                var parts = mask.Split('!', '@');
                if (Match(parts[0], Nick) && Match(parts[1], User) && Match(parts[2], Hostname))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if the given hostmask matches the given mask.
        /// </summary>
        public static bool Match(string mask, string value)
        {
            if (value == null)
                value = string.Empty;
            int i = 0;
            int j = 0;
            for (; j < value.Length && i < mask.Length; j++)
            {
                if (mask[i] == '?')
                    i++;
                else if (mask[i] == '*')
                {
                    i++;
                    if (i >= mask.Length)
                        return true;
                    while (++j < value.Length && value[j] != mask[i]) ;
                    if (j-- == value.Length)
                        return false;
                }
                else
                {
                    if (char.ToUpper(mask[i]) != char.ToUpper(value[j]))
                        return false;
                    i++;
                }
            }
            return i == mask.Length && j == value.Length;
        }

        /// <summary>
        /// True if this user is equal to another (compares hostmasks).
        /// </summary>
        public bool Equals(IrcUser other)
        {
            return other.Hostmask == Hostmask;
        }

        /// <summary>
        /// True if this user is equal to another (compares hostmasks).
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is IrcUser)
                return Equals((IrcUser)obj);
            return false;
        }

        /// <summary>
        /// Returns the hash code of the user's hostmask.
        /// </summary>
        public override int GetHashCode()
        {
            return Hostmask.GetHashCode();
        }

        /// <summary>
        /// Returns the user's hostmask.
        /// </summary>
        public override string ToString()
        {
            return Hostmask;
        }
        /// <summary>
        /// Client-to-client protocol
        /// </summary>
        /// <remarks>
        /// See https://en.wikipedia.org/wiki/Client-to-client_protocol for details
        /// </remarks>
        public class CTCP
        {
            /// <summary>
            /// First and last character of CTCP command and reply
            /// </summary>
            private const string FirstAndLastCharacter = "\u0001";
            /// <summary>
            /// Sends a private message with CTCP command to target user
            /// </summary>
            /// <param name="ircClient"></param>
            /// <param name="nick">Nick of the target user</param>
            /// <param name="command">CTCP command</param>
            /// <remarks>
            /// See https://en.wikipedia.org/wiki/Client-to-client_protocol for details
            /// </remarks>
            public void Command(ChatSharp.IrcClient ircClient, string nick, string command)
            { ircClient.SendMessage(CTCP.FirstAndLastCharacter + command + CTCP.FirstAndLastCharacter, nick); }
            /// <summary>
            /// CTCP replies
            /// </summary>
            /// <remarks>
            ///   key: CTCP command of the reply
            /// value: CTCP reply
            /// </remarks>
            private Dictionary<string, string> Replies = new Dictionary<string, string>();
            /// <summary>
            /// Occurs when a notice recieved.
            /// </summary>
            /// <param name="ircClient">"</param>
            /// <param name="Notice">"</param>
            /// <param name="source">The source of the notice. Example: "ChanServ!ChanServ@services."</param>
            public void NoticeRecieved(ChatSharp.IrcClient ircClient, string Notice, string source)
            {
                string[] array = 
                    new System.Text.RegularExpressions.Regex(@CTCP.FirstAndLastCharacter + "(\\S*) (.*)" + CTCP.FirstAndLastCharacter).Split(Notice);//Example: "\u0001VERSION Web IRC client - Bonalink  https://localhost/Chat/?tab=IRC  Browser - Chrome 70. UTC - Windows 10 desktop\u0001"
                if (array.Length < 2)
                    return;
                string command = array[1],//CTCP command. Example: VERSION
                    reply = array[2];//CTCP reply. Example: "KVIrc 4.2.0 svn-6190 'Equilibrium' 20120701 - build 2012-07-04 14:48:08 UTC - Windows 8  (x64)  (Build 9200)"
                if (this.Replies.ContainsKey(command))
                    this.Replies[command] = reply;
                else this.Replies.Add(command, reply);
                ircClient.OnNoticeRecievedCTCP(new Events.IrcNoticeEventCTCPArgs(command, reply, source));
            }
        }
        /// <summary>
        /// Client-to-client protocol
        /// </summary>
        /// <remarks>
        /// See https://en.wikipedia.org/wiki/Client-to-client_protocol for details
        /// </remarks>
        public CTCP ctcp;
    }
}
