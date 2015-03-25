using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Codentia.Common.Net.FTP;
using NUnit.Framework;

namespace Codentia.Common.Net.Test.FTP
{
    /// <summary>
    /// Unit testing framework for FTPClient class
    /// </summary>
    [TestFixture]
    public class FTPClientTest
    {
        private static string _ftpHost = "srv01.mattchedit.com";
        private static string _ftpUser = "backup";
        private static string _ftpPass = "959530A8-F945-4F73-B18A-03785440661E";
        private TcpListener _tcpListener;
        private TcpListener _tcpListenerData;

        /// <summary>
        /// Perform set up
        /// </summary>
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
        }

        /// <summary>
        /// Perform clean up
        /// </summary>
        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            // delete all files in test folder
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot/test", _ftpUser, _ftpPass);
            string[] files = client.GetFiles("test*.txt");
            for (int i = 0; i < files.Length; i++)
            {
                client.DeleteRemoteFile(files[i]);
            }
        }

        /// <summary>
        /// Scenario: Create object with default constructor
        /// Expected: Properties match default values
        /// </summary>
        [Test]
        public void _001_Constructor_Default()
        {
            FTPClient client = new FTPClient();
            Assert.That(client.Connected, Is.False);
            Assert.That(client.Credentials, Is.Null);
            Assert.That(client.Port, Is.EqualTo(21));
            Assert.That(client.Mode, Is.EqualTo(FTPTransferMode.ASCII));
        }

        /// <summary>
        /// Scenario: Create object with first constructor (Host, Port, Path, Credentials)
        /// Expected: Properties match input values where specified, else default
        /// </summary>
        [Test]
        public void _002_Constructor_HostPortPathCredentials()
        {
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass);
            Assert.That(client.Connected, Is.False);
            Assert.That(client.Credentials, Is.Not.Null);            
            Assert.That(client.Credentials.UserName, Is.EqualTo(_ftpUser));
            Assert.That(client.Credentials.Password, Is.EqualTo(_ftpPass));
            Assert.That(client.Port, Is.EqualTo(21));
            Assert.That(client.Mode, Is.EqualTo(FTPTransferMode.ASCII));
            Assert.That(client.RemotePath, Is.EqualTo("/ftproot"));
            Assert.That(client.RemoteHost, Is.EqualTo(_ftpHost));
        }

        /// <summary>
        /// Scenario: Create object with second constructor (Host, Port, Path, Credentials, Mode)
        /// Expected: Properties match input values where specified, else default
        /// </summary>
        [Test]
        public void _003_Constructor_HostPortPathCredentialsMode()
        {
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            Assert.That(client.Connected, Is.False);
            Assert.That(client.Credentials, Is.Not.Null);
            Assert.That(client.Credentials.UserName, Is.EqualTo(_ftpUser));
            Assert.That(client.Credentials.Password, Is.EqualTo(_ftpPass));
            Assert.That(client.Port, Is.EqualTo(21));
            Assert.That(client.Mode, Is.EqualTo(FTPTransferMode.Binary));
            Assert.That(client.RemotePath, Is.EqualTo("/ftproot"));
            Assert.That(client.RemoteHost, Is.EqualTo(_ftpHost));
        }

        /// <summary>
        /// Scenario: Create object with overload (path, mode)
        /// Expected: Properties match input values where specified, else default from app.config (host, user, password)
        /// </summary>
        [Test]
        public void _003b_Constructor_HostPortPathCredentialsMode()
        {
            FTPClient client = new FTPClient("/ftproot", FTPTransferMode.Binary);
            Assert.That(client.Connected, Is.False);
            Assert.That(client.Credentials, Is.Not.Null);
            Assert.That(client.Credentials.UserName, Is.EqualTo(ConfigurationManager.AppSettings["DefaultFTPUser"]));
            Assert.That(client.Credentials.Password, Is.EqualTo(ConfigurationManager.AppSettings["DefaultFTPPassword"]));
            Assert.That(client.Port, Is.EqualTo(21));
            Assert.That(client.Mode, Is.EqualTo(FTPTransferMode.Binary));
            Assert.That(client.RemotePath, Is.EqualTo("/ftproot"));
            Assert.That(client.RemoteHost, Is.EqualTo(ConfigurationManager.AppSettings["DefaultFTPHost"]));
        }

        /// <summary>
        /// Scenario: Try to connect to an unknown host (unresolvable hostname)
        /// Expected: SocketException(No such host is known)
        /// </summary>
        [Test]
        public void _004_Connect_UnknownHost()
        {
            FTPClient client = new FTPClient(string.Format("{0}.com", Guid.NewGuid()), 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            Assert.That(delegate { client.Connect(); }, Throws.Exception);          
        }

        /// <summary>
        /// Scenario: Attempt to connect to a valid server, but on a port where there is no ftp server
        /// Expected: FTPException(0, Unable to connect to remote server)
        /// </summary>
        [Test]
        public void _005_Connect_NoFTPServerRunning()
        {
            FTPClient client = new FTPClient("192.168.0.1", 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            Assert.That(delegate { client.Connect(); }, Throws.Exception.With.Message.EqualTo("Unable to connect to remote server"));     
        }

        /// <summary>
        /// Scenario: Attempt to connect to a valid server, with an invalid username
        /// Expected: FTPException(530, User xx cannot log in)
        /// </summary>
        [Test]
        public void _006_Connect_IncorrectUser()
        {
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", "wibble", "wobble", FTPTransferMode.Binary);
            Assert.That(delegate { client.Connect(); }, Throws.InstanceOf<FTPException>());
        }

        /// <summary>
        /// Scenario: Attempt to connect to a valid server, but with an invalid password
        /// Expected: FTPException(530, User xx cannot log in)
        /// </summary>
        [Test]
        public void _007_Connect_IncorrectPassword()
        {
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, "wobble", FTPTransferMode.Binary);
            Assert.That(delegate { client.Connect(); }, Throws.InstanceOf<FTPException>());
        }

        /// <summary>
        /// Scenario: Connect to a valid host and port using a valid username and password
        /// Expected: Completes without error, connected flag is true after connection
        /// </summary>
        [Test]
        public void _008_Connect_ValidUserNamePassword()
        {
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.Connect();
            Assert.That(client.Connected, Is.True);
            client.Close();
        }

        /// <summary>
        /// Scenario: Change the RemotePath of an FTPClient once it is connected
        /// Expected: Path changes, ChDir command executed.
        /// </summary>
        [Test]
        public void _009_RemotePath_Connected()
        {
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.Connect();
            client.RemotePath = "/ftproot/test";
            Assert.That(client.RemotePath, Is.EqualTo("/ftproot/test"));
            client.Close();
        }

        /// <summary>
        /// Scenario: Change the RemotePath of an FTPClient which is not connected
        /// Expected: Client connects, Path changes, ChDir command executed.
        /// </summary>
        [Test]
        public void _010_RemotePath_NotConnected()
        {
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.RemotePath = "/ftproot/test";            
            Assert.That(client.RemotePath, Is.EqualTo("/ftproot/test"));
            client.Close();
        }

        /// <summary>
        /// Scenario: Change the RemotePath of an FTPClient once it is connected to an invalid value
        /// Expected: FTPException(550, xxx: The system cannot find the file specified)
        /// </summary>
        [Test]
        public void _011_RemotePath_Invalid()
        {
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.Connect();

            string path = client.RemotePath;
            string message = string.Empty;

            Assert.That(delegate { client.RemotePath = "/invalidpath/"; }, Throws.InstanceOf<FTPException>());
            Assert.That(client.RemotePath, Is.EqualTo(path), "Path changed - should not be");

            client.Close();
        }

        /// <summary>
        /// Scenario: Generate and upload a test file, resume disabled
        /// Expected: File uploaded successfully
        /// </summary>
        [Test]
        public void _012_UploadFile_Connected_NoResume()
        {
            string filename = string.Format("test{0}.txt", Guid.NewGuid());
            StreamWriter sw = File.CreateText(filename);
            sw.WriteLine("This is a test file");
            sw.Close();
            sw.Dispose();

            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.Connect();
            client.RemotePath = "/ftproot/test/";
            client.UploadFile(filename);
            client.Close();
        }

        // <summary>
        // Scenario: Generate and upload a test file, resume enabled but not required
        // Expected: File uploaded successfully
        // </summary>
        /*
        [Test]
        public void _013_UploadFile_Connected_Resume_NotRequired()
        {
            string filename = string.Format("test{0}.txt", Guid.NewGuid());
            StreamWriter sw = File.CreateText(filename);
            sw.WriteLine("This is a test file");
            sw.Close();
            sw.Dispose();

            FTPClient client = new FTPClient(_FTPHost, 21, "/ftproot", _FTPUser, _FTPPass, FTPTransferMode.Binary);
            client.Connect();
            client.RemotePath = "/ftproot/test";
            client.UploadFile(filename, true);
            client.Close();
        }*/

        /// <summary>
        /// Scenario: Generate and upload a test file, then delete it
        /// Expected: File uploaded and deleted
        /// </summary>
        [Test]
        public void _014_DeleteFile_Connected_ValidFile()
        {
            string filename = string.Format("test{0}.txt", Guid.NewGuid());
            StreamWriter sw = File.CreateText(filename);
            sw.WriteLine("This is a test file");
            sw.Close();
            sw.Dispose();

            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.Connect();
            client.RemotePath = "/ftproot/test";
            client.UploadFile(filename);
            client.DeleteRemoteFile(filename);
            client.Close();
        }

        /// <summary>
        /// Scenario: Generate and upload a test file, then delete it (not connected)
        /// Expected: File uploaded and deleted
        /// </summary>
        [Test]
        public void _015_DeleteFile_NotConnected_ValidFile()
        {
            string filename = string.Format("test{0}.txt", Guid.NewGuid());
            StreamWriter sw = File.CreateText(filename);
            sw.WriteLine("This is a test file");
            sw.Close();
            sw.Dispose();

            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.Connect();
            client.RemotePath = "/ftproot/test";
            client.UploadFile(filename);
            client.Close();

            client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.RemotePath = "/ftproot/test";
            client.DeleteRemoteFile(filename);
            client.Close();
        }

        /// <summary>
        /// Scenario: Attempt to delete a non-existant remote file
        /// Expected: FTPException(550, xxx: The system cannot find the file specified)
        /// </summary>
        [Test]
        public void _016_DeleteFile_Connected_InvalidFilename()
        {
            string filename = string.Format("test{0}.txt", Guid.NewGuid());
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.Connect();
            client.RemotePath = "/ftproot/test";
            Assert.That(delegate { client.DeleteRemoteFile(filename); }, Throws.InstanceOf<FTPException>());
            client.Close();
        }

        /// <summary>
        /// Scenario: Get/Set Port while not connected
        /// Expected: Value set is retrieved
        /// </summary>
        [Test]
        public void _017_Port_GetSet_NotConnected()
        {
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.Port = 100;
            Assert.That(client.Port, Is.EqualTo(100));
        }

        /// <summary>
        /// Scenario: Get/Set Port while connected
        /// Expected: FTPException(0, Cannot change Port while connected)
        /// </summary>
        [Test]
        public void _018_Port_GetSet_Connected()
        {
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.Connect();
            Assert.That(delegate { client.Port = 100; }, Throws.InstanceOf<FTPException>().With.Message.EqualTo("Cannot change Port while connected"));
            Assert.That(client.Port, Is.EqualTo(21));            
            client.Close();
        }

        /// <summary>
        /// Scenario: Get/Set RemoteHost while not connected
        /// Expected: Value set is retrieved
        /// </summary>
        [Test]
        public void _019_RemoteHost_GetSet_NotConnected()
        {
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.RemoteHost = "flibble.com";
            Assert.That(client.RemoteHost, Is.EqualTo("flibble.com"));
        }

        /// <summary>
        /// Scenario: Get/Set RemoteHost while connected
        /// Expected: FTPException(0, Cannot change RemoteHost while connected)
        /// </summary>
        [Test]
        public void _020_RemoteHost_GetSet_Connected()
        {
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.Connect();
            Assert.That(delegate { client.RemoteHost = "flibble.com"; }, Throws.InstanceOf<FTPException>().With.Message.EqualTo("Cannot change RemoteHost while connected"));
            Assert.That(client.RemoteHost, Is.EqualTo(_ftpHost));  
            client.Close();
        }

        /// <summary>
        /// Scenario: Get/Set Credentials while not connected
        /// Expected: Value set is retrieved
        /// </summary>
        [Test]
        public void _021_Credentials_GetSet_NotConnected()
        {
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            NetworkCredential credentials = new NetworkCredential("test", "test");
            client.Credentials = credentials;
            Assert.That(client.Credentials, Is.EqualTo(credentials));
        }

        /// <summary>
        /// Scenario: Get/Set Credentials while connected
        /// Expected: FTPException(0, Cannot change Credentials while connected)
        /// </summary>
        [Test]
        public void _022_Credentials_GetSet_Connected()
        {
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            NetworkCredential credentials = client.Credentials;
            client.Connect();
            Assert.That(delegate { client.Credentials = new NetworkCredential("test", "test"); }, Throws.InstanceOf<FTPException>().With.Message.EqualTo("Cannot change Credentials while connected"));            
            Assert.That(client.Credentials, Is.EqualTo(credentials));
            client.Close();
        }

        /// <summary>
        /// Scenario: Create and Delete a test folder
        /// Expected: No errors occur
        /// </summary>
        [Test]
        public void _023_MKDir_RMDir_Valid_Connected()
        {
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot/test", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.Connect();

            string dirname = string.Format("testdir{0}", Guid.NewGuid());
            client.MKDir(dirname);
            client.RMDir(dirname);

            client.Close();
        }

        /// <summary>
        /// Scenario: Create and Delete a test folder (not connected)
        /// Expected: No errors occur
        /// </summary>
        [Test]
        public void _024_MKDir_RMDir_Valid_NotConnected()
        {
            string dirname = string.Format("testdir{0}", Guid.NewGuid());

            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot/test", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.MKDir(dirname);
            client.Close();

            client = new FTPClient(_ftpHost, 21, "/ftproot/test", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.RMDir(dirname);
            client.Close();
        }

        /// <summary>
        /// Scenario: Attempt to create a directory within a non-existant path
        /// Expected: FTPException(550, xxx: The system cannot find the path specified.)
        /// </summary>
        [Test]
        public void _025_MKDir_InvalidName()
        {
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.Connect();
            Assert.That(delegate { client.MKDir("/flibble/flobble/"); }, Throws.InstanceOf<FTPException>());
            client.Close();
        }

        /// <summary>
        /// Scenario: Attempt to delete a directory within a non-existant path
        /// Expected: FTPException(550, xxx: The system cannot find the path specified.)
        /// </summary>
        [Test]
        public void _026_RMDir_InvalidName()
        {
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.Connect();
            Assert.That(delegate { client.RMDir("/flibble/flobble/"); }, Throws.InstanceOf<FTPException>());           
            client.Close();
        }

        /// <summary>
        /// Scenario: Upload a file, rename it (without connecting).
        /// Expected: Connects, renames file
        /// </summary>
        [Test]
        public void _027_RenameRemoteFile_NotConnected_Valid()
        {
            string filename = string.Format("test{0}.txt", Guid.NewGuid());
            string filename2 = string.Format("test{0}.txt", Guid.NewGuid());
            StreamWriter sw = File.CreateText(filename);
            sw.WriteLine("This is a test file");
            sw.Close();
            sw.Dispose();

            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.Connect();
            client.RemotePath = "/ftproot/test";
            client.UploadFile(filename);
            client.Close();

            client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.RemotePath = "/ftproot/test";
            client.RenameRemoteFile(filename, filename2);
            client.Close();
        }

        /// <summary>
        /// Scenario: Upload a file, rename it (already connected)
        /// Expected: Renames file
        /// </summary>
        [Test]
        public void _028_RenameRemoteFile_Connected_Valid()
        {
            string filename = string.Format("test{0}.txt", Guid.NewGuid());
            string filename2 = string.Format("test{0}.txt", Guid.NewGuid());
            StreamWriter sw = File.CreateText(filename);
            sw.WriteLine("This is a test file");
            sw.Close();
            sw.Dispose();

            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.Connect();
            client.RemotePath = "/ftproot/test";
            client.UploadFile(filename);
            client.RenameRemoteFile(filename, filename2);
            client.Close();
        }

        /// <summary>
        /// Scenario: Attempt to rename a file to an invalid name
        /// Expected: FTPException(550, xxx: The system cannot find the path specified.)
        /// </summary>
        [Test]
        public void _029_RenameRemoteFile_Connected_ValidFromOnly()
        {
            string filename = string.Format("test{0}.txt", Guid.NewGuid());                        
            StreamWriter sw = File.CreateText(filename);
            sw.WriteLine("This is a test file");
            sw.Close();
            sw.Dispose();

            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.Connect();
            client.RemotePath = "/ftproot/test";
            client.UploadFile(filename);
            Assert.That(delegate { client.RenameRemoteFile(filename, "/flibble/flobble.txt"); }, Throws.InstanceOf<FTPException>()); 
            client.Close();
        }

        /// <summary>
        /// Scenario: Attempt to rename a file from an invalid name
        /// Expected: FTPException(550, xxx: The system cannot find the path specified.)
        /// </summary>
        [Test]
        public void _030_RenameRemoteFile_Connected_ValidToOnly()
        {
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.Connect();
            client.RemotePath = "/ftproot/test";
            Assert.That(delegate { client.RenameRemoteFile("/flibble/flobble.txt", "/flibble/ftproot/test.txt"); }, Throws.InstanceOf<FTPException>()); 
            client.Close();
        }

        /// <summary>
        /// Scenario: Get Files (by mask) when not connected. None found.
        /// Expected: zero length array
        /// </summary>
        [Test]
        public void _031_GetFiles_NotConnected_ByMask_NoneFound()
        {
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            string[] files = client.GetFiles("*.txt");
            Assert.That(files.Length, Is.EqualTo(0));
        }

        /// <summary>
        /// Scenario: Get Files (by mask) when connected. None found.
        /// Expected: zero length array
        /// </summary>
        [Test]
        public void _032_GetFiles_Connected_ByMask_NoneFound()
        {
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.Connect();
            string[] files = client.GetFiles("*.txt");
            Assert.That(files.Length, Is.EqualTo(0));
        }

        /// <summary>
        /// Scenario: Get Files (by mask and path) when not connected. None found.k
        /// Expected: zero length array
        /// </summary>
        /// [Test]
        public void _034_GetFiles_NotConnected_ByMaskAndPath_NoneFound()
        {
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            string[] files = client.GetFiles("/ftproot", "*.txt");
            Assert.That(files.Length, Is.EqualTo(0));
        }

        /// <summary>
        /// Scenario: Get Files (by mask and path) when connected. None found.
        /// Expected: zero length array
        /// </summary>
        [Test]
        public void _035_GetFiles_Connected_ByMaskAndPath_NoneFound()
        {
            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot", _ftpUser, _ftpPass, FTPTransferMode.Binary);
            client.Connect();
            string[] files = client.GetFiles("/ftproot", "*.txt");
            Assert.That(files.Length, Is.EqualTo(0));
        }

        /// <summary>
        /// Scenario: Connect to a dummy, local server which does not return 220 (ready for user) immediately
        /// Expected: Client closes, exception thrown
        /// </summary>
        [Test]
        public void _036_Connect_NoServer220()
        {
            Thread testThread = new Thread(new ThreadStart(FtpServer_Return_421_AtLogin));
            testThread.Start();

            FTPClient client = new FTPClient("127.0.0.1", 3000, "/", "test", "test", FTPTransferMode.Binary);

            Assert.That(delegate { client.Connect(); }, Throws.InstanceOf<FTPException>().With.Property("ResponseCode").EqualTo(421));
        }

        /// <summary>
        /// Scenario: Connect to a dummy, local server which does not return appropriately on the USER command
        /// Expected: Client closes, exception thrown
        /// </summary>
        [Test]
        public void _037_Connect_ErrorCodeOnUSER()
        {
            Thread testThread = new Thread(new ThreadStart(FtpServer_Return_421_AtUSER));
            testThread.Start();

            FTPClient client = new FTPClient("127.0.0.1", 3000, "/", "test", "test", FTPTransferMode.Binary);
            
            Assert.That(delegate { client.Connect(); }, Throws.InstanceOf<FTPException>().With.Property("ResponseCode").EqualTo(421));
        }

        /// <summary>
        /// Scenario: Connect to a dummy, local server which does not return 227 to PASV
        /// Expected: Client closes, exception thrown
        /// </summary>
        [Test]
        public void _038_Connect_ErrorCodeOnPASV()
        {
            Thread testThread = new Thread(new ThreadStart(FtpServer_Return_421_AtPASV));
            testThread.Start();

            FTPClient client = new FTPClient("127.0.0.1", 3000, "/", "test", "test", FTPTransferMode.Binary);
            client.Connect();
            Assert.That(delegate { client.GetFiles("*"); }, Throws.InstanceOf<FTPException>().With.Property("ResponseCode").EqualTo(421));
        }

        /// <summary>
        /// Scenario: Handle bad PASV response (malformed IP)
        /// Expected: Client closes, exception thrown
        /// </summary>
        [Test]
        public void _039_Connect_MalformedPASVReply1()
        {
            Thread testThread = new Thread(new ThreadStart(FtpServer_Return_BadPASVResponse1));
            testThread.Start();

            FTPClient client = new FTPClient("127.0.0.1", 3000, "/", "test", "test", FTPTransferMode.Binary);
            client.Connect();
            Assert.That(delegate { client.GetFiles("*"); }, Throws.InstanceOf<FTPException>().With.Property("ResponseCode").EqualTo(227));
        }

        /// <summary>
        /// Scenario: PASV response is valid but data socket is not available
        /// Expected: Client closes, exception thrown
        /// </summary>
        [Test]
        public void _040_Connect_ValidPASV_UnableToConnectDataSocket()
        {
            Thread testThread = new Thread(new ThreadStart(FtpServer_Return_ValidPASVResponse));
            testThread.Start();

            FTPClient client = new FTPClient("127.0.0.1", 3000, "/", "test", "test", FTPTransferMode.Binary);
            client.Connect();
            Assert.That(delegate { client.GetFiles("*"); }, Throws.InstanceOf<FTPException>().With.Message.EqualTo("Unable to connect to remote server"));
        }

        /// <summary>
        /// Scenario: Connect to a dummy, local server which does not return 125/150 to NLST
        /// Expected: Client closes, exception thrown
        /// </summary>
        [Test]
        public void _041_GetFiles_ErrorCodeOnNLST()
        {
            Thread testThread = new Thread(new ThreadStart(FtpServer_Return_421_AtNLST));
            testThread.Start();

            FTPClient client = new FTPClient("127.0.0.1", 3000, "/", "test", "test", FTPTransferMode.Binary);
            client.Connect();
            Assert.That(delegate { client.GetFiles("*"); }, Throws.InstanceOf<FTPException>().With.Property("ResponseCode").EqualTo(421));
        }

        /// <summary>
        /// Scenario: Connect to a dummy, local server which does not return 226 at end of data from NLST
        /// Expected: Client closes, exception thrown
        /// </summary>
        [Test]
        public void _042_GetFiles_ErrorCodeAfterNLST()
        {
            Thread testThread = new Thread(new ThreadStart(FtpServer_Return_421_AfterNLST));
            testThread.Start();

            FTPClient client = new FTPClient("127.0.0.1", 3000, "/", "test", "test", FTPTransferMode.Binary);
            client.Connect();
            Assert.That(delegate { client.GetFiles("*"); }, Throws.InstanceOf<FTPException>().With.Property("ResponseCode").EqualTo(421));
        }

        /// <summary>
        /// Scenario: Create a test file, send it to the server, get the length back
        /// Expected: Matches local length of test file
        /// </summary>
        [Test]
        public void _043_GetFileSize_NotConnected()
        {
            FTPClient ftp = new FTPClient(_ftpHost, 21, "/ftproot/test", _ftpUser, _ftpPass, FTPTransferMode.Binary);

            // create test file
            string filename = string.Format("test{0}.txt", Guid.NewGuid());
            StreamWriter sw = File.CreateText(filename);
            sw.WriteLine("This is a test file");
            sw.Close();
            sw.Dispose();

            ftp.UploadFile(filename);

            ftp.Close();

            long sizeInBytes = ftp.GetFileSize(filename);

            FileInfo fi = new FileInfo(filename);
            Assert.That(sizeInBytes, Is.EqualTo(fi.Length));
        }

        /// <summary>
        /// Scenario: Connect to a dummy, local server which does not return 200 to TYPE
        /// Expected: Client closes, exception thrown
        /// </summary>
        [Test]
        public void _044_Connect_ErrorCodeOnTYPE()
        {
            Thread testThread = new Thread(new ThreadStart(FtpServer_Return_421_AtTYPE));
            testThread.Start();

            FTPClient client = new FTPClient("127.0.0.1", 3000, "/", "test", "test", FTPTransferMode.Binary);
            Assert.That(delegate { client.Connect(); }, Throws.InstanceOf<FTPException>().With.Property("ResponseCode").EqualTo(421));
        }

        /// <summary>
        /// Scenario: Connect to a dummy, local server which does not return 125/150 to NLST
        /// Expected: Client closes, exception thrown
        /// </summary>
        [Test]
        public void _045_UploadFile_ErrorCodeOnSTOR()
        {
            Thread testThread = new Thread(new ThreadStart(FtpServer_Return_421_AtSTOR));
            testThread.Start();

            // create test file
            string filename = string.Format("test{0}.txt", Guid.NewGuid());
            StreamWriter sw = File.CreateText(filename);
            sw.WriteLine("This is a test file");
            sw.Close();
            sw.Dispose(); 
            
            FTPClient client = new FTPClient("127.0.0.1", 3000, "/", "test", "test", FTPTransferMode.Binary);
            client.Connect();
            Assert.That(delegate { client.UploadFile(filename); }, Throws.InstanceOf<FTPException>().With.Property("ResponseCode").EqualTo(421));
        }

        /// <summary>
        /// Scenario: Connect to a dummy, local server which does not return 226 at end of data from NLST
        /// Expected: Client closes, exception thrown
        /// </summary>
        [Test]
        public void _046_UploadFile_ErrorCodeAfterSTOR()
        {
            Thread testThread = new Thread(new ThreadStart(FtpServer_Return_421_AfterSTOR));
            testThread.Start();

            // create test file
            string filename = string.Format("test{0}.txt", Guid.NewGuid());
            StreamWriter sw = File.CreateText(filename);
            sw.WriteLine("This is a test file");
            sw.Close();
            sw.Dispose(); 
            
            FTPClient client = new FTPClient("127.0.0.1", 3000, "/", "test", "test", FTPTransferMode.Binary);
            client.Connect();
            Assert.That(delegate { client.UploadFile(filename); }, Throws.InstanceOf<FTPException>().With.Property("ResponseCode").EqualTo(421));
        }

        /// <summary>
        /// Scenario: Create a file, upload it, rename it, download it
        /// Expected: Downloads correctly
        /// </summary>
        [Test]
        public void _047_DownloadFile_DoesNotExistLocally()
        {
            // create test file
            string filename = string.Format("test{0}.txt", Guid.NewGuid());
            StreamWriter sw = File.CreateText(filename);
            sw.WriteLine("This is a test file");
            sw.Close();
            sw.Dispose();

            string filename2 = string.Format("test{0}.txt", Guid.NewGuid());

            FTPClient client = new FTPClient(_ftpHost, 21, "/ftproot/test", _ftpUser, _ftpPass, FTPTransferMode.ASCII);
            client.Connect();
            client.UploadFile(filename);
            client.RenameRemoteFile(filename, filename2);

            client.Close();

            Assert.That(delegate { client.DownloadFile(filename2, string.Empty); }, Throws.InstanceOf<FTPException>().With.Message.EqualTo("LocalFileName is required"));
            client.DownloadFile(filename2, filename2);

            Assert.That(File.Exists(filename2));
        }

        /// <summary>
        /// Scenario: attempt to get file size, code returned is not 213
        /// Expected: Matches local length of test file
        /// </summary>
        [Test]
        public void _048_GetFileSize_ErrorCodeReturned()
        {
            Thread testThread = new Thread(new ThreadStart(FtpServer_Return_421_AtSIZE));
            testThread.Start();

            FTPClient client = new FTPClient("127.0.0.1", 3000, "/", "test", "test", FTPTransferMode.Binary);
            client.Connect();

            Assert.That(delegate { client.GetFileSize("testfile.txt"); }, Throws.InstanceOf<FTPException>().With.Property("ResponseCode").EqualTo(421));
        }

        /// <summary>
        /// Scenario: Connect to a dummy, local server which does not return 125/150 to NLST
        /// Expected: Client closes, exception thrown
        /// </summary>
        [Test]
        public void _049_DownloadFile_ErrorCodeOnRETR()
        {
            Thread testThread = new Thread(new ThreadStart(FtpServer_Return_421_AtRETR));
            testThread.Start();
            
            FTPClient client = new FTPClient("127.0.0.1", 3000, "/", "test", "test", FTPTransferMode.Binary);
            client.Connect();
            Assert.That(delegate { client.DownloadFile("test049", "test049"); }, Throws.InstanceOf<FTPException>().With.Property("ResponseCode").EqualTo(421));
        }

        /// <summary>
        /// Scenario: Connect to a dummy, local server which does not return 226 at end of data from NLST
        /// Expected: Client closes, exception thrown
        /// </summary>
        [Test]
        public void _050_Download_ErrorCodeAfterRETR()
        {
            Thread testThread = new Thread(new ThreadStart(FtpServer_Return_421_AfterRETR));
            testThread.Start();
            
            FTPClient client = new FTPClient("127.0.0.1", 3000, "/", "test", "test", FTPTransferMode.Binary);
            client.Connect();
            Assert.That(delegate { client.DownloadFile("test050", "test050"); }, Throws.InstanceOf<FTPException>().With.Property("ResponseCode").EqualTo(421));
        }

        private void FtpServer_Return_421_AtLogin()
        {
            _tcpListener = new TcpListener(IPAddress.Loopback, 3000);
            _tcpListener.Start();
            TcpClient client = _tcpListener.AcceptTcpClient();

            NetworkStream clientStream = client.GetStream();
            ASCIIEncoding encoder = new ASCIIEncoding();
            byte[] buffer = encoder.GetBytes("421 Server Shutting Down\n");

            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();
            clientStream.Close();
            client.Close();
            _tcpListener.Stop();
        }

        private void FtpServer_Return_421_AtUSER()
        {
            _tcpListener = new TcpListener(IPAddress.Loopback, 3000);
            _tcpListener.Start();
            TcpClient client = _tcpListener.AcceptTcpClient();

            NetworkStream clientStream = client.GetStream();
            ASCIIEncoding encoder = new ASCIIEncoding();

            byte[] buffer = encoder.GetBytes("220 OK Please Login\n");
            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();

            while (true)
            {
                byte[] message = new byte[4096];
                int bytesRead = 0;

                bytesRead = 0;
                bytesRead = clientStream.Read(message, 0, 4096);
                
                // message has successfully been received
                string messageString = encoder.GetString(message, 0, bytesRead);

                if (messageString.ToLower().StartsWith("user"))
                {
                    buffer = encoder.GetBytes("421 Server Shutting Down\n");
                    clientStream.Write(buffer, 0, buffer.Length);
                    clientStream.Flush();
                    break;
                }
            }

            clientStream.Close();
            client.Close();
            _tcpListener.Stop();
        }

        private void FtpServer_Return_421_AtPASV()
        {
            _tcpListener = new TcpListener(IPAddress.Loopback, 3000);
            _tcpListener.Start();
            TcpClient client = _tcpListener.AcceptTcpClient();

            NetworkStream clientStream = client.GetStream();
            ASCIIEncoding encoder = new ASCIIEncoding();

            byte[] buffer = encoder.GetBytes("220 OK Please Login\n");
            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();

            while (true)
            {
                byte[] message = new byte[4096];
                int bytesRead = 0;

                bytesRead = 0;
                bytesRead = clientStream.Read(message, 0, 4096);
                
                // message has successfully been received
                string messageString = encoder.GetString(message, 0, bytesRead);

                if (messageString.ToLower().StartsWith("pasv"))
                {
                    buffer = encoder.GetBytes("421 Server Shutting Down\n");
                    clientStream.Write(buffer, 0, buffer.Length);
                    clientStream.Flush();
                    break;
                }
                else
                {
                    if (messageString.ToLower().StartsWith("user"))
                    {
                        buffer = encoder.GetBytes("230 OK\n");
                        clientStream.Write(buffer, 0, buffer.Length);
                        clientStream.Flush();
                    }
                    else
                    {
                        if (messageString.ToLower().StartsWith("type"))
                        {
                            buffer = encoder.GetBytes("200 OK\n");
                            clientStream.Write(buffer, 0, buffer.Length);
                            clientStream.Flush();
                        }
                        else
                        {
                            if (messageString.ToLower().StartsWith("cwd "))
                            {
                                buffer = encoder.GetBytes("250 OK\n");
                                clientStream.Write(buffer, 0, buffer.Length);
                                clientStream.Flush();
                            }
                            else
                            {
                                buffer = encoder.GetBytes("220 OK\n");
                                clientStream.Write(buffer, 0, buffer.Length);
                                clientStream.Flush();
                            }
                        }
                    }
                }
            }

            clientStream.Close();
            client.Close();
            _tcpListener.Stop();
        }

        private void FtpServer_Return_BadPASVResponse1()
        {
            _tcpListener = new TcpListener(IPAddress.Loopback, 3000);
            _tcpListener.Start();
            TcpClient client = _tcpListener.AcceptTcpClient();

            NetworkStream clientStream = client.GetStream();
            ASCIIEncoding encoder = new ASCIIEncoding();

            byte[] buffer = encoder.GetBytes("220 OK Please Login\n");
            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();

            while (true)
            {
                byte[] message = new byte[4096];
                int bytesRead = 0;

                bytesRead = 0;

                bytesRead = clientStream.Read(message, 0, 4096);
                
                // message has successfully been received
                string messageString = encoder.GetString(message, 0, bytesRead);

                if (messageString.ToLower().StartsWith("pasv"))
                {
                    buffer = encoder.GetBytes("227 (192-168,0,1)\n");
                    clientStream.Write(buffer, 0, buffer.Length);
                    clientStream.Flush();
                    break;
                }
                else
                {
                    if (messageString.ToLower().StartsWith("user"))
                    {
                        buffer = encoder.GetBytes("230 OK\n");
                        clientStream.Write(buffer, 0, buffer.Length);
                        clientStream.Flush();
                    }
                    else
                    {
                        if (messageString.ToLower().StartsWith("type"))
                        {
                            buffer = encoder.GetBytes("200 OK\n");
                            clientStream.Write(buffer, 0, buffer.Length);
                            clientStream.Flush();
                        }
                        else
                        {
                            if (messageString.ToLower().StartsWith("cwd "))
                            {
                                buffer = encoder.GetBytes("250 OK\n");
                                clientStream.Write(buffer, 0, buffer.Length);
                                clientStream.Flush();
                            }
                            else
                            {
                                buffer = encoder.GetBytes("220 OK\n");
                                clientStream.Write(buffer, 0, buffer.Length);
                                clientStream.Flush();
                            }
                        }
                    }
                }
            }

            clientStream.Close();
            client.Close();
            _tcpListener.Stop();
        }

        private void FtpServer_Return_ValidPASVResponse()
        {
            _tcpListener = new TcpListener(IPAddress.Loopback, 3000);
            _tcpListener.Start();
            TcpClient client = _tcpListener.AcceptTcpClient();

            NetworkStream clientStream = client.GetStream();
            ASCIIEncoding encoder = new ASCIIEncoding();

            byte[] buffer = encoder.GetBytes("220 OK Please Login\n");
            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();

            while (true)
            {
                byte[] message = new byte[4096];
                int bytesRead = 0;

                bytesRead = 0;
                bytesRead = clientStream.Read(message, 0, 4096);
                
                // message has successfully been received
                string messageString = encoder.GetString(message, 0, bytesRead);

                if (messageString.ToLower().StartsWith("pasv"))
                {
                    buffer = encoder.GetBytes("227 (127,0,0,1,3,100)\n");
                    clientStream.Write(buffer, 0, buffer.Length);
                    clientStream.Flush();
                    break;
                }
                else
                {
                    if (messageString.ToLower().StartsWith("user"))
                    {
                        buffer = encoder.GetBytes("230 OK\n");
                        clientStream.Write(buffer, 0, buffer.Length);
                        clientStream.Flush();
                    }
                    else
                    {
                        if (messageString.ToLower().StartsWith("type"))
                        {
                            buffer = encoder.GetBytes("200 OK\n");
                            clientStream.Write(buffer, 0, buffer.Length);
                            clientStream.Flush();
                        }
                        else
                        {
                            if (messageString.ToLower().StartsWith("cwd "))
                            {
                                buffer = encoder.GetBytes("250 OK\n");
                                clientStream.Write(buffer, 0, buffer.Length);
                                clientStream.Flush();
                            }
                            else
                            {
                                buffer = encoder.GetBytes("220 OK\n");
                                clientStream.Write(buffer, 0, buffer.Length);
                                clientStream.Flush();
                            }
                        }
                    }
                }
            }

            clientStream.Close();
            client.Close();
            _tcpListener.Stop();
        }

        private void FtpServer_Return_421_AtNLST()
        {
            _tcpListener = new TcpListener(IPAddress.Loopback, 3000);
            _tcpListener.Start();
            TcpClient client = _tcpListener.AcceptTcpClient();

            NetworkStream clientStream = client.GetStream();
            ASCIIEncoding encoder = new ASCIIEncoding();

            byte[] buffer = encoder.GetBytes("220 OK Please Login\n");
            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();

            while (true)
            {
                byte[] message = new byte[4096];
                int bytesRead = 0;

                bytesRead = 0;
                bytesRead = clientStream.Read(message, 0, 4096);
                
                // message has successfully been received
                string messageString = encoder.GetString(message, 0, bytesRead);

                if (messageString.ToLower().StartsWith("nlst"))
                {
                    buffer = encoder.GetBytes("421 Server Shutting Down\n");
                    clientStream.Write(buffer, 0, buffer.Length);
                    clientStream.Flush();
                    break;
                }
                else
                {
                    if (messageString.ToLower().StartsWith("pasv"))
                    {
                        _tcpListenerData = new TcpListener(IPAddress.Loopback, 5220);
                        _tcpListenerData.Start();
                        buffer = encoder.GetBytes("227 (127,0,0,1,20,100)\n"); // port 5220 ((20*256) +100)
                        clientStream.Write(buffer, 0, buffer.Length);
                        clientStream.Flush();
                    }
                    else
                    {
                        if (messageString.ToLower().StartsWith("user"))
                        {
                            buffer = encoder.GetBytes("230 OK\n");
                            clientStream.Write(buffer, 0, buffer.Length);
                            clientStream.Flush();
                        }
                        else
                        {
                            if (messageString.ToLower().StartsWith("type"))
                            {
                                buffer = encoder.GetBytes("200 OK\n");
                                clientStream.Write(buffer, 0, buffer.Length);
                                clientStream.Flush();
                            }
                            else
                            {
                                if (messageString.ToLower().StartsWith("cwd "))
                                {
                                    buffer = encoder.GetBytes("250 OK\n");
                                    clientStream.Write(buffer, 0, buffer.Length);
                                    clientStream.Flush();
                                }
                                else
                                {
                                    buffer = encoder.GetBytes("220 OK\n");
                                    clientStream.Write(buffer, 0, buffer.Length);
                                    clientStream.Flush();
                                }
                            }
                        }
                    }
                }
            }

            clientStream.Close();
            client.Close();
            _tcpListener.Stop();

            if (_tcpListenerData != null)
            {
                _tcpListenerData.Stop();
            }
        }

        private void FtpServer_Return_421_AfterNLST()
        {
            _tcpListener = new TcpListener(IPAddress.Loopback, 3000);
            _tcpListener.Start();
            TcpClient client = _tcpListener.AcceptTcpClient();

            NetworkStream clientStream = client.GetStream();
            ASCIIEncoding encoder = new ASCIIEncoding();

            byte[] buffer = encoder.GetBytes("220 OK Please Login\n");
            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();

            while (true)
            {
                byte[] message = new byte[4096];
                int bytesRead = 0;

                bytesRead = 0;
                bytesRead = clientStream.Read(message, 0, 4096);
                
                // message has successfully been received
                string messageString = encoder.GetString(message, 0, bytesRead);

                if (messageString.ToLower().StartsWith("nlst"))
                {
                    buffer = encoder.GetBytes("150 Sending data\n");
                    clientStream.Write(buffer, 0, buffer.Length);
                    clientStream.Flush();

                    TcpClient client2 = _tcpListenerData.AcceptTcpClient();
                    NetworkStream clientStream2 = client2.GetStream();
                    
                    // buffer = encoder.GetBytes("random data\n");
                    // clientStream.Write(buffer, 0, buffer.Length);
                    clientStream2.Flush();
                    clientStream2.Close();
                    client2.Close();
                    _tcpListenerData.Stop();

                    buffer = encoder.GetBytes("421 Shutting down\n");
                    clientStream.Write(buffer, 0, buffer.Length);
                    clientStream.Flush();
                    break;
                }
                else
                {
                    if (messageString.ToLower().StartsWith("pasv"))
                    {
                        _tcpListenerData = new TcpListener(IPAddress.Loopback, 5220);
                        _tcpListenerData.Start();
                        buffer = encoder.GetBytes("227 (127,0,0,1,20,100)\n"); // port 5220 ((20*256) +100)
                        clientStream.Write(buffer, 0, buffer.Length);
                        clientStream.Flush();
                    }
                    else
                    {
                        if (messageString.ToLower().StartsWith("user"))
                        {
                            buffer = encoder.GetBytes("230 OK\n");
                            clientStream.Write(buffer, 0, buffer.Length);
                            clientStream.Flush();
                        }
                        else
                        {
                            if (messageString.ToLower().StartsWith("type"))
                            {
                                buffer = encoder.GetBytes("200 OK\n");
                                clientStream.Write(buffer, 0, buffer.Length);
                                clientStream.Flush();
                            }
                            else
                            {
                                if (messageString.ToLower().StartsWith("cwd "))
                                {
                                    buffer = encoder.GetBytes("250 OK\n");
                                    clientStream.Write(buffer, 0, buffer.Length);
                                    clientStream.Flush();
                                }
                                else
                                {
                                    buffer = encoder.GetBytes("220 OK\n");
                                    clientStream.Write(buffer, 0, buffer.Length);
                                    clientStream.Flush();
                                }
                            }
                        }
                    }
                }
            }

            clientStream.Close();
            client.Close();
            _tcpListener.Stop();
        }

        private void FtpServer_Return_421_AtTYPE()
        {
            _tcpListener = new TcpListener(IPAddress.Loopback, 3000);
            _tcpListener.Start();
            TcpClient client = _tcpListener.AcceptTcpClient();

            NetworkStream clientStream = client.GetStream();
            ASCIIEncoding encoder = new ASCIIEncoding();

            byte[] buffer = encoder.GetBytes("220 OK Please Login\n");
            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();

            while (true)
            {
                byte[] message = new byte[4096];
                int bytesRead = 0;

                bytesRead = 0;
                bytesRead = clientStream.Read(message, 0, 4096);
                
                // message has successfully been received
                string messageString = encoder.GetString(message, 0, bytesRead);

                if (messageString.ToLower().StartsWith("type"))
                {
                    buffer = encoder.GetBytes("421 Server Shutting Down\n");
                    clientStream.Write(buffer, 0, buffer.Length);
                    clientStream.Flush();
                    break;
                }
                else
                {
                    if (messageString.ToLower().StartsWith("user"))
                    {
                        buffer = encoder.GetBytes("230 OK\n");
                        clientStream.Write(buffer, 0, buffer.Length);
                        clientStream.Flush();
                    }
                    else
                    {
                        // if (messageString.ToLower().StartsWith("type"))
                        // {
                        //    buffer = encoder.GetBytes("200 OK\n");
                        //    clientStream.Write(buffer, 0, buffer.Length);
                        //    clientStream.Flush();
                        // }
                        // else
                        // {
                        if (messageString.ToLower().StartsWith("cwd "))
                        {
                            buffer = encoder.GetBytes("250 OK\n");
                            clientStream.Write(buffer, 0, buffer.Length);
                            clientStream.Flush();
                        }
                        else
                        {
                            buffer = encoder.GetBytes("220 OK\n");
                            clientStream.Write(buffer, 0, buffer.Length);
                            clientStream.Flush();
                        }

                        // }
                    }
                }
            }

            clientStream.Close();
            client.Close();
            _tcpListener.Stop();
        }

        private void FtpServer_Return_421_AtSTOR()
        {
            _tcpListener = new TcpListener(IPAddress.Loopback, 3000);
            _tcpListener.Start();
            TcpClient client = _tcpListener.AcceptTcpClient();

            NetworkStream clientStream = client.GetStream();
            ASCIIEncoding encoder = new ASCIIEncoding();

            byte[] buffer = encoder.GetBytes("220 OK Please Login\n");
            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();

            while (true)
            {
                byte[] message = new byte[4096];
                int bytesRead = 0;

                bytesRead = 0;
                bytesRead = clientStream.Read(message, 0, 4096);
                
                // message has successfully been received
                string messageString = encoder.GetString(message, 0, bytesRead);

                if (messageString.ToLower().StartsWith("stor"))
                {
                    buffer = encoder.GetBytes("421 Server Shutting Down\n");
                    clientStream.Write(buffer, 0, buffer.Length);
                    clientStream.Flush();
                    break;
                }
                else
                {
                    if (messageString.ToLower().StartsWith("pasv"))
                    {
                        _tcpListenerData = new TcpListener(IPAddress.Loopback, 5220);
                        _tcpListenerData.Start();
                        buffer = encoder.GetBytes("227 (127,0,0,1,20,100)\n"); // port 5220 ((20*256) +100)
                        clientStream.Write(buffer, 0, buffer.Length);
                        clientStream.Flush();
                    }
                    else
                    {
                        if (messageString.ToLower().StartsWith("user"))
                        {
                            buffer = encoder.GetBytes("230 OK\n");
                            clientStream.Write(buffer, 0, buffer.Length);
                            clientStream.Flush();
                        }
                        else
                        {
                            if (messageString.ToLower().StartsWith("type"))
                            {
                                buffer = encoder.GetBytes("200 OK\n");
                                clientStream.Write(buffer, 0, buffer.Length);
                                clientStream.Flush();
                            }
                            else
                            {
                                if (messageString.ToLower().StartsWith("cwd "))
                                {
                                    buffer = encoder.GetBytes("250 OK\n");
                                    clientStream.Write(buffer, 0, buffer.Length);
                                    clientStream.Flush();
                                }
                                else
                                {
                                    buffer = encoder.GetBytes("220 OK\n");
                                    clientStream.Write(buffer, 0, buffer.Length);
                                    clientStream.Flush();
                                }
                            }
                        }
                    }
                }
            }

            clientStream.Close();
            client.Close();
            _tcpListener.Stop();

            if (_tcpListenerData != null)
            {
                _tcpListenerData.Stop();
            }
        }

        private void FtpServer_Return_421_AfterSTOR()
        {
            _tcpListener = new TcpListener(IPAddress.Loopback, 3000);
            _tcpListener.Start();
            TcpClient client = _tcpListener.AcceptTcpClient();

            NetworkStream clientStream = client.GetStream();
            ASCIIEncoding encoder = new ASCIIEncoding();

            byte[] buffer = encoder.GetBytes("220 OK Please Login\n");
            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();

            while (true)
            {
                byte[] message = new byte[4096];
                int bytesRead = 0;

                bytesRead = 0;
                bytesRead = clientStream.Read(message, 0, 4096);
                
                // message has successfully been received
                string messageString = encoder.GetString(message, 0, bytesRead);

                if (messageString.ToLower().StartsWith("stor"))
                {
                    buffer = encoder.GetBytes("150 receiving data\n");
                    clientStream.Write(buffer, 0, buffer.Length);
                    clientStream.Flush();

                    TcpClient client2 = _tcpListenerData.AcceptTcpClient();
                    NetworkStream clientStream2 = client2.GetStream();
                    
                    // buffer = encoder.GetBytes("random data\n");
                    // clientStream.Write(buffer, 0, buffer.Length);
                    clientStream2.Flush();
                    clientStream2.Close();
                    client2.Close();
                    _tcpListenerData.Stop();

                    buffer = encoder.GetBytes("421 Shutting down\n");
                    clientStream.Write(buffer, 0, buffer.Length);
                    clientStream.Flush();
                    break;
                }
                else
                {
                    if (messageString.ToLower().StartsWith("pasv"))
                    {
                        _tcpListenerData = new TcpListener(IPAddress.Loopback, 5220);
                        _tcpListenerData.Start();
                        buffer = encoder.GetBytes("227 (127,0,0,1,20,100)\n"); // port 5220 ((20*256) +100)
                        clientStream.Write(buffer, 0, buffer.Length);
                        clientStream.Flush();
                    }
                    else
                    {
                        if (messageString.ToLower().StartsWith("user"))
                        {
                            buffer = encoder.GetBytes("230 OK\n");
                            clientStream.Write(buffer, 0, buffer.Length);
                            clientStream.Flush();
                        }
                        else
                        {
                            if (messageString.ToLower().StartsWith("type"))
                            {
                                buffer = encoder.GetBytes("200 OK\n");
                                clientStream.Write(buffer, 0, buffer.Length);
                                clientStream.Flush();
                            }
                            else
                            {
                                if (messageString.ToLower().StartsWith("cwd "))
                                {
                                    buffer = encoder.GetBytes("250 OK\n");
                                    clientStream.Write(buffer, 0, buffer.Length);
                                    clientStream.Flush();
                                }
                                else
                                {
                                    buffer = encoder.GetBytes("220 OK\n");
                                    clientStream.Write(buffer, 0, buffer.Length);
                                    clientStream.Flush();
                                }
                            }
                        }
                    }
                }
            }

            clientStream.Close();
            client.Close();
            _tcpListener.Stop();
        }

        private void FtpServer_Return_421_AtSIZE()
        {
            _tcpListener = new TcpListener(IPAddress.Loopback, 3000);
            _tcpListener.Start();
            TcpClient client = _tcpListener.AcceptTcpClient();

            NetworkStream clientStream = client.GetStream();
            ASCIIEncoding encoder = new ASCIIEncoding();

            byte[] buffer = encoder.GetBytes("220 OK Please Login\n");
            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();

            while (true)
            {
                byte[] message = new byte[4096];
                int bytesRead = 0;

                bytesRead = 0;
                bytesRead = clientStream.Read(message, 0, 4096);
                
                // message has successfully been received
                string messageString = encoder.GetString(message, 0, bytesRead);

                if (messageString.ToLower().StartsWith("size"))
                {
                    buffer = encoder.GetBytes("421 Server Shutting Down\n");
                    clientStream.Write(buffer, 0, buffer.Length);
                    clientStream.Flush();
                    break;
                }
                else
                {
                    if (messageString.ToLower().StartsWith("pasv"))
                    {
                        _tcpListenerData = new TcpListener(IPAddress.Loopback, 5220);
                        _tcpListenerData.Start();
                        buffer = encoder.GetBytes("227 (127,0,0,1,20,100)\n"); // port 5220 ((20*256) +100)
                        clientStream.Write(buffer, 0, buffer.Length);
                        clientStream.Flush();
                    }
                    else
                    {
                        if (messageString.ToLower().StartsWith("user"))
                        {
                            buffer = encoder.GetBytes("230 OK\n");
                            clientStream.Write(buffer, 0, buffer.Length);
                            clientStream.Flush();
                        }
                        else
                        {
                            if (messageString.ToLower().StartsWith("type"))
                            {
                                buffer = encoder.GetBytes("200 OK\n");
                                clientStream.Write(buffer, 0, buffer.Length);
                                clientStream.Flush();
                            }
                            else
                            {
                                if (messageString.ToLower().StartsWith("cwd "))
                                {
                                    buffer = encoder.GetBytes("250 OK\n");
                                    clientStream.Write(buffer, 0, buffer.Length);
                                    clientStream.Flush();
                                }
                                else
                                {
                                    buffer = encoder.GetBytes("220 OK\n");
                                    clientStream.Write(buffer, 0, buffer.Length);
                                    clientStream.Flush();
                                }
                            }
                        }
                    }
                }
            }

            clientStream.Close();
            client.Close();
            _tcpListener.Stop();

            if (_tcpListenerData != null)
            {
                _tcpListenerData.Stop();
            }
        }

        private void FtpServer_Return_421_AtRETR()
        {
            _tcpListener = new TcpListener(IPAddress.Loopback, 3000);
            _tcpListener.Start();
            TcpClient client = _tcpListener.AcceptTcpClient();

            NetworkStream clientStream = client.GetStream();
            ASCIIEncoding encoder = new ASCIIEncoding();

            byte[] buffer = encoder.GetBytes("220 OK Please Login\n");
            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();

            while (true)
            {
                byte[] message = new byte[4096];
                int bytesRead = 0;

                bytesRead = 0;
                bytesRead = clientStream.Read(message, 0, 4096);
                
                // message has successfully been received
                string messageString = encoder.GetString(message, 0, bytesRead);

                if (messageString.ToLower().StartsWith("retr"))
                {
                    buffer = encoder.GetBytes("421 Server Shutting Down\n");
                    clientStream.Write(buffer, 0, buffer.Length);
                    clientStream.Flush();
                    break;
                }
                else
                {
                    if (messageString.ToLower().StartsWith("pasv"))
                    {
                        _tcpListenerData = new TcpListener(IPAddress.Loopback, 5220);
                        _tcpListenerData.Start();
                        buffer = encoder.GetBytes("227 (127,0,0,1,20,100)\n"); // port 5220 ((20*256) +100)
                        clientStream.Write(buffer, 0, buffer.Length);
                        clientStream.Flush();
                    }
                    else
                    {
                        if (messageString.ToLower().StartsWith("user"))
                        {
                            buffer = encoder.GetBytes("230 OK\n");
                            clientStream.Write(buffer, 0, buffer.Length);
                            clientStream.Flush();
                        }
                        else
                        {
                            if (messageString.ToLower().StartsWith("type"))
                            {
                                buffer = encoder.GetBytes("200 OK\n");
                                clientStream.Write(buffer, 0, buffer.Length);
                                clientStream.Flush();
                            }
                            else
                            {
                                if (messageString.ToLower().StartsWith("cwd "))
                                {
                                    buffer = encoder.GetBytes("250 OK\n");
                                    clientStream.Write(buffer, 0, buffer.Length);
                                    clientStream.Flush();
                                }
                                else
                                {
                                    buffer = encoder.GetBytes("220 OK\n");
                                    clientStream.Write(buffer, 0, buffer.Length);
                                    clientStream.Flush();
                                }
                            }
                        }
                    }
                }
            }

            clientStream.Close();
            client.Close();
            _tcpListener.Stop();

            if (_tcpListenerData != null)
            {
                _tcpListenerData.Stop();
            }
        }

        private void FtpServer_Return_421_AfterRETR()
        {
            _tcpListener = new TcpListener(IPAddress.Loopback, 3000);
            _tcpListener.Start();
            TcpClient client = _tcpListener.AcceptTcpClient();

            NetworkStream clientStream = client.GetStream();
            ASCIIEncoding encoder = new ASCIIEncoding();

            byte[] buffer = encoder.GetBytes("220 OK Please Login\n");
            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();

            while (true)
            {
                byte[] message = new byte[4096];
                int bytesRead = 0;

                bytesRead = 0;
                bytesRead = clientStream.Read(message, 0, 4096);
                
                // message has successfully been received
                string messageString = encoder.GetString(message, 0, bytesRead);

                if (messageString.ToLower().StartsWith("retr"))
                {
                    buffer = encoder.GetBytes("150 receiving data\n");
                    clientStream.Write(buffer, 0, buffer.Length);
                    clientStream.Flush();

                    TcpClient client2 = _tcpListenerData.AcceptTcpClient();
                    NetworkStream clientStream2 = client2.GetStream();
                    
                    // buffer = encoder.GetBytes("random data\n");
                    // clientStream.Write(buffer, 0, buffer.Length);
                    clientStream2.Flush();
                    clientStream2.Close();
                    client2.Close();
                    _tcpListenerData.Stop();

                    buffer = encoder.GetBytes("421 Shutting down\n");
                    clientStream.Write(buffer, 0, buffer.Length);
                    clientStream.Flush();
                    break;
                }
                else
                {
                    if (messageString.ToLower().StartsWith("pasv"))
                    {
                        _tcpListenerData = new TcpListener(IPAddress.Loopback, 5220);
                        _tcpListenerData.Start();
                        buffer = encoder.GetBytes("227 (127,0,0,1,20,100)\n"); // port 5220 ((20*256) +100)
                        clientStream.Write(buffer, 0, buffer.Length);
                        clientStream.Flush();
                    }
                    else
                    {
                        if (messageString.ToLower().StartsWith("user"))
                        {
                            buffer = encoder.GetBytes("230 OK\n");
                            clientStream.Write(buffer, 0, buffer.Length);
                            clientStream.Flush();
                        }
                        else
                        {
                            if (messageString.ToLower().StartsWith("type"))
                            {
                                buffer = encoder.GetBytes("200 OK\n");
                                clientStream.Write(buffer, 0, buffer.Length);
                                clientStream.Flush();
                            }
                            else
                            {
                                if (messageString.ToLower().StartsWith("cwd "))
                                {
                                    buffer = encoder.GetBytes("250 OK\n");
                                    clientStream.Write(buffer, 0, buffer.Length);
                                    clientStream.Flush();
                                }
                                else
                                {
                                    buffer = encoder.GetBytes("220 OK\n");
                                    clientStream.Write(buffer, 0, buffer.Length);
                                    clientStream.Flush();
                                }
                            }
                        }
                    }
                }
            }

            clientStream.Close();
            client.Close();
            _tcpListener.Stop();
        }        
    }
}
