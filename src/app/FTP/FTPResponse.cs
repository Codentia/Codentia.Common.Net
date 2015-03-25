using System;
using System.Collections.Generic;
using System.Text;

namespace Codentia.Common.Net.FTP
{
    /// <summary>
    /// The server response to an FTP Request
    /// </summary>
    public struct FTPResponse
    {
        private int _code;
        private string _message;

        /// <summary>
        /// Initializes a new instance of the <see cref="FTPResponse"/> struct.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="message">The message.</param>
        public FTPResponse(int code, string message)
        {
            _code = code;
            _message = message.Trim();
        }

        /// <summary>
        /// Gets the code.
        /// </summary>
        public int Code
        {
            get
            {
                return _code;
            }
        }

        /// <summary>
        /// Gets the message.
        /// </summary>
        public string Message
        {
            get
            {
                return _message;
            }
        }
    }
}
