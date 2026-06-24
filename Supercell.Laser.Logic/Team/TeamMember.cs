namespace Supercell.Laser.Logic.Team
{
    using Supercell.Laser.Logic.Avatar.Structures;
    using Supercell.Laser.Logic.Helper;
    using Supercell.Laser.Titan.DataStream;

    public class TeamMember
    {
        public bool IsOwner;
        public long AccountId;

        public int CharacterId;
        public int SkinId;

        public int HeroTrophies;
        public int HeroHighestTrophies;
        public int HeroLevel;

        public int State;
        public bool IsReady;

        public PlayerDisplayData DisplayData;

        public void Encode(ByteStream stream)
        {
            stream.WriteLong(AccountId);
            stream.WriteString(DisplayData.Name);
            stream.WriteVInt(0); //unused player level
            ByteStreamHelper.WriteDataReference(stream, CharacterId);
            ByteStreamHelper.WriteDataReference(stream, SkinId);
            
            stream.WriteVInt(HeroTrophies);
            stream.WriteVInt(HeroHighestTrophies);
            stream.WriteVInt(0);//todo level
            stream.WriteVInt(State);
            stream.WriteVInt(1);
            stream.WriteBoolean(IsReady);

            stream.WriteVInt(1); // team
        }
    }
}
