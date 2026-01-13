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
        public List<string> AnnounceUrl { get; set; } = new();


        public static TorrentMetaData Load(string path) { 
          byte[] data = File.ReadAllBytes(path);
            var reader = new BencodeReader(data);
            var root = (Dictionary<string,object>)reader.ReadNext();
            var meta = new TorrentMetaData();

            if (root.ContainsKey("announce")) 
            {
                meta.AnnounceUrl.Add(Encoding.UTF8.GetString((byte[])root["announce"]));
            }

            if (root.ContainsKey("announce-list")) 
            {
                var lists = (List<object>)root["announce-list"];
                foreach (var tier in lists) 
                {
                    foreach (var url in (List<object>)tier) 
                    {
                        meta.AnnounceUrl.Add(Encoding.UTF8.GetString((byte[])url));
                    }
                }
            }

           
            var info = (Dictionary<string,object>)root["info"];

            meta.Name = Encoding.ASCII.GetString((byte[])info["name"]);
            meta.Length = (long)info["length"];
            meta.PieceLength = (int)(long)info["piece length"];

            byte[] pieces = (byte[])info["pieces"];
            meta.PieceHashes = SplitPieceHashes(pieces);
            meta.InfoHash = ComputeInfoHash(info);
            return meta;
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

        private static byte[] ComputeInfoHash(Dictionary<string, object> info) 
        {
            byte[] bencodedInfo = BencodeEncoder.Encode(info);
            return SHA1.HashData(bencodedInfo);

        }

    }
}
