using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentConsole.Core
{
    public class DiskManager
    {
        private readonly string _FilePath;
        private readonly int _PieceLength;

        public DiskManager(string filePath, int pieceLength)
        {
            _FilePath = filePath;
            _PieceLength = pieceLength;
        }

        public void WritePiece(int index, byte[] data) {
            using var fs = new FileStream(_FilePath, FileMode.OpenOrCreate, FileAccess.Write);
            fs.Seek((long)index * _PieceLength, SeekOrigin.Begin);
            fs.Write(data, 0, data.Length);
        }
    }
}
