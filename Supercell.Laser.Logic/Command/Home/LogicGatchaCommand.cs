namespace Supercell.Laser.Logic.Command.Home
{
    using Supercell.Laser.Logic.Home;
    using Supercell.Laser.Logic.Home.Gatcha;
    using Supercell.Laser.Logic.Message.Home;
    using Supercell.Laser.Titan.DataStream;

    public class LogicGatchaCommand : Command
    {
        public bool isGold;

        public LogicGatchaCommand() : base()
        {
            ;
        }

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            isGold = stream.ReadBoolean();
        }

        public override int Execute(HomeMode homeMode)
        {
            if (isGold) if (!homeMode.Avatar.UseGold(100)) return -1;
            if (!isGold) if (!homeMode.Avatar.UseDiamonds(0)) return -1;
            
        
            LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
            DeliveryUnit unit = new DeliveryUnit(10);
            homeMode.SimulateGatcha(unit);
            command.DeliveryUnits.Add(unit);
            command.Execute(homeMode);

            AvailableServerCommandMessage message = new AvailableServerCommandMessage();
            message.Command = command;
            homeMode.GameListener.SendMessage(message);

            return 0;
        }

        public override int GetCommandType()
        {
            return 500;
        }
    }
}
