namespace Supercell.Laser.Logic.Command.Home
{
    using Supercell.Laser.Logic.Helper;
    using Supercell.Laser.Logic.Home;
    using Supercell.Laser.Titan.DataStream;

    public class LogicDebugCommand : Command
    {
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            stream.ReadVInt(); // debug commmand
            stream.ReadVInt(); // param
            stream.ReadString(); // pc build input
            ByteStreamHelper.ReadDataReference(stream); // target
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 1000;
        }
    }
}
