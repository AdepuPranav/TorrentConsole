using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TorrentConsole.Network;
using System.Windows.Forms;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;

namespace TorrentConsole.Core
{
    public class TorrentClient : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        private StreamWriter _logWriter;
        private DateTime _startTime;

        private double _speed;
        public double Speed
        {
            get => _speed;
            set { _speed = value; OnPropertyChanged(nameof(Speed)); }
        }

        public long _totalBytesDownloaded;
        public long TotalBytesDownloaded
        {
            get => _totalBytesDownloaded;
            set { _totalBytesDownloaded = value; OnPropertyChanged(nameof(TotalBytesDownloaded)); }
        }

        private int _connectedPeers;
        public int ConnectedPeers
        {
            get => _connectedPeers;
            set { _connectedPeers = value; OnPropertyChanged(nameof(ConnectedPeers)); }
        }

        public long TotalSize { get; set; }

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private readonly TorrentMetaData _metaData;
        private readonly PieceManager _pieceManager;
        private readonly DiskManager _diskManager;
        private readonly string _downloadPath;
        private readonly SemaphoreSlim _connectionLimit = new SemaphoreSlim(20);
        private readonly TorrentClient _client;
        private string finalPath;
        private string filePath;








        private long _lastBytes;
        private DateTime _lastTime = DateTime.Now;






        public TorrentClient(TorrentMetaData meta, string downloadpath)
        {


            _metaData = meta;
            _downloadPath = downloadpath;
            _pieceManager = new PieceManager(meta.PieceHashes.Length);
            finalPath = Path.Combine(_downloadPath, meta.Name);
            Directory.CreateDirectory(finalPath);
            filePath = Path.Combine(finalPath, meta.Name);
            _diskManager = new DiskManager(filePath, meta.PieceLength);
            Console.WriteLine($"📁 Writing file to: {filePath}");
            TotalSize = _metaData.Length;
            _client = this;


            //log file setup 
            string logfile = $"log_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            _logWriter = new StreamWriter(logfile);
            _logWriter.WriteLine("Time,SpeedMbps,DownloadedMB,Peers,Progress");
            _startTime = DateTime.Now;


        }

        public void UpdateSpeed()
        {
            var now = DateTime.Now;
            var timeDiff = (now - _lastTime).TotalSeconds;
            if (timeDiff >= 1)
            {
                var bytesDiff = _totalBytesDownloaded - _lastBytes;
                Speed = bytesDiff / timeDiff / 1024.0 / 1024.0;
                _lastBytes = _totalBytesDownloaded;
                _lastTime = now;
            }
        }

        public void OnPieceDownloaded(int index, byte[] data)
        {
            bool valid = HashVerifier.VerifyPiece(data, _metaData.PieceHashes[index]);
            if (!valid)
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

        public async Task StartAsync()
        {
            Console.WriteLine("Contacting Tracker ...........");

            foreach (var url in _metaData.AnnounceUrl)
            {
                Console.WriteLine($"Trying Tracker : {url}");
                var tracker = new TrackerClient(url, _metaData.InfoHash);
                var peers = await tracker.AnnounceAsync(_metaData.Length);
                _connectedPeers = peers.Count;

                if (peers.Count > 0)
                {

                    Console.WriteLine($"Connected to {peers.Count} Peers");
                    foreach (var peer in peers)
                    {
                        await _connectionLimit.WaitAsync();
                        _ = Task.Run(async () =>
                            {
                                try
                                {
                                    Console.WriteLine($"Peer IP : {peer.IP} /n Peer Port : {peer.Port}");
                                    string peerId = TrackerClient.GeneratePeerId();
                                    var Conn = new PeerConnection(peer, _metaData, _pieceManager, peerId, filePath, _diskManager, _client);
                                    await Conn.StartAsync();
                                    
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Peer Error : {ex.Message}");
                                }
                                finally
                                {
                                    _connectionLimit.Release();
                                }
                            }

                            );




                    }
                    await Task.Delay(-1);


                }

            }
            Console.WriteLine("No trackers responded with peers.");

        }

        public async Task ShowProgressAsync()
        {
            var sw = Stopwatch.StartNew();
            long lastBytes = 0;

            while (TotalBytesDownloaded < TotalSize)
            {
                double progress = (double)TotalBytesDownloaded / TotalSize * 100;

                long currentBytes = TotalBytesDownloaded;
                long delta = currentBytes - lastBytes;
                lastBytes = currentBytes;

                double speed = delta / 1024.0 / 1024.0;

                Console.Clear();

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
            }
        }

        public void LogStats() 
        {
            Console.WriteLine("Entered Log stat");
         double time = (DateTime.Now - _startTime).TotalSeconds;
        double downloadedMB = TotalBytesDownloaded / 1024.0 / 1024.0;
            double progress = TotalSize == 0 ? 0 : (double)TotalBytesDownloaded/TotalSize * 100;
            _logWriter.WriteLine($"{time:F2},{Speed:F2},{downloadedMB:F2},{ConnectedPeers},{progress:F2}");

        }

        public void EndLog() 
        {
            _logWriter?.Close();
        }



    }
}
