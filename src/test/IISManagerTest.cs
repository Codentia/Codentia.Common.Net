using System;
using System.Net;
using Microsoft.Web.Administration;
using NUnit.Framework;

namespace Codentia.Common.Net.Test
{
    /// <summary>
    /// Unit testing framework for IISManager
    /// </summary>
    [TestFixture]
    public class IISManagerTest
    {
        /// <summary>
        /// Scenario: Method called with a non-existant or invalid VDirector
        /// Expected: Exception
        /// </summary>
        [Test]
        public void _001_AuthoriseIPAddress_InvalidVDir()
        {
            Assert.That(delegate { IISManager.AuthoriseIPAddress(null, IPAddress.Parse("192.168.0.1")); }, Throws.Exception.With.Message.EqualTo("virtualDirectory was not specified"));
            Assert.That(delegate { IISManager.AuthoriseIPAddress(string.Empty, IPAddress.Parse("192.168.0.1")); }, Throws.Exception.With.Message.EqualTo("virtualDirectory was not specified"));
            Assert.That(delegate { IISManager.AuthoriseIPAddress("VDirDoesNotExist", IPAddress.Parse("192.168.0.1")); }, Throws.Exception);
        }

        /// <summary>
        /// Scenario: Method called with a valid extant VDirectory
        /// Expected: Runs without error
        /// Notes: Needs to be manually verified at this stage, vdir must be manually created
        /// </summary>
        [Test]
        public void _002_AuthoriseIPAddress_ValidVDir()
        {
            IISManager.AuthoriseIPAddress("Default", IPAddress.Parse("192.168.0.1"));
        }

        /// <summary>
        /// Scenario: Attempt to create a duplicate entry
        /// Expected: Does not create entry, does not fail
        /// </summary>
        [Test]
        public void _003_AuthoriseIPAddress_ValidVDir_Duplicate()
        {
            IISManager.AuthoriseIPAddress("Default", IPAddress.Parse("192.168.0.1"));
            IISManager.AuthoriseIPAddress("Default", IPAddress.Parse("192.168.0.1"));
        }

        /// <summary>
        /// Scenario: Method called with a non-existant or invalid VDirector
        /// Expected: Exception
        /// </summary>
        [Test]
        public void _004_DeAuthoriseIPAddress_InvalidVDir()
        {
            Assert.That(delegate { IISManager.DeAuthoriseIPAddress(null, IPAddress.Parse("192.168.0.1")); }, Throws.Exception.With.Message.EqualTo("virtualDirectory was not specified"));
            Assert.That(delegate { IISManager.DeAuthoriseIPAddress(string.Empty, IPAddress.Parse("192.168.0.1")); }, Throws.Exception.With.Message.EqualTo("virtualDirectory was not specified"));
            Assert.That(delegate { IISManager.DeAuthoriseIPAddress("VDirDoesNotExist", IPAddress.Parse("192.168.0.1")); }, Throws.Exception);
        }

        /// <summary>
        /// Scenario: Method called with a valid extant VDirectory
        /// Expected: Runs without error
        /// Notes: Needs to be manually verified at this stage, vdir must be manually created
        /// </summary>
        [Test]
        public void _005_DeAuthoriseIPAddress_ValidVDir()
        {
            IISManager.DeAuthoriseIPAddress("Default", IPAddress.Parse("192.168.0.1"));
        }

        /// <summary>
        /// Scenario: Method called with a non-existant or invalid VDirector
        /// Expected: Exception
        /// </summary>
        [Test]
        public void _006_DeAuthoriseVirtualDirectory_InvalidVDir()
        {
            Assert.That(delegate { IISManager.DeAuthoriseVirtualDirectory(null); }, Throws.Exception.With.Message.EqualTo("virtualDirectory was not specified"));
            Assert.That(delegate { IISManager.DeAuthoriseVirtualDirectory(string.Empty); }, Throws.Exception.With.Message.EqualTo("virtualDirectory was not specified"));
            Assert.That(delegate { IISManager.DeAuthoriseVirtualDirectory("VDirDoesNotExist"); }, Throws.Exception);
        }

        /// <summary>
        /// Scenario: Method called with a valid extant VDirectory
        /// Expected: Runs without error
        /// Notes: Needs to be manually verified at this stage, vdir must be manually created
        /// </summary>
        [Test]
        public void _007_DeAuthoriseVirtualDirectory_ValidVDir()
        {
            IISManager.AuthoriseIPAddress("Default", IPAddress.Parse("192.168.0.1"));
            IISManager.DeAuthoriseVirtualDirectory("Default");
        }

        /// <summary>
        /// Scenario: Test live scenario where interaction between two users caused issues (adding two ips at once)
        /// Expected: Success
        /// </summary>
        [Test]
        public void _008_TwoIPs_Interactions()
        {
            string virtualDirectory = "Default";

            IISManager.DeAuthoriseVirtualDirectory(virtualDirectory);

            // check no IPs are authorised
            ServerManager iis = new ServerManager();
            Configuration config = iis.GetApplicationHostConfiguration();
            ConfigurationSection ipSecuritySection = config.GetSection("system.webServer/security/ipSecurity", virtualDirectory);
            ConfigurationElementCollection ipSecurityCollection = ipSecuritySection.GetCollection();
            Assert.That(ipSecurityCollection.Count, Is.EqualTo(0));

            // add one
            IISManager.AuthoriseIPAddress("Default", IPAddress.Parse("192.168.0.1"));

            // check 1 IPs are authorised
            iis = new ServerManager();
            config = iis.GetApplicationHostConfiguration();
            ipSecuritySection = config.GetSection("system.webServer/security/ipSecurity", virtualDirectory);
            ipSecurityCollection = ipSecuritySection.GetCollection();
            Assert.That(ipSecurityCollection.Count, Is.EqualTo(1));

            // add one
            IISManager.AuthoriseIPAddress("Default", IPAddress.Parse("192.168.0.2"));

            // check 2 IPs are authorised
            iis = new ServerManager();
            config = iis.GetApplicationHostConfiguration();
            ipSecuritySection = config.GetSection("system.webServer/security/ipSecurity", virtualDirectory);
            ipSecurityCollection = ipSecuritySection.GetCollection();
            Assert.That(ipSecurityCollection.Count, Is.EqualTo(2));
        }

        /// <summary>
        /// Scenario: Call method with an invalid or non-existant name
        /// Expected: Exception
        /// </summary>
        [Test]
        public void _009_RecycleApplicationPool_InvalidName()
        {
            // null
            Assert.That(delegate { IISManager.RecycleApplicationPool(null); }, Throws.Exception.With.Message.EqualTo("Invalid ApplicationPool Name ''"));

            // empty string
            Assert.That(delegate { IISManager.RecycleApplicationPool(string.Empty); }, Throws.Exception.With.Message.EqualTo("Invalid ApplicationPool Name ''"));

            // invalid string
            string invalid = Guid.NewGuid().ToString();
            Assert.That(delegate { IISManager.RecycleApplicationPool(invalid); }, Throws.Exception.With.Message.EqualTo(string.Format("Invalid ApplicationPool Name '{0}'", invalid)));
        }

        /// <summary>
        /// Scenario: Call method with a valid name
        /// Expected: Executes without error
        /// </summary>
        [Test]
        public void _010_RecycleApplicationPool_ValidName()
        {
            IISManager.RecycleApplicationPool("DefaultAppPool");
        }
    }
}
