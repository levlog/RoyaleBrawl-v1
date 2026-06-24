namespace Supercell.Laser.Logic.Util
{
    using Supercell.Laser.Logic.Battle;
    using Supercell.Laser.Titan.Debug;

    public static class GameModeUtil
    {
        public static bool PlayersCollectPowerCubes(int variation)
        {
            int v1 = variation - 6;
            if (v1 <= 8)
                return ((0x119 >> v1) & 1) != 0;
            else
                return false;
        }
        public static int GetBattleTicks(int a1, BattleMode LogicBattleModeServer)
        {
            int v2; // w8
            int v3; // x8

            if ( LogicBattleModeServer.m_ART_TEST )
                return 99999999;
            v2 = a1;
            if ( v2 != 6)
                v3 = LogicBattleModeServer.m_NORMAL_TICKS;
            else
                v3 = LogicBattleModeServer.m_SHOWDOWN_TICKS;
            return v3;
        }

        public static int GetRespawnSeconds(int variation)
        {
            switch (variation)
            {
                case 0:
                case 2:
                    return 3;
                case 3:
                    return 1;
                default:
                    return 5;
            }
        }

        public static bool PlayersCollectBountyStars(int variation)
        {
            return variation == 3;
        }

        public static bool HasTwoTeams(int variation)
        {
            return variation != 6;
        }

        public static int GetGameModeVariation(string mode)
        {
            switch (mode)
            {
                case "CoinRush":
                    return 0;
                case "Campaign":
                    Console.WriteLine("[WARNING] Campaign only works on DEV BUILD");
                    return 1;
                case "AttackDefend":
                    return 2;
                case "BountyHunter":
                    return 3;
                case "Artifact":
                    return 4;
                case "LaserBall":
                    return 5;
                case "BattleRoyale":
                    return 6;
                default:
                    Debugger.Error("Wrong game mode!");
                    return -1;
            }
        }
    }
}
