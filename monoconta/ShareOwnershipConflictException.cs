using System;
using System.Runtime.Serialization;

namespace monoconta
{
    [Serializable]
    internal class ShareOwnershipConflictException : Exception
    {
        public ShareOwnershipConflictException() : base("Share ownership conflict!")
        {
        }

        public ShareOwnershipConflictException(string message) : base(message)
        {
        }

        public ShareOwnershipConflictException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ShareOwnershipConflictException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}