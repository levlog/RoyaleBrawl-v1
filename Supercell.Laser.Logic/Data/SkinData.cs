namespace Supercell.Laser.Logic.Data
{
    public class SkinData : LogicData
    {
        public SkinData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }

        public string Name { get; set; }
        public string Character { get; set; }
        public string PetSkin { get; set; }


        public int CostGems { get; set; }

       
    }
}
