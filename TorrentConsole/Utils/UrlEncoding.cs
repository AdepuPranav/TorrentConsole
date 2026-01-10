using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text;

namespace TorrentConsole.Utils
{
    public class UrlEncoding
    {
        public static string Encode(byte[] data) {
            var sb = new StringBuilder();
            foreach (byte b in data) { 
              sb.Append('%');
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
