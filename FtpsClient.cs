using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using V2MSoftware.Ftps.Exceptions;
using V2MSoftware.Ftps.Network;

namespace V2MSoftware.Ftps {

    public class FtpsClient {

        /// <summary>
        /// Specifies that either the read or write operations should block the calling thread until some new information 
        /// arrives to the stream.
        /// </summary>
        public const int INFINITE_TIMEOUT = Timeout.Infinite;

        /// <summary>
        /// Private class properties that handle the communication with the
        /// FTP server through SSL socket layer.
        /// </summary>
        #region NETWORK PROPERTIES
        private TcpClient TcpClient { get; set; }
        private SslStream SslStream { get; set; }
        private ResponseQueue ResponseQueue { get; set; }
        #endregion

        /// <summary>
        /// Private class properties that hold client's object data.
        /// </summary>
        #region PRIVATE PROPERTIES
        private FtpsClientConfigurations Configuration { get; set; }
        #endregion

        #region PRIVATE METHODS
        /// <summary>
        /// Checks wether the client object is connected or not with the remote server.
        /// </summary>
        /// <returns>True if the client is already connected. False otherwise.</returns>
        private Boolean IsConnected() {
            if (this.TcpClient != null && this.TcpClient.Connected) return true;
            return false;
        }

        /// <summary>
        /// Performs the login handshake with the remote FTP server. If credentials are required
        /// the information on FtpsClientConfiguration object will be used. Otherwise, an anonymous
        /// connection will be established.
        /// IMPORTANT: This method will throw a InvalidCredentials exception if the authentication
        /// process fails due to invalid user or password.
        /// </summary>
        private void Authenticate() {

            //Send the username to FTP server and wait for response.
            this.WriteCommand("USER " + this.Configuration.Username);

            //Read the response and send or not the password depending o what
            //server has requested.
            FtpsResponse res = this.ResponseQueue.Dequeue();
            if (res.Code == 331) {
                this.WriteCommand("PASS " + this.Configuration.Password);
                res = this.ResponseQueue.Dequeue();
                if (res.Code != 230) {
                    throw new InvalidCredentialsException(res.Message);
                }
            }

        }
        
        /// <summary>
        /// Sends a new command to remote FTP server using the SslStream object.
        /// </summary>
        /// <param name="command">The command that has to be sent to the remote server.</param>
        private void WriteCommand(String command) {
            byte[] commandBytes = Encoding.ASCII.GetBytes(command + Environment.NewLine);
            this.SslStream.Write(commandBytes, 0, commandBytes.Length);
            this.SslStream.Flush();
        }
        #endregion

        #region PUBLIC METHODS

        /// <summary>
        /// Connects the client object to the FTP server using configuration and credentials specified
        /// in the FtpsClientConfigurations object.
        /// </summary>
        /// <param name="configuration">The configuration object that defines FTP server configuration parameters.</param>
        public void Connect(FtpsClientConfigurations configuration = null) {

            //Check if the connection is already open.
            if (this.IsConnected())
                throw new AlreadyConnectedException("Ftps Error: You are already connected to " + this.Configuration.Host + ":" + this.Configuration.Port.ToString() + ".");

            //If new FtpsClientConfiguration is provided, override current saved settings.
            if (configuration != null) {
                this.Configuration = configuration;
            }

            //Check if the callback for accepting certificates is defined. If it is, configure the SslStream to use it. Otherwise,
            //decide, by default, to accept all certificates from any server.
            this.TcpClient = new TcpClient(this.Configuration.Host, this.Configuration.Port);
            if (this.Configuration.CertificateAcceptCallback != null) {
                this.SslStream = new SslStream(this.TcpClient.GetStream(), true, this.Configuration.CertificateAcceptCallback);
            } else {
                this.SslStream = new SslStream(this.TcpClient.GetStream(), true, new RemoteCertificateValidationCallback(this.AcceptAllCertificatesCallback));
            }

            //Set the socket timeout values.
            this.SslStream.ReadTimeout = configuration.SocketReadTimeout;
            this.SslStream.WriteTimeout = configuration.SocketWriteTimeout;

            //Create the response queue object.
            this.ResponseQueue = new ResponseQueue(this.SslStream);

            //Connect as localhost anonymous client.
            this.SslStream.AuthenticateAsClient("localhost");

            //Read server responses and check for errors.
            while (this.ResponseQueue.Count > 0) {
                FtpsResponse res = this.ResponseQueue.Dequeue();
                if (res.Code != 220) throw new Exception("FtpsClient Error: " + res.Message);
            }

            //Connect and authenticate with server.
            this.Authenticate();

            //Navigate to FTP's root directory.
            this.NavigateTo("/");

        }

        /// <summary>
        /// Disconnects from the FTP server. The active connections will be closed but not immediately.
        /// This means that if there is still a upload or download operation executing, the server will
        /// deny any new command but won't abort the file transfer operation.
        /// </summary>
        public void Disconnect() {
            this.WriteCommand("QUIT");
            FtpsResponse res = this.ResponseQueue.Dequeue();
            if (res.Code != 221) {
                throw new UnexpectedErrorException(res.Message);
            }
            this.ResponseQueue = null;
            this.SslStream.Dispose();
            this.TcpClient.Close();
        }

