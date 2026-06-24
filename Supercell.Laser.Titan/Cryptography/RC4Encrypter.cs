namespace Supercell.Laser.Titan.Cryptography
{
    using System;
    using System.IO;
    using System.Text;
    using System.Text.Json;
    using Supercell.Laser.Titan.Library;
    public class RC4Encrypter
    {
        private CryptoRC4 RC4_Stream;
        private CryptoRC4 RC4_Stream2;

        public RC4Encrypter()
        {
            string settingsText = File.ReadAllText("Settings.json");
            var settings = JsonSerializer.Deserialize<Settings>(settingsText);

            byte[] key = Encoding.UTF8.GetBytes("c2GJQU5hCjW56525cLA9MQ9JINk3zZk4nJsjYq");//settings.RC4Key);
            byte[] nonce = Encoding.UTF8.GetBytes("nonce");

            byte[] keyNonce = new byte[key.Length + nonce.Length];
            Buffer.BlockCopy(key, 0, keyNonce, 0, key.Length);
            Buffer.BlockCopy(nonce, 0, keyNonce, key.Length, nonce.Length);

            RC4_Stream = new CryptoRC4(keyNonce);
            RC4_Stream.Update(keyNonce); // Skip

            RC4_Stream2 = new CryptoRC4(keyNonce);
            RC4_Stream2.Update(keyNonce); // Skip
        }

        public byte[] Decrypt(byte[] data)
        {
            return RC4_Stream.Update(data);
        }

        public byte[] Encrypt(byte[] data)
        {
            return RC4_Stream2.Update(data);
        }

        private class Settings
        {
            public string RC4Key { get; set; }
        }
    }
}