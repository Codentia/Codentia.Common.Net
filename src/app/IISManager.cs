using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.Web.Administration;

namespace Codentia.Common.Net
{
    /// <summary>
    /// IIS Manager
    /// http://blogs.iis.net/bills/archive/2008/06/01/how-do-i-script-automate-iis7-configuration.aspx
    /// </summary>
    public static class IISManager
    {
        private static object _iisLock = new object();

        /// <summary>
        /// Authorises the IP address.
        /// </summary>
        /// <param name="virtualDirectory">The virtual directory.</param>
        /// <param name="address">The address.</param>
        public static void AuthoriseIPAddress(string virtualDirectory, IPAddress address)
        {
            lock (_iisLock)
            {
                if (string.IsNullOrEmpty(virtualDirectory))
                {
                    throw new Exception("virtualDirectory was not specified");
                }

                ServerManager iis = new ServerManager();
                Configuration config = iis.GetApplicationHostConfiguration();

                ConfigurationSection ipSecuritySection = config.GetSection("system.webServer/security/ipSecurity", virtualDirectory);
                ipSecuritySection["allowUnlisted"] = false;

                ConfigurationElementCollection ipSecurityCollection = ipSecuritySection.GetCollection();

                ConfigurationElement addElement = FindElement(ipSecurityCollection, "add", "ipAddress", address.ToString(), "subnetMask", @"255.255.255.255", "domainName", string.Empty);
                if (addElement == null)
                {
                    addElement = ipSecurityCollection.CreateElement("add");
                    addElement["ipAddress"] = address.ToString();
                    addElement["allowed"] = true;
                    ipSecurityCollection.Add(addElement);
                }

                iis.CommitChanges();
            }
        }

        /// <summary>
        /// Des the authorise IP address.
        /// </summary>
        /// <param name="virtualDirectory">The virtual directory.</param>
        /// <param name="address">The address.</param>
        public static void DeAuthoriseIPAddress(string virtualDirectory, IPAddress address)
        {
            lock (_iisLock)
            {
                if (string.IsNullOrEmpty(virtualDirectory))
                {
                    throw new Exception("virtualDirectory was not specified");
                }

                ServerManager iis = new ServerManager();
                Configuration config = iis.GetApplicationHostConfiguration();

                ConfigurationSection ipSecuritySection = config.GetSection("system.webServer/security/ipSecurity", virtualDirectory);
                ConfigurationElementCollection ipSecurityCollection = ipSecuritySection.GetCollection();

                ConfigurationElement addElement = FindElement(ipSecurityCollection, "add", "ipAddress", address.ToString(), "subnetMask", @"255.255.255.255", "domainName", string.Empty);
                if (addElement != null)
                {
                    ipSecurityCollection.Remove(addElement);
                }

                iis.CommitChanges();
            }
        }

        /// <summary>
        /// DeAuthorises a virtual directory.
        /// </summary>
        /// <param name="virtualDirectory">The virtual directory.</param>
        public static void DeAuthoriseVirtualDirectory(string virtualDirectory)
        {
            lock (_iisLock)
            {
                if (string.IsNullOrEmpty(virtualDirectory))
                {
                    throw new Exception("virtualDirectory was not specified");
                }

                ServerManager iis = new ServerManager();
                Configuration config = iis.GetApplicationHostConfiguration();

                ConfigurationSection ipSecuritySection = config.GetSection("system.webServer/security/ipSecurity", virtualDirectory);
                ConfigurationElementCollection ipSecurityCollection = ipSecuritySection.GetCollection();
                ipSecurityCollection.Clear();

                iis.CommitChanges();
            }
        }

        /// <summary>
        /// Recycles the application pool.
        /// </summary>
        /// <param name="applicationPool">The application pool.</param>
        public static void RecycleApplicationPool(string applicationPool)
        {
            ServerManager iis = new ServerManager();

            ApplicationPool appPool = null;

            try
            {
                appPool = iis.ApplicationPools[applicationPool];
            }
            catch (Exception)
            {
            }

            if (appPool == null)
            {
                throw new System.Exception(string.Format("Invalid ApplicationPool Name '{0}'", applicationPool));
            }

            appPool.Recycle();
            iis.CommitChanges();
        }

        private static ConfigurationElement FindElement(ConfigurationElementCollection collection, string elementTagName, params string[] keyValues)
        {
            ConfigurationElement foundElement = null;

            foreach (ConfigurationElement element in collection)
            {
                if (string.Equals(element.ElementTagName, elementTagName, StringComparison.OrdinalIgnoreCase))
                {
                    bool matches = true;

                    for (int i = 0; i < keyValues.Length; i += 2)
                    {
                        object o = element.GetAttributeValue(keyValues[i]);
                        string value = null;
                        if (o != null)
                        {
                            value = o.ToString();
                        }

                        if (!string.Equals(value, keyValues[i + 1], StringComparison.OrdinalIgnoreCase))
                        {
                            matches = false;
                            break;
                        }
                    }

                    if (matches)
                    {
                        foundElement = element;
                        break;
                    }
                }
            }

            return foundElement;
        }
}
}
