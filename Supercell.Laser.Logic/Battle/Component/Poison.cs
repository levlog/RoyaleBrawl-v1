namespace Supercell.Laser.Logic.Battle.Component
{
    using Supercell.Laser.Logic.Battle.Objects;

    public class Poison
    {
        private Character m_source;
        private int m_timer;
        private int m_damage;
        private int m_tickCount;

        public Poison(Character source, int damage, int tickCount)
        {
            m_timer = 20;
            m_source = source;
            m_damage = damage / tickCount;
            m_tickCount = tickCount;
        }

        public void RefreshPoison(Character source, int damage, int tickCount)
        {
            m_tickCount = tickCount;
            m_source = source;
            m_damage = damage / tickCount;
        }

        public bool Tick(Character character)
        {
            int v3 = m_timer;
            m_timer = v3 - 1;
            if (v3 > 1)
                return false;

            bool v4 = true;
            character.CauseDamage(m_source, m_damage, true);

            int v5 = m_tickCount;
            m_timer = 20;
            m_tickCount = v5 - 1;
            if (v5 >= 2)
                v4 = false;
            return v4;
        }

        public void Destruct()
        {
            m_timer = -1;
            m_damage = 0;
            m_tickCount = 0;

            m_source = null;
        }

        public static int GetTickCount(int a1)
        {
            int v1 = 4;
            if (a1 == 4)
                v1 = 2;

            return v1;
        }
    }
}
