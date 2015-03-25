using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Codentia.Common.Net.FTP
{
    /// <summary>
    /// The FTP Client class, providing the facility to upload and download files via FTP
    /// </summary>
    public class FTPClient
    {
        private const int BUFFERSIZE = 512;

        private string _remoteHost;
        private string _remotePath;
        private NetworkCredential _credentials;
        private int _port;
        private Socket _controlSocket;
        private bool _connected;
        private IPAddress _connectedToAddress;

        private FTPTransferMode _mode;

        private byte[] buffer = new byte[BUFFERSIZE];

        /// <summary>
        /// Initializes a new instance of the <see cref="FTPClient"/> class. (Default constructor)
        /// </summary>
        /// <remarks>
        /// By default the FTPClient class provides a connection on port 21 using ASCII transfers
        /// </remarks>
        public FTPClient()
        {
            _port = 21;
            _connected = false;
            _mode = FTPTransferMode.ASCII;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FTPClient"/> class.
        /// </summary>
        /// <param name="remotePath">The remote path.</param>
        /// <param name="mode">The mode.</param>
        public FTPClient(string remotePath, FTPTransferMode mode)
            : this(ConfigurationManager.AppSettings["DefaultFTPHost"], 21, remotePath, ConfigurationManager.AppSettings["DefaultFTPUser"], ConfigurationManager.AppSettings["DefaultFTPPassword"], mode)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FTPClient"/> class.
        /// </summary>
        /// <param name="host">The host to connect to</param>
        /// <param name="port">The host port to connect to</param>
        /// <param name="remotePath">The remote path to connect to on the host</param>
        /// <param name="user">The username to use for authentication</param>
        /// <param name="password">The password to use for authentication</param>
        public FTPClient(string host, int port, string remotePath, string user, string password)
            : this()
        {
            _remoteHost = host;
            _port = port;
            _remotePath = remotePath;

            _credentials = new NetworkCredential(user, password);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FTPClient"/> class.
        /// </summary>
        /// <param name="host">The host to connect to</param>
        /// <param name="port">The host port to connect to</param>
        /// <param name="remotePath">The remote path to connect to on the host</param>
        /// <param name="user">The username to use for authentication</param>
        /// <param name="password">The password to use for authentication</param>
        /// <param name="mode">The transfer mode to use</param>
        public FTPClient(string host, int port, string remotePath, string user, string password, FTPTransferMode mode)
            : this(host, port, remotePath, user, password)
        {
            _mode = mode;
        }

        /// <summary>
        /// Gets a value indicating whether this FTP Client is connected?
        /// </summary>
        public bool Connected
        {
            get
            {
                return _connected;
            }
        }

        /// <summary>
        /// Gets or sets the name of the remote host
        /// </summary>
        /// <remarks>
        /// This is read only whilst connected
        /// </remarks>
        public string RemoteHost
        {
            get
            {
                return _remoteHost;
            }

            set
            {
                if (this.Connected)
                {
                    throw new FTPException("Cannot change RemoteHost while connected");
                }

                _remoteHost = value;
            }
        }

        /// <summary>
        /// Gets or sets the port to use on the remote host
        /// </summary>
        /// <remarks>
        /// This is read only whilst connected
        /// </remarks>
        public int Port
        {
            get
            {
                return _port;
            }

            set
            {
                if (this.Connected)
                {
                    throw new FTPException("Cannot change Port while connected");
                }

                _port = value;
            }
        }

        /// <summary>
        /// Gets or sets the remote path to use on the remote host
        /// </summary>
        public string RemotePath
        {
            get
            {
                return _remotePath;
            }

            set
            {
                string safeValue = value.Replace("//", "/");

                if (this.Connected)
                {
                    CHDir(safeValue);
                }

                _remotePath = safeValue;
            }
        }

        /// <summary>
        /// Gets or sets the credentials to use
        /// </summary>
        /// <remarks>
        /// This is read only whilst connected
        /// </remarks>
        public NetworkCredential Credentials
        {
            get
            {
                return _credentials;
            }

            set
            {
                if (this.Connected)
                {
                    throw new FTPException("Cannot change Credentials while connected");
                }

                _credentials = value;
            }
        }

        /// <summary>
        /// Gets or sets the active transfer mopde
        /// </summary>
        public FTPTransferMode Mode
        {
            get
            {
                return _mode;
            }

            set
            {
                _mode = value;

                if (this.Connected)
                {
                    FTPResponse response = SendCommand("TYPE " + (_mode == FTPTransferMode.ASCII ? "A" : "I"));

                    if (response.Code != 200)
                    {
                        throw new FTPException(response);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the files.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="mask">The mask.</param>
        /// <returns>string array</returns>
        public string[] GetFiles(string path, string mask)
        {
            this.RemotePath = path;

            return this.GetFiles(mask);
        }

        /// <summary>
        /// Gets the files.
        /// </summary>
        /// <param name="mask">The mask.</param>
        /// <returns>string array</returns>
        public string[] GetFiles(string mask)
        {
            FTPResponse response;

            if (!this.Connected)
            {
                Connect();
            }

            Socket socket = GetDataSocket();

            response = SendCommand("NLST " + mask);

            // handle 550 for iis ftp 7.5
            if (!(response.Code == 150 || response.Code == 125 || response.Code == 550 || response.Code == 226))
            {
                throw new FTPException(response);
            }

            StringBuilder output = new StringBuilder();

            while (true && response.Code != 550)
            {
                int bytes = socket.Receive(buffer, buffer.Length, 0);
                output.Append(Encoding.ASCII.GetString(buffer, 0, bytes));

                if (bytes < buffer.Length)
                {
                    break;
                }
            }

            string[] lines = output.ToString().Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Trim();
            }

            socket.Close();

            if (response.Code != 550)
            {
                response = GetResponse();

                if (response.Code != 226)
                {
                    throw new FTPException(response);
                }
            }

            return lines;
        }

        /// <summary>
        /// Retrieve the size of a file
        /// </summary>
        /// <param name="fileName">The URI of the file to obtain the size for</param>
        /// <returns>long of the filesize</returns>
        public long GetFileSize(string fileName)
        {
            long size = 0;

            if (!this.Connected)
            {
                Connect();
            }

            FTPResponse response = SendCommand(string.Format("SIZE {0}", fileName));

            if (response.Code == 213)
            {
                size = long.Parse(response.Message);
            }
            else
            {
                throw new FTPException(response);
            }

            return size;
        }

        /// <summary>
        /// Connect to the remote host
        /// </summary>
        public void Connect()
        {
            FTPResponse response;

            _controlSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress hostAddress = null;

            if (!IPAddress.TryParse(_remoteHost, out hostAddress))
            {
                hostAddress = Dns.GetHostEntry(_remoteHost).AddressList[0];
            }

            IPEndPoint endPoint = new IPEndPoint(hostAddress, _port);

            try
            {
                _controlSocket.Connect(endPoint);
            }
            catch (Exception ex)
            {
                throw new FTPException("Unable to connect to remote server", ex);
            }

            _connectedToAddress = hostAddress;

            response = GetResponse();

            if (response.Code != 220)
            {
                Close();

                throw new FTPException(response);
            }

            response = SendCommand("USER " + _credentials.UserName);

            if (!(response.Code == 331 || response.Code == 230))
            {
                Close();
                throw new FTPException(response);
            }

            if (response.Code != 230)
            {
                response = SendCommand("PASS " + _credentials.Password);

                if (!(response.Code == 230 || response.Code == 202))
                {
                    Close();
                    throw new FTPException(response);
                }
            }

            _connected = true;
            Console.Out.WriteLine("Connected to " + _remoteHost);

            // set transfer mode
            this.Mode = _mode;

            if (!string.IsNullOrEmpty(_remotePath))
            {
                CHDir(_remotePath);
            }
        }

        /// <summary>
        /// Download a file from the remote host
        /// </summary>
        /// <param name="remoteFileName">The remote file to download</param>
        /// <param name="localFileName">The filename</param>
        public void DownloadFile(string remoteFileName, string localFileName)
        {
            Socket dataSocket;
            FileStream outputFile;
            FTPResponse response;

            if (!this.Connected)
            {
                Connect();
            }

            if (string.IsNullOrEmpty(localFileName))
            {
                throw new FTPException("LocalFileName is required");
            }

            if (!File.Exists(localFileName))
            {
                Stream st = File.Create(localFileName);
                st.Close();
            }

            outputFile = File.Create(localFileName);

            dataSocket = GetDataSocket();

            response = SendCommand("RETR " + remoteFileName);

            if (!(response.Code == 150 || response.Code == 125))
            {
                throw new FTPException(response);
            }

            int bytes = 0;
            byte[] buffer = new byte[BUFFERSIZE];

            while (true)
            {
                bytes = dataSocket.Receive(buffer, buffer.Length, 0);
                outputFile.Write(buffer, 0, bytes);

                if (bytes <= 0)
                {
                    break;
                }
            }

            outputFile.Close();
            outputFile.Dispose();

            if (dataSocket.Connected)
            {
                dataSocket.Close();
            }

            response = GetResponse();

            if (!(response.Code == 226 || response.Code == 250))
            {
                throw new FTPException(response);
            }
        }

        /// <summary>
        /// Upload a file to the remote host
        /// </summary>
        /// <param name="fileName">The name of the file to upload</param>
        public void UploadFile(string fileName)
        {
            UploadFile(fileName, false);
        }

        /// <summary>
        /// Delete a file on the remote host
        /// </summary>
        /// <param name="fileName">The name of the file to delete</param>
        public void DeleteRemoteFile(string fileName)
        {
            if (!this.Connected)
            {
                Connect();
            }

            FTPResponse response = SendCommand("DELE " + fileName);

            if (response.Code != 250)
            {
                throw new FTPException(response);
            }
        }

        /// <summary>
        /// Rename a file on the remote host
        /// </summary>
        /// <param name="oldFileName">The name of the file to rename</param>
        /// <param name="newFileName">The new name to give the remote file</param>
        public void RenameRemoteFile(string oldFileName, string newFileName)
        {
            FTPResponse response;

            if (!this.Connected)
            {
                Connect();
            }

            response = SendCommand("RNFR " + oldFileName);

            if (response.Code != 350)
            {
                throw new FTPException(response);
            }

            // known problem
            // rnto will not take care of existing file.
            // i.e. It will overwrite if newFileName exist
            response = SendCommand("RNTO " + newFileName);
            if (response.Code != 250)
            {
                throw new FTPException(response);
            }
        }

        /// <summary>
        /// Create a directory on the remote host
        /// </summary>
        /// <param name="directoryName">The name of the directory to create</param>
        public void MKDir(string directoryName)
        {
            if (!this.Connected)
            {
                Connect();
            }

            FTPResponse response = SendCommand("MKD " + directoryName);

            if (response.Code != 250 && response.Code != 257)
            {
                throw new FTPException(response);
            }
        }

        /// <summary>
        /// Remove a directory on the remote host
        /// </summary>
        /// <param name="directoryName">The name of the directory to remove on the remote host</param>
        public void RMDir(string directoryName)
        {
            if (!this.Connected)
            {
                Connect();
            }

            FTPResponse response = SendCommand("RMD " + directoryName);

            if (response.Code != 250 && response.Code != 257)
            {
                throw new FTPException(response);
            }
        }

        /// <summary>
        /// Close the FTP Connection
        /// </summary>
        public void Close()
        {
            if (_controlSocket != null)
            {
                if (_controlSocket.Connected)
                {
                    SendCommand("QUIT");
                    _controlSocket.Close();
                }

                _controlSocket = null;
            }

            _connected = false;
        }

        /// <summary>
        /// Upload a file to the remote host
        /// </summary>
        /// <param name="fileName">The name of the file to upload</param>
        /// <param name="resumeTransfer">Should interrupted transfers be resumed?</param>
        private void UploadFile(string fileName, bool resumeTransfer)
        {
            Socket dataSocket;
            FTPResponse response;

            if (!this.Connected)
            {
                Connect();
            }

            dataSocket = GetDataSocket();

            response = SendCommand("STOR " + Path.GetFileName(fileName));

            if (!(response.Code == 125 || response.Code == 150))
            {
                throw new FTPException(response);
            }

            // open input stream to read source file
            FileStream inputFile = new FileStream(fileName, FileMode.Open);

            Console.WriteLine("Uploading file " + fileName + " to " + _remotePath);

            int bytes = 0;
            byte[] buffer = new byte[BUFFERSIZE];

            while ((bytes = inputFile.Read(buffer, 0, buffer.Length)) > 0)
            {
                dataSocket.Send(buffer, bytes, 0);
            }

            inputFile.Close();
            inputFile.Dispose();

            if (dataSocket.Connected)
            {
                dataSocket.Close();
            }

            response = GetResponse();

            if (!(response.Code == 226 || response.Code == 250))
            {
                throw new FTPException(response);
            }
        }

        /// <summary>
        /// Change the current directory on the remote host
        /// </summary>
        /// <param name="directoryName">The directory to make current</param>
        private void CHDir(string directoryName)
        {
            if (!directoryName.Equals("."))
            {
                FTPResponse response = SendCommand("CWD " + directoryName);

                if (response.Code != 250)
                {
                    throw new FTPException(response);
                }

                Console.Out.WriteLine("Current directory is " + _remotePath);
            }
        }

        private FTPResponse SendCommand(string command)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(string.Format("{0}\r\n", command).ToCharArray());
            _controlSocket.Send(bytes);

            return GetResponse();
        }

        private FTPResponse GetResponse()
        {
            // read response from the socket
            byte[] buffer = new byte[BUFFERSIZE];
            int bytes = 0;
            string message = string.Empty;

            while (true)
            {
                bytes = 0;

                try
                {
                    bytes = _controlSocket.Receive(buffer, buffer.Length, 0);
                    message = string.Format("{0}{1}", message, Encoding.ASCII.GetString(buffer, 0, bytes));

                    if (bytes < buffer.Length)
                    {
                        break;
                    }
                }
                catch (Exception)
                {
                    // do nothing, just break
                    break;
                }
            }

            if (bytes > 0)
            {
                string[] lines = message.Split('\n');

                message = lines.Length > 2 ? lines[lines.Length - 2] : lines[0];

                // now interpret it
                return message.Substring(3, 1).Equals(" ") ? new FTPResponse(int.Parse(message.Substring(0, 3)), message.Substring(4)) : this.GetResponse();
            }
            else
            {
                return new FTPResponse(-1, "Server closed connection");
            }
        }

        private Socket GetDataSocket()
        {
            FTPResponse response = SendCommand("PASV");

            if (response.Code != 227)
            {
                throw new FTPException(response);
            }

            int startIndex = response.Message.IndexOf('(');
            int endIndex = response.Message.IndexOf(')');

            string ipData = response.Message.Substring(startIndex + 1, endIndex - startIndex - 1);
            int[] parts = new int[6];

            int len = ipData.Length;
            int partCount = 0;
            string buf = string.Empty;

            for (int i = 0; i < len && partCount <= 6; i++)
            {
                char ch = char.Parse(ipData.Substring(i, 1));

                if (char.IsDigit(ch))
                {
                    buf += ch;
                }
                else
                {
                    if (ch != ',')
                    {
                        throw new FTPException(response, "Malformed PASV reply");
                    }
                }

                if (ch == ',' || i + 1 == len)
                {
                    parts[partCount++] = int.Parse(buf);
                    buf = string.Empty;
                }
            }

            string ipAddress = string.Format("{0}.{1}.{2}.{3}", parts[0], parts[1], parts[2], parts[3]);
            int port = (parts[4] << 8) + parts[5];

            // cope with PASV implementations that send 'same IP' instruction
            IPAddress addressToUse = ipAddress == "0.0.0.0" ? _connectedToAddress : IPAddress.Parse(ipAddress);

            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(addressToUse, port);

            try
            {
                s.Connect(endPoint);
            }
            catch (Exception ex)
            {
                throw new FTPException("Unable to connect to remote server", ex);
            }

            return s;
        }
    }
}