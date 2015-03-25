using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Codentia.Common.Helper;

namespace Codentia.Common.Net.Web
{
    /// <summary>
    /// Basic Soap Request
    /// </summary>
    public static class BasicSoapRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BasicSoapRequest"/> class.
        /// </summary>
        /// <param name="url">the url</param>
        /// <param name="soapAction">The SOAP action.</param>
        /// <param name="webMethod">The web Method.</param>
        /// <param name="xmlNamespace">The XML namespace.</param>
        /// <param name="timeout">The timeout (in milliseconds)</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>BasicResponse object</returns>
       public static BasicResponse ExecuteRequest(string url, string soapAction, string webMethod, string xmlNamespace, int timeout, Dictionary<string, string> parameters)
       {
           ParameterCheckHelper.CheckIsValidString(url, "url", false);
           ParameterCheckHelper.CheckIsValidString(soapAction, "soapAction", false);
           ParameterCheckHelper.CheckIsValidString(webMethod, "webMethod", false);
           ParameterCheckHelper.CheckIsValidString(xmlNamespace, "xmlNamespace", false);

           HttpWebResponse response = null;           
           string responseString = string.Empty;

           // Create the request
           HttpWebRequest request = CreateWebRequest(url, soapAction, timeout);
           
           // get the response from the web service
           try
           {
               // write the soap envelope to request stream
               using (Stream requestStream = request.GetRequestStream())
               {
                   using (StreamWriter stmw = new StreamWriter(requestStream))
                   {
                       stmw.Write(CreateSoapEnvelope(webMethod, xmlNamespace, parameters));
                   }
               }
          
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

           Stream responseStream = response.GetResponseStream();
           StreamReader reader = new StreamReader(responseStream);
           responseString = reader.ReadToEnd();           

           return new BasicResponse(true, ((HttpWebResponse)response).StatusCode, responseString);
       }

       /// <summary>
       /// Strips the response.
       /// </summary>
       /// <param name="soapResponse">The SOAP response.</param>
       /// <param name="webMethod">The web method.</param>
       /// <returns>string of stripped response</returns>
       public static string StripSOAPResponse(string soapResponse, string webMethod)
       {
           string regexExtract = string.Format(@"<{0}Result>(?<Result>.*?)</{0}Result>", webMethod);
           return Regex.Match(soapResponse, regexExtract).Groups["Result"].Captures[0].Value;
       }

       private static string CreateSoapEnvelope(string webMethod, string xmlNamespace, Dictionary<string, string> parameters)
       {
           string soapEnvelope = @"<soap:Envelope
                                xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
                                xmlns:xsd='http://www.w3.org/2001/XMLSchema'
                                xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>
                                <soap:Body></soap:Body></soap:Envelope>";

           StringBuilder sbParameters = new StringBuilder();

           if (parameters.Count > 0)
           {               
               IEnumerator<string> ie = parameters.Keys.GetEnumerator();
               while (ie.MoveNext())
               {
                   string name = ie.Current;
                   string value = parameters[ie.Current];
                   sbParameters.Append(string.Format("<{0}>{1}</{0}>", name, value));
               }
           }

           string methodCall = string.Format(@"<{0} xmlns=""{1}""> {2} </{0}>", webMethod, xmlNamespace, sbParameters.ToString());
           StringBuilder sb = new StringBuilder(soapEnvelope);
           sb.Insert(sb.ToString().IndexOf("</soap:Body>"), methodCall);
           return sb.ToString();
       }

       private static HttpWebRequest CreateWebRequest(string url, string soapAction, int timeout)
       {
           HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
           webRequest.Headers.Add("SOAPAction", "\"" + soapAction + "\"");               
           webRequest.Headers.Add("To", url);
           webRequest.ContentType = "text/xml;charset=\"utf-8\"";
           webRequest.Accept = "text/xml";
           webRequest.Method = "POST";
           webRequest.Timeout = timeout;
           return webRequest;
       }
    }
}
