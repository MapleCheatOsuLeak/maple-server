using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace maple_server_hotfix
{
    public class DelimitedPacketBuffer
    {
        private bool _isFirst = true;
        private readonly List<byte> _data = new List<byte>();

        public void AddByte(byte b) => Add(new[] { b });

        public void Add(string s) => Add(Encoding.ASCII.GetBytes(s));

        public void Add(byte[] bytes)
        {
            if (!_isFirst)
                _data.AddRange(Constants.Delimiter);

            _data.AddRange(bytes);

            _isFirst = false;
        }

        public byte[] GetBuffer() => _data.ToArray();

        public byte[] GetEncryptedBuffer(MapleClient mapleClient)
        {
            return mapleClient.Crypto.AesEncrypt(GetBuffer(), mapleClient.Key, mapleClient.Iv);
        }

        public void WriteToStream(Stream stream)
        {
            byte[] packetBuffer = GetBuffer();

            stream.Write(BitConverter.GetBytes(packetBuffer.Length), 0, 4);
            stream.Write(packetBuffer.ToArray(), 0, packetBuffer.Length);
        }
    }
}