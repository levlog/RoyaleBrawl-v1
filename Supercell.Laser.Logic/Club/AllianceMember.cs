namespace Supercell.Laser.Logic.Club
{
    using Newtonsoft.Json;
    using Supercell.Laser.Logic.Avatar;
    using Supercell.Laser.Logic.Avatar.Structures;
    using Supercell.Laser.Logic.Data.Helper;
    using Supercell.Laser.Logic.Helper;
    using Supercell.Laser.Logic.Listener;
    using Supercell.Laser.Titan.DataStream;

    public class AllianceMember
    {
        [JsonProperty("displayData")] public PlayerDisplayData DisplayData { get; set; }
        [JsonProperty("accountId")] public long AccountId { get; set; }
        [JsonProperty("trophies")] public int Trophies { get; set; }
        [JsonProperty("role")] public AllianceRole Role { get; set; }

        [JsonIgnore] public ClientAvatar Avatar
        {
            get
            {
                return LogicServerListener.Instance.GetAvatar(AccountId);
            }
        }

        [JsonIgnore]
        public bool IsOnline
        {
            get
            {
                return LogicServerListener.Instance.IsPlayerOnline(AccountId);
            }
        }

        public AllianceMember()
        {
            // For json...
        }

        public AllianceMember(ClientAvatar avatar)
        {
            DisplayData = new PlayerDisplayData(avatar.HomeMode.Home.ThumbnailId, avatar.Name);
            AccountId = avatar.AccountId;
            Trophies = avatar.Trophies;
            Role = avatar.AllianceRole;
        }

        public void Encode(ByteStream stream)
        {
            ClientAvatar avatar = Avatar;

            stream.WriteLong(AccountId);
            stream.WriteString(DisplayData.Name);
            stream.WriteVInt((int)Role);
            stream.WriteVInt(0); // exp level
            stream.WriteVInt(avatar.Trophies);
            ByteStreamHelper.WriteDataReference(stream, DisplayData.ThumbnailId);
        }
    }
}