        /// <summary>
        /// Retrieves the list of files and folders that exist on the current remote directory. The
        /// response data will be returned in a list of FtpsItemDescription objects.
        /// </summary>
        /// <returns>A list of FtpsItemDescription objects that contains the the file or directory information.</returns>
        public List<FtpsItemDescription> ListFilesAndDirectories() {

            //Check if connection has been established.
            if (!this.IsConnected()) throw new NotConnectedException("Ftps Error: You must be connected to execute a listing operation.");

            String dataChannelInformation = "";
            List<FtpsItemDescription> files = new List<FtpsItemDescription>();
            FtpsResponse res = null;

            //Change to PASSIVE mode.
            this.WriteCommand("PASV");

            //Read the PASV response parsing destination host and port for data connection.
            res = this.ResponseQueue.Dequeue();
            if (res.Code != 227) {
                throw new UnexpectedErrorException("FtpsClient Error: " + res.Message);
            }
            dataChannelInformation = res.Message;
            String[] info = dataChannelInformation.Split(',', '(', ')');
            String host = info[1] + "." + info[2] + "." + info[3] + "." + info[4];
            int port = int.Parse(info[5]) * 256 + int.Parse(info[6]);

            //Open the connection and read the directory information.
            TcpClient dataClient = new TcpClient(host, port);
            NetworkStream dataStream = dataClient.GetStream();

            //Check for connection acceptation.
            this.WriteCommand("MLSD");
            res = this.ResponseQueue.Dequeue();

            byte[] resp = new byte[2048];
            var memStream = new MemoryStream();
            int bytesread = dataStream.Read(resp, 0, resp.Length);
            while (bytesread > 0) {
                memStream.Write(resp, 0, bytesread);
                bytesread = dataStream.Read(resp, 0, resp.Length);
            }
            String receivedData = new String(Encoding.ASCII.GetChars(memStream.ToArray()));

            //Convert the responses to FtpsResponse objects and add to responses queue.
            String[] responses = receivedData.ToString().Split('\r', '\n');
            foreach (String response in responses) {
                if (response.Length > 0) {
                    files.Add(this.GetFtpsItemDescriptionFromString(response));
                }
            }

            //Check the data transmission has succeeded.
            res = this.ResponseQueue.Dequeue();
            if (res.Code != 226)
                throw new UnexpectedErrorException("FtpsClient Error: " + res.Message);

            //Close data stream.
            dataStream.Dispose();
            dataClient.Close();

            return files;
        }

        /// <summary>
        /// Changes the current on the remote FTP server.
        /// </summary>
        /// <param name="path">Absolute path of the directory where the FTP server must navigate to.</param>
        public void NavigateTo(String path) {
            
            //Check if the connection has been established.
            if (!this.IsConnected()) throw new NotConnectedException("Ftps Error: You must be connected to execute a navigation operation.");

            this.WriteCommand("CWD " + path);
            FtpsResponse res = this.ResponseQueue.Dequeue();
            if (res.Code != 250) {
                throw new InvalidPathException("Ftps Error: " + res.Message);
            }

        }

        /// <summary>
        /// Uploads a file to FTP server. This method is synchronous and will block the
        /// calling thread untill the upload operation is finished.
        /// </summary>
        /// <param name="localFilePath">Absolute path of the file that has to be uploaded.</param>
        /// <param name="remoteFilePath">Remote path where the file has to be placed in.</param>
        public void UploadFile(String localFilePath, String remoteFilePath) {

            //Check if the connection has been established.
            if (!this.IsConnected()) throw new NotConnectedException("Ftps Error: You must be connected to execute an upload operation.");

            FtpsResponse res = null;
            String dataChannelInformation = "";

            //Change to PASSIVE mode.
            this.WriteCommand("PASV");

            //Read the PASV response parsing destination host and port for data connection.
            res = this.ResponseQueue.Dequeue();
            if (res.Code != 227) {
                throw new UnexpectedErrorException("FtpsClient Error: " + res.Message);
            }
            dataChannelInformation = res.Message;
            String[] info = dataChannelInformation.Split(',', '(', ')');
            String host = info[1] + "." + info[2] + "." + info[3] + "." + info[4];
            int port = int.Parse(info[5]) * 256 + int.Parse(info[6]);

            //Open the connection and read the directory information.
            TcpClient dataClient = new TcpClient(host, port);
            NetworkStream dataStream = dataClient.GetStream();

            //Check for connection acceptation.
            this.WriteCommand("STOR " + remoteFilePath);
            res = this.ResponseQueue.Dequeue();
            if (res.Code != 150)
                throw new UnexpectedErrorException("Ftps Error: " + res.Message);       
            
            //Open the local file, read chunks of data and send them to FTPs server.
            FileStream fileStream = new FileStream(localFilePath, FileMode.Open);
            byte[] buff = new byte[8192];
            int bytesRead = 0;
            do {
                bytesRead = fileStream.Read(buff, 0, buff.Length);
                if(bytesRead > 0) dataStream.Write(buff, 0, bytesRead);
            } while (bytesRead > 0);
            fileStream.Close();

            //Close data stream.
            dataStream.Dispose();
            dataClient.Close();

            //Check the data transmission has succeeded.
            res = this.ResponseQueue.Dequeue();
            if (res.Code != 226)
                throw new UnexpectedErrorException("FtpsClient Error: " + res.Message);

        }

