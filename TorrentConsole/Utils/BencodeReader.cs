using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentConsole.Utils
{
    public class BencodeReader
    {
        private readonly byte[] _data;
        private int _index;

        public BencodeReader(byte[] data) 
        {
            _data = data;
            _index = 0;
        }

        public object ReadNext() 
        {
            byte current = _data[_index];
            if (current == (byte)'i') return ReadInteger();
            else if (current == (byte)'l') return ReadList();
            else if (current == (byte)'d') return ReadDictionary();
            else if (current>= (byte)'0' && current <= (byte)'9') return ReadByteString();
            else throw new Exception($"Invalid bencode format at index {_index}");

        }

        private long ReadInteger() 
        {
            _index++;
            int start = _index;
            while (_data[_index] != (byte)'e') _index++;

            string number = Encoding.ASCII.GetString(_data, start, _index - start);
           _index++;
            return long.Parse(number);
        }

        private byte[] ReadByteString() 
        { int start = _index;   
            while(_data[_index] != (byte)':') _index++;
            string lengthStr = Encoding.ASCII.GetString(_data, start, _index - start);
            int length = int.Parse(lengthStr);
            _index++;

            byte[] result = new byte[length];
            Array.Copy(_data, _index, result, 0, length);
            _index += length;
            return result;
        }

        private List<object> ReadList() 
        {
            _index++;
            var list = new List<object>();
            while (_data[_index] != (byte)'e') 
            {
               list.Add(ReadNext());
            }
            _index++;
            return list;
        
        }

        private Dictionary<string, object> ReadDictionary() 
        {
            _index++;
            var dict = new Dictionary<string, object>();
            while (_data[_index] != (byte)'e')
            {
                byte[] KeyBytes = ReadByteString();
                string key = Encoding.ASCII.GetString(KeyBytes);
                object value = ReadNext();
                dict[key] = value;
            }
            _index++;
            return dict;
        }

        

    }
}
