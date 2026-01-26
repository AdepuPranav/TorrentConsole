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

namespace TorrentConsole.Network
{
    public class PeerConnection
    {

        private readonly Peer _peer;
        private readonly TorrentMetaData _metaData; 
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private readonly ValidHandShake _validate;
        private bool isChoked = false;

        public PeerConnection(Peer peer,TorrentMetaData metaData)
        {
            _peer = peer;
            _metaData = metaData;
        }

        public async Task ConnectAsync() 
        {
            TcpClient _client = new TcpClient();
            await _client.ConnectAsync(_peer.IP, _peer.Port);
            _networkStream = _client.GetStream();

            await SendHandShake();
            await ReceiveHandShake();
        }

        private async Task SendHandShake() 
        {
            //HandShake initiated
            byte[] handshake = HandShakeBuilder.BuildHandshake(_metaData.InfoHash, TrackerClient.GeneratePeerId());

            //sending that we are INTERESTED
            await Interested();


        }

        private async Task ReceiveHandShake() 
        {
            byte[] response = new byte[68];
            int read = 0;

            //Reading every character of response
            while (read < 68)
            {
                read += await _networkStream.ReadAsync(response, read, 68 - read);
            }

            //checking if the response is valid 
            _validate.ValidateHandShake(response, _metaData);

            //receiving the bitfield to check the available pieces from that peer.
            await bitfieldreceive();

        }

        private async Task bitfieldreceive() 
        {
            byte[] Bitfieldmsg = new byte[4];
            await _networkStream.ReadAsync(Bitfieldmsg, 0, 4);
            int length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(Bitfieldmsg));

            if (length == 0) return;
            int messageId = _networkStream.ReadByte();
            if (messageId == 5)
            {
                byte[] bitfield = new byte[length - 1];
                await _networkStream.ReadAsync(bitfield, 0, length - 1);
                _peer.Bitfield = bitfield;
                Console.WriteLine("Bitfield Received");
            }

            if (messageId == 1) isChoked = false;
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

        
    }
}
