using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using TorrentConsole.Models;
using TorrentConsole.Core;
using TorrentConsole.Network.Messages;
using TorrentConsole.Network;
using System.Net;
using System.IO;

namespace TorrentConsole.Network
{
    public class PeerConnection
    {
        private const int BLOCK_SIZE = 16 * 1024;
        private readonly Peer _peer;
        private readonly TorrentMetaData _metaData;
        private TcpClient _client;
        private NetworkStream _networkStream;
        private readonly ValidHandShake _validate;
        private bool isChoked = false;

        private readonly PieceManager _pieceManager;
        private readonly string _peerId;
        private readonly string FinalPath;
        private readonly DiskManager _diskManager;
        private readonly TorrentClient _Tclient;

        public PeerConnection(Peer peer, TorrentMetaData metaData, PieceManager pieceManager, string peerID, string filePath, DiskManager diskManager,TorrentClient client)
        {
            
            _peer = peer;
            _metaData = metaData;
            _pieceManager = pieceManager;
            _peerId = peerID;
            _validate = new ValidHandShake();
            FinalPath = filePath;
            _diskManager = diskManager;
            _Tclient = client;
        }

        public async Task StartAsync()
        {
            Console.WriteLine("Start Async stared!!");
            await ConnectAsync();
        }

        public async Task ConnectAsync()
        {
            Console.WriteLine($"Connecting to {_peer.IP} : {_peer.Port}");
             _client = new TcpClient();
            await _client.ConnectAsync(_peer.IP, _peer.Port);

            Console.WriteLine("TCP CLIENT CONNECTED!!");
            _networkStream = _client.GetStream();


            Console.WriteLine("Sending Handshake!!");
            await SendHandShake();
            Console.WriteLine("HandShake Sent! , Receiving handshake..");

            await ReceiveHandShake();
            await bitfieldreceive();

            Console.WriteLine("Handshake completed");
            //sending that we are INTERESTED
            await Interested();

            await WaitforUnchokeAsync();
            
            while (!_pieceManager.IsTorrentComplete()) 
            {
                var availablePieces = GetPeerPieces();
                int? piece = _pieceManager.GetNextPiece(_peerId, availablePieces);
                if (piece == null) 
                {
                    await Task.Delay(1000);
                    continue;
                }
                try
                {
                    await DownloadPiece(piece.Value);
                    _pieceManager.MarkPieceCompleted(piece.Value);
                }
                catch 
                {
                    _pieceManager.ReleasePiece(piece.Value, _peerId);
                }
            }
            
        }

        private async Task SendHandShake()
        {
            //HandShake initiated
            byte[] handshake = HandShakeBuilder.BuildHandshake(_metaData.InfoHash, _peerId);
            await _networkStream.WriteAsync(handshake, 0 ,handshake.Length);
            



        }

        private async Task ReceiveHandShake()
        {
            Console.WriteLine("Reached ReceiveHandShake function");
            byte[] response = new byte[68];
            await ReadExactAsync(response,0, 68);
            //checking if we received the handshake response correctly
            Console.WriteLine(Encoding.ASCII.GetString(response, 1, 19));
            //checking if the response is valid 
            _validate.ValidateHandShake(response, _metaData);

            //receiving the bitfield to check the available pieces from that peer.
            

        }

        private async Task bitfieldreceive()
        {
            byte[] Bitfieldmsg = new byte[4];
            await _networkStream.ReadAsync(Bitfieldmsg, 0, 4);
            int length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(Bitfieldmsg));

            if (length == 0) return;
            int messageId = _networkStream.ReadByte();
            if (messageId != 5) return;
            if (messageId == 5)
            {
                byte[] bitfield = new byte[length - 1];
                await _networkStream.ReadAsync(bitfield, 0, length - 1);
                _peer.Bitfield = bitfield;
                bool[] pieces = new bool[_metaData.PieceHashes.Length];

                for (int i = 0; i < pieces.Length; i++) 
                {
                    int byteIndex = i / 8;
                    int bitIndex = 7 - (i % 8);
                    if (byteIndex < bitfield.Length)
                    {
                        pieces[i] = (bitfield[byteIndex] & (1 << bitIndex)) != 0;
                    }

                }
                _pieceManager.UpdatePeerBitfield(_peerId, pieces);
                Console.WriteLine("Bitfield Received");
            }


        }

