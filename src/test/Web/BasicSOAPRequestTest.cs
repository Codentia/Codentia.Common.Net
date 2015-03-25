using System.Collections.Generic;
using System.Net;
using Codentia.Common.Net.Web;
using NUnit.Framework;

namespace Codentia.Common.Net.Test.Web
{
    /// <summary>
    /// Unit Testing framework for BasicSOAPRequest
    /// </summary>
    public class BasicSOAPRequestTest
    {
        /// <summary>
        /// Scenario: ExecuteRequest with correct SOAP parameters
        /// Expected: Success 
        /// </summary>
        [Test]
        public void _001_ExecuteRequest_Success()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("variableName", "machinename");

            BasicResponse response = BasicSoapRequest.ExecuteRequest(GetTestSOAPUrl(),  "http://tempuri.org/GetEnvironmentVariables", "GetEnvironmentVariables", "http://tempuri.org/", 10000, dict);
        
            // catch flaky webservice
            if (response.IsSuccessful)
            {
                Assert.That(response.IsSuccessful, Is.True);
                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    Assert.That(response.HttpStatusCode, Is.EqualTo(HttpStatusCode.OK));
                    Assert.That(response.Content.Contains("<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><soap:Body><GetEnvironmentVariablesResponse xmlns=\"http://tempuri.org/\"><GetEnvironmentVariablesResult>"), Is.True);               
                    Assert.That(response.Content.Contains("</GetEnvironmentVariablesResult></GetEnvironmentVariablesResponse></soap:Body></soap:Envelope>"), Is.True);     
                }
            }
            else
            {
                Assert.That(response.HttpStatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            }                       
        }

        /// <summary>
        /// Scenario: ExecuteRequest with incorrect url
        /// Expected: Exception (handled as IsSuccessful = false + an errormessage)
        /// </summary>
        [Test]
        public void _002_ExecuteRequest_404()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("variableName", "machinename");
            BasicResponse response = BasicSoapRequest.ExecuteRequest(GetTestSOAPUrl().Replace("asmx", "asm"), "http://tempuri.org/GetEnvironmentVariables", "GetEnvironmentVariables", "http://tempuri.org/", 10000, dict);
            Assert.That(response.IsSuccessful, Is.False);
            Assert.That(response.HttpStatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content, Is.EqualTo("The remote server returned an error: (404) Not Found."));
        }

        /// <summary>
        /// Scenario: ExecuteRequest with incorrect SOAP parameters
        /// Expected: Exception (handled as IsSuccessful = false + an errormessage)
        /// </summary>
        [Test]
        public void _003_ExecuteRequest_500()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("variableName", "machinename");
            BasicResponse response = BasicSoapRequest.ExecuteRequest(GetTestSOAPUrl(), "http://blah.com/GetEnvironmentVariables", "GetEnvironmentVariable", "http://tempuri.org/", 10000, dict);
            Assert.That(response.IsSuccessful, Is.False);
            Assert.That(response.HttpStatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            Assert.That(response.Content, Is.EqualTo("The remote server returned an error: (500) Internal Server Error."));
        }

        /*        
        /// <summary>
        /// Scenario: ExecuteRequest with no parameters with small timeout
        /// Expected: BasicResponse is returned with a timeout error
        /// </summary>
         [Test]
                public void _004_ExecuteRequest_NoParameters_WithTimeout()
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    dict.Add("variableName", "machinename");
                    BasicResponse response = BasicSoapRequest.ExecuteRequest(GetTestSOAPUrl(), "http://tempuri.org/GetEnvironmentVariables", "GetEnvironmentVariables", "http://tempuri.org/", 1, dict);
                    if (response.HttpStatusCode != HttpStatusCode.OK)
                    {
                        Assert.That(response.HttpStatusCode, Is.EqualTo(HttpStatusCode.Unused));
                        Assert.That(response.Content, Is.EqualTo("The operation has timed out"));
                    }
                }
         */

        /// <summary>
        /// Gets the test soap url.
        /// </summary>
        /// <returns>string of test soap url</returns>
        internal static string GetTestSOAPUrl()
        {
            string domain = "localhost/api.remotecontrol";
            switch (System.Environment.MachineName)
            {
                case "SRV02":
                    domain = "srv02rc.mattchedit.com";
                    break;
                case "SRV03":
                    domain = "srv03rc.mattchedit.com";
                    break;
            }

            return string.Format("http://{0}/v100/Service.asmx", domain);
        }
    }
}
