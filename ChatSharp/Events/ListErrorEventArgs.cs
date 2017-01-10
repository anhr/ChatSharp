using System;

namespace ChatSharp.Events
{
    /// <summary>
    /// Raised for add channel errors.
    /// </summary>
    public class ListErrorEventArgs : EventArgs
    {
        /// <summary>
        /// The error that has occured.
        /// </summary>
        public string Error { get; set; }
        internal ListErrorEventArgs(string error)
        {
            Error = error;
        }
    }
}
