using System;
using System.Collections.Generic;
using System.Text;

namespace Codentia.Common.Net.FTP
{
    /// <summary>
    /// An exception raised by the FTPClient class
    /// </summary>
    public class FTPException : System.Exception
    {
        private int _ftpResponseCode = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="FTPException"/> class with only a message
        /// </summary>
        /// <param name="message">The detail for the exception</param>
        public FTPException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FTPException"/> class with a message and a contained exception
        /// </summary>
        /// <param name="message">The detail for the exception</param>
        /// <param name="innerException">The contained exception</param>
        public FTPException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FTPException"/> class with only an FTP Response Code
        /// </summary>
        /// <param name="response">The FTPResponse to construct the exception from</param>
        public FTPException(FTPResponse response)
            : base(response.Message)
        {
            _ftpResponseCode = response.Code;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FTPException"/> class.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="innerException">The inner exception.</param>
        public FTPException(FTPResponse response, Exception innerException)
            : base(response.Message, innerException)
        {
            _ftpResponseCode = response.Code;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FTPException"/> class.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="messagePrefix">The message prefix.</param>
        public FTPException(FTPResponse response, string messagePrefix)
            : base(string.Format("{0}: {1}", messagePrefix, response.Message))
        {
            _ftpResponseCode = response.Code;
        }

        /// <summary>
        /// Gets the FTP Response Code returned by the server request
        /// </summary>
        public int ResponseCode
        {
            get
            {
                return _ftpResponseCode;
            }
        }
    }
}
