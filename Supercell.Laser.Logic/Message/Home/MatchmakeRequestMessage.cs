using Supercell.Laser.Logic.Helper;

namespace Supercell.Laser.Logic.Message.Home
{
    public class MatchmakeRequestMessage : GameMessage
    {
        public MatchmakeRequestMessage() : base()
        {
            ;
        }

        public int CharacterInstanceId;
        public int SelectedCard;
        public int EventSlot;

        public override void Decode()
        {
            Stream.ReadVInt();
            SelectedCard = ByteStreamHelper.ReadDataReference(Stream);
            EventSlot = Stream.ReadVInt();
            Stream.ReadVInt();
            //if (EventSlot > 4) EventSlot = 1;
        }

        public override int GetMessageType()
        {
            return 14103;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
