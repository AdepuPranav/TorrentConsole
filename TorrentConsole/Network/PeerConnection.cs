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
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private readonly ValidHandShake _validate;
        private bool isChoked = false;

        private readonly PieceManager _pieceManager;
        private readonly string _peerId;

        public PeerConnection(Peer peer, TorrentMetaData metaData, PieceManager pieceManager, string peerID)
        {
            _peer = peer;
            _metaData = metaData;
            _pieceManager = pieceManager;
            _peerId = peerID;
            _validate = new ValidHandShake();
        }

        public async Task StartAsync()
        {
            Console.WriteLine("Start Async stared!!");
            await ConnectAsync();
        }

        public async Task ConnectAsync()
        {
            Console.WriteLine($"Connecting to {_peer.IP} : {_peer.Port}");
            TcpClient _client = new TcpClient();
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
            while (!_pieceManager.IsComplete()) 
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
            byte[] response = await ReadExactAsync(_networkStream, 68);
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
            int pieceLength = _metaData.PieceLength;
            byte[] pieceBuffer = new byte[pieceLength];

            int offset = 0;

            while (offset < pieceLength) 
            {
                int requestSize = Math.Min(BLOCK_SIZE, pieceLength - offset);
                await SendRequestAsync(PieceIndex, offset, requestSize);
                await ReceiveBlockAsync(pieceBuffer);
                offset += requestSize;
            }

            byte[] hash = SHA1.HashData(pieceBuffer);
            if (!hash.AsSpan().SequenceEqual(_metaData.PieceHashes[PieceIndex]))
                throw new Exception("Piece has failed");

            Console.WriteLine($"Piece {PieceIndex} verified!");

            using var fs = new FileStream(_metaData.Name, FileMode.OpenOrCreate);
            fs.Seek((long)PieceIndex * _metaData.PieceLength, SeekOrigin.Begin);
            fs.Write(pieceBuffer);
           

            Console.WriteLine($"Piece {PieceIndex} is saved!!");

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

        private async Task ReceiveBlockAsync(byte[] buffer) 
        {
            int length = await ReadIntAsync();
            int id = _networkStream.ReadByte();

            if (id != 7) return;

            int index = await ReadIntAsync();
            int begin = await ReadIntAsync();

            int blockLength = length - 9;
            await _networkStream.ReadAsync(buffer, begin, blockLength);
        }

        private static void WriteInt(byte[] buffer, int offset, int value) 
        {
            Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value)), 0, buffer, offset, 4);
        }

        private async Task<int> ReadIntAsync() 
        {
            byte[] buf = new byte[4];
            await _networkStream.ReadAsync(buf, 0, 4);
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

        private async Task<byte[]> ReadExactAsync(NetworkStream stream, int length) 
        {
            byte[] buffer = new byte[length];
            int totalread = 0;

            while (totalread < length) 
            {
                int read = await stream.ReadAsync(buffer, totalread, length - totalread);
                if (read == 0)
                    throw new Exception("Peer closed connection during handshake");

                totalread += read;
            }
            return buffer;
        }
    

        
    }
}
