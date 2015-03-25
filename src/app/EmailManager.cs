using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.Xml;
using Codentia.Common.Logging;
using Codentia.Common.Logging.BL;

namespace Codentia.Common.Net
{
    /// <summary>
    /// Static class to handle email functionality
    /// </summary>
    public sealed class EmailManager : IDisposable
    {
        private static object _instanceLock = new object();
        private static EmailManager _instance;

        private string _templateBasePath = null;
        private HttpContext _context = null;
        private bool _fakeEmailFailure = false;

        private EmailManager()
        {
        }
       
        /// <summary>
        /// Gets or sets the base path from which email templates should be loaded
        /// </summary>
        public string TemplateBasePath
        {
            get
            {
                return _templateBasePath;
            }

            set
            {
                if (this.Context == null || this.Context.Server == null)
                {
                    _templateBasePath = value;
                }
                else
                {
                    _templateBasePath = this.Context.Server.MapPath(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the context.
        /// </summary>
        /// <value>The context.</value>
        public HttpContext Context
        {
            get
            {
                return _context;
            }

            set
            {
                _context = value;
            }
        }

        /// <summary>
        /// Sets a value indicating whether [fake email failure].
        /// </summary>
        /// <value><c>true</c> if [fake email failure]; otherwise, <c>false</c>.</value>
        public bool FakeEmailFailure
        {
            set
            {
                _fakeEmailFailure = value;
            }
        }

        /// <summary>
        /// Get an instance of EmailManager for use
        /// </summary>
        /// <returns>EmailManager object</returns>
        public static EmailManager GetInstance()
        {
            return GetInstance(null);
        }

        /// <summary>
        /// Get an instance of EmailManager for use in a web context (supports mapping paths using server object)        
        /// </summary>
        /// <param name="context">HttpContext for use in mapping paths (ignored if null)</param>
        /// <returns>EmailManager object</returns>
        public static EmailManager GetInstance(HttpContext context)
        {
            lock (_instanceLock)
            {
                if (_instance == null)
                {
                    _instance = new EmailManager();
                    _instance.Context = context;
                }
            }

            return _instance;
        }

        /// <summary>
        /// Creates the email body from template.
        /// </summary>
        /// <param name="isHtml">if set to <c>true</c> [is HTML].</param>
        /// <param name="template">The template.</param>
        /// <param name="replacementValues">The replacement values.</param>
        /// <returns>string of email body</returns>
        public string CreateEmailBodyFromInlineTemplate(bool isHtml, string template, Dictionary<string, string> replacementValues)
        {
            // now strip carriage returns and tabs from html
            template = template.Replace("\r", string.Empty);
            template = template.Replace("\n", string.Empty);
            template = template.Replace("\t", string.Empty);

            if (replacementValues != null)
            {
                Dictionary<string, string>.Enumerator replacementEnumerator = replacementValues.GetEnumerator();
                while (replacementEnumerator.MoveNext())
                {
                    template = template.Replace(string.Format("~~{0}~~", replacementEnumerator.Current.Key), replacementEnumerator.Current.Value).Trim();
                }
            }

            if (string.IsNullOrEmpty(template) || template.Contains("~~"))
            {
                LogValues(replacementValues);
                throw new Exception("Failed to complete generation of content for template");
            }
    
            return template;
        }

            /// <summary>
        /// Create a formatted email body string (content)
        /// </summary>
        /// <param name="isHtml">Should the HTML template be used?</param>
        /// <param name="templateFile">Template to use (filename only)</param>
        /// <param name="replacementValues">Dictionary of tag replacement values to insert into email template</param>
        /// <returns>string of email body</returns>
        public string CreateEmailBodyFromTemplate(bool isHtml, string templateFile, Dictionary<string, string> replacementValues)
        {
            // check for null only, not empty string
            if (_templateBasePath == null)
            {
                throw new Exception("TemplateBasePath has not been set");
            }

            XmlDocument template = new XmlDocument();
            string content = string.Empty;

            try
            {
                template.Load(string.Format("{0}{1}{2}.xml", _templateBasePath, System.IO.Path.DirectorySeparatorChar, templateFile));
            }
            catch (Exception ex)
            {
                LogValues(replacementValues);
                LogManager.Instance.AddToLog(LogMessageType.NonFatalError, "EmailManager", string.Format("Unable to load XML template {0}", templateFile));
                LogManager.Instance.AddToLog(ex, "EmailManager");
            }

            if (template.DocumentElement != null)
            {
                XmlNode templateNode = null;

                if (isHtml)
                {
                    templateNode = template.DocumentElement.SelectSingleNode("html");
                    content = templateNode.OuterXml;
                }
                else
                {
                    templateNode = template.DocumentElement.SelectSingleNode("plaintext");
                    content = templateNode.InnerText;
                }

                content = CreateEmailBodyFromInlineTemplate(isHtml, content, replacementValues);
            }
            else
            {
                LogValues(replacementValues);
                throw new Exception(string.Format("Unable to load message template: {0}", templateFile));
            }

            return content;
        }

        /// <summary>
        /// Sends the email.
        /// </summary>
        /// <param name="fromAddress">From address.</param>
        /// <param name="replyToAddress">The reply to address.</param>
        /// <param name="toAddress">To address.</param>
        /// <param name="subject">The subject.</param>
        /// <param name="body">The body.</param>
        /// <param name="isHtml">if set to <c>true</c> [is HTML].</param>
        public void SendEmail(string fromAddress, string replyToAddress, string toAddress, string subject, string body, bool isHtml)
        {
            this.SendEmail(fromAddress, replyToAddress, toAddress, subject, body, isHtml, null);
        }

        /// <summary>
        /// Send an email.
        /// </summary>
        /// <param name="fromAddress">From address - this should be a valid address authorised to send email (e.g. a system one)</param>
        /// <param name="replyToAddress">ReplyTo address - this should be the address to which replies should be sent</param>
        /// <param name="toAddress">to address</param>
        /// <param name="subject">subject of email</param>
        /// <param name="body">body of email</param>
        /// <param name="isHtml">is this an html message?</param>
        /// <param name="attachments">The attachments.</param>
        public void SendEmail(string fromAddress, string replyToAddress, string toAddress, string subject, string body, bool isHtml, Attachment[] attachments)
        {
            // if in test "fake email failure" mode, then fail
            if (_fakeEmailFailure)
            {
                throw new Exception("FakeEmailFailure=true, message not sent");
            }

            if (ConfigurationManager.AppSettings["SendEmails"] != "N")
            {
                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(replyToAddress) || string.IsNullOrEmpty(toAddress))
                {
                    LogManager.Instance.AddToLog(LogMessageType.FatalError, "Common.Net", string.Format("EmailManager.SendEmail from={0}, to={1}, replyTo={2}, subject={3}, body={4}, isHtml={5}", fromAddress, toAddress, replyToAddress, subject, body, isHtml));
                }

                LogManager.Instance.AddToLog(LogMessageType.Information, "Common.Net", string.Format("EmailManager.SendEmail from={0}, to={1}, subject={2}, body={3}, isHtml={4}", fromAddress, toAddress, subject, body, isHtml));

                try
                {
                    SmtpClient client = new SmtpClient();

                    MailMessage msg = new MailMessage(fromAddress, toAddress, subject, body);                    
                    msg.ReplyToList.Add(new MailAddress(replyToAddress));
                    msg.IsBodyHtml = isHtml;

                    if (attachments != null)
                    {
                        for (int i = 0; i < attachments.Length; i++)
                        {
                            msg.Attachments.Add(attachments[i]);
                        }
                    }

                    client.Send(msg);
                }
                catch (Exception ex)
                {
                    LogManager.Instance.AddToLog(ex, "Codentia.Common.Net.EmailManager.SendEmail");
                }
            }
        }

        /// <summary>
        /// Dispose the current instance
        /// </summary>
        public void Dispose()
        {
            lock (_instanceLock)
            {
                _instance = null;
            }
        }

        private void LogValues(Dictionary<string, string> values)
        {
            StringBuilder sb = new StringBuilder();
            
            if (values != null)
            {
                Dictionary<string, string>.Enumerator valueEnum = values.GetEnumerator();
                while (valueEnum.MoveNext())
                {
                    sb.AppendFormat("{2}{0}={1}", valueEnum.Current.Key, valueEnum.Current.Value, sb.Length > 0 ? ", " : string.Empty);
                }
            }
            else
            {
                sb.Append("No values were specified");
            }

            LogManager.Instance.AddToLog(LogMessageType.Information, "Codentia.Common.Net", sb.ToString());
        }              
    }
}
