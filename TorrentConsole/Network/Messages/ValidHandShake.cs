using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TorrentConsole.Core;

namespace TorrentConsole.Network.Messages
{
    public class ValidHandShake
    {
        public void ValidateHandShake(byte[] response,TorrentMetaData _meta) 
        {
            if (response[0] != 19)
            {
                throw new Exception("Invalid handshake: incorrect protocol length");
            }

            string protocol = Encoding.ASCII.GetString(response, 1, 19);
            if (protocol != "Bittorent protocol") 
            {
                throw new Exception("Invalid Protocol");
            }

            for (int i = 0; i < 20; i++) 
            {
                if (response[28+i] != _meta.InfoHash[i])
                    throw new Exception("Info Hash mismatch");
            }

            Console.WriteLine("HandShake is successful! BINGO!");
        }

    }
}
