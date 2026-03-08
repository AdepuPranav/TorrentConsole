using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentConsole.Core
{

    public enum PieceState 
    { Missing,
      Downloading,
      Downloaded
    }

   
    public class PieceManager
    {
        //the pieces downloaded
        

        public PieceState[] _state;
        private readonly int pieceCount;

        //the peers who have that piece
        private readonly Dictionary<int, HashSet<string>> availability;

        //the peers who are currently downloading a piece
        //private readonly Dictionary<int, HashSet<string>> activeDownloads;

        private readonly object _lock = new object();

        public PieceManager(int totalPieces)
        {
            this.pieceCount = totalPieces;

            availability = new Dictionary<int, HashSet<string>>();
            _state = new PieceState[pieceCount];
           // activeDownloads = new Dictionary<int, HashSet<string>>();

            for (int i = 0; i < pieceCount; i++) 
            {
                _state[i] = PieceState.Missing;
                availability[i] = new HashSet<string>();
                //activeDownloads[i] = new HashSet<string>();
            }
           
        }

     

        public void MarkComplete(int index)
        {
            lock(_lock) _state[index] = PieceState.Downloaded;
        }

        //Checks available peers for the pieces and add's their peer address to the dictionary
        public void UpdatePeerBitfield(string PeerID, bool[] bitfield) 
        {
            lock (_lock) 
            {
                for (int i = 0; i < bitfield.Length && i < pieceCount; i++) 
                {
                    if (bitfield[i]) availability[i].Add(PeerID);
                }
            }
        }


        public int? GetNextPiece(string PeerID, HashSet<int> peerPieces) 
        {
            lock (_lock) 
            {
                var candidates = availability.Where(p => _state[p.Key] == PieceState.Missing && peerPieces.Contains(p.Key)).OrderBy(p => p.Value.Count).Select(p => p.Key).ToList();

                if (candidates.Count == 0) return null;
                
                int selected = candidates[0];
                _state[selected] = PieceState.Downloading;

                return selected;
            }
        }


        public void MarkPieceCompleted(int PieceIndex)
        {
            lock (_lock)
            {
                _state[PieceIndex] = PieceState.Downloaded;
                
            }
        }


        public List<int> GetEndgamePieces(HashSet<int> peerPieces) 
        {
            lock (_lock) 
            {
                return availability.Where(p => _state[p.Key] != PieceState.Downloaded && peerPieces.Contains(p.Key)).OrderBy(p => p.Value.Count).Select(p => p.Key).ToList();
            }
        }

        public bool IsTorrentComplete() 
        {
            lock (_lock) return _state.All(x => x == PieceState.Downloaded);
        }

        public bool IsComplete(int index) 
        {
         lock(_lock) return _state[index] == PieceState.Downloaded;
        }

        

        public void ReleasePiece(int pieceIndex, string peerId)
        {
            lock (_lock)
            {
                if (_state[pieceIndex] == PieceState.Downloading)
                {
                    _state[pieceIndex] = PieceState.Missing;
                }
            }
        }

       

    }
}
