using System;
using System.IO;
using System.Web;
using Codentia.Common.Helper;
using Codentia.Test.Helper;
using NUnit.Framework;

namespace Codentia.Common.Net.Test
{
    /// <summary>
    /// Unit testing framework for CDNManager static class
    /// </summary>
    [TestFixture]
    public class CDNManagerTest
    {
        /// <summary>
        /// Scenario: Method called with invalid parameter (null/empty)
        /// Expected: Exception
        /// </summary>
        [Test]
        public void _001_PutFile_InvalidApplication()
        {
            Assert.That(delegate { CDNManager.PutFile(null, "test", "test", "test"); }, Throws.Exception.With.Message.EqualTo("applicationName is not specified"));
            Assert.That(delegate { CDNManager.PutFile(string.Empty, "test", "test", "test"); }, Throws.Exception.With.Message.EqualTo("applicationName is not specified"));
        }

        /// <summary>
        /// Scenario: Method called with invalid parameter (null/empty)
        /// Expected: Exception
        /// </summary>
        [Test]
        public void _002_PutFile_InvalidDomain()
        {
            Assert.That(delegate { CDNManager.PutFile("test", null, "test", "test"); }, Throws.Exception.With.Message.EqualTo("domainName is not specified"));
            Assert.That(delegate { CDNManager.PutFile("test", string.Empty, "test", "test"); }, Throws.Exception.With.Message.EqualTo("domainName is not specified"));
        }

        /// <summary>
        /// Scenario: Method called with invalid parameter (null/empty)
        /// Expected: Exception
        /// </summary>
        [Test]
        public void _003_PutFile_InvalidFolder()
        {
            Assert.That(delegate { CDNManager.PutFile("test", "test", null, "test"); }, Throws.Exception.With.Message.EqualTo("folder is not specified"));
            Assert.That(delegate { CDNManager.PutFile("test", "test", string.Empty, "test"); }, Throws.Exception.With.Message.EqualTo("folder is not specified"));
        }

        /// <summary>
        /// Scenario: Method called with invalid parameter (null/empty)
        /// Expected: Exception
        /// </summary>
        [Test]
        public void _005_PutFile_InvalidLocalFile()
        {
            Assert.That(delegate { CDNManager.PutFile("test", "test", "test", "filedoesnotexist.txt"); }, Throws.Exception.With.Message.EqualTo("file: 'filedoesnotexist.txt' does not exist"));
        }

        /// <summary>
        /// Scenario: Method called with valid arguments, folder exists
        /// Expected: file uploaded successfully
        /// </summary>
        [Test]
        public void _006_PutFile_Valid()
        {
            string filename = string.Format("test{0}.txt", Guid.NewGuid());
            StreamWriter sw = File.CreateText(filename);
            sw.WriteLine("This is a test file");
            sw.Close();
            sw.Dispose();

            // there should be no exception
            CDNManager.PutFile("test", "test", "test", filename);
        }

        /// <summary>
        /// Scenario: Method called with invalid parameter (null/empty)
        /// Expected: Exception
        /// </summary>
        [Test]
        public void _007_PutFiles_InvalidApplication()
        {
            Assert.That(delegate { CDNManager.PutFiles(null, "test", "test", "test"); }, Throws.Exception.With.Message.EqualTo("applicationName is not specified"));
            Assert.That(delegate { CDNManager.PutFiles(string.Empty, "test", "test", "test"); }, Throws.Exception.With.Message.EqualTo("applicationName is not specified"));
        }

        /// <summary>
        /// Scenario: Method called with invalid parameter (null/empty)
        /// Expected: Exception
        /// </summary>
        [Test]
        public void _008_PutFiles_InvalidDomain()
        {
            Assert.That(delegate { CDNManager.PutFiles("test", null, "test", "test"); }, Throws.Exception.With.Message.EqualTo("domainName is not specified"));
            Assert.That(delegate { CDNManager.PutFiles("test", string.Empty, "test", "test"); }, Throws.Exception.With.Message.EqualTo("domainName is not specified"));
        }

