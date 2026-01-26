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

namespace TorrentConsole.Network
{
    public class PeerConnection
    {

        private readonly Peer _peer;
        private readonly TorrentMetaData _metaData; 
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private readonly ValidHandShake _validate;

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
            byte[] handshake = HandShakeBuilder.BuildHandshake(_metaData.InfoHash, TrackerClient.GeneratePeerId());
        }

        private async Task ReceiveHandShake() 
        {
            byte[] response = new byte[68];
            int read = 0;

            while (read < 68)
            {
                read += await _networkStream.ReadAsync(response, read, 68 - read);
            }
            _validate.ValidateHandShake(response, _metaData);
            }

        
    }
}
