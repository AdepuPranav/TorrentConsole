using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentConsole.Network.Messages
{
    public class HandShakeBuilder
    {
        public static byte[] BuildHandshake(byte[] infoHash, string peerID)
        {
            if (infoHash.Length != 20) { throw new Exception("info hash is not correct"); }
            if (peerID.Length != 20) { throw new Exception("peerID is not correct"); }

            byte[] buffer = new byte[68];
            int offset = 0;

            buffer[offset++] = 19;
            Encoding.ASCII.GetBytes("BitTorrent protocol", 0, 19, buffer, offset);
            offset += 19;

            offset += 8;

            Array.Copy(infoHash, 0, buffer, offset, 20);
            offset += 20;

            Encoding.ASCII.GetBytes(peerID, 0, 20, buffer, offset);
            offset += 20;

            Console.WriteLine("Builded Handshake!");
            return buffer;
        }
    }
}