        /// <summary>
        /// Scenario: Method called with invalid parameter (null/empty)
        /// Expected: Exception
        /// </summary>
        [Test]
        public void _009_PutFiles_InvalidFolder()
        {
            Assert.That(delegate { CDNManager.PutFiles("test", "test", null, "test"); }, Throws.Exception.With.Message.EqualTo("folder is not specified"));
            Assert.That(delegate { CDNManager.PutFiles("test", "test", string.Empty, "test"); }, Throws.Exception.With.Message.EqualTo("folder is not specified"));
        }

        /// <summary>
        /// Scenario: Method called with invalid parameter (null/empty)
        /// Expected: Exception
        /// </summary>
        [Test]
        public void _011_PutFiles_InvalidLocalFolder()
        {
            Assert.That(delegate { CDNManager.PutFiles("test", "test", "test", null); }, Throws.Exception.With.Message.EqualTo("local folder is not specified"));
            Assert.That(delegate { CDNManager.PutFiles("test", "test", "test", string.Empty); }, Throws.Exception.With.Message.EqualTo("local folder is not specified"));
        }

        /// <summary>
        /// Scenario: Method called with valid arguments, folder exists
        /// Expected: file uploaded successfully
        /// </summary>
        [Test]
        public void _012_PutFiles_Valid()
        {
            if (!Directory.Exists("testfolder"))
            {
                Directory.CreateDirectory("testfolder");
            }

            string filename = string.Format("testfolder/test{0}.txt", Guid.NewGuid());
            StreamWriter sw = File.CreateText(filename);
            sw.WriteLine("This is a test file");
            sw.Close();
            sw.Dispose();

            // there should be no expection
            CDNManager.PutFiles("test", "test", "test", "testfolder");
        }

        /// <summary>
        /// Scenario: Method called, compared to anticipated value
        /// </summary>
        [Test]
        public void _013_GetPublicUrl()
        {
            Assert.That(CDNManager.GetPublicUrl("ecommerce", "jerseymarket.co.uk", "products", "PR001.jpg"), Is.EqualTo(string.Format("http://cdn.mattchedit.com/ecommerce/{0}/jerseymarket.co.uk/products/PR001.jpg", SiteHelper.SiteEnvironment)));
        }

        /// <summary>
        /// Scenario: Method called with invalid parameter (null/empty)
        /// Expected: Exception
        /// </summary>
        [Test]
        public void _014_WriteImage_InvalidParameter()
        {
            Assert.That(delegate { CDNManager.WriteImage(null, null, "test", "test", "test"); }, Throws.Exception);
            Assert.That(delegate { CDNManager.WriteImage(null, string.Empty, "test", "test", "test"); }, Throws.Exception);
            Assert.That(delegate { CDNManager.WriteImage(null, "test", null, "test", "test"); }, Throws.Exception);
            Assert.That(delegate { CDNManager.WriteImage(null, "test", string.Empty, "test", "test"); }, Throws.Exception);
            Assert.That(delegate { CDNManager.WriteImage(null, "test", "test", null, "test"); }, Throws.Exception);
            Assert.That(delegate { CDNManager.WriteImage(null, "test", "test", string.Empty, "test"); }, Throws.Exception);
            Assert.That(delegate { CDNManager.WriteImage(null, "test", "test", "test", null); }, Throws.Exception);
            Assert.That(delegate { CDNManager.WriteImage(null, "test", "test", "test", string.Empty); }, Throws.Exception);
        }

        /// <summary>
        /// Scenario: Method called with valid arguments
        /// Expected: File is written out to response object appropriately
        /// </summary>
        [Test]
        public void _015_WriteImage_Valid()
        {
            HttpContext testContext = HttpHelper.CreateHttpContext("test");
            CDNManager.WriteImage(testContext.Response, "test", "test", "test", "tick.jpg");
        }

        /// <summary>
        /// Scenario: Call a non-existant file
        /// Expected: Failed silently (logs outcome)
        /// </summary>
        [Test]
        public void _016_WriteImage_404()
        {
            HttpContext testContext = HttpHelper.CreateHttpContext("test");
            CDNManager.WriteImage(testContext.Response, "test", "test", "test", Guid.NewGuid().ToString());
        }

