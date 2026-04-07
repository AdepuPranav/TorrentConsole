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
      Verifying,
      Downloaded
    }

    public enum BlockState 
    {
     Missing,
     Requested,
     Downloaded
    }


    public class PieceManager
    {
        //the pieces downloaded
        private readonly Dictionary<int, BlockState[]> _blockStates = new();
        private const int BlockSize = 16 * 1024; //16KB blocks

        public PieceState[] _state;
        private readonly int pieceCount;

        //the peers who have that piece
        private readonly Dictionary<int, HashSet<string>> availability;

        //the peers who are currently downloading a piece
        //private readonly Dictionary<int, HashSet<string>> activeDownloads;

        private readonly object _lock = new object();
        private TorrentClient Tclient;

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
            lock (_lock) {
                
                _state[index] = PieceState.Downloaded;
                Console.WriteLine($"Piece {index} is downloaded");
            }



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
                var candidates = availability.Where(p => (_state[p.Key] == PieceState.Missing || _state[p.Key] == PieceState.Downloading) && peerPieces.Contains(p.Key) && HasMissingBlocks(p.Key)).OrderBy(p => p.Value.Count).Select(p => p.Key).ToList();

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
            Tclient.EndLog();
        }

        public bool IsComplete(int index)
        {
            lock (_lock) return _state[index] == PieceState.Downloaded;
        }



        public void ReleasePiece(int pieceIndex, string peerId)
        {
            lock (_lock)
            {
                if (_state[pieceIndex] == PieceState.Downloading) return;


                _state[pieceIndex] = PieceState.Missing;
                if (_blockStates.ContainsKey(pieceIndex))
                {
                    bool hasDownloadedBlocks = false;

                    for (int i = 0; i < _blockStates[pieceIndex].Length; i++)
                    {
                        if (_blockStates[pieceIndex][i] == BlockState.Requested)
                        {
                            _blockStates[pieceIndex][i] = BlockState.Missing;
                        }
                        else if (_blockStates[pieceIndex][i] == BlockState.Downloaded)
                        {
                            hasDownloadedBlocks = true;
                        }
                    }

                    if (!hasDownloadedBlocks) _state[pieceIndex] = PieceState.Missing;
                    else if (_state[pieceIndex] == PieceState.Verifying) _state[pieceIndex] = PieceState.Downloading;
                    else _state[pieceIndex] = PieceState.Downloading;

                }
            }
        }

        public void InitializeBlocks(int pieceIndex, int pieceLength)
        {
            lock (_lock)
            {
                if (!_blockStates.ContainsKey(pieceIndex))
                {
                    int totalBlocks = (int)Math.Ceiling((double)pieceLength / BlockSize);
                    _blockStates[pieceIndex] = new BlockState[totalBlocks];
                }
            }
        }

        public int? GetNextMissingBlock(int pieceIndex)
        {
            lock (_lock)
            {
                var blocks = _blockStates[pieceIndex];
                for (int i = 0; i < blocks.Length; i++)
                {
                    blocks[i] = BlockState.Requested;
                    return i * BlockSize;
                }
            }
            return null;
        }

        public void MarkBlockDownloaded(int pieceIndex, int offset)
        {
            lock (_lock)
            {
                int blockindex = offset / BlockSize;
                _blockStates[pieceIndex][blockindex] = BlockState.Downloaded;
            }
        }

        private bool HasMissingBlocks(int pieceIndex)
        {
            if (!_blockStates.ContainsKey(pieceIndex)) return true;
            return _blockStates[pieceIndex].Any(b => b == BlockState.Missing);
        }

        public bool IsPieceComplete(int pieceIndex)
        {
            if (!_blockStates.ContainsKey(pieceIndex)) return false;
            return _blockStates[pieceIndex].All(b => b == BlockState.Downloaded);
        }

        public bool TryMarkVerifying(int pieceIndex)
        {
            lock (_lock)
            {
                if (_state[pieceIndex] == PieceState.Downloading)
                {
                    _state[pieceIndex] = PieceState.Verifying;
                    return true;
                }
                return false;
            }

        }

        public void ReleaseBlocks(int pieceIndex, HashSet<int> blockOffsets)
        {
            lock (_lock)
            {
                if (!_blockStates.ContainsKey(pieceIndex)) return;

                foreach (int offset in blockOffsets)
                {
                    int blockIndex = offset / (16 * 1024);
                    if (blockIndex < _blockStates[pieceIndex].Length && _blockStates[pieceIndex][blockIndex] == BlockState.Requested)
                    {
                        _blockStates[pieceIndex][blockIndex] = BlockState.Missing;
                    }
                }

                if (_state[pieceIndex] == PieceState.Verifying)
                {
                    _state[pieceIndex] = PieceState.Downloading;
                }
            }
        }
    }
}
