using System;

namespace ChatSharp.Events
{
    /// <summary>
    /// Describes the RPL_MYINFO server reply.
    /// </summary>
    /// <remarks>See RFC2812 5.1 Command responses https://tools.ietf.org/html/rfc2812#section-5.1 for details</remarks>
    public class MyInfoEventArgs : EventArgs
    {
        /// <summary>
        /// The My Info featureset.
        /// </summary>
        public ServerInfo.MyInfo MyInfo { get; set; }
        internal MyInfoEventArgs(ServerInfo.MyInfo myInfo)
        {
            MyInfo = myInfo;
        }
    }
}
