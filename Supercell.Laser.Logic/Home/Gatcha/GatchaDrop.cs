namespace Supercell.Laser.Logic.Home.Gatcha
{
    using Supercell.Laser.Logic.Avatar;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Helper;
    using Supercell.Laser.Logic.Home.Structures;
    using Supercell.Laser.Titan.DataStream;

    public class GatchaDrop
    {
        public int Count;
        public int rarity;
        public int DataGlobalId;
        public int PinGlobalId;
        public int Type;

        public GatchaDrop(int type, int Rarity)
        {
            Type = type;
            rarity = Rarity;
        }

        public void DoDrop(HomeMode homeMode)
        {
            ClientAvatar avatar = homeMode.Avatar;
            System.Console.WriteLine($"[DEBUG] DoDrop: Type={Type}, Count={Count}");

            switch (Type)
            {
                case 1: // Unlock a hero
                    CardData cardData = DataTables.Get(DataType.Card).GetDataByGlobalId<CardData>(DataGlobalId);
                    if (cardData == null) return;

                    CharacterData characterData = DataTables.Get(DataType.Character).GetData<CharacterData>(cardData.Target);
                    if (characterData == null) return;

                    avatar.UnlockHero(characterData.GetGlobalId(), cardData.GetGlobalId());
                    break;
                case 5: // Add chips
                    avatar.AddChips(Count);
                    break;
                case 6: // Add elexir
                    avatar.AddElexir(Count);
                    break;
                case 7: // Add gold
                    avatar.AddGold(Count);
                    break;
                case 8: // Add Gems (Bonus)
                    avatar.AddDiamonds(Count);
                    break;
            }
        }

        public void Encode(ByteStream stream)
        {
            if (Type == 1 || Type == 5) // Unlock or Duplicate Brawler
            {
                stream.WriteVInt(0);
                stream.WriteVInt(1);
                ByteStreamHelper.WriteDataReference(stream, DataGlobalId);
            }
            else // Resources (Elixir)
            {
                stream.WriteVInt(5); // Class 5 (Resource)
                stream.WriteVInt(6); // Instance 6 (Elixir)
                stream.WriteVInt(Count); // Elixir count
                stream.WriteVInt(0); // null secondRef
            }
        }
    }
}
