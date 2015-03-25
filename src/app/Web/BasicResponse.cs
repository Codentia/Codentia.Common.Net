using System.Net;

namespace Codentia.Common.Net.Web
{
    /// <summary>
    /// Basic Response
    /// </summary>
    public struct BasicResponse
    {
        private HttpStatusCode _httpStatusCode;
        private string _content;
        private bool _isSuccessful;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicResponse"/> struct.
        /// </summary>
        /// <param name="isSuccessful">if set to <c>true</c> [is successful].</param>
        /// <param name="httpStatusCode">The http status code.</param>
        /// <param name="content">The content.</param>
        public BasicResponse(bool isSuccessful, HttpStatusCode httpStatusCode, string content)
        {            
            _content = content;
            _httpStatusCode = httpStatusCode;
            _isSuccessful = isSuccessful;
       }
        
        /// <summary>
        /// Gets the response content.
        /// </summary>
        public string Content
        {
            get
            {
                return _content;
            }
        }

        /// <summary>
        /// Gets the http status code.
        /// </summary>
        public HttpStatusCode HttpStatusCode
        {
            get
            {
                return _httpStatusCode;
            }
        }   

        /// <summary>
        /// Gets a value indicating whether the response has been successful.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this response has been successful; otherwise, <c>false</c>.
        /// </value>
        public bool IsSuccessful
        {
            get
            {
                return _isSuccessful;
            }
        }
    }
}