        /// <summary>
        /// Downloads a remote file to local filesystem. This method is synchronous and will block the calling thread until
        /// the download operation has been completed.
        /// </summary>
        /// <param name="remoteFilePath"></param>
        /// <param name="localFilePath"></param>
        public void DownloadFile(String remoteFilePath, String localFilePath) {

            //Check if the connection has been established.
            if (!this.IsConnected()) throw new NotConnectedException("Ftps Error: You must be connected to execute a download operation.");

            String dataChannelInformation = "";
            List<String> files = new List<String>();
            FtpsResponse res = null;

            //Change to PASSIVE mode.
            this.WriteCommand("PASV");

            //Read the PASV response parsing destination host and port for data connection.
            res = this.ResponseQueue.Dequeue();
            if (res.Code != 227) {
                throw new UnexpectedErrorException("FtpsClient Error: " + res.Message);
            }
            dataChannelInformation = res.Message;
            String[] info = dataChannelInformation.Split(',', '(', ')');
            String host = info[1] + "." + info[2] + "." + info[3] + "." + info[4];
            int port = int.Parse(info[5]) * 256 + int.Parse(info[6]);

            //Open the connection and read the directory information.
            TcpClient dataClient = new TcpClient(host, port);
            NetworkStream dataStream = dataClient.GetStream();

            //Check for connection acceptation.
            this.WriteCommand("RETR " + remoteFilePath);
            res = this.ResponseQueue.Dequeue();
            if (res.Code != 150)
                throw new UnexpectedErrorException("Ftps Error: " + res.Message);

            //Create or open the local file, download the remote file in chunks of data and
            //save them to the local file.
            FileStream fileStream = new FileStream(localFilePath, FileMode.OpenOrCreate);
            byte[] buff = new byte[8192];
            int bytesRead = 0;
            do {
                bytesRead = dataStream.Read(buff, 0, buff.Length);
                if (bytesRead > 0) fileStream.Write(buff, 0, bytesRead);
            } while (bytesRead > 0);
            fileStream.Close();

            dataStream.Dispose();
            dataClient.Close();

            res = this.ResponseQueue.Dequeue();
            if (res.Code != 226)
                throw new UnexpectedErrorException("Ftps Error: " + res.Message);

        }

        /// <summary>
        /// This is the default certificate validation callback that will force the FtpsClient object to accept all certificates
        /// by default. See the CertificateAcceptCallback property on FtpsClientConfiguration object if you want to override this
        /// behaviour.
        /// </summary>
        /// <param name="sender">Object or instance that is calling the method.</param>
        /// <param name="certificate">The certificate that has to be validated.</param>
        /// <param name="chain">The current certificate's chain object..</param>
        /// <param name="sslPolicyErrors">The SSL policy errors obtained during current certificate validation.</param>
        /// <returns></returns>
        private bool AcceptAllCertificatesCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
            return true;
        }

        /// <summary>
        /// Parses a file information string into a new FtpsItemDescription object.
        /// </summary>
        /// <param name="data">The string returned by the FTP server containing the file or directory information.</param>
        /// <returns>A FtpsItemDescription object filled with the string data.</returns>
        private FtpsItemDescription GetFtpsItemDescriptionFromString(String data) {

            FtpsItemDescription retObject = new FtpsItemDescription();

            //Build data dictionary.
            Dictionary<String, String> dictionary = new Dictionary<String, String>();
            String[] entries = data.Split(';');
            for (int i = 0; i < entries.Length; i++) {
                String[] entry = entries[i].Split('=');
                if (entry.Length == 1) {
                    dictionary.Add("name", entry[0]);
                } else {
                    dictionary.Add(entry[0], entry[1]);
                }

            }


            //Parse file type.
            if (dictionary["type"].ToLower() == "dir") {
                retObject.Type = FtpItemType.Directory;
            } else if (dictionary["type"].ToLower() == "file") {
                retObject.Type = FtpItemType.File;
            } else {
                retObject.Type = FtpItemType.Unknown;
            }

            //Parse last modification date.
            retObject.Date = DateTime.ParseExact(dictionary["modify"], "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);

            //Parse the file name.
            if (dictionary.ContainsKey("name")) {
                retObject.Name = dictionary["name"].Trim();
            }

            //Parse the file size.
            if (dictionary.ContainsKey("size")) {
                retObject.Size = long.Parse(dictionary["size"]);
            } else {
                retObject.Size = 0;
            }

            return retObject;

        }
        #endregion

    }
}