        /// <summary>
        /// Scenario: Call method for a path which needs to be created
        /// Expected: Executes with no error, path exists
        /// </summary>
        [Test]
        public void _017_EnsurePathExists()
        {
            // call the method
            string domain = "testpaths_" + Guid.NewGuid().ToString();
            string folder = Guid.NewGuid().ToString();

            CDNManager.EnsurePathExists("test", domain, folder);

            // now try to upload a file to prove folder exists
            string filename = string.Format("testfolder/test_{0}.txt", Guid.NewGuid().ToString());
            StreamWriter sw = File.CreateText(filename);
            sw.WriteLine("This is a test file");
            sw.Close();
            sw.Dispose();

            // there should be no expection
            CDNManager.PutFile("test", domain, folder, filename);            
        }

        /// <summary>
        /// Scenario: Method called with invalid parameter (null/empty)
        /// Expected: Exception
        /// </summary>
        [Test]
        public void _018_WriteBinaryFile_InvalidParameter()
        {
            Assert.That(delegate { CDNManager.WriteBinaryFile(null, null, "test", "test", "test"); }, Throws.Exception);
            Assert.That(delegate { CDNManager.WriteBinaryFile(null, string.Empty, "test", "test", "test"); }, Throws.Exception);
            Assert.That(delegate { CDNManager.WriteBinaryFile(null, "test", null, "test", "test"); }, Throws.Exception);
            Assert.That(delegate { CDNManager.WriteBinaryFile(null, "test", string.Empty, "test", "test"); }, Throws.Exception);
            Assert.That(delegate { CDNManager.WriteBinaryFile(null, "test", "test", null, "test"); }, Throws.Exception);
            Assert.That(delegate { CDNManager.WriteBinaryFile(null, "test", "test", string.Empty, "test"); }, Throws.Exception);
            Assert.That(delegate { CDNManager.WriteBinaryFile(null, "test", "test", "test", null); }, Throws.Exception);
            Assert.That(delegate { CDNManager.WriteBinaryFile(null, "test", "test", "test", string.Empty); }, Throws.Exception);
        }
        
        /// <summary>
        /// Scenario: Method called with valid arguments
        /// Expected: File is written out to response object appropriately
        /// </summary>
        [Test]
        public void _019_WriteBinaryFile_Valid()
        {
            HttpContext testContext = HttpHelper.CreateHttpContext("test");
            CDNManager.WriteBinaryFile(testContext.Response, "test", "test", "test", "tick.jpg");
        }

        /// <summary>
        /// Scenario: Call a non-existant file
        /// Expected: Failed silently (logs outcome)
        /// </summary>
        [Test]
        public void _020_WriteBinaryFile_404()
        {
            HttpContext testContext = HttpHelper.CreateHttpContext("test");
            CDNManager.WriteBinaryFile(testContext.Response, "test", "test", "test", Guid.NewGuid().ToString());
        }

        /// <summary>
        /// Scenario: Prove we can delete a file
        /// Expected: Deletes file without error
        /// </summary>
        [Test]
        public void _021_DeleteFile_Valid()
        {
            // create file
            string filename = string.Format("test{0}.txt", Guid.NewGuid());
            StreamWriter sw = File.CreateText(filename);
            sw.WriteLine("This is a test file");
            sw.Close();
            sw.Dispose();
            CDNManager.PutFile("test", "test", "test", filename);

            // delete file
            CDNManager.DeleteFile("test", "test", "test", filename);
        }

        /// <summary>
        /// Scenario: Test the exists method before and after a file exists
        /// Expected: Correct value
        /// </summary>
        [Test]
        public void _022_FileExists()
        {
            string filename = string.Format("test{0}.txt", Guid.NewGuid());
            StreamWriter sw = File.CreateText(filename);
            sw.WriteLine("This is a test file");
            sw.Close();
            sw.Dispose();

            // should not exist
            bool exists = CDNManager.FileExists("test", "test", "test", filename);
            Assert.That(exists, Is.False);

            CDNManager.PutFile("test", "test", "test", filename);

            exists = CDNManager.FileExists("test", "test", "test", filename);
            Assert.That(exists, Is.True);
        }
    }
}
