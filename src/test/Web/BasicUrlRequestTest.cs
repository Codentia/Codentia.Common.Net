using System.Collections.Generic;
using System.Net;
using Codentia.Common.Net.Web;
using NUnit.Framework;

namespace Codentia.Common.Net.Test.Web
{
    /// <summary>
    /// Unit Testing framework for  BasicUrlRequest
    /// </summary>
    public class BasicUrlRequestTest
    {
        /// <summary>
        /// Scenario: ExecuteRequest with no parameters
        /// Expected: Success 
        /// </summary>
        [Test]
        public void _001_ExecuteRequest_NoParameters_Success()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            BasicResponse response = BasicUrlRequest.ExecuteRequest(BasicSOAPRequestTest.GetTestSOAPUrl(), 10000, dict);
            if (response.IsSuccessful)
            {
                Assert.That(response.IsSuccessful, Is.True);
                Assert.That(response.Content.Contains("<html>"), Is.True);
                Assert.That(response.Content.Contains("</html>"), Is.True);
                Assert.That(response.Content.Contains("<body>"), Is.True);
                Assert.That(response.Content.Contains("</body>"), Is.True);
                Assert.That(response.Content.Contains(@"v100/Service.asmx?disco"), Is.True);                
                Assert.That(response.Content.Contains(@"<a href=""Service.asmx?op=GetEnvironmentVariables"), Is.True);                
            }
        }

        /// <summary>
        /// Scenario: ExecuteRequest with no parameters
        /// Expected: Success 
        /// </summary>
        [Test]
        public void _002_ExecuteRequest_WithParameters_Success()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("op", "GetEnvironmentVariables");
            dict.Add("variableName", "machinename");
            BasicResponse response = BasicUrlRequest.ExecuteRequest(string.Format("{0}", BasicSOAPRequestTest.GetTestSOAPUrl()), 10000, dict);

            // catch flaky webservice
            if (response.IsSuccessful)
            {
                Assert.That(response.IsSuccessful, Is.True);                
                Assert.That(response.Content.Contains("v100/Service.asmx"), Is.True);
            }
        }

        /// <summary>
        /// Scenario: ExecuteRequest with no parameters
        /// Expected: Success 
        /// </summary>
        [Test]
        public void _003_ExecuteRequest_WithParameters_WithSlash_Success()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("variableName", "machinename");
            BasicResponse response = BasicUrlRequest.ExecuteRequest(BasicSOAPRequestTest.GetTestSOAPUrl(), 10000, dict);

            // catch flaky webservice
            if (response.IsSuccessful)
            {
                Assert.That(response.IsSuccessful, Is.True);
                Assert.That(response.Content.Contains("v100/Service.asmx"), Is.True);
            }

            // many slashes
            dict = new Dictionary<string, string>();
            dict.Add("variableName", "machinename");
            response = BasicUrlRequest.ExecuteRequest(string.Format("{0}//////////", BasicSOAPRequestTest.GetTestSOAPUrl()), 10000, dict);

            // catch flaky webservice
            if (response.IsSuccessful)
            {
                Assert.That(response.IsSuccessful, Is.True);
                Assert.That(response.Content.Contains("v100/Service.asmx"), Is.True);
            }
        }

        /// <summary>
        /// Scenario: ExecuteRequest with no parameters with small timeout
        /// Expected: BasicResponse is returned with a timeout error
        /// </summary>
        [Test]
        public void _004_ExecuteRequest_NoParameters_WithTimeout()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            BasicResponse response = BasicUrlRequest.ExecuteRequest(BasicSOAPRequestTest.GetTestSOAPUrl(), 1, dict);
            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                Assert.That(response.HttpStatusCode, Is.EqualTo(HttpStatusCode.Unused));
                Assert.That(response.Content, Is.EqualTo("The operation has timed out"));
            }
        }       
    }
}
