using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Web;
using Codentia.Common.Helper;
using Codentia.Common.Logging;
using Codentia.Common.Logging.BL;
using Codentia.Common.Net.FTP;

namespace Codentia.Common.Net
{
    /// <summary>
    /// Management class for interfacing with Mattched IT CDN system
    /// </summary>
    public static class CDNManager
    {
        /// <summary>
        /// Puts the file. Requires CDN credentials to be stored in app.config
        /// </summary>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="domainName">Name of the domain.</param>
        /// <param name="folder">The folder.</param>
        /// <param name="fileNameAndPath">The file name and path.</param>
        public static void PutFile(string applicationName, string domainName, string folder, string fileNameAndPath)
        {
            ParameterCheckHelper.CheckIsValidString(applicationName, "applicationName", false);
            ParameterCheckHelper.CheckIsValidString(domainName, "domainName", false);
            ParameterCheckHelper.CheckIsValidString(folder, "folder", false);
            ParameterCheckHelper.CheckIsValidString(fileNameAndPath, "fileNameAndPath", false);

            if (!File.Exists(fileNameAndPath))
            {
                throw new Exception(string.Format("file: '{0}' does not exist", fileNameAndPath));
            }

            // ftp to cdn and write the file
            FTPClient ftp = new FTPClient(ConfigurationManager.AppSettings["CDNFtpHost"], 21, "/", ConfigurationManager.AppSettings["CDNFtpUser"], ConfigurationManager.AppSettings["CDNFtpPassword"], FTPTransferMode.Binary);
            ftp.RemotePath = string.Format("/{0}/{1}/{2}/{3}/{4}", ConfigurationManager.AppSettings["CDNFtpRemotePath"], applicationName, SiteHelper.SiteEnvironment, domainName, folder);
            ftp.UploadFile(fileNameAndPath);
            ftp.Close();
        }

        /// <summary>
        /// Upload the contents of a folder (flat - no recursion) to CDN using app.config credentials
        /// </summary>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="domainName">Name of the domain.</param>
        /// <param name="folder">The folder.</param>
        /// <param name="folderPath">The folder path.</param>
        public static void PutFiles(string applicationName, string domainName, string folder, string folderPath)
        {
            ParameterCheckHelper.CheckIsValidString(applicationName, "applicationName", false);
            ParameterCheckHelper.CheckIsValidString(domainName, "domainName", false);
            ParameterCheckHelper.CheckIsValidString(folder, "folder", false);
            ParameterCheckHelper.CheckIsValidString(folderPath, "local folder", false);

            // ftp to cdn and store all files in the folder
            FTPClient ftp = new FTPClient(ConfigurationManager.AppSettings["CDNFtpHost"], 21, "/", ConfigurationManager.AppSettings["CDNFtpUser"], ConfigurationManager.AppSettings["CDNFtpPassword"], FTPTransferMode.Binary);
            ftp.RemotePath = string.Format("/{0}/{1}/{2}/{3}/{4}", ConfigurationManager.AppSettings["CDNFtpRemotePath"], applicationName, SiteHelper.SiteEnvironment, domainName, folder);

            string[] files = Directory.GetFiles(folderPath);
            for (int i = 0; i < files.Length; i++)
            {
                ftp.UploadFile(files[i]);
            }

            ftp.Close();
        }

        /// <summary>
        /// Ensures the path exists.
        /// </summary>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="domainName">Name of the domain.</param>
        /// <param name="folder">The folder.</param>
        public static void EnsurePathExists(string applicationName, string domainName, string folder)
        {
            ParameterCheckHelper.CheckIsValidString(applicationName, "applicationName", false);
            ParameterCheckHelper.CheckIsValidString(domainName, "domainName", false);
            ParameterCheckHelper.CheckIsValidString(folder, "folder", false);

            // ftp to cdn and write the file
            FTPClient ftp = new FTPClient(ConfigurationManager.AppSettings["CDNFtpHost"], 21, "/", ConfigurationManager.AppSettings["CDNFtpUser"], ConfigurationManager.AppSettings["CDNFtpPassword"], FTPTransferMode.Binary);

            // go to base path (must exist)
            ftp.RemotePath = string.Format("/{0}/{1}/{2}", ConfigurationManager.AppSettings["CDNFtpRemotePath"], applicationName, SiteHelper.SiteEnvironment);
            ftp.Connect();

            // test for client folders and create if necessary
            try
            {
                ftp.RemotePath = domainName;
            }
            catch (Exception)
            {
                LogManager.Instance.AddToLog(LogMessageType.Information, "CDNManager.EnsurePathExists", string.Format("domainName {0} does not exist in {1} - creating..", domainName, ftp.RemotePath));
                ftp.MKDir(domainName);
                ftp.RemotePath = domainName;
            }

            try
            {
                LogManager.Instance.AddToLog(LogMessageType.Information, "CDNManager.EnsurePathExists", string.Format("folder {0} does not exist in {1} - creating..", folder, ftp.RemotePath));
                ftp.RemotePath = folder;
            }
            catch (Exception)
            {
                ftp.MKDir(folder);
            }

            ftp.Close();
        }

