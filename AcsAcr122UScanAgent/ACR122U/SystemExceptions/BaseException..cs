namespace AcsAcr122UScanAgent.ACR122U.SystemExceptions
{
    using System;
    using System.Runtime.Serialization;

    public class BaseException : Exception
    {
        public virtual int ErrorCode { get; set; }
        public BaseException()
        {
            // Add implementation (if required)
        }

        public BaseException(string message)
            : base(message)
        {
            // Add implementation (if required)
        }

        public BaseException(int errorCode, string message)
            : base(message)
        {
            this.ErrorCode = errorCode;
        }

        public BaseException(string message, Exception inner)
            : base(message, inner)
        {
            // Add implementation (if required)
        }

        public BaseException(int errorCode, string message, Exception inner)
            : base(message, inner)
        {
            this.ErrorCode = errorCode;
            // Add implementation (if required)
        }

        protected BaseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // Add implementation (if required)
        }
    }
}
