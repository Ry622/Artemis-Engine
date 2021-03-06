﻿#region Using Statements

using System;
using System.Runtime.Serialization;

#endregion

namespace Artemis.Engine
{
    /// <summary>
    /// An exception thrown when something goes wrong in the DisplayManager.
    /// </summary>
    [Serializable]
    public class DisplayManagerException : Exception
    {
        public DisplayManagerException() : base() { }
        public DisplayManagerException(string msg) : base(msg) { }
        public DisplayManagerException(string msg, Exception inner) : base(msg, inner) { }
        public DisplayManagerException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
