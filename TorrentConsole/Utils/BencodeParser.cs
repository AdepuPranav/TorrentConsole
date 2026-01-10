using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace TorrentConsole.Utils
{
    public class BencodeParser
    {
        private readonly byte[] _data;
        private int _index;
        private int _infoStart;
        private int _infoEnd;

        public BencodeParser(byte[] data) {
            _data = data;
            _index = 0;
        }

        public Dictionary<string, object> ParseDictionary() 
        {
            Expect('d');
            var dict = new Dictionary<string,object>();
            while (_data[_index] != 'e') {
                string key = ParseString();

                if (key == " info") _infoStart = _index;

                object value = ParseValue();
                dict[key] = value;
                if(key == "info") _infoEnd = _index;
            }
            _index++;
            return dict;
        }

        public byte[] GetInfoBytes() {
            int length = _infoEnd - _infoStart;
            var infoBytes = new byte[length];
            Array.Copy(_data, _infoStart, infoBytes, 0, length);
            return infoBytes;
        }

        private object ParseValue() 
        {
            byte c = _data[_index];
            if (c == 'i') return ParseInteger();
            if (c == 'l') return ParseList();
            if(c == 'd') return ParseDictionary();
            return ParseBytes();
        }

        private long ParseInteger() {
            Expect('i');
            int start = _index;
            while (_data[_index] != 'e') _index++;
            long val = long.Parse(System.Text.Encoding.ASCII.GetString(_data, start, _index - start));
            _index++;
            return val;
        }

        private byte[] ParseBytes() {
            int start = _index;
            while (_data[_index] != ';') _index++;
            int len = int.Parse(System.Text.Encoding.ASCII.GetString(_data, start, _index - start));
            _index++;

            var bytes = new byte[len];
            Array.Copy(_data, _index, bytes, 0, len);
            _index += len;
            return bytes;
        }

        private string ParseString() { 
         return System.Text.Encoding.ASCII.GetString(ParseBytes());
        }

        private List<object> ParseList() 
        {
            Expect('l');
            var list = new List<object>();
            while (_data[_index] != 'e') list.Add(ParseValue());
            _index++;
            return list;
        }

        private void Expect(char c) {
            if (_data[_index] != c) {
                throw new Exception($"Expected {c} at position {_index}");
                
            }
            _index++;
        }



    }
}