        /// <summary>
        /// Gets the public URL.
        /// </summary>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="domainName">Name of the domain.</param>
        /// <param name="folder">The folder.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>string (url)</returns>
        public static string GetPublicUrl(string applicationName, string domainName, string folder, string fileName)
        {
            return string.Format("http://cdn.mattchedit.com/{0}/{1}/{2}/{3}/{4}", applicationName, SiteHelper.SiteEnvironment, domainName, folder, fileName);
        }

        /// <summary>
        /// Writes the file for download as if it were a local resource (handling of images in secure context, etc)
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="domainName">Name of the domain.</param>
        /// <param name="folder">The folder.</param>
        /// <param name="fileName">Name of the file.</param>
        public static void WriteImage(HttpResponse response, string applicationName, string domainName, string folder, string fileName)
        {
            // get the file via http and output it with the appropriate mime type based on it's extension
            // top table here http://www.w3schools.com/media/media_mimeref.asp (must set content type appropriately)
            byte[] imageContent = null;
            string sourceUrl = CDNManager.GetPublicUrl(applicationName, domainName, folder, fileName);

            try
            {
                WebRequest requestPic = WebRequest.Create(sourceUrl);
                WebResponse responsePic = requestPic.GetResponse();

                // create an image object, using the filename we just retrieved
                System.Drawing.Image image = System.Drawing.Image.FromStream(responsePic.GetResponseStream());

                // make a memory stream to work with the image bytes
                MemoryStream imageStream = new MemoryStream();
                image.Save(imageStream, System.Drawing.Imaging.ImageFormat.Jpeg);

                // make byte array the same size as the image
                imageContent = new byte[imageStream.Length];

                // rewind the memory stream
                imageStream.Position = 0;

                // load the byte array with the image
                imageStream.Read(imageContent, 0, (int)imageStream.Length);

                // return byte array to caller with image type
                response.ContentType = "image/jpeg";
            }
            catch (Exception ex)
            {
                LogManager.Instance.AddToLog(LogMessageType.FatalError, "CDNManager.WriteImage", string.Format("applicationName={0}, domainName={1}, folder={2}, fileName={3}, sourceUrl={4}", applicationName, domainName, folder, fileName, sourceUrl));
                LogManager.Instance.AddToLog(ex, "CDNManager.WriteImage");
            }

            if (imageContent != null)
            {
                try
                {
                    response.BinaryWrite(imageContent);
                }
                catch (Exception)
                {
                    // do nothing, this is for test purposes
                }
            }

            response.End();
        }

