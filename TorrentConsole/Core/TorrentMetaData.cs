using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using TorrentConsole.Utils;

namespace TorrentConsole.Core
{
    public class TorrentMetaData
    {
        public string Name { get; set; }
        public long Length { get; set; }
        public int PieceLength { get; set; }
        public byte[][] PieceHashes { get; set; }
        public byte[] InfoHash { get; set; }
        public string AnnounceUrl { get; set; }


        public static TorrentMetaData Load(string path) { 
          var data = File.ReadAllBytes(path);
            var parser = new BencodeParser(data);
            var root = parser.ParseDictionary();

            var info = (Dictionary<string , object>)root["info"];

            var metadata = new TorrentMetaData
            {
                AnnounceUrl = (string)root["announce"],
                Name = (string)info["name"],
                Length = (long)info["length"],
                PieceLength = (int)(long)info["piece length"],
                PieceHashes = SplitPieceHashes((byte[])info["pieces"]),
                InfoHash = HashVerifier.Sha1(parser.GetInfoBytes())
            };
            return metadata;
        }

        private static byte[][] SplitPieceHashes(byte[] pieces)
        {
            int count = pieces.Length / 20;
            var hashes = new byte[count][];
            for (int i = 0; i < count; i++)
            {
                hashes[i] = new byte[20];
                Array.Copy(pieces, i * 20, hashes[i], 0, 20);
            }
            return hashes;
        } 

    }
}
