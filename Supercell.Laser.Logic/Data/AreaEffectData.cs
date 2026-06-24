namespace Supercell.Laser.Logic.Data
{
    public class AreaEffectData : LogicData
    {
        public AreaEffectData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }

        public string Name { get; set; }

        public string FileName { get; set; }

        public string BlueExportName { get; set; }

        public string RedExportName { get; set; }


        public string Layer { get; set; }

        public string Effect { get; set; }

        public int Scale { get; set; }

        public int TimeMs { get; set; }

        public int Radius { get; set; }

        public int Damage { get; set; }

        public int CustomValue { get; set; }

        public string Type { get; set; }

        public string BulletExplosionBullet { get; set; }

        public int BulletExplosionBulletDistance { get; set; }

        public bool DestroysEnvironment { get; set; }

        public int PushbackStrength { get; set; }

        public int PushbackStrengthSelf { get; set; }

        public int FreezeStrength { get; set; }

        public int GetAreaEffectType()
        {
            string v1 = Type;
            int v2 = -1;
            if (v1 == "Damage")
            {
                v2 = 0;
            }
            else if (v1 == "SmokeScreen")
            {
                v2 = 1;
            }
            else if (v1 == "Dot")
            {
                v2 = 2;
            }
            else if (v1 == "Heal")
            {
                v2 = 3;
            }
            else if (v1 == "Hot")
            {
                v2 = 4;
            }
            else if (v1 == "BulletExplosion")
            {
                v2 = 5;
            }
            else if (v1 == "Effect")
            {
                v2 = 6;
            }
            else
            {
                Console.WriteLine("Area Effect has invalid type!");
            }
            return v2;
        }
    }
}
