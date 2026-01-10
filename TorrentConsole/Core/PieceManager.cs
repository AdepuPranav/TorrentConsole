using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentConsole.Core
{
    public class PieceManager
    {
        private readonly bool[] _completed;

        public PieceManager(int totalPieces)
        {
            _completed = new bool[totalPieces];
        }

        public bool IsComplete(int index) => _completed[index];

        public void MarkComplete(int index)
        {
            _completed[index] = true;
        }

        public int GetNextMissingPiece()
        {
            for (int i = 0; i < _completed.Length; i++)
            {
                if (!_completed[i])
                { return i; }
            }
            return -1;
        }
    }
}
