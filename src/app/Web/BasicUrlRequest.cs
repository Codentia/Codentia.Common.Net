using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Codentia.Common.Helper;

namespace Codentia.Common.Net.Web
{
	/// <summary>
	/// Basic Url Request
	/// </summary>
	public static class BasicUrlRequest
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BasicUrlRequest"/> class.
		/// </summary>
		/// <param name="url">the url</param>
		/// <param name="timeout">The timeout (in milliseconds)</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns>BasicResponse object</returns>
		public static BasicResponse ExecuteRequest(string url, int timeout, Dictionary<string, string> parameters)
		{
			ParameterCheckHelper.CheckIsValidString(url, "url", false);

			// used to build entire input
			StringBuilder sbResponse = new StringBuilder();
			HttpWebResponse response;

			// used on each read operation
			byte[] buf = new byte[8192];

			// prepare the web page we will be asking for
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(CreateFullUrl(url, parameters));
			request.Timeout = timeout;

			// get the response from the url
			try
			{
				response = (HttpWebResponse)request.GetResponse();
			}
			catch (WebException ex)
			{                
				if (ex.Response == null)
				{
					return new BasicResponse(false, HttpStatusCode.Unused, ex.Message);
				}
				else
				{
				return new BasicResponse(false, ((HttpWebResponse)ex.Response).StatusCode, ex.Message);
				}
			}

			// we will read data via the response stream
			Stream responseStream = response.GetResponseStream();

			string tempString = null;
			int count = 0;

			do
			{
				// fill the buffer with data
				count = responseStream.Read(buf, 0, buf.Length);

				// make sure we read some data
				if (count != 0)
				{
					// translate from bytes to ASCII text
					tempString = Encoding.ASCII.GetString(buf, 0, count);

					// continue building the string
					sbResponse.Append(tempString);
				}
			}
			while (count > 0); // any more data to read?
			
			return new BasicResponse(true, ((HttpWebResponse)response).StatusCode, sbResponse.ToString());
		}

		private static string CreateFullUrl(string url, Dictionary<string, string> parameters)
		{
			string retVal = url;
			while (retVal.EndsWith("/"))
			{
				retVal = retVal.Substring(0, retVal.Length - 1);
			}

			StringBuilder sbParameters = new StringBuilder();
			int parameterCount = 0;
			if (parameters.Count > 0)
			{                
				IEnumerator<string> ie = parameters.Keys.GetEnumerator();
				while (ie.MoveNext())
				{
					sbParameters.Append(parameterCount == 0 ? "?" : "&");
					string name = ie.Current;
					string value = parameters[ie.Current];
					sbParameters.Append(string.Format("{0}={1}", name, value));
					parameterCount++;
				}
			}

			return string.Format("{0}{1}", retVal, sbParameters.ToString());
		}
	}
}
