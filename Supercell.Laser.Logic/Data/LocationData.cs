namespace Supercell.Laser.Logic.Data
{
    public class LocationData : LogicData
    {
        public LocationData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }

        public string Name { get; set; }

        public string TID { get; set; }

        public string TileSetPrefix { get; set; }

        public string GameMode { get; set; }

        public string AllowedMaps { get; set; }

        public string Music { get; set; }

    }
}
