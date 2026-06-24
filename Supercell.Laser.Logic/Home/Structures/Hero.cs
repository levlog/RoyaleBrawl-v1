namespace Supercell.Laser.Logic.Home.Structures
{
    using Newtonsoft.Json;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Helper;
    using Supercell.Laser.Titan.DataStream;
    using Supercell.Laser.Titan.Math;

    [JsonObject(MemberSerialization.OptIn)]
    public class Hero
    {
        [JsonProperty] public int CharacterId;
        [JsonProperty] public int CardId;

        [JsonProperty] public int Trophies;
        [JsonProperty] public int HighestTrophies;
        [JsonProperty] public int[] Upgrades;
        public CharacterData CharacterData => DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(CharacterId);
        public CardData CardData => DataTables.Get(DataType.Card).GetDataByGlobalId<CardData>(CardId);

        public Hero(int characterId, int cardId)
        {
            CharacterId = characterId;
            CardId = cardId;
        }

        public void AddTrophies(int trophies)
        {
            Trophies += trophies;
            HighestTrophies = LogicMath.Max(HighestTrophies, Trophies);
        }

        public void Encode(ByteStream stream)
        {
            ByteStreamHelper.WriteDataReference(stream, CharacterData);
            ByteStreamHelper.WriteDataReference(stream, null);
            stream.WriteVInt(Trophies);
            stream.WriteVInt(HighestTrophies);
            stream.WriteVInt(0); //todo the painful upgrade system
        }
    }

    internal class Upgrades
    {
    }
}
