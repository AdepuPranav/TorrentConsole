using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentConsole.Utils
{
    public class BencodeEncoder
    {
        public static byte[] Encode(object obj) 
        {
            var buffer = new List<byte>();
            EncodeInternal(obj, buffer);
            return buffer.ToArray();
        }

        private static void EncodeInternal(object obj, List<byte> buffer) 
        {
            switch (obj) 
            {
                case long i:
                    buffer.Add((byte)'i');
                    buffer.AddRange(Encoding.ASCII.GetBytes(i.ToString()));
                    buffer.Add((byte)'e');
                    break;

                case byte[] bytes:
                    buffer.AddRange(Encoding.ASCII.GetBytes(bytes.Length.ToString()));
                    buffer.Add((byte)':');
                    buffer.AddRange(bytes);
                    break;

                case List<object> list:
                    buffer.Add((byte)'l');
                    foreach (var item in list) EncodeInternal(item, buffer);
                    buffer.Add((byte)'e');
                    break;

                case Dictionary<string, object> dict:
                    buffer.Add((byte)'d');
                    foreach (var key in dict.Keys) 
                    { 
                     var KeyBytes = Encoding.UTF8.GetBytes(key);
                     EncodeInternal(KeyBytes, buffer);
                     EncodeInternal(dict[key], buffer);
                    }
                    buffer.Add((byte)'e');
                    break;

            }
        }
    }
}
