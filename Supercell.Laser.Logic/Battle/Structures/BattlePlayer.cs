namespace Supercell.Laser.Logic.Battle.Structures
{
    using Newtonsoft.Json;
    using Supercell.Laser.Logic.Avatar;
    using Supercell.Laser.Logic.Avatar.Structures;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Data.Helper;
    using Supercell.Laser.Logic.Helper;
    using Supercell.Laser.Logic.Home;
    using Supercell.Laser.Logic.Home.Structures;
    using Supercell.Laser.Logic.Listener;
    using Supercell.Laser.Titan.DataStream;
    using Supercell.Laser.Titan.Math;
    using Supercell.Laser.Titan.Util;

    public class BattlePlayer
    {
        public long AccountId;
        public int PlayerIndex;
        public int TeamIndex;

        public long TeamId = -1;

        public PlayerDisplayData DisplayData;
        public int CharacterId;
        public int SkinId;

        public long SessionId;
        public LogicGameListener GameListener;

        public int OwnObjectId;
        public int LastHandledInput;

        public int Trophies, HighestTrophies;

        public int HeroPowerLevel;

        private int Score;
        private LogicVector2 SpawnPoint;

        private int StartUsingPinTicks;
        private int PinIndex;

        private bool Bot;

        private int UltiCharge;

        public bool IsAlive;
        public int BattleRoyaleRank;

        public List<PlayerKillEntry> KillList;

        public int Kills;
        public int Damage;
        public int Heals;
        public BattlePlayer()
        {
            DisplayData = new PlayerDisplayData();
            SpawnPoint = new LogicVector2();

            StartUsingPinTicks = -9999;

            HeroPowerLevel = 0;
            BattleRoyaleRank = -1;
            IsAlive = true;

            KillList = new List<PlayerKillEntry>();
        }

        public void Healed(int heals)
        {
            Heals += heals;
        }

        public void DamageDealed(int damage)
        {
            Damage += damage;
        }

        public void KilledPlayer(int index, int bountyStars)
        {
            KillList.Add(new PlayerKillEntry()
            {
                PlayerIndex = index,
                BountyStarsEarned = bountyStars
            });

            Kills++;
        }

        public bool HasUlti()
        {
            return UltiCharge >= 1000;
        }

        public int GetUltiCharge()
        {
            return UltiCharge;
        }

        public void AddUltiCharge(int amount)
        {
            UltiCharge = LogicMath.Min(1000, UltiCharge + amount);
        }
        public void ChargeUlti(CharacterData CharacterData, int a2)
        {
            int v5; // w20
            int v6; // w8
            int result;
            if (CharacterData != null)
            {
                result = CharacterData.UltiChargeDiv;
                if (result != 0)
                {
                    v5 = CharacterData.UltiChargeMul * a2;
                    result = CharacterData.UltiChargeDiv;
                    v6 = UltiCharge + v5 / (int)result;
                    if (v6 > 1000)
                        v6 = 1000;
                    UltiCharge = v6;
                }
            }
        }

        public void UseUlti()
        {
            UltiCharge = 0;
        }

        public bool IsBot()
        {
            return Bot;
        }

        public BattlePlayer(ClientHome home, ClientAvatar avatar) : this()
        {
            Home = home;
            Avatar = avatar;
        }

        public void AddScore(int a)
        {
            Score += a;
        }

        public void ResetScore()
        {
            Score = 0;
        }

        public void UsePin(int index, int ticks)
        {
            StartUsingPinTicks = ticks;
            PinIndex = index;
        }

        public bool IsUsingPin(int ticks)
        {
            return ticks - StartUsingPinTicks < 80;
        }

        public int GetPinIndex()
        {
            return PinIndex;
        }

        public int GetPinUseCooldown(int ticks)
        {
            return StartUsingPinTicks + 100;
        }

        public int GetScore()
        {
            return Score;
        }

        public void SetSpawnPoint(int x, int y)
        {
            SpawnPoint.Set(x, y);
        }

        public LogicVector2 GetSpawnPoint()
        {
            return SpawnPoint.Clone();
        }

        public void Encode(ByteStream stream)
        {
            stream.WriteLong(AccountId);
            stream.WriteString(DisplayData.Name);
            stream.WriteVInt(PlayerIndex);
            stream.WriteVInt(TeamIndex);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteInt(0);

            ByteStreamHelper.WriteDataReference(stream, CharacterId);
            ByteStreamHelper.WriteDataReference(stream, null);
            stream.WriteInt(0);
        }

        [JsonIgnore] public readonly ClientHome Home;
        [JsonIgnore] public readonly ClientAvatar Avatar;

        public static BattlePlayer Create(ClientHome home, ClientAvatar avatar, int playerIndex, int teamIndex)
        {
            BattlePlayer player = new BattlePlayer(home, avatar);
            player.DisplayData.Name = avatar.Name;
            player.DisplayData.ThumbnailId = home.ThumbnailId;
            player.AccountId = avatar.AccountId;
            player.CharacterId = home.CharacterId;
            player.SkinId = 0;
            player.PlayerIndex = playerIndex;
            player.TeamIndex = teamIndex;

            Hero hero = avatar.GetHero(home.CharacterId);
            player.Trophies = hero.Trophies;
            player.HighestTrophies = hero.HighestTrophies;

            return player;
        }

        public static BattlePlayer CreateBotInfo(string name, int playerIndex, int teamIndex, int character = 16000000)
        {
            BattlePlayer player = new BattlePlayer();
            player.DisplayData.Name = name;
            player.DisplayData.ThumbnailId = GlobalId.CreateGlobalId(28, 0);
            player.AccountId = 100000 + playerIndex;
            player.CharacterId = character;
            player.PlayerIndex = playerIndex;
            player.TeamIndex = teamIndex;
            player.SessionId = -1;
            player.Bot = true;

            return player;
        }
    }
}
