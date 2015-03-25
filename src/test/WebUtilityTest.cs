using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Hosting;
using NUnit.Framework;

namespace Codentia.Common.Net.Test
{
    /// <summary>
    /// Unit Testing framework for WebUtility
    /// <para></para>
    /// Note that this testing is somewhat limited, until we are able to use NMock to represent some aspects.
    /// </summary>
    [TestFixture]
    public class WebUtilityTest
    {
        /// <summary>
        /// Scenario: Method called with valid arguments, no exclusion is matched.
        /// Expected: Correct response parameters
        /// </summary>
        [Test]
        public void _001_ValidArguments_NoExclusion()
        {
            // build test context
            System.IO.TextWriter tw = new System.IO.StringWriter();
            HttpWorkerRequest wr = new SimpleWorkerRequest("/webapp", "c:\\inetpub\\wwwroot\\webapp\\", "default.aspx", string.Empty, tw);            
            HttpContext.Current = new HttpContext(wr);

            // call method
            WebUtility.EnsureCorrectHost("www.mysite.com", "secure.mysite.com", new string[] { "test", "localhost" });

            // test resulting response object
            HttpResponse hr = HttpContext.Current.Response;
            Assert.That(hr.StatusCode, Is.EqualTo(301));
            Assert.That(hr.Status, Is.EqualTo("301 Moved Permanently"));
            Assert.That(hr.RedirectLocation, Is.EqualTo("http://www.mysite.com/webapp/default.aspx"));
        }

        /// <summary>
        /// Scenario: Method with valid arguments and an exclusion
        /// Expected: Exclusion handled appropriately
        /// </summary>
        [Test]
        public void _002_ValidArguments_Excluded()
        {
            // build test context
            System.IO.TextWriter tw = new System.IO.StringWriter();
            HttpWorkerRequest wr = new SimpleWorkerRequest("/webapp", "c:\\inetpub\\wwwroot\\webapp\\", "default.aspx", string.Empty, tw);            
            HttpContext.Current = new HttpContext(wr);

            // call method
            WebUtility.EnsureCorrectHost("www.mysite.com", "secure.mysite.com", new string[] { "127.0.0.1" });
        }

        /// <summary>
        /// Scenario: Method called with arguments which should result in a secure URL
        /// Expected: Valid url, correctly built
        /// </summary>
        [Test]
        public void _003_ValidArguments_Secure()
        {
            // build test context
            System.IO.TextWriter tw = new System.IO.StringWriter();
            HttpWorkerRequest wr = new SimpleWorkerRequest("/webapp", "c:\\inetpub\\wwwroot\\webapp\\", "secure/default.aspx", string.Empty, tw);
            HttpContext.Current = new HttpContext(wr);

            // call method
            WebUtility.EnsureCorrectHost("www.mysite.com", "secure.mysite.com", "secure/", new string[] { "test", "localhost" });

            // test resulting response object
            HttpResponse hr = HttpContext.Current.Response;
            Assert.That(hr.StatusCode, Is.EqualTo(301));
            Assert.That(hr.Status, Is.EqualTo("301 Moved Permanently"));
            Assert.That(hr.RedirectLocation, Is.EqualTo("https://www.mysite.com/webapp/secure/default.aspx"));
        }
    }
}
