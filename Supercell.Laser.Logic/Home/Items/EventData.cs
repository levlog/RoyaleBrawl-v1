namespace Supercell.Laser.Logic.Home.Items
{
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Helper;
    using Supercell.Laser.Titan.DataStream;

    public class EventData
    {
        public int Slot;
        public int LocationId;
        public DateTime EndTime;
        public LocationData Location => DataTables.Get(DataType.Location).GetDataByGlobalId<LocationData>(LocationId);

        public void Encode(ByteStream encoder)
        {
            encoder.WriteVInt(Slot);
            Console.WriteLine(Slot);
            encoder.WriteVInt(Slot);
            encoder.WriteVInt(0);
            encoder.WriteVInt((int)(EndTime - DateTime.Now).TotalSeconds);
            encoder.WriteVInt(10);
            encoder.WriteVInt(10);
            encoder.WriteVInt(60);
            encoder.WriteBoolean(true);
            encoder.WriteBoolean(true);
            ByteStreamHelper.WriteDataReference(encoder, Location);
            encoder.WriteVInt(0); // 0xacecc0
            encoder.WriteVInt(2); // 0xacec7c
            encoder.WriteString("Brawl v1.1714 server by lev.l"); // 0xacecac
        }
    }
}
