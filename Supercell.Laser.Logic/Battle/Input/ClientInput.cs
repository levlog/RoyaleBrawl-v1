namespace Supercell.Laser.Logic.Battle.Input
{
    using Supercell.Laser.Titan.DataStream;

    public class ClientInput
    {
        public ClientInput()
        {
            ;
        }

        public int Index;
        public int Type;

        public int X, Y;

        public bool AutoAttack;
        public int AutoAttackTarget; // global id

        public long OwnerSessionId;

        public void Decode(ByteStream Stream)
        {
            Index = Stream.ReadVInt();
            Stream.ReadVInt(); // always 1 lol
            Type = Stream.ReadVInt();

            X = Stream.ReadVInt();
            Y = Stream.ReadVInt();
        }
    }
}
