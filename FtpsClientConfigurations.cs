using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace V2MSoftware.Ftps {
    /// <summary>
    /// Public class that is used to set the required configuration options 
    /// to establish the FTPs connection.
    /// </summary>
    public class FtpsClientConfigurations {
        public String Host { get; set; }
        public int Port { get; set; }
        public String Username { get; set; }
        public String Password { get; set; }
        public int SocketReadTimeout { get; set; }
        public int SocketWriteTimeout { get; set; }
        //If set, defines how to validate SslCertificates. If null, all certificates will be accepted.
        public RemoteCertificateValidationCallback CertificateAcceptCallback { get; set; }
    }
}
