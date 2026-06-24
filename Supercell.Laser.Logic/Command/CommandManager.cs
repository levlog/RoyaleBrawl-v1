namespace Supercell.Laser.Logic.Command
{
    using Supercell.Laser.Logic.Command.Home;
    using Supercell.Laser.Titan.DataStream;
    using Supercell.Laser.Titan.Debug;

    public class CommandManager
    {
        private static Dictionary<int, Type> CommandTypes;

        static CommandManager()
        {
            CommandTypes = new Dictionary<int, Type>()
            {
                {203, typeof(LogicOpenBoxCommand)},
                {500, typeof(LogicGatchaCommand)},
                {506, typeof(LogicSetPlayerThumbnailCommand)},
                {507, typeof(LogicSelectSkinCommand)},
                {1000, typeof(LogicDebugCommand)}
            };
        }

        public static Command DecodeCommand(ByteStream stream)
        {
            int type = stream.ReadVInt();
            Command command = CommandManager.CreateCommand(type);
            if (command == null)
            {
                Debugger.Warning("Command is unhandled: " + type);
                return null;
            }

            command.Decode(stream);
            return command;
        }

        public static Command CreateCommand(int type)
        {
            if (CommandTypes.ContainsKey(type))
            {
                return (Command)Activator.CreateInstance(CommandTypes[type]);
            }
            return null;
        }
    }
}