        /// <summary>
        /// Gets the binary file contents.
        /// </summary>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="domainName">Name of the domain.</param>
        /// <param name="folder">The folder.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>byte array</returns>
        public static byte[] GetBinaryFileContents(string applicationName, string domainName, string folder, string fileName)
        {
            List<byte> fileContents = new List<byte>();
            string sourceUrl = CDNManager.GetPublicUrl(applicationName, domainName, folder, fileName);

            try
            {
                WebRequest requestPic = WebRequest.Create(sourceUrl);
                WebResponse responsePic = requestPic.GetResponse();

                // create an image object, using the filename we just retrieved

                // 50kB chunking
                byte[] buffer = new byte[51200];
                int index = 0;
                BinaryReader reader = new BinaryReader(responsePic.GetResponseStream());

                int bytesRead = 0;
                while ((bytesRead = reader.Read(buffer, index, buffer.Length)) > 0)
                {
                    byte[] actuallyRead = new byte[bytesRead];
                    Array.Copy(buffer, 0, actuallyRead, 0, bytesRead);

                    fileContents.AddRange(actuallyRead);
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.AddToLog(LogMessageType.NonFatalError, "CDNManager.GetBinaryFileContents", string.Format("applicationName={0}, domainName={1}, folder={2}, fileName={3}, sourceUrl={4}", applicationName, domainName, folder, fileName, sourceUrl));
                LogManager.Instance.AddToLog(ex, "CDNManager.GetBinaryFileContents");
            }

            return fileContents.ToArray();
        }

        /// <summary>
        /// Writes the file.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="domainName">Name of the domain.</param>
        /// <param name="folder">The folder.</param>
        /// <param name="fileName">Name of the file.</param>
        public static void WriteBinaryFile(HttpResponse response, string applicationName, string domainName, string folder, string fileName)
        {
            byte[] contents = GetBinaryFileContents(applicationName, domainName, folder, fileName);
            string sourceUrl = CDNManager.GetPublicUrl(applicationName, domainName, folder, fileName);

            try
            {
                response.ContentType = "application/octet-stream";
                response.AddHeader("content-disposition", string.Format("attachment; filename=\"{0}\"", fileName));
                response.BinaryWrite(contents);
            }
            catch (Exception ex)
            {
                LogManager.Instance.AddToLog(LogMessageType.NonFatalError, "CDNManager.WriteBinaryFile", string.Format("applicationName={0}, domainName={1}, folder={2}, fileName={3}, sourceUrl={4}", applicationName, domainName, folder, fileName, sourceUrl));
                LogManager.Instance.AddToLog(ex, "CDNManager.WriteBinaryFile");
            }

            response.End();
        }

        /// <summary>
        /// Deletes the file.
        /// </summary>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="domainName">Name of the domain.</param>
        /// <param name="folder">The folder.</param>
        /// <param name="fileName">Name of the file.</param>
        public static void DeleteFile(string applicationName, string domainName, string folder, string fileName)
        {
            ParameterCheckHelper.CheckIsValidString(applicationName, "applicationName", false);
            ParameterCheckHelper.CheckIsValidString(domainName, "domainName", false);
            ParameterCheckHelper.CheckIsValidString(folder, "folder", false);
            ParameterCheckHelper.CheckIsValidString(fileName, "fileName", false);

            // ftp to cdn and write the file
            FTPClient ftp = new FTPClient(ConfigurationManager.AppSettings["CDNFtpHost"], 21, "/", ConfigurationManager.AppSettings["CDNFtpUser"], ConfigurationManager.AppSettings["CDNFtpPassword"], FTPTransferMode.Binary);
            ftp.RemotePath = string.Format("/{0}/{1}/{2}/{3}/{4}", ConfigurationManager.AppSettings["CDNFtpRemotePath"], applicationName, SiteHelper.SiteEnvironment, domainName, folder);
            ftp.DeleteRemoteFile(fileName);
            ftp.Close();
        }

        /// <summary>
        /// Files the exists.
        /// </summary>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="domainName">Name of the domain.</param>
        /// <param name="folder">The folder.</param>
        /// <param name="fileNameAndPath">The file name and path.</param>
        /// <returns>bool - true if file exists, otherwise false</returns>
        public static bool FileExists(string applicationName, string domainName, string folder, string fileNameAndPath)
        {
            ParameterCheckHelper.CheckIsValidString(applicationName, "applicationName", false);
            ParameterCheckHelper.CheckIsValidString(domainName, "domainName", false);
            ParameterCheckHelper.CheckIsValidString(folder, "folder", false);
            ParameterCheckHelper.CheckIsValidString(fileNameAndPath, "fileNameAndPath", false);

            // ftp to cdn and write the file
            FTPClient ftp = new FTPClient(ConfigurationManager.AppSettings["CDNFtpHost"], 21, "/", ConfigurationManager.AppSettings["CDNFtpUser"], ConfigurationManager.AppSettings["CDNFtpPassword"], FTPTransferMode.Binary);
            ftp.RemotePath = string.Format("/{0}/{1}/{2}/{3}/{4}", ConfigurationManager.AppSettings["CDNFtpRemotePath"], applicationName, SiteHelper.SiteEnvironment, domainName, folder);

            string[] listResults = ftp.GetFiles(fileNameAndPath);

            ftp.Close();

            return listResults.Length == 1;
        }
    }
}
