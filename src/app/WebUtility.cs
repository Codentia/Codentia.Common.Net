using System;
using System.Collections.Generic;
using System.Web;
using Codentia.Common.Helper;
using Codentia.Common.Logging;
using Codentia.Common.Logging.BL;

namespace Codentia.Common.Net
{
    /// <summary>
    /// This static class encapsulates web utility methods
    /// </summary>
    public class WebUtility
    {
        private static Dictionary<string, string> _rules404 = null;

        /// <summary>
        /// Ensure the current request references the correct host (authority), and redirect if this is not the case.
        /// </summary>
        /// <param name="expectedHost">Anticipated HTTP authority</param>
        /// <param name="expectedSecureHost">Anticipated HTTPS authority</param>
        /// <param name="securePath">The secure path.</param>
        /// <param name="exclusions">Excluded Authorities e.g. test.mattchedit.com, localhost, 127.0.0.1 etc</param>
        public static void EnsureCorrectHost(string expectedHost, string expectedSecureHost, string securePath, string[] exclusions)
        {
            string authority = HttpContext.Current.Request.Url.Authority.ToLower();
            string pathAndQuery = HttpContext.Current.Request.Url.PathAndQuery.ToLower();

            bool shouldBeSecure = !string.IsNullOrEmpty(securePath) && pathAndQuery.Contains(securePath.ToLower());

            for (int i = 0; i < exclusions.Length; i++)
            {
                if (authority.Contains(exclusions[i]))
                {
                    return;
                }
            }

            bool isSecure = WebUtility.IsCurrentConnectionSecure();

            if ((authority != expectedHost && !isSecure) || (authority != expectedSecureHost && isSecure) || (shouldBeSecure != isSecure))
            {
                string correctUrl = HttpContext.Current.Request.Url.ToString().Replace(HttpContext.Current.Request.Url.Authority, isSecure ? expectedSecureHost : expectedHost);

                if (shouldBeSecure)
                {
                    correctUrl = correctUrl.Replace("http://", "https://");
                }
                else
                {
                    correctUrl = correctUrl.Replace("https://", "http://");
                }

                HttpContext.Current.Response.Clear();
                HttpContext.Current.Response.Status = "301 Moved Permanently";
                HttpContext.Current.Response.AddHeader("Location", correctUrl);
                HttpContext.Current.Response.End();
            }
        }

        /// <summary>
        /// Ensures the correct host.
        /// </summary>
        /// <param name="expectedHost">The expected host.</param>
        /// <param name="expectedSecureHost">The expected secure host.</param>
        /// <param name="exclusions">The exclusions.</param>
        public static void EnsureCorrectHost(string expectedHost, string expectedSecureHost, string[] exclusions)
        {
            WebUtility.EnsureCorrectHost(expectedHost, expectedSecureHost, null, exclusions);
        }

        /// <summary>
        /// Handles the website error.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="request">The request.</param>
        /// <param name="response">The response.</param>
        /// <param name="domainName">Name of the domain.</param>
        /// <param name="redirectOn404">if set to <c>true</c> [redirect on404].</param>
        /// <param name="relative404path">The relative404path.</param>
        /// <param name="redirectOn500">if set to <c>true</c> [redirect on500].</param>
        /// <param name="relative500path">The relative500path.</param>
        public static void HandleWebsiteError(HttpServerUtility server, HttpRequest request, HttpResponse response, string domainName, bool redirectOn404, string relative404path, bool redirectOn500, string relative500path)
        {
            bool is404 = false;
            bool suppressLogging = false;
            Exception ex = server.GetLastError();

            if (ex is HttpException)
            {
                HttpException hex = (HttpException)ex;

                if (hex.GetHttpCode() == 404)
                {
                    is404 = true;
                    suppressLogging = true;
                }
                else
                {
                    // catch 'dangerous path' errors, log and don't email
                    if (hex.Message.StartsWith("A potentially dangerous Request.Path value was detected from the client"))
                    {
                        suppressLogging = true;
                        LogManager.Instance.AddToLog(LogMessageType.NonFatalError, "WebUtility.HandleWebsiteError", string.Format("Caught dangerous path: {0} from {1}/{2}", HttpContext.Current.Request.Path, HttpContext.Current.Request.UserHostAddress, HttpContext.Current.Request.UserAgent));
                    }
                }
            }

            while (ex != null)
            {
                if (!(ex is System.Web.UI.ViewStateException))
                {
                    if (!suppressLogging)
                    {
                        LogManager.Instance.AddToLog(ex, string.Format("{0} ({1}) - {2}", domainName, SiteHelper.SiteEnvironment, ex.GetType().ToString()));
                    }
                }

                ex = ex.InnerException;
            }

            if (is404)
            {
                if (redirectOn404)
                {
                    response.Redirect(string.Format("{0}?aspxerrorpath={1}", relative404path, request.Path));
                }
            }
            else
            {
                if (redirectOn500)
                {
                    response.Redirect(string.Format("{0}", relative500path));
                }
            }
        }

        /// <summary>
        /// Add404s the rule.
        /// </summary>
        /// <param name="oldPath">The old path.</param>
        /// <param name="newPath">The new path.</param>
        public static void Add404Rule(string oldPath, string newPath)
        {
            if (_rules404 == null)
            {
                _rules404 = new Dictionary<string, string>();
            }

            oldPath = oldPath.ToLower();

            if (!_rules404.ContainsKey(oldPath))
            {
                _rules404.Add(oldPath, newPath);
            }
        }

        /// <summary>
        /// Handles the 404 and redirects.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="response">The response.</param>
        /// <param name="domainName">Name of the domain.</param>
        public static void Handle404Redirect(HttpRequest request, HttpResponse response, string domainName)
        {
            string url = string.IsNullOrEmpty(request["aspxerrorpath"]) ? request.RawUrl : request["aspxerrorpath"];

            if (url != null)
            {
                string oldPath = url.ToLower();

                if (_rules404.ContainsKey(oldPath))
                {
                    response.RedirectPermanent(_rules404[oldPath]);
                }

                if (!string.IsNullOrEmpty(url))
                {
                    try
                    {
                        LogManager.Instance.AddToLog(LogMessageType.NonFatalError, string.Concat(domainName, " (404)"), string.Format("path={1}, IP={2}, Referral={3}, UserAgent={4}", domainName, url, request.UserHostAddress, request.UrlReferrer != null ? request.UrlReferrer.ToString() : "none", request.UserAgent));
                    }
                    catch (Exception)
                    {
                        // disregard any exceptions
                    }
                }
            }
        }

        /// <summary>
        /// Is Current Connection Secure?
        /// </summary>
        /// <returns>bool - true if it is, otherwise false</returns>
        internal static bool IsCurrentConnectionSecure()
        {
            return HttpContext.Current != null && HttpContext.Current.Request != null && HttpContext.Current.Request.IsSecureConnection;
        }
    }
}
