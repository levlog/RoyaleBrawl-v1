namespace Supercell.Laser.Logic.Home.Gatcha
{
    using Supercell.Laser.Titan.DataStream;

    public class DeliveryUnit
    {
        public readonly int Type;
        private List<GatchaDrop> Drops;

        public DeliveryUnit(int type)
        {
            Type = type;
            Drops = new List<GatchaDrop>();
        }

        public void AddDrop(GatchaDrop drop)
        {
            if (drop != null)
            {
                Drops.Add(drop);
            }
        }

        public GatchaDrop[] GetDrops()
        {
            return Drops.ToArray();
        }

        public void Encode(ByteStream stream)
        {
            GatchaDrop dropInfo = null;
            foreach (GatchaDrop drop in Drops)
            {
                dropInfo = drop;
            }
            stream.WriteVInt(dropInfo != null ? dropInfo.rarity : 0);
            if (dropInfo != null)
            {
                dropInfo.Encode(stream);
            }
        }
    }
}
