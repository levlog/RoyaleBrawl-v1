namespace Supercell.Laser.Logic.Battle.Component
{
    using Supercell.Laser.Logic.Battle.Objects;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Util;

    public class Buff
    {
        public enum BuffTypes
        {
            Damage = 1,
            DamageAndSize = 2,
            SpeedSlower = 3,
            SpeedFaster = 4,
            Damage2 = 5,
            HealthRegen = 10,
        }
        public int Type;
        public int Duration;
        public int dword8;
        public int Modifier;
        public int SizeIncrease;
        public int SourceIndex;
        public int EffectType;
        public int NormalDMG;
        public Buff(int Type, int Dura, int Modi, int SizeInc)
        {
            this.Type = Type;
            this.Duration = Dura;
            this.dword8 = Dura;
            this.Modifier = Modi;
            SizeIncrease = SizeInc;
        }
        public bool Tick(Character a2)
        {
            Duration--;
            if (Duration < 1)
            {
                OnBuffEnd(a2);
                return true;
            }
            return false;
        }
        public bool CanStack()
        {
            return Type == 10;
        }
        public void OnBuffEnd(Character Owner)
        {
            ;
        }
    }
}
