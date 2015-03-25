using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using Codentia.Common.Data.Caching;
using Codentia.Common.Logging;
using Codentia.Common.Logging.BL;

namespace Codentia.Common.Net.BotPlug
{
    /// <summary>
    /// BotPlug management class
    /// </summary>
    public static class BotPlugManager
    {
        private const string CACHEKEYREQUESTS = "BotPlug_Requests";
        private const string CACHEKEYBLOCKED = "BotPlug_Blocked";

        private static DateTime _lastCleanUp = DateTime.MinValue;
        private static object _cleanupLock = new object();

        /// <summary>
        /// Checks the request.
        /// </summary>
        /// <param name="request">The request.</param>
        public static void CheckRequest(HttpRequest request)
        {
            // do no work at all (not even get config) unless the url requested is aspx/ashx
            if (!request.Url.ToString().Contains(".aspx") && !request.Url.ToString().Contains(".ashx"))
            {
                return;
            }

            BotPlugConfiguration config = BotPlugConfiguration.GetConfig();
            DateTime now = DateTime.Now;
            bool isBlocked = false;

            // do nothing if we are using URL redirection blocking and the page in question IS the blocked page
            BlockedRequestAction blockedAction = (BlockedRequestAction)Enum.Parse(typeof(BlockedRequestAction), config.Settings["BlockedAction"].Value, true);

            if (blockedAction == BlockedRequestAction.Url)
            {
                if (config.Settings["BlockedUrl"].Value.Replace("~", string.Empty) == request.Url.PathAndQuery)
                {
                    return;
                }
            }

            string currentIPAddress = string.IsNullOrEmpty(request.UserHostAddress) ? "127.0.0.1" : request.UserHostAddress;

            // check whitelisted ip
            if (BotPlugManager.IsPatternMatch(config.IPAllowed, currentIPAddress))
            {
                return;
            }

            // check blacklisted ip
            if (BotPlugManager.IsPatternMatch(config.IPDenied, currentIPAddress))
            {
                BotPlugManager.HandleBlockedRequest(config, request, currentIPAddress, now);
            }

            // check blacklisted url pattern
            if (BotPlugManager.IsPatternMatch(config.URLDenied, request.Url.PathAndQuery))
            {
                BotPlugManager.HandleBlockedRequest(config, request, currentIPAddress, now);
            }

            // first, check if the current IP is blocked
            if (DataCache.DictionaryContainsKey<string, DateTime>(CACHEKEYBLOCKED, currentIPAddress))
            {
                DateTime blockedAt = DataCache.GetFromDictionary<string, DateTime>(CACHEKEYBLOCKED, currentIPAddress);

                if (blockedAt.AddMinutes(Convert.ToInt32(config.Settings["BlockDurationMinutes"].Value)) >= now)
                {
                    isBlocked = true;
                    BotPlugManager.HandleBlockedRequest(config, request, currentIPAddress, now);
                }
            }

            if (!isBlocked)
            {
                // if there are previous requests from this IP, check they are compliant
                if (DataCache.DictionaryContainsKey<string, List<DateTime>>(CACHEKEYREQUESTS, currentIPAddress))
                {
                    List<DateTime> requests = DataCache.GetFromDictionary<string, List<DateTime>>(CACHEKEYREQUESTS, currentIPAddress);
                    requests.Add(now);

                    // remove any entries older than a minute
                    List<DateTime> inTheLastMinute = new List<DateTime>();
                    DateTime oldestPermissible = now.AddSeconds(-60);

                    for (int i = 0; i < requests.Count; i++)
                    {
                        if (requests[i] >= oldestPermissible)
                        {
                            inTheLastMinute.Add(requests[i]);
                        }
                    }

                    // check if the count is above either tolerance and take the appropriate action
                    if (inTheLastMinute.Count > Convert.ToInt32(config.Settings["BlockRequestsPerMinute"].Value))
                    {
                        // end the request - it's blocked
                        BotPlugManager.HandleBlockedRequest(config, request, currentIPAddress, now);
                    }
                    else
                    {
                        if (inTheLastMinute.Count > Convert.ToInt32(config.Settings["ThrottleRequestsPerMinute"].Value))
                        {
                            // throttle the request by pausing (1s per request made, so gap grows)
                            System.Threading.Thread.Sleep((requests.Count - Convert.ToInt32(config.Settings["ThrottleRequestsPerMinute"].Value)) * 500);
                        }
                        else
                        {
                            // record the request, take no action
                            DataCache.AddToDictionary<string, List<DateTime>>(CACHEKEYREQUESTS, currentIPAddress, inTheLastMinute);
                        }
                    }
                }
                else
                {
                    // otherwise, just log it
                    List<DateTime> requests = new List<DateTime>();
                    requests.Add(now);

                    DataCache.AddToDictionary<string, List<DateTime>>(CACHEKEYREQUESTS, currentIPAddress, requests);
                }
            }

            BotPlugManager.HandleCleanUp(config, now);
        }

