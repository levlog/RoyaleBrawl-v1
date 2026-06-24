namespace Supercell.Laser.Logic.Data
{
    public class CardData : LogicData
    {
        public CardData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }

        public string Name { get; set; }

        public string IconSWF { get; set; }

        public string IconExportName { get; set; }

        public string Target { get; set; }

        public string RequiresCard { get; set; }

        public string Type { get; set; }

        public string Skill { get; set; }

        public int Value { get; set; }

        public int Value2 { get; set; }

        public string Rarity { get; set; }

        public string TID { get; set; }

        public string PowerNumberTID { get; set; }

        public string PowerNumber2TID { get; set; }

        public string PowerIcon1ExportName { get; set; }

        public string PowerIcon2ExportName { get; set; }
    }
}
