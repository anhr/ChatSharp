using System;

namespace ChatSharp
{
    /// <summary>
    /// The results of an IRC NAMES query.  See https://tools.ietf.org/html/rfc1459#section-4.2.5 for details
    /// </summary>
    public class NamesState
    {
        /// <summary>
        /// User, created from 353 RPL_NAMREPLY "&lt;channel&gt; :[[@|+]&lt;nick&gt; [[@|+]&lt;nick&gt; [...]]]" response to a NAMES message.
        /// </summary>
        public IrcUser User { get; set; }
        /// <summary>
        /// Channel where user has added to.
        /// </summary>
        public IrcChannel Channel { get; set; }
        /// <summary>
        /// Called when a IRC server's 366 RPL_ENDOFNAMES "&lt;channel&gt; :End of /NAMES list" response to a NAMES message.
        /// </summary>
        public Action<NamesState> CallbackEnd { get; set; }
        /// <summary>
        /// IRC server's 366 RPL_ENDOFNAMES "&lt;channel&gt; :End of /NAMES list" reply.
        /// </summary>
        public IrcMessage Message { get; set; }
        internal NamesState(Action<NamesState> callbackEnd)
        {
            CallbackEnd = callbackEnd;
        }
    }
}