        /// <summary>
        /// Blocks the request.
        /// </summary>
        public static void BlockCurrentRequest()
        {
            BotPlugConfiguration config = BotPlugConfiguration.GetConfig();

            HttpRequest currentRequest = HttpContext.Current == null ? null : HttpContext.Current.Request;
            string currentIPAddress = currentRequest == null || string.IsNullOrEmpty(currentRequest.UserHostAddress) ? "127.0.0.1" : currentRequest.UserHostAddress;

            HandleBlockedRequest(config, currentRequest, currentIPAddress, DateTime.Now);
        }

        /// <summary>
        /// Determines whether [is pattern match] [the specified config].
        /// </summary>
        /// <param name="config">The config.</param>
        /// <param name="valueToCheck">The value to check.</param>
        /// <returns>
        ///   <c>true</c> if [is pattern match] [the specified config]; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsPatternMatch(BotPlugPatternConfigurationCollection config, string valueToCheck)
        {
            bool matched = false;

            for (int i = 0; i < config.Count && !matched; i++)
            {
                if (Regex.IsMatch(valueToCheck, config[i].Pattern))
                {
                    matched = true;
                }
            }

            return matched;
        }

        /// <summary>
        /// Handles the blocked request.
        /// </summary>
        /// <param name="config">The config.</param>
        /// <param name="request">The request.</param>
        /// <param name="ipAddress">The ip address.</param>
        /// <param name="now">The now.</param>
        private static void HandleBlockedRequest(BotPlugConfiguration config, HttpRequest request, string ipAddress, DateTime now)
        {
            BlockedRequestAction action = (BlockedRequestAction)Enum.Parse(typeof(BlockedRequestAction), config.Settings["BlockedAction"].Value, true);

            // if not already blocked, log event
            if (!DataCache.DictionaryContainsKey<string, DateTime>(CACHEKEYBLOCKED, ipAddress))
            {
                LogManager.Instance.AddToLog(LogMessageType.Information, "BotPlug", string.Format("Blocking IP={0} as limits have been exceeded", ipAddress));
            }

            switch (action)
            {
                case BlockedRequestAction.Terminate:
                    DataCache.AddToDictionary<string, DateTime>(CACHEKEYBLOCKED, ipAddress, now);

                    if (request != null)
                    {
                        request.RequestContext.HttpContext.Response.End();
                    }

                    break;
                case BlockedRequestAction.Url:
                    DataCache.AddToDictionary<string, DateTime>(CACHEKEYBLOCKED, ipAddress, now);

                    if (request != null)
                    {
                        request.RequestContext.HttpContext.Response.Redirect(config.Settings["BlockedUrl"].Value);
                    }

                    break;
            }
        }

        /// <summary>
        /// Handles the clean up.
        /// </summary>
        /// <param name="config">The config.</param>
        /// <param name="now">The now.</param>
        private static void HandleCleanUp(BotPlugConfiguration config, DateTime now)
        {
            if (_lastCleanUp.AddMinutes(Convert.ToInt32(config.Settings["CleanUpAfterMinutes"].Value)) < now)
            {
                _lastCleanUp = now;
                int blockRequestMinutes = Convert.ToInt32(config.Settings["BlockRequestsPerMinute"].Value);

                lock (_cleanupLock)
                {
                    // remove any IPs from the "watch" list if they haven't been seen for two minutes or more
                    Dictionary<string, List<DateTime>> requests = DataCache.GetSingleObject<Dictionary<string, List<DateTime>>>(CACHEKEYREQUESTS);
                    List<string> keysToRemove = new List<string>();

                    Dictionary<string, List<DateTime>>.Enumerator ipEnumerator = requests.GetEnumerator();
                    while (ipEnumerator.MoveNext())
                    {
                        // we can safely assume that the newest hit is the last one, so remove if that is too old (or there are no items)
                        if (ipEnumerator.Current.Value.Count == 0 || ipEnumerator.Current.Value[ipEnumerator.Current.Value.Count - 1].AddMinutes(2) < now)
                        {
                            keysToRemove.Add(ipEnumerator.Current.Key);
                        }
                    }

                    for (int i = 0; i < keysToRemove.Count; i++)
                    {
                        DataCache.RemoveFromDictionary<string, List<DateTime>>(CACHEKEYREQUESTS, keysToRemove[i]);
                    }

                    // now remove all blocked IPs whose records are older than the specified limit
                    Dictionary<string, DateTime> blocks = DataCache.GetSingleObject<Dictionary<string, DateTime>>(CACHEKEYBLOCKED);

                    if (blocks != null)
                    {
                        keysToRemove = new List<string>();

                        Dictionary<string, DateTime>.Enumerator blockEnumerator = blocks.GetEnumerator();
                        while (blockEnumerator.MoveNext())
                        {
                            if (blockEnumerator.Current.Value.AddMinutes(blockRequestMinutes) < now)
                            {
                                keysToRemove.Add(blockEnumerator.Current.Key);
                            }
                        }

                        for (int i = 0; i < keysToRemove.Count; i++)
                        {
                            DataCache.RemoveFromDictionary<string, DateTime>(CACHEKEYBLOCKED, keysToRemove[i]);
                        }
                    }
                }
            }
        }
    }
}
