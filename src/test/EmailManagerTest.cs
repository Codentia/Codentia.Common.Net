using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using Codentia.Common.Logging.BL;
using Codentia.Common.Net;
using Codentia.Test.Helper;
using NUnit.Framework;

namespace Codentia.Common.Net.Test
{
    /// <summary>
    /// Unit testing framework for EmailManager
    /// </summary>
    [TestFixture]
    public class EmailManagerTest
    {
        /// <summary>
        /// Perform any test setup activities
        /// </summary>
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
        }

        /// <summary>
        /// Perform any post-testing cleanup activities
        /// </summary>
        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            EmailManager.GetInstance().Dispose();
            LogManager.Instance.Dispose();
        }

        /// <summary>
        /// Scenario: Create an instance of EmailManager (default context)
        /// Expected: Not null
        /// </summary>
        [Test]
        public void _001_GetInstance()
        {
            EmailManager x = EmailManager.GetInstance();
            Assert.That(x, Is.Not.Null);
            x.Dispose();            
        }

        /// <summary>
        /// Scenario: Create an instance of EmailManager (web context)
        /// Expected: Not null
        /// </summary>
        [Test]
        public void _002_GetInstance_WithContext()
        {
            HttpContext context = HttpHelper.CreateHttpContext(string.Empty);

            EmailManager x = EmailManager.GetInstance(context);
            Assert.That(x, Is.Not.Null);
            x.Dispose();            
        }

        /// <summary>
        /// Scenario: Get/Set the TemplateBasePath property
        /// Expected: Property accepts and holds any given value
        /// </summary>
        [Test]
        public void _003_TemplateBasePath()
        {
            EmailManager x = EmailManager.GetInstance();

            x.TemplateBasePath = null;            
            Assert.That(x.TemplateBasePath, Is.Null);

            x.TemplateBasePath = string.Empty;
            Assert.That(x.TemplateBasePath, Is.EqualTo(string.Empty));

            x.TemplateBasePath = @"c:\stuff\";            
            Assert.That(x.TemplateBasePath, Is.EqualTo(@"c:\stuff\"));
            x.Dispose();
        }

        /// <summary>
        /// Scenario: Generate an email from a test template
        /// Expected: Valid html output as predicted, all tags replaced
        /// </summary>
        [Test]
        public void _004_CreateEmailBodyFromTemplate_Html_Valid()
        {
            EmailManager.GetInstance().TemplateBasePath = "TestData/EmailTemplates";
            
            Dictionary<string, string> tags = new Dictionary<string, string>();
            tags.Add("TAG", "Test004");

            string methodValue = EmailManager.GetInstance().CreateEmailBodyFromTemplate(true, "Template1", tags);

            Console.Out.WriteLine(methodValue);
            Assert.That(methodValue, Is.EqualTo("<html><b>Test004</b></html>"), "Incorrect output (see console)");
        }

        /// <summary>
        /// Scenario: Generate an email from a test template
        /// Expected: Valid text output as predicted, all tags replaced
        /// </summary>
        [Test]
        public void _005_CreateEmailBodyFromTemplate_Plain_Valid()
        {
            EmailManager.GetInstance().TemplateBasePath = "TestData/EmailTemplates";

            Dictionary<string, string> tags = new Dictionary<string, string>();
            tags.Add("TAG", "Test005");

            string methodValue = EmailManager.GetInstance().CreateEmailBodyFromTemplate(false, "Template1", tags);

            Console.Out.WriteLine(methodValue);
            Assert.That(methodValue, Is.EqualTo("Test005"), "Incorrect output (see console)");
        }

        /// <summary>
        /// Scenario: Try to create a formatted email body while referencing an invalid template file
        /// Expected: Exception(Unable to load message template)
        /// </summary>
        [Test]
        public void _006_CreateEmailBodyFromTemplate_InvalidTemplate()
        {
            EmailManager.GetInstance().TemplateBasePath = "TestData/EmailTemplates";            
            Dictionary<string, string> values = new Dictionary<string, string>(); 
            values.Add("Key1", "Value1");

            Assert.That(delegate { EmailManager.GetInstance().CreateEmailBodyFromTemplate(true, null, values); }, Throws.InstanceOf<Exception>().With.Message.EqualTo("Unable to load message template: "));
            Assert.That(delegate { EmailManager.GetInstance().CreateEmailBodyFromTemplate(true, string.Empty, values); }, Throws.InstanceOf<Exception>().With.Message.EqualTo("Unable to load message template: "));
            Assert.That(delegate { EmailManager.GetInstance().CreateEmailBodyFromTemplate(true, "wibble", values); }, Throws.InstanceOf<Exception>().With.Message.EqualTo("Unable to load message template: wibble"));
            Assert.That(delegate { EmailManager.GetInstance().CreateEmailBodyFromTemplate(false, null, null); }, Throws.InstanceOf<Exception>().With.Message.EqualTo("Unable to load message template: "));
            Assert.That(delegate { EmailManager.GetInstance().CreateEmailBodyFromTemplate(false, string.Empty, null); }, Throws.InstanceOf<Exception>().With.Message.EqualTo("Unable to load message template: "));
            Assert.That(delegate { EmailManager.GetInstance().CreateEmailBodyFromTemplate(false, "wibble", null); }, Throws.InstanceOf<Exception>().With.Message.EqualTo("Unable to load message template: wibble"));       
        }

        /// <summary>
        /// Scenario: Try to create a formatted email body while giving invalid or missing replacement tag values
        /// Expected: Exception(Failed to complete generation of content)
        /// </summary>
        [Test]
        public void _007_CreateEmailBodyFromTemplate_InvalidReplacements()
        {
            EmailManager.GetInstance().TemplateBasePath = "TestData/EmailTemplates";

            Assert.That(delegate { EmailManager.GetInstance().CreateEmailBodyFromTemplate(true, "Template1", null); }, Throws.InstanceOf<Exception>().With.Message.EqualTo("Failed to complete generation of content for template"));
            Assert.That(delegate { EmailManager.GetInstance().CreateEmailBodyFromTemplate(true, "Template1", new Dictionary<string, string>()); }, Throws.InstanceOf<Exception>().With.Message.EqualTo("Failed to complete generation of content for template"));       
        }

        /// <summary>
        /// Scenario: Send an email in plaintext
        /// Expected: no errors
        /// </summary>
        [Test]
        public void _008_SendEmail_ValidParameters_PlainText()
        {
            EmailManager.GetInstance().SendEmail("system@test.mattchedit.co.uk", "system@test.mattchedit.co.uk", "system@test.mattchedit.co.uk", "Common.Net.EmailManager Test008", "Testing..", false);
        }

        /// <summary>
        /// Scenario: Send an email in html format
        /// Expected: no errors
        /// </summary>
        [Test]
        public void _009_SendEmail_ValidParameters_Html()
        {
            EmailManager.GetInstance().SendEmail("system@test.mattchedit.co.uk", "system@test.mattchedit.co.uk", "system@test.mattchedit.co.uk", "Common.Net.EmailManager Test009", "<b>Testing..</b>", true);
        }

        /// <summary>
        /// Scenario: Try to send an email which fails
        /// Expected: Email not sent, message filed in error log, no exception thrown
        /// </summary>
        [Test]
        public void _010_SendEmail_InvalidParameters()
        {
            EmailManager.GetInstance().SendEmail("invalid address", "system@test.mattchedit.co.uk", "system@test.mattchedit.co.uk", "Common.Net.EmailManager Test010", "<b>Testing..</b>", true);
        }

        /// <summary>
        /// Scenario: Call method without setting template path
        /// Expected: Exception
        /// </summary>
        [Test]
        public void _011_SendEmailFromTemplate_NoTemplatePathSet()
        {
            EmailManager.GetInstance().Dispose();

            Assert.That(delegate { EmailManager.GetInstance().CreateEmailBodyFromTemplate(false, "Template1", new Dictionary<string, string>()); }, Throws.Exception.With.Message.EqualTo("TemplateBasePath has not been set"));
        }

        /// <summary>
        /// Scenario: Set TemplateBasePath with HttpContext present
        /// Expected: Completes without error
        /// </summary>
        [Test]
        public void _012_TemplateBasePath_Set_WithContext()
        {
            EmailManager.GetInstance().Dispose();

            HttpContext context = HttpHelper.CreateHttpContext(string.Empty);

            EmailManager.GetInstance(context).TemplateBasePath = "/template";
        }

        /// <summary>
        /// Scenario: Send an email with a null/empty string
        /// Expected: Error message generated, message sent in log information
        /// Notes: Test included for coverage, no evaluation required
        /// </summary>
        [Test]
        public void _013_SendEmail_WithNull()
        {
            EmailManager.GetInstance().SendEmail(null, null, null, "subject", "body", false);
        }

        /// <summary>
        /// Scenario: Call method and specify an attachment
        /// Expected: Completes without error
        /// </summary>
        [Test]
        public void _014_SendEmail_WithAttachment()
        {
            // create a temp file
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "Test File Contents - EmailManagerTest014", Encoding.ASCII);

            EmailManager.GetInstance().SendEmail("test@mattchedit.com", "test@mattchedit.com", "test@mattchedit.com", "EmailManagerTest014", "Test with attachment", true, new System.Net.Mail.Attachment[] { new System.Net.Mail.Attachment(tempFile) });
        }

        /// <summary>
        /// Scenario: Set the 'fake email failure' testing flag and prove it generates an exception
        /// Expected: Exception
        /// </summary>
        [Test]
        public void _015_FakeEmailFailure()
        {
            EmailManager.GetInstance().FakeEmailFailure = true;

            Assert.That(delegate { EmailManager.GetInstance().SendEmail("test@mattchedit.com", "test@mattchedit.com", "test@mattchedit.com", "test", "test", false); }, Throws.Exception.With.Message.EqualTo("FakeEmailFailure=true, message not sent"));
        }
    }
}
