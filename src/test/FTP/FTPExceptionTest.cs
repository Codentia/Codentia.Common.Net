using System;
using System.Collections.Generic;
using System.Text;
using Codentia.Common.Net.FTP;
using NUnit.Framework;

namespace Codentia.Common.Net.Test.FTP
{
    /// <summary>
    /// Unit testing framework for FTPException class
    /// </summary>
    [TestFixture]
    public class FTPExceptionTest
    {
        /// <summary>
        /// Scenario: Construct and evaluate properties
        /// Expected: Correct values
        /// </summary>
        [Test]
        public void _001_Constructor_Message()
        {
            FTPException ex = new FTPException("Test Message");
            Assert.That(ex.Message, Is.EqualTo("Test Message"));
            Assert.That(ex.ResponseCode, Is.EqualTo(0));
        }

        /// <summary>
        /// Scenario: Construct and evaluate properties
        /// Expected: Correct values
        /// </summary>
        [Test]
        public void _002_Constructor_MessageInnerEx()
        {
            FTPException ex1 = new FTPException("Test Msg 1");
            FTPException ex2 = new FTPException("Test Msg 2", ex1);

            Assert.That(ex2.Message, Is.EqualTo("Test Msg 2"));
            Assert.That(ex2.InnerException, Is.SameAs(ex1));
        }

        /// <summary>
        /// Scenario: Construct and evaluate properties
        /// Expected: Correct values
        /// </summary>
        [Test]
        public void _003_Constructor_FTPResponse()
        {
            FTPResponse fr = new FTPResponse(100, "Test Message");
            FTPException ex = new FTPException(fr);

            Assert.That(ex.ResponseCode, Is.EqualTo(fr.Code));
            Assert.That(ex.Message, Is.EqualTo(fr.Message));
        }

        /// <summary>
        /// Scenario: Construct and evaluate properties
        /// Expected: Correct values
        /// </summary>
        [Test]
        public void _004_Constructor_FTPResponseInnerEx()
        {
            FTPException ex1 = new FTPException("Test Msg 1");
            FTPResponse fr = new FTPResponse(100, "Test Message");
            FTPException ex2 = new FTPException(fr, ex1);

            Assert.That(ex2.ResponseCode, Is.EqualTo(fr.Code));
            Assert.That(ex2.Message, Is.EqualTo(fr.Message));
            Assert.That(ex2.InnerException, Is.SameAs(ex1));
        }

        /// <summary>
        /// Scenario: Construct and evaluate properties
        /// Expected: Correct values
        /// </summary>
        [Test]
        public void _004_Constructor_FTPResponseMessagePrefix()
        {
            FTPResponse fr = new FTPResponse(100, "Test Message");
            FTPException ex = new FTPException(fr, "prefix");

            Assert.That(ex.ResponseCode, Is.EqualTo(fr.Code));
            Assert.That(ex.Message, Is.EqualTo(string.Format("prefix: {0}", fr.Message)));
        }
    }
}
