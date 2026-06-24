namespace Supercell.Laser.Logic.Command.Home
{
    using Supercell.Laser.Logic.Home;
    using Supercell.Laser.Logic.Home.Gatcha;
    using Supercell.Laser.Logic.Message.Home;
    using Supercell.Laser.Titan.DataStream;

    public class LogicOpenBoxCommand : Command
    {
        public int BoxID { get; set; }
        public int BoxCount { get; set; }

        public LogicOpenBoxCommand() : base()
        {
        }

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            try
            {
                BoxID = stream.ReadVInt();
                BoxCount = stream.ReadVInt();
            }
            catch (System.Exception)
            {
                // ignore
            }
            System.Console.WriteLine($"[LOGIC] command 203! BoxID: {BoxID}, BoxCount: {BoxCount}");
        }

        public override int Execute(HomeMode homeMode)
        {
            System.Console.WriteLine($"[LOGIC] command 203! ID: {BoxID}");
            return 0;
        }

        public override int GetCommandType()
        {
            return 203;
        }
    }
}