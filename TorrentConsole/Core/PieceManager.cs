using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentConsole.Core
{
    public class PieceManager
    {
        //the pieces downloaded
        private readonly bool[] _completed;

        private readonly int pieceCount;

        //the peers who have that piece
        private readonly Dictionary<int, HashSet<string>> availability;

        //the peers who are currently downloading a piece
        private readonly Dictionary<int, HashSet<string>> activeDownloads;

        private readonly object _lock = new object();

        public PieceManager(int totalPieces)
        {
            this.pieceCount = totalPieces;

            availability = new Dictionary<int, HashSet<string>>();
            _completed = new bool[pieceCount];
            activeDownloads = new Dictionary<int, HashSet<string>>();

            for (int i = 0; i < pieceCount; i++) 
            {
                availability[i] = new HashSet<string>();
                activeDownloads[i] = new HashSet<string>();
            }
           
        }

        public bool IsComplete(int index) => _completed[index];

        public void MarkComplete(int index)
        {
            _completed[index] = true;
        }

        //Checks available peers for the pieces and add's their peer address to the dictionary
        public void UpdatePeerBitfield(string PeerID, bool[] bitfield) 
        {
            lock (_lock) 
            {
                for (int i = 0; i < bitfield.Length; i++) 
                {
                    if (bitfield[i]) availability[i].Add(PeerID);
                }
            }
        }


        public int? GetNextPiece(string PeerID, HashSet<int> peerPieces) 
        {
            lock (_lock) 
            {
                var candidates = availability.Where(p => !_completed[p.Key] && peerPieces.Contains(p.Key) && !activeDownloads[p.Key].Contains(PeerID)).OrderBy(p => p.Value.Count).Select(p => p.Key).ToList();

                if (candidates.Count == 0) return null;
                
                int selected = candidates[0];
                activeDownloads[selected].Add(PeerID);

                return selected;
            }
        }


        public void MarkPieceCompleted(int PieceIndex)
        {
            lock (_lock)
            {
                _completed[PieceIndex] = true;
                activeDownloads[PieceIndex].Clear();
            }
        }


        public List<int> GetEndgamePieces(HashSet<int> peerPieces) 
        {
            lock (_lock) 
            {
                return availability.Where(p => !_completed[p.Key] && peerPieces.Contains(p.Key)).OrderBy(p => p.Value.Count).Select(p => p.Key).ToList();
            }
        }

        public bool IsComplete() 
        {
            lock (_lock) return _completed.All(x => x);
        }

        public void ReleasePiece(int pieceIndex, string peerId)
        {
            lock (_lock)
            {
                if (activeDownloads.ContainsKey(pieceIndex))
                {
                    activeDownloads[pieceIndex].Remove(peerId);
                }
            }
        }

    }
}
