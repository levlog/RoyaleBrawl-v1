namespace Supercell.Laser.Logic.Util
{
    public static class GamePlayUtil
    {
        public static bool IsJumpCharge(int chargeType)
        {
            uint v1 = (uint)(chargeType - 2);
            if (v1 <= 9)
                return ((0x293u >> (int)v1) & 1) != 0;
            else
                return false;
        }

        public static int GetPlayerCountWithGameModeVariation(int gameMode)
        {
            switch (gameMode)
            {
                case 6:
                    return 10;
                default:
                    return 6;
            }
        }
    }
}
