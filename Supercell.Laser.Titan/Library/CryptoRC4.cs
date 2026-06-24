namespace Supercell.Laser.Titan.Library
{
    using System;
    using System.IO;

    public class CryptoRC4
    {
        private byte[] _box = new byte[256];
        private int _i = 0;
        private int _j = 0;

        public CryptoRC4(byte[] key)
        {
            for (int i = 0; i < 256; i++)
            {
                _box[i] = (byte)i;
            }

            int j = 0;
            for (int i = 0; i < 256; i++)
            {
                j = (j + _box[i] + key[i % key.Length]) & 0xFF;
                Swap(i, j);
            }
        }

        private void Swap(int i, int j)
        {
            byte temp = _box[i];
            _box[i] = _box[j];
            _box[j] = temp;
        }

        public byte[] Update(byte[] msg)
        {
            byte[] outBytes = new byte[msg.Length];
            for (int k = 0; k < msg.Length; k++)
            {
                _i = (_i + 1) & 0xFF;
                _j = (_j + _box[_i]) & 0xFF;
                Swap(_i, _j);
                int t = (_box[_i] + _box[_j]) & 0xFF;
                outBytes[k] = (byte)(msg[k] ^ _box[t]);
            }
            return outBytes;
        }

        public void Skip(int n)
        {
            for (int count = 0; count < n; count++)
            {
                _i = (_i + 1) & 0xFF;
                _j = (_j + _box[_i]) & 0xFF;
                Swap(_i, _j);
            }
        }
    }
}