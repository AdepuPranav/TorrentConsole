using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentConsole.Models
{
    public class Peer
    {
        public string IP { get; }
        public int Port { get; }
        public Peer(string ip, int port) {
            IP = ip;
            Port = port;
        }

        public override string ToString()
        {
             return $"{IP}:{Port}";
        }
    }
}
