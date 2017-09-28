namespace ChatSharp
{
    /// <summary>
    /// Information provided by the server about its featureset.
    /// </summary>
    public class ServerInfo
    {
        internal ServerInfo()
        {
            // Guess for some defaults
            Prefixes = new[] { "ov", "@+" };
            SupportedChannelModes = new ChannelModes();
            IsGuess = true;
        }

        /// <summary>
        /// Gets the mode for a given channel user list prefix.
        /// </summary>
        public char? GetModeForPrefix(char prefix)
        {
            if (Prefixes[1].IndexOf(prefix) == -1)
                return null;
            return Prefixes[0][Prefixes[1].IndexOf(prefix)];
        }

        /// <summary>
        /// Gets the user list prefix for a given channel mode.
        /// </summary>
        public char? GetPrefixForMode(char? mode)
        {
            if ((mode == null) || (Prefixes[0].IndexOf((char)mode) == -1))
                return null;
            return Prefixes[1][Prefixes[0].IndexOf((char)mode)];
        }

        /// <summary>
        /// ChatSharp makes some assumptions about what the server supports in order to function properly.
        /// If it has not recieved a 005 message giving it accurate information, this value will be true.
        /// </summary>
        public bool IsGuess { get; internal set; }
        /// <summary>
        /// Nick prefixes for special modes in channel user lists
        /// </summary>
        public string[] Prefixes { get; internal set; }
        /// <summary>
        /// Supported channel prefixes (i.e. '#')
        /// </summary>
        public char[] ChannelTypes { get; internal set; }
        /// <summary>
        /// Channel modes supported by this server
        /// </summary>
        public ChannelModes SupportedChannelModes { get; set; }
        /// <summary>
        /// The maximum number of MODE changes possible with a single command
        /// </summary>
        public int? MaxModesPerCommand { get; set; }
        /// <summary>
        /// The maximum number of channels a user may join
        /// </summary>
        public int? MaxChannelsPerUser { get; set; } // TODO: Support more than just # channels
        /// <summary>
        /// Maximum length of user nicks on this server
        /// </summary>
        public int? MaxNickLength { get; set; }
        /// <summary>
        /// The limits imposed on list modes, such as +b
        /// </summary>
        public ModeListLimit[] ModeListLimits { get; set; }
        /// <summary>
        /// The name of the network, as identified by the server
        /// </summary>
        public string NetworkName { get; set; }
        /// <summary>
        /// Set to ban exception character if this server supports ban exceptions
        /// </summary>
        public char? SupportsBanExceptions { get; set; }
        /// <summary>
        /// Set to invite exception character if this server supports invite exceptions
        /// </summary>
        public char? SupportsInviteExceptions { get; set; }
        /// <summary>
        /// Set to maximum topic length for this server
        /// </summary>
        public int? MaxTopicLength { get; set; }
        /// <summary>
        /// Set to the maximum length of a KICK comment
        /// </summary>
        public int? MaxKickCommentLength { get; set; }
        /// <summary>
        /// Set to the maximum length of a channel name
        /// </summary>
        public int? MaxChannelNameLength { get; set; }
        /// <summary>
        /// Set to the maximum length of an away message
        /// </summary>
        public int? MaxAwayLength { get; set; }

        /// <summary>
        /// RPL_MYINFO server reply
        /// </summary>
        /// <remarks>See RFC2812 5.1 Command responses https://tools.ietf.org/html/rfc2812#section-5.1 for details</remarks>
        public class MyInfo
        {
            /// <summary>
            /// Save of the RPL_MYINFO server reply
            /// </summary>
            internal MyInfo(IrcMessage message)
            {
                if (message.Command != "004")
                {
                    System.Diagnostics.Trace.Fail("message.Command: " + message.Command + " != 004");
                    return;
                }
                if (message.Parameters.Length < 5)
                {
                    System.Diagnostics.Trace.Fail("message.Parameters.Length: " + message.Parameters.Length + " != 5");
                    return;
                }
                Servername = message.Parameters[1];
                Version = message.Parameters[2];
                AvailableUserModes = message.Parameters[3];
                AvailableChannelModes = message.Parameters[4];
            }
            /// <summary>
            /// Server name
            /// </summary>
            public string Servername { get; private set; }

            /// <summary>
            /// version
            /// </summary>
            public string Version { get; private set; }

            /// <summary>
            /// Available user modes that affect either how the client is seen by others or what 'extra' messages the client is sent.
            /// </summary>
            /// <remarks>See  RFC1459 4.2.3.2 User modes https://tools.ietf.org/html/rfc1459#section-4.2.3.2
            /// and RFC2812 3.1.5 User mode message https://tools.ietf.org/html/rfc2812#section-3.1.5 for more details</remarks>
            public string AvailableUserModes { get; private set; }

            /// <summary>
            /// Available channel modes that channel operators may change the characteristics of `their' channel.
            /// It is also required that servers be able to change channel modes so that channel operators may be created.
            /// </summary>
            /// <remarks>See RFC1459 4.2.3.1 Channel modes https://tools.ietf.org/html/rfc1459#section-4.2.3.1 for details</remarks>
            public string AvailableChannelModes { get; private set; }
        }

        /// <summary>
        /// RPL_MYINFO server reply
        /// </summary>
        public MyInfo myInfo { get; set; }

        /// <summary>
        /// Modes a server supports that are applicable to channels.
        /// </summary>
        public class ChannelModes
        {
            internal ChannelModes()
            {
                // Guesses
                ChannelLists = "eIbq";
                ParameterizedSettings = "k";
                OptionallyParameterizedSettings = "flj";
                Settings = string.Empty;
                ChannelUserModes = "vo"; // I have no idea what I'm doing here
            }

            /// <summary>
            /// Modes that are used for lists (i.e. bans).
            /// </summary>
            public string ChannelLists { get; internal set; }
            /// <summary>
            /// Modes that can be set on a user of a channel (i.e. ops, voice, etc).
            /// </summary>
            public string ChannelUserModes { get; set; }
            /// <summary>
            /// Modes that take a parameter (i.e. +k).
            /// </summary>
            public string ParameterizedSettings { get; internal set; }
            /// <summary>
            /// Modes that take an optional parameter (i.e. +f).
            /// </summary>
            public string OptionallyParameterizedSettings { get; internal set; }
            /// <summary>
            /// Modes that change channel settings.
            /// </summary>
            public string Settings { get; internal set; }
        }

        /// <summary>
        /// Limits imposed on channel lists, such as the maximum bans per channel.
        /// </summary>
        public class ModeListLimit
        {
            internal ModeListLimit(char mode, int maximum)
            {
                Mode = mode;
                Maximum = maximum;
            }

            /// <summary>
            /// The mode character this applies to (i.e. 'b')
            /// </summary>
            public char Mode { get; internal set; }
            /// <summary>
            /// The maximum entries for this list.
            /// </summary>
            public int Maximum { get; internal set; }
        }
    }
}
