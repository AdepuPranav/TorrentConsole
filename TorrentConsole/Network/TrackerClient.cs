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

        public async Task<List<Peer>> AnnounceAsync(long left) {

            string url = BuildAnnounceUrl(left);

            using var http = new HttpClient(); 
            byte[] responseBytes = await http.GetByteArrayAsync(url);
            return ParsePeers(responseBytes);
        }


        private string BuildAnnounceUrl(long left) {
            return $"{_announceUrl}?" + $"info_hash = {UrlEncoding.Encode(_infoHash)}" + $"&peer_id = {_peerId}" + $"&port = {_port}" + $"&uploaded = 0" + "&downloaded=0" + $"&left = {left}" + "&compact = 1";
        }

        private static string GeneratePeerId() {

            return "-CS0001-" + Guid.NewGuid().ToString("N")[..12];
        }

        private List<Peer> ParsePeers(byte[] response) {
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

        } 

    }
}
