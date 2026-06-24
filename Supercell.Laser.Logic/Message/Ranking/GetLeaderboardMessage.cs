using Supercell.Laser.Logic.Helper;

namespace Supercell.Laser.Logic.Message.Ranking
{
    public class GetLeaderboardMessage : GameMessage
    {
        public bool IsRegional { get; set; }
        public int LeaderboardType { get; set; }
        public int TargetBrawler { get; set; }
        public override void Decode()
        {
            base.Decode();

            IsRegional = Stream.ReadBoolean();
            LeaderboardType = Stream.ReadVInt();
            TargetBrawler = ByteStreamHelper.ReadDataReference(Stream);
        }

        public override int GetMessageType()
        {
            return 14403;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
