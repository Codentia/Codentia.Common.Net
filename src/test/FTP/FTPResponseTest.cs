using Codentia.Common.Net.FTP;
using NUnit.Framework;

namespace Codentia.Common.Net.Test.FTP
{
    /// <summary>
    /// Unit testing framework for FTPResponse struct
    /// </summary>
    [TestFixture]
    public class FTPResponseTest
    {
        /// <summary>
        /// Scenario: Create a new object with default constructor
        /// Expected: Properties match default values
        /// </summary>
        [Test]
        public void _001_Constructor_Default()
        {
            FTPResponse fr = new FTPResponse();
            Assert.That(fr.Code, Is.EqualTo(0));
            Assert.That(fr.Message, Is.Null);
        }

        /// <summary>
        /// Scenario: Create a new object with parameterised constructor
        /// Expected: Properties match parameter values
        /// </summary>
        [Test]
        public void _002_Constructor_Parameters()
        {
            FTPResponse fr = new FTPResponse(10, "test");            
            Assert.That(fr.Code, Is.EqualTo(10));
            Assert.That(fr.Message, Is.EqualTo("test"));
        }
    }
}