        private async Task Interested()
        {
            byte[] msg = new byte[5];
            Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(1)), 0, msg, 0, 4);
            msg[4] = 2;

            await _networkStream.WriteAsync(msg, 0, msg.Length);
            await _networkStream.FlushAsync();
            Console.WriteLine("Sent INTERESTED");
        }

        private async Task WaitforUnchokeAsync() 
        {
            while (true) 
            {
                int length = await ReadIntAsync();
                int id = _networkStream.ReadByte();

                if(id == 1)
                {

                    Console.WriteLine("Unchoked !!");
                    return;
                }
            }
        }

        private async Task DownloadPiece(int PieceIndex) 
        {
            int pieceLength = (PieceIndex == _metaData.PieceHashes.Length - 1) ? (int)(_metaData.Length - (long)PieceIndex * _metaData.PieceLength) : _metaData.PieceLength;
            byte[] pieceBuffer = new byte[pieceLength];
        
            int MaxPipeline = 5;
            int inFlight = 0;
            

            var receivedOffsets = new HashSet<int>();
            int receivedBytes = 0;  
            int offset = 0;
            while (receivedBytes < pieceLength)
            {
                while ( !isChoked && inFlight < MaxPipeline && offset < pieceLength)
                {
                    int requestSize = Math.Min(BLOCK_SIZE, pieceLength - offset);
                    await SendRequestAsync(PieceIndex, offset, requestSize);

                    offset += requestSize;
                    inFlight++;
                }
                
                
                   var (index,Blockoffset,data) = await ReceiveBlockAsync();
                Interlocked.Add(ref _Tclient._totalBytesDownloaded, data.Length);
                if (index != PieceIndex) continue;
                Buffer.BlockCopy(data, 0, pieceBuffer, Blockoffset, data.Length);
                if(!receivedOffsets.Contains(Blockoffset)) {receivedOffsets.Add(Blockoffset); receivedBytes += data.Length; }
                inFlight--;
                


            }

            byte[] hash = SHA1.HashData(pieceBuffer);
            if (!hash.AsSpan().SequenceEqual(_metaData.PieceHashes[PieceIndex]))
                throw new Exception("Piece has failed");

            Console.WriteLine($"Piece {PieceIndex} verified!");
            
            /*using var fs = new FileStream(FinalPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            fs.Seek((long)PieceIndex * _metaData.PieceLength, SeekOrigin.Begin);
            fs.Write(pieceBuffer); */
            _diskManager.WritePiece(PieceIndex, pieceBuffer);


            Console.WriteLine($"Piece {PieceIndex} is saved!!");
            _Tclient.UpdateSpeed();
            _Tclient.LogStats();


        }

        private async Task SendRequestAsync(int index, int begin, int length) 
        {
            byte[] msg = new byte[17];
            WriteInt(msg, 0, 13);
            msg[4] = 6;
            WriteInt(msg, 5, index);
            WriteInt(msg, 9, begin);
            WriteInt(msg, 13, length);

            await _networkStream.WriteAsync(msg, 0, msg.Length);
        }

        private async Task<(int index, int offset, byte[] data)> ReceiveBlockAsync() 
        {   while (true) 
            {
                int length = await ReadIntAsync();
                if (length == 0) continue;
                byte[] idbuffer = new byte[1];
                await ReadExactAsync(idbuffer, 0, 1);   
                int id = idbuffer[0];

                if (id == 7)
                {
                    int index = await ReadIntAsync();
                    int begin = await ReadIntAsync();

                    int blockLength = length - 9;
                    byte[] blockdata = new byte[blockLength];
                    await ReadExactAsync(blockdata, 0, blockLength);
                    return (index, begin, blockdata);
                }
                else if (id == 0) isChoked = true;
                else if (id == 1) isChoked = false;
                else
                {
                    byte[] skip = new byte[length - 1];
                    await ReadExactAsync(skip, 0, length - 1);
                }
            }
                
        }

        private static void WriteInt(byte[] buffer, int offset, int value) 
        {
            Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)), 0, buffer, offset, 4);
        }

        private async Task<int> ReadIntAsync() 
        {
            byte[] buf = new byte[4];
            await ReadExactAsync(buf, 0, 4);
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf));
        }

        private HashSet<int> GetPeerPieces() 
        {
            var set = new HashSet<int>();
            var bitfield = _peer.Bitfield;

            for (int i = 0; i < _metaData.PieceHashes.Length; i++) 
            {
                int byteIndex = i / 8;
                int bitIndex = 7 - (i % 8);

                if (byteIndex < bitfield.Length && (bitfield[byteIndex] & (1 << bitIndex)) != 0) { set.Add(i); }
            }
            return set;
        }

        private async Task ReadExactAsync(byte[] buffer,int offset, int length) 
        {
            
            int totalread = 0;

            while (totalread < length) 
            {
                int read = await _networkStream.ReadAsync(buffer, totalread + offset, length - totalread);
                if (read == 0)
                    throw new Exception("Peer closed connection during handshake");

                totalread += read;
            }
            
        }
    

        
    }
}
