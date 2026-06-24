namespace Supercell.Laser.Logic.Message.Battle
{
    using Supercell.Laser.Logic.Battle.Input;
    using Supercell.Laser.Logic.Message;
    using Supercell.Laser.Titan.DataStream;

    public class ClientInputMessage : GameMessage
    {
        public Queue<ClientInput> Inputs;

        public ClientInputMessage() : base()
        {
            Inputs = new Queue<ClientInput>();
        }

        public override void Decode()
        {
            Stream.ReadVInt();
            Stream.ReadVInt();

            int count = Stream.ReadVInt();
            for (int i = 0; i < count; i++)
            {
                ClientInput input = new ClientInput();
                input.Decode(Stream);
                Inputs.Enqueue(input);
            }
        }

        public override int GetMessageType()
        {
            return 10555;
        }

        public override int GetServiceNodeType()
        {
            return 27;
        }
    }
}
