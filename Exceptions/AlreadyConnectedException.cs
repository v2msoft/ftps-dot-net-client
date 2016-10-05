using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V2MSoftware.Ftps.Exceptions {

    public class AlreadyConnectedException : Exception {
        public AlreadyConnectedException(String message) : base(message) { }
    }
}
