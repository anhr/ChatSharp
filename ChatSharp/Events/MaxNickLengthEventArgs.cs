using System;

namespace ChatSharp
{
    /// <summary>
    /// Raised when nick's length is great of max nick length.
    /// </summary>
    public class MaxNickLengthEventArgs : EventArgs
    {
        internal MaxNickLengthEventArgs(int maxNickLength)
        {
            MaxNickLength = maxNickLength;
        }

        /// <summary>
        /// Max nick length.
        /// </summary>
        public int MaxNickLength { get; set; }
    }
}

