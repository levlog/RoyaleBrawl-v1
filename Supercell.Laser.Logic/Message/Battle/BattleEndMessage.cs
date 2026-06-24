namespace Supercell.Laser.Logic.Message.Battle
{
    using Supercell.Laser.Logic.Battle.Structures;
    using Supercell.Laser.Logic.Helper;
    using Supercell.Laser.Logic.Home;
    using Supercell.Laser.Logic.Home.Items;

    public class BattleEndMessage : GameMessage
    {
        public BattleEndMessage() : base()
        {
        }
        public Milestones milestones  = new();
        public HomeMode HomeMode;
        public int Result;
        public int TokensReward;
        public int TrophiesReward;
        public List<BattlePlayer> Players;
        public BattlePlayer OwnPlayer;
        public bool StarToken;

        public int GameMode;

        public bool IsPvP;

        public override void Encode()
        {
            Stream.WriteVInt(GameMode); // game mode
            Stream.WriteVInt(0);

            Stream.WriteVInt(TokensReward); // tokens reward
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteBoolean(false);
            Stream.WriteVInt(Result);
            Stream.WriteVInt(TrophiesReward);
            ByteStreamHelper.WriteDataReference(Stream, OwnPlayer.DisplayData.ThumbnailId); //HomeMode.Home.Thumbnail); todo fuck C#
            Stream.WriteBoolean(false);
            Stream.WriteBoolean(IsPvP);
            Stream.WriteVInt(50);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);

            Stream.WriteVInt(Players.Count);
            foreach (BattlePlayer player in Players)
            {
                Stream.WriteStringReference(player.DisplayData.Name);
                Stream.WriteBoolean(player.AccountId == OwnPlayer.AccountId); // is own player
                Stream.WriteBoolean(player.TeamIndex != OwnPlayer.TeamIndex); // is enemy
                Stream.WriteBoolean(false); // Star player

                ByteStreamHelper.WriteDataReference(Stream, player.CharacterId);
                ByteStreamHelper.WriteDataReference(Stream, player.SkinId); // skin

                Stream.WriteVInt(player.Trophies); // trophies
            }

            Stream.WriteVInt(2);
            {
                Stream.WriteVInt(0);
                Stream.WriteVInt(0); //exp
                Stream.WriteVInt(8);
                Stream.WriteVInt(0); //star player exp
            }
            Stream.WriteVInt(0); // milestones grah

            Stream.WriteVInt(2);
            {
                Stream.WriteVInt(1);
                Stream.WriteVInt(OwnPlayer.Trophies); // Trophies
                Stream.WriteVInt(OwnPlayer.HighestTrophies); // Highest Trophies

                Stream.WriteVInt(5);
                Stream.WriteVInt(100);
                Stream.WriteVInt(100);
            }

            Stream.WriteBoolean(true);

            milestones.WriteMilestones(Stream);
        }

        public override int GetMessageType()
        {
            return 23456;
        }

        public override int GetServiceNodeType()
        {
            return 27;
        }
    }
}
