namespace Supercell.Laser.Logic.Home
{
    using Supercell.Laser.Logic.Avatar;
    using Supercell.Laser.Logic.Command;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Data.Helper;
    using Supercell.Laser.Logic.Home.Gatcha;
    using Supercell.Laser.Logic.Home.Items;
    using Supercell.Laser.Logic.Home.Structures;
    using Supercell.Laser.Logic.Listener;
    using Supercell.Laser.Logic.Message.Home;
    using Supercell.Laser.Titan.Math;

    public class HomeMode
    {
        public const int UNLOCKABLE_HEROES_COUNT = 38;

        public readonly LogicGameListener GameListener;

        public ClientHome Home;
        public ClientAvatar Avatar;

        public Action<int> CharacterChanged;

        public HomeMode(ClientHome home, ClientAvatar avatar, LogicGameListener gameListener)
        {
            Home = home;
            Avatar = avatar;

            Home.HomeMode = this;
            Avatar.HomeMode = this;

            GameListener = gameListener;
        }

        public static HomeMode LoadHomeState(LogicGameListener gameListener, ClientHome home, ClientAvatar avatar, EventData[] events)
        {
            home.Events = events;

            HomeMode homeMode = new HomeMode(home, avatar, gameListener);
            homeMode.Enter(DateTime.UtcNow);

            return homeMode;
        }

        private bool GetRandomBrawlerForGatcha(Random rand, DeliveryUnit unit)
        {
            int brawlersCount = UNLOCKABLE_HEROES_COUNT;
            int brawlerId = -1;

            bool done = false;
            int attempts = 0;
            int rarity = 0;
            CardData card = null;
            while (!done && attempts < 25)
            {
                attempts++;
                brawlerId = GlobalId.CreateGlobalId(16, rand.Next(0, brawlersCount));

                CharacterData data = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(brawlerId);
                if (data == null)
                {
                    done = false;
                    continue;
                }

                done = !data.Disabled;

                if (done)
                {
                    card = DataTables.Get(DataType.Card).GetData<CardData>(data.Name + "_unlock");
                    if (card == null)
                    {
                        done = false;
                        continue;
                    }

                    if (card.Rarity == "common")
                    {
                        rarity = 0;
                    }
                    else if (card.Rarity == "rare")
                    {
                        rarity = 1;
                    }
                    else if (card.Rarity == "epic")
                    {
                        rarity = 2;
                    }
                    else if (card.Rarity == "legendary")
                    {
                        rarity = 3;
                    }
                }

            }

            if (done && !Avatar.HasHero(brawlerId))
            {
                GatchaDrop drop = new GatchaDrop(1, rarity);
                drop.DataGlobalId = card.GetGlobalId();
                drop.Count = 1;
                unit.AddDrop(drop);
            }
            else if (done && Avatar.HasHero(brawlerId))
            {
                GatchaDrop drop = new GatchaDrop(5, rarity);
                drop.DataGlobalId = card.GetGlobalId();
                if (rarity == 0)
                {
                    drop.Count = 1;
                }
                else if (rarity == 1)
                {
                    drop.Count = 2;
                }
                else if (rarity == 2)
                {
                    drop.Count = 10;
                }
                else if (rarity == 3)
                {
                    drop.Count = 60;
                }
                unit.AddDrop(drop);
            }
            return done;
        }

            
        

        public void SimulateGatcha(DeliveryUnit unit)
        {
            Random rand = new Random();

            int roll = rand.Next(0, 100);
            if (roll < 25) // 25% chance to roll a Brawler or duplicate (Chips)
            {
                if (GetRandomBrawlerForGatcha(rand, unit))
                {
                    return;
                }
            }

            // 75% chance for Elixir (or fallback if brawler roll failed)
            int elixirRarity = rand.Next(0, 100);
            int rarity = 0;
            int elixirCount = 1;

            if (elixirRarity < 60) // 60% Common: 1 Elixir
            {
                rarity = 0;
                elixirCount = 1;
            }
            else if (elixirRarity >= 60 && elixirRarity < 85) // 25% Rare: 2 Elixir
            {
                rarity = 1;
                elixirCount = 2;
            }
            else if (elixirRarity >= 85 && elixirRarity < 97) // 12% Epic: 5 Elixir
            {
                rarity = 2;
                elixirCount = 5;
            }
            else // 3% Legendary: 10 Elixir
            {
                rarity = 3;
                elixirCount = 10;
            }

            GatchaDrop elixirDrop = new GatchaDrop(6, rarity);
            elixirDrop.Count = elixirCount;
            unit.AddDrop(elixirDrop);
        }

        public void Enter(DateTime dateTime)
        {
            Home.HomeVisited();
        }

        public void ClientTurnReceived(int tick, int checksum, List<Command> commands)
        {
            foreach (Command command in commands)
            {
                if (command.Execute(this) != 0)
                {
                    OutOfSyncMessage outOfSync = new OutOfSyncMessage();
                    GameListener.SendMessage(outOfSync);
                }
            }
            Home.Tick();
        }
    }
}
