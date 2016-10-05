using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V2MSoftware.Ftps.Network {
   
    /// <summary>
    /// Private class that formats the text-based FTP response code and
    /// message into a C# object.
    /// </summary>
    internal class FtpsResponse {

        public int Code { get; set; }
        public String Message { get; set; }

        public FtpsResponse(String response) {
            this.Code = 0;
            for (int i = 0; i < response.Length; i++) {
                if (Char.IsNumber(response[i])) {
                    this.Code = this.Code * 10 + response[i] - '0';
                    continue;
                }
                this.Message = response.Substring(i);
                break;
            }
        }

        public override string ToString() {
            return this.Code.ToString() + this.Message;
        }

    }
}
