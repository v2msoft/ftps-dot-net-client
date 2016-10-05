using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V2MSoftware.Ftps {

    public enum FtpItemType {
        File,
        Directory,
        Unknown
    }

    public class FtpsItemDescription {

        public FtpItemType Type { get; set; }

        public String Name { get; set; }

        public long Size { get; set; }

        public DateTime Date { get; set; }

        public override String ToString() {
            String type;
            switch (this.Type) {
                case FtpItemType.Directory:
                    type = "Directory";
                    break;
                case FtpItemType.File:
                    type = "File";
                    break;
                default:
                    type = "Unknown";
                    break;
            }
            return "Name: " + this.Name + Environment.NewLine +
                    "Type: " + type + Environment.NewLine +
                    "Size: " + this.Size + Environment.NewLine +
                    "Last Modification: " + this.Date.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + Environment.NewLine;
        }

    }
}
