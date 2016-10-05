using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V2MSoftware.Ftps.Network {

    internal class ResponseQueue : Queue<FtpsResponse> {
        
        private Stream NetworkStream { get; set; }

        public ResponseQueue(Stream networkStream) {
            this.NetworkStream = networkStream;
        }

        public new FtpsResponse Dequeue() {
            if(this.NetworkStream != null && this.Count == 0) {
                this.ReadFromInputStream();
            }
            if(base.Count > 0)
                return base.Dequeue();
            return null;
        }

        public new int Count {
            get {
                if(base.Count == 0) {
                    this.ReadFromInputStream();
                }
                return base.Count;
            }
        }

        private void ReadFromInputStream() {

            byte[] buffer = new byte[2048];
            StringBuilder messageData = new StringBuilder();
            int bytes = -1;

            do {
                try {
                    bytes = this.NetworkStream.Read(buffer, 0, buffer.Length);
                } catch (Exception) {
                    break;
                }
                Decoder decoder = Encoding.ASCII.GetDecoder();
                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                decoder.GetChars(buffer, 0, bytes, chars, 0);
                messageData.Append(chars);

                //Check for the EOF markers.
                if (messageData.ToString().IndexOf("\r\n") != -1) {
                    break;
                }
                if (messageData.ToString().IndexOf("\n") != -1) {
                    break;
                }

            } while (bytes != -1);

            //Convert the responses to FtpsResponse objects and add to responses queue.
            String[] responses = messageData.ToString().Split('\r', '\n', '\u0017','\u0003','\u0001','\0','?');
            foreach (String response in responses) {
                if (response.Length > 0) {
                    this.Enqueue(new FtpsResponse(response));
                }
            }
        }

    }
}
