using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Collections;

namespace TorrentConsole.Core
{
    public class HashVerifier
    {

        public static bool VerifyPiece(byte[] pieceData, byte[] ExpectedHash) 
        {
            using var sha1 = SHA1.Create();
            byte[] computedHash = sha1.ComputeHash(pieceData);
            return StructuralComparisons.StructuralEqualityComparer.Equals(computedHash, ExpectedHash);
        }

        public static byte[] Sha1(byte[] data) 
        {
            using var sha1 = SHA1.Create();
            return sha1.ComputeHash(data);
        }
    }
}
