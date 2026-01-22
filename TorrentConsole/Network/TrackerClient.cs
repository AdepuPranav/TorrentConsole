using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using TorrentConsole.Utils;
using TorrentConsole.Models;
using System.Text;

namespace TorrentConsole.Network
{
    public class TrackerClient
    {
        private readonly string _announceUrl;
        private readonly byte[] _infoHash;
        private readonly string _peerId;
        private readonly int _port;

        public TrackerClient(string announceUrl, byte[] infoHash, int port = 6881) 
        { 
          _announceUrl = announceUrl;
            _infoHash = infoHash;
            _port = port;
            _peerId = GeneratePeerId();
        }

        public async Task<List<string>> AnnounceAsync(long left) {
            var peers = new List<string>();
            using var http = new HttpClient();
            try
            {
                var url = BuildAnnounceUrl(left);
                var response = await http.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to announce to tracker: {response.StatusCode}");
                    return peers;
                }
                var data = await response.Content.ReadAsByteArrayAsync();
                peers = ParseTrackerResponse(data);
            }

            catch (Exception ex) 
            {
             Console.WriteLine($"Error announcing to tracker: {ex.Message}");
            }
            return peers;
        }

        private static string GeneratePeerId() {

            return "-CS0001-" + Guid.NewGuid().ToString("N")[..12];
        }

        /*private List<Peer> ParsePeers(byte[] response) {
            var parser = new Utils.BencodeParser(response);
            var dict = parser.ParseDictionary();

            var peersBytes = (byte[])dict["peers"];
            var peers = new List<Peer>();

            for (int i = 0; i < peersBytes.Length; i += 6) 
            {
                string ip = $"{peersBytes[i]}.{peersBytes[i + 1]}.{peersBytes[i + 2]}.{peersBytes[i + 3]}";
                int port = peersBytes[i + 4] << 8 | peersBytes[i + 5];
                peers.Add(new Peer(ip, port));
            }
            return peers;

        }*/

        private string BuildAnnounceUrl(long left) 
        {
            return $"{_announceUrl}?" +
                   $"info_hash={UrlEncoding.Encode(_infoHash)}" +
                   $"&peer_id={_peerId}" +
                   $"&port={_port}" +
                   $"&uploaded=0" +
                   "&downloaded=0" +
                   $"&left={left}" +
                   "&compact=1";
        }   

        private List<string> ParseTrackerResponse(byte[] data) 
        {
            var peers = new List<string>();

            var parser = new Utils.BencodeReader(data);
            var dict = (Dictionary<string,object>)parser.ReadNext();

            if(dict.ContainsKey("failure reason")) 
            {
                string reason = Encoding.UTF8.GetString((byte[])dict["failure reason"]);
                Console.WriteLine($"Tracker failure: {reason}");
                return peers;
            }

            if (!dict.ContainsKey("peers"))
                return peers;

            var peersBytes = (byte[])dict["peers"] ;
            if (peersBytes == null)
                return peers;

            for (int i = 0; i + 5 < peersBytes.Length; i += 6)
            {
                string ip =
                    $"{peersBytes[i]}." +
                    $"{peersBytes[i + 1]}." +
                    $"{peersBytes[i + 2]}." +
                    $"{peersBytes[i + 3]}";

                int port = (peersBytes[i + 4] << 8) | peersBytes[i + 5];

                peers.Add($"{ip}:{port}");
            }
            Console.WriteLine($"Received {peers.Count} peers from tracker.");
            return peers;
        } 

    }
}
