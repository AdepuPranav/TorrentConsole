using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TorrentConsole.Core;

namespace TorrentConsole
{
     class Program
    {
        static async Task Main(string[] args) 
        {
            if (args.Length == 0) {
                Console.WriteLine("Usage : TorrentClient <file.Torrent>");
                return;
            }
            var torrentPath = args[0];
            var outputfile = args.Length > 1 ? args[1] : "downloaded.data";

            TorrentMetaData metaData = TorrentMetaData.Load(torrentPath);

            var client = new TorrentClient(metaData,outputfile);
            await client.StartAsync();

            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }
    }
}
