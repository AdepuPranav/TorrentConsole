using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TorrentConsole.Core
{
    public class DiskManager
    {
        private readonly string _FilePath;
        private readonly int _PieceLength;
        private readonly object _diskLock = new object();

        public DiskManager(string filePath, int pieceLength)
        {
            _FilePath = filePath;
            _PieceLength = pieceLength;
        }

        public void WritePiece(int index, byte[] data) {
            lock (_diskLock) 
            {
                using var fs = new FileStream(_FilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
                fs.Seek((long)index * _PieceLength, SeekOrigin.Begin);
                fs.Write(data, 0, data.Length);
            }
           
        }

        public void WriteBlock(int pieceIndex, int blockOffset, byte[] data) 
        {
            lock (_diskLock) 
            {
                using var fs = new FileStream(_FilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
                fs.Seek((long)pieceIndex * _PieceLength + blockOffset, SeekOrigin.Begin);
                fs.Write(data, 0, data.Length);

            }
        
        }


        public byte[] ReadPiece(int index, int length) 
        {
            lock (_diskLock) 
            {
                using var fs = new FileStream(_FilePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
                byte[] buffer = new byte[length];
                fs.Seek((long)index * _PieceLength, SeekOrigin.Begin);
                fs.ReadExactly(buffer, 0, length);
                return buffer;
            }
        }
       
    }
}
