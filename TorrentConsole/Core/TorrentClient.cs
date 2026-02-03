using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TorrentConsole.Network;

namespace TorrentConsole.Core
{
    public class TorrentClient
    {
        private readonly TorrentMetaData _metaData;
        private readonly PieceManager _pieceManager;
        private readonly DiskManager _diskManager;

        

        public TorrentClient(TorrentMetaData meta, string outputfile) { 
             _metaData = meta;
            _pieceManager = new PieceManager(meta.PieceHashes.Length);
            _diskManager = new DiskManager(outputfile, meta.PieceLength);
        }

        public void OnPieceDownloaded(int index, byte[] data) 
        {
            bool valid = HashVerifier.VerifyPiece(data, _metaData.PieceHashes[index]);
            if(!valid) 
            {
                Console.WriteLine($"Piece {index} failed hash verification.");
                return;
            }
            else 
            {
                _diskManager.WritePiece(index, data);
                _pieceManager.MarkComplete(index);

                Console.WriteLine($"Piece {index} is saved");
            }

        }

        public async Task StartAsync() {
            Console.WriteLine("Contacting Tracker ...........");

            foreach (var url in _metaData.AnnounceUrl) {
                Console.WriteLine($"Trying Tracker : {url}");
                var tracker = new TrackerClient(url, _metaData.InfoHash);
                var peers = await tracker.AnnounceAsync(_metaData.Length);

                if (peers.Count > 0)
                {
                    
                    Console.WriteLine($"Connected to {peers.Count} Peers");
                    foreach (var peer in peers) 
                    {
                        Console.WriteLine($"Peer IP : {peer.IP} /n Peer Port : {peer.Port}"); 
                        string peerId = TrackerClient.GeneratePeerId();
                        var Conn = new PeerConnection(peer, _metaData,_pieceManager,peerId);
                        _ =  Conn.StartAsync(); 
                      
                        

                    }
                    await Task.Delay(-1);


                }

            }
            Console.WriteLine("No trackers responded with peers.");

        }

    }
}
