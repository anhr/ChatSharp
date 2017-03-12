using System;

namespace ChatSharp.Events
{
    /// <summary>
    /// Describes an invalid nick event.
    /// </summary>
    public class NickInUseEventArgs : EventArgs
    {
        private static Random random;
        private static string GenerateRandomNick()
        {
            const string nickCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

            if (random == null)
                random = new Random();
            var nick = new char[8];
            for (int i = 0; i < nick.Length; i++)
                nick[i] = nickCharacters[random.Next(nickCharacters.Length)];
            return new string(nick);
        }

        /// <summary>
        /// The nick that was not accepted by the server.
        /// </summary>
        /// <value>The invalid nick.</value>
        public string InvalidNick { get; set; }
        /// <summary>
        /// The nick ChatSharp intends to use instead.
        /// </summary>
        /// <value>The new nick.</value>
        public string NewNick { get; set; }
        /// <summary>
        /// Set to true to instruct ChatSharp NOT to send a valid nick.
        /// </summary>
        public bool DoNotHandle { get; set; }

        internal NickInUseEventArgs(string invalidNick)
        {
            InvalidNick = invalidNick;
            NewNick = GenerateRandomNick();
            DoNotHandle = false;
        }
    }

    /// <summary>
    /// Describes an invalid nick event.
    /// </summary>
    public class ErronousNickEventArgs : EventArgs
    {
        /// <summary>
        ///    "431" ERR_NONICKNAMEGIVEN ":No nickname given" - Returned when a nickname parameter expected for a command and isn't found.
        /// or "432" ERR_ERRONEUSNICKNAME "nick :Erroneus nickname" - Returned after receiving a NICK message which contains characters which do not fall in the defined set.
        /// or "436" ERR_NICKCOLLISION "nick :Nickname collision KILL" - Returned by a server to a client when it detects a nickname collision (registered of a NICK that already exists by another server).
        /// </summary>
        public string Command { get; set; }
        /// <summary>
        /// The nick that was not accepted by the server.
        /// </summary>
        /// <value>The invalid nick.</value>
        public string InvalidNick { get; set; }
        /// <summary>
        /// Message from server.
        /// </summary>
        public string Message { get; set; }
        internal ErronousNickEventArgs(IrcMessage ircMessage)
        {
            Command = ircMessage.Command;
            InvalidNick = ircMessage.Parameters[1];
            Message = ircMessage.Parameters[2];
        }
    }
}
