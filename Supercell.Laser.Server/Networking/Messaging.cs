namespace Supercell.Laser.Server.Networking
{
    using Supercell.Laser.Logic.Message;
    using Supercell.Laser.Server.Message;
    using Supercell.Laser.Titan.Cryptography;
    using Supercell.Laser.Titan.Library;
    using Supercell.Laser.Titan.Math;
    using System.Linq;

    public class Messaging
    {

        private Connection Connection;
        private RC4Encrypter RC4 = new RC4Encrypter();

        private MessageFactory MessageFactory;
        public Messaging(Connection connection)
        {
            Connection = connection;
            MessageFactory = MessageFactory.Instance;
            
        }

        public void Send(GameMessage message)
        {
            Processor.Send(Connection, message);
        }

        public void EncryptAndWrite(GameMessage message)
        {
            if (message.GetEncodingLength() == 0) message.Encode();

            byte[] payload = new byte[message.GetEncodingLength()];
            Buffer.BlockCopy(message.GetMessageBytes(), 0, payload, 0, payload.Length);

            int messageType = message.GetMessageType();
            int version = message.GetVersion();
            //Console.WriteLine(string.Join(" ", payload.Select(b => b.ToString("X2"))));
            byte[] encrypted = RC4.Encrypt(payload);
            payload = encrypted;
            byte[] stream = new byte[payload.Length + 7];

            int length = payload.Length;

            stream[0] = (byte)(messageType >> 8);
            stream[1] = (byte)(messageType);
            stream[2] = (byte)(length >> 16);
            stream[3] = (byte)(length >> 8);
            stream[4] = (byte)(length);
            stream[5] = (byte)(version >> 8);
            stream[6] = (byte)(version);

            Buffer.BlockCopy(payload, 0, stream, 7, payload.Length);
            Connection.Write(stream);
            //Console.WriteLine($"Encoding length: {message.GetEncodingLength()}");
            //Console.WriteLine($"Real message bytes length: {message.GetMessageBytes().Length}");

        }

        public int OnReceive()
        {
            long position = Connection.Memory.Position;
            Connection.Memory.Position = 0;

            byte[] headerBuffer = new byte[7];
            Connection.Memory.Read(headerBuffer, 0, 7);

            // Messaging::readHeader inling? yes.
            int type = headerBuffer[0] << 8 | headerBuffer[1];
            int length = headerBuffer[2] << 16 | headerBuffer[3] << 8 | headerBuffer[4];
            int version = headerBuffer[5] << 8 | headerBuffer[6];

            byte[] payload = new byte[length];
            if (Connection.Memory.Read(payload, 0, length) < length)
            { // Packet still not received
                Connection.Memory.Position = position;
                return 0;
            }

            if (this.ReadNewMessage(type, length, version, payload) != 0)
            {
                return -1;
            }

            byte[] all = Connection.Memory.ToArray();
            byte[] buffer = all.Skip(length + 7).ToArray();

            Connection.Memory = new MemoryStream();
            Connection.Memory.Write(buffer, 0, buffer.Length);

            if (buffer.Length >= 7) OnReceive();
            return 0;
        }

        private int ReadNewMessage(int type, int length, int version, byte[] payload)
        {
            
            byte[] decrypted = RC4.Decrypt(payload);
            payload = decrypted;

            GameMessage message = MessageFactory.CreateMessageByType(type);
            if (message != null)
            {
                message.GetByteStream().SetByteArray(payload, payload.Length);
                message.Decode();
                Processor.Receive(Connection, message);
            }
            else
            {
                Logger.Print("Ignoring message of unknown type " + type);
            }

            return 0;
        }
    }
}
