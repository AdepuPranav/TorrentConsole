using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TorrentConsole.Models
{
    public class Peer
    {
        public IPAddress IP { get; }
        public int Port { get; }

        public byte[] Bitfield { get; set; }
        public Peer(IPAddress ip, int port) {
            IP = ip;
            Port = port;
        }

        public override string ToString()
        {
             return $"{IP}:{Port}";
        }
    }
}
