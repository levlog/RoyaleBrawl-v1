namespace Supercell.Laser.Logic.Message.Home
{
    public class MatchMakingStatusMessage : GameMessage
    {
        public int Seconds;
        public int Found;
        public int Max;
        public bool ShowTips;
        public bool Status; 

        public override void Encode()
        {
            Stream.WriteInt(Seconds);
            Stream.WriteInt(Found);
            for (int i = 0; i < Found; i++)
            {
                Stream.WriteString("dudka");
                Stream.WriteBoolean(Status); // todo leave matchmaking
                Stream.WriteLong(i);
            }
            Stream.WriteInt(Max);
        }

        public override int GetMessageType()
        {
            return 20405;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
