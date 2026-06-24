using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Supercell.Laser.Logic.Battle
{
    using Supercell.Laser.Logic.Battle.Objects;
    using Supercell.Laser.Logic.Battle.Input;
    using Supercell.Laser.Logic.Battle.Level;
    using Supercell.Laser.Logic.Battle.Level.Factory;
    using Supercell.Laser.Logic.Battle.Structures;
    using Supercell.Laser.Logic.Data;
    using Supercell.Laser.Logic.Data.Helper;
    using Supercell.Laser.Logic.Home;
    using Supercell.Laser.Logic.Home.Structures;
    using Supercell.Laser.Logic.Listener;
    using Supercell.Laser.Logic.Message.Battle;
    using Supercell.Laser.Logic.Time;
    using Supercell.Laser.Logic.Util;
    using Supercell.Laser.Titan.DataStream;
    using Supercell.Laser.Titan.Debug;
    using Supercell.Laser.Titan.Math;
    using System;
    using System.Threading;

    public struct DiedEntry
    {
        public int DeathTick;
        public BattlePlayer Player;
    }

    public class BattleMode
    {
        public bool isArtifactSpawned =false;
        public const int ORBS_TO_COLLECT_NORMAL = 0xA;

        private Timer m_updateTimer;
        public bool m_ART_TEST;
        public bool m_BATTLE_SIM;
        public int m_NORMAL_TICKS = 3600;
        public int m_SHOWDOWN_TICKS = 16000;
        public bool m_USE_BOT_DEBUG;
        private int m_locationId;
        private int m_gameModeVariation;
        private int m_playersCountWithGameModeVariation;
        private Queue<ClientInput> m_inputQueue;

        private List<BattlePlayer> m_players;
        private Dictionary<long, BattlePlayer> m_playersBySessionId;
        private Dictionary<int, BattlePlayer> m_playersByObjectGlobalId;
        private Dictionary<long, LogicGameListener> m_spectators;
        private GameObjectManager m_gameObjectManager;

        private Rect m_playArea;
        private TileMap m_tileMap;
        private GameTime m_time;
        private LogicRandom m_random;
        private int m_randomSeed;
        public Character character;
        private int m_winnerTeam;

        private Queue<DiedEntry> m_deadPlayers;
        private int m_playersAlive;
        
        public int m_gemGrabCountdown;

        public bool IsGameOver { get; set; }

        public BattleMode(int locationId)
        {
            m_winnerTeam = -1;

            m_locationId = locationId;
            m_gameModeVariation = GameModeUtil.GetGameModeVariation(Location.GameMode);
            m_playersCountWithGameModeVariation = GamePlayUtil.GetPlayerCountWithGameModeVariation(m_gameModeVariation);

            m_inputQueue = new Queue<ClientInput>();

            m_randomSeed = 0;
            m_random = new LogicRandom(m_randomSeed);

            m_players = new List<BattlePlayer>();
            m_playersBySessionId = new Dictionary<long, BattlePlayer>();
            m_playersByObjectGlobalId = new Dictionary<int, BattlePlayer>();
            m_deadPlayers = new Queue<DiedEntry>();

            m_time = new GameTime();
            m_tileMap = TileMapFactory.CreateTileMap(Location.AllowedMaps);
            m_playArea = new Rect(0, 0, m_tileMap.LogicWidth, m_tileMap.LogicHeight);
            m_gameObjectManager = new GameObjectManager(this);
            m_ART_TEST = false;
            //character.m_ART_TEST = false;
            m_spectators = new Dictionary<long, LogicGameListener>();
        }
        public BattleMode GetBattleMode()
        {
            return this;
        } 
        public int GetGemGrabCountdown()
        {
            return m_gemGrabCountdown;
        }

        public int GetPlayersAliveCountForBattleRoyale()
        {
            return m_playersAlive;
        }

        private void TickSpawnHeroes()
        {
            DiedEntry[] entries = m_deadPlayers.ToArray();
            m_deadPlayers.Clear();

            foreach (DiedEntry entry in entries)
            {
                if (GetTicksGone() - entry.DeathTick < GameModeUtil.GetRespawnSeconds(m_gameModeVariation) * 20)
                {
                    m_deadPlayers.Enqueue(entry);
                    continue;
                }

                BattlePlayer player = entry.Player;
                LogicVector2 spawnPoint = player.GetSpawnPoint();

                Character character = new Character(16, GlobalId.GetInstanceId(player.CharacterId));
                character.SetIndex(player.PlayerIndex + (16 * player.TeamIndex));
                character.SetHeroLevel(player.HeroPowerLevel);
                character.SetPosition(spawnPoint.X, spawnPoint.Y, 0);
                character.SetBot(player.IsBot());
                character.SetImmunity(60, 100);
                m_gameObjectManager.AddGameObject(character);
                player.OwnObjectId = character.GetGlobalID();
                m_playersByObjectGlobalId.Add(player.OwnObjectId, player);
            }
        }

        public void PlayerDied(BattlePlayer player)
        {
            if (m_gameModeVariation == 6)
            {
                int rank = m_playersAlive;
                m_playersAlive--;
                player.IsAlive = false;
                player.BattleRoyaleRank = rank;

                BattleEndMessage message = new BattleEndMessage();
                message.GameMode = 5;
                message.IsPvP = true;
                message.Players = new List<BattlePlayer>();
                message.Players.Add(player);
                message.OwnPlayer = player;

                if (player.Avatar == null) return;
                player.Avatar.BattleId = -1;

                if (player.GameListener == null) return;

                Hero hero = player.Avatar.GetHero(player.CharacterId);

                message.Result = rank;
                int tokensReward = 40 / rank;
                message.TokensReward = tokensReward;
                if (rank > 5)
                {
                    int trophiesReward = -(rank - 5);
                    if (hero.Trophies < -trophiesReward) trophiesReward = -hero.Trophies;
                    message.TrophiesReward = trophiesReward;

                    player.Avatar.AddChips(tokensReward);
                    player.Home.TokenReward += tokensReward;
                    hero.AddTrophies(message.TrophiesReward);
                }
                else
                {
                    int trophiesReward = (5 - rank) * 2;
                    message.TrophiesReward = trophiesReward;

                    player.Avatar.AddChips(tokensReward);
                    player.Home.TokenReward += tokensReward;
                    player.Home.TrophiesReward += trophiesReward;
                    hero.AddTrophies(message.TrophiesReward);
                }
                

                player.GameListener.SendTCPMessage(message);

                return;
            }

            if (m_gameModeVariation == 0 || m_gameModeVariation == 4)
            {
                player.ResetScore();
            }

            player.OwnObjectId = 0;

            DiedEntry entry = new DiedEntry();
            entry.Player = player;
            entry.DeathTick = GetTicksGone();

            m_deadPlayers.Enqueue(entry);
        }

        public BattlePlayer GetPlayerWithObject(int globalId)
        {
            if (m_playersByObjectGlobalId.ContainsKey(globalId))
            {
                return m_playersByObjectGlobalId[globalId];
            }
            return null;
        }

        public TileMap GetTileMap()
        {
            return m_tileMap;
        }

        public void Start()
        {
            m_updateTimer = new Timer(new TimerCallback(Update), null, 0, 1000 / 20);
        }

        public void Update(object stateInfo)
        {
            this.ExecuteOneTick();
        }

        public BattlePlayer GetPlayer(int globalId)
        {
            if (m_playersByObjectGlobalId.ContainsKey(globalId))
            {
                return m_playersByObjectGlobalId[globalId];
            }
            return null;
        }

        public void AddSpectator(long sessionId, LogicGameListener gameListener)
        {
            m_spectators.Add(sessionId, gameListener);
        }

        public void ChangePlayerSessionId(long old, long newId)
        {
            if (m_playersBySessionId.ContainsKey(old))
            {
                BattlePlayer player = m_playersBySessionId[old];
                player.LastHandledInput = 0;
                m_playersBySessionId.Remove(old);
                m_playersBySessionId.Add(newId, player);
            }
        }

        public BattlePlayer GetPlayerBySessionId(long sessionId)
        {
            if (m_playersBySessionId.ContainsKey(sessionId))
            {
                return m_playersBySessionId[sessionId];
            }
            return null;
        }

        public void AddPlayer(BattlePlayer player, long sessionId)
        {
            if (Debugger.DoAssert(player != null, "LogicBattle::AddPlayer - player is NULL!"))
            {
                player.SessionId = sessionId;
                m_players.Add(player);
                if (sessionId > 0)
                {
                    m_playersBySessionId.Add(sessionId, player);
                }
                if (player.Avatar != null)
                {
                    player.Avatar.BattleId = Id;
                    player.Avatar.TeamIndex = player.TeamIndex;
                    player.Avatar.OwnIndex = player.PlayerIndex;
                }
            }
        }

        public void AddGameObjects()
        {
            m_playersAlive = m_players.Count;

            int team1Indexer = 0;
            int team2Indexer = 0;

            foreach (BattlePlayer player in m_players)
            {
                Character character = new Character(16, GlobalId.GetInstanceId(player.CharacterId));
                character.SetIndex(player.PlayerIndex + (16 * player.TeamIndex));
                character.SetHeroLevel(player.HeroPowerLevel);
                character.SetBot(player.IsBot());
                character.SetImmunity(60, 100);

                if (GameModeUtil.HasTwoTeams(m_gameModeVariation))
                {
                    if (player.TeamIndex == 0)
                    {
                        Tile tile = m_tileMap.SpawnPointsTeam1[team1Indexer++];
                        character.SetPosition(tile.X, tile.Y, 0);
                        player.SetSpawnPoint(tile.X, tile.Y);
                    }
                    else
                    {
                        Tile tile = m_tileMap.SpawnPointsTeam2[team2Indexer++];
                        character.SetPosition(tile.X, tile.Y, 0);
                        player.SetSpawnPoint(tile.X, tile.Y);
                    }
                }
                else
                {
                    Tile tile = m_tileMap.SpawnPointsTeam1[team1Indexer++];
                    character.SetPosition(tile.X, tile.Y, 0);
                    player.SetSpawnPoint(tile.X, tile.Y);
                }

                m_gameObjectManager.AddGameObject(character);
                player.OwnObjectId = character.GetGlobalID();
                m_playersByObjectGlobalId.Add(player.OwnObjectId, player);
            }
            foreach (Tile tile in m_tileMap.LootBoxes)
            {

                if (m_gameModeVariation == 6)
                {
                    bool shouldSpawnBox = GetRandomInt(0, 120) < 60;
                    if (shouldSpawnBox)
                    {
                        CharacterData data = DataTables.Get(16).GetData<CharacterData>("LootBox");
                        Character box = new Character(16, data.GetInstanceId());
                        box.SetPosition(tile.X, tile.Y, 0);
                        box.SetIndex(10 * 16);
                        m_gameObjectManager.AddGameObject(box);
                    }
                    }
                else
                {
                    CharacterData data = DataTables.Get(16).GetData<CharacterData>("ExplodingBarrel");
                    Character box = new Character(16, data.GetInstanceId());
                    box.SetPosition(tile.X, tile.Y, 0);
                    box.SetIndex(22);
                    m_gameObjectManager.AddGameObject(box);
                }
                
            }
            foreach (Tile tile in m_tileMap.PoisonBarrels)
            {
                CharacterData data = DataTables.Get(16).GetData<CharacterData>("PoisonBarrel");
                Character box = new Character(16, data.GetInstanceId());
                box.SetPosition(tile.X, tile.Y, 0);
                box.SetIndex(22);
                m_gameObjectManager.AddGameObject(box);
                
            }
            if (m_gameModeVariation == 0 || m_gameModeVariation == 4 || m_gameModeVariation == -1)
            {
                ItemData data = DataTables.Get(18).GetData<ItemData>("OrbSpawner");
                Item item = new Item(18, data.GetInstanceId());
                item.SetPosition(3150, 4950, 0);
                item.DisableAppearAnimation();
                m_gameObjectManager.AddGameObject(item);
            }
            if (m_gameModeVariation == 2)
            {
                if (m_tileMap.Safe != null)
                {
                    CharacterData data = DataTables.Get(16).GetData<CharacterData>(m_tileMap.SafeName);
                    Character box = new Character(16, data.GetInstanceId());
                    box.SetPosition(m_tileMap.Safe.X, m_tileMap.Safe.Y, 0);
                    box.SetIndex(22); // 22 for ennemies 6 for allies todo
                    m_gameObjectManager.AddGameObject(box);
                }
                else
                {
                    Console.WriteLine("Can't find safe!");
                    return;
                }
            }
            if (m_gameModeVariation == 3)
            {
                ItemData data = DataTables.Get(18).GetData<ItemData>("Money");
                Item item = new Item(18, data.GetInstanceId());
                item.SetPosition(3150, 4950, 0);
                item.DisableAppearAnimation();
                m_gameObjectManager.AddGameObject(item);
            } 
            if (m_gameModeVariation == 5)
            {
                CharacterData brawlBall = DataTables.Get(16).GetData<CharacterData>("LaserBall");
                Character ball = new Character(16, brawlBall.GetInstanceId());
                ball.SetPosition(3150, 4950, 0);
                ball.SetIndex(0);
                m_gameObjectManager.AddGameObject(ball);
            }
            if (m_gameModeVariation == 6)
            {
                ;
            }
        }

        public void RemoveSpectator(long id)
        {
            if (m_spectators.ContainsKey(id))
            {
                m_spectators.Remove(id);
            }
        }

        public bool IsInPlayArea(int x, int y)
        {
            return m_playArea.IsInside(x, y);
        }

        public int GetTeamPlayersCount(int teamIndex)
        {
            int result = 0;
            foreach (BattlePlayer player in GetPlayers())
            {
                if (player.TeamIndex == teamIndex) result++;
            }
            return result;
        }

        public void AddClientInput(ClientInput input, long sessionId)
        {
            if (!m_playersBySessionId.ContainsKey(sessionId)) return;

            input.OwnerSessionId = sessionId;
            m_inputQueue.Enqueue(input);
        }

        public void HandleSpectatorInput(ClientInput input, long sessionId)
        {
            if (input == null) return;

            if (!m_spectators.ContainsKey(sessionId)) return;
            m_spectators[sessionId].HandledInputs = input.Index;
        }

        private void HandleClientInput(ClientInput input)
        {
            if (input == null) return;

            BattlePlayer player = GetPlayerBySessionId(input.OwnerSessionId);

            if (player == null) return;
            if (player.LastHandledInput >= input.Index) return;

            player.LastHandledInput = input.Index;
            switch (input.Type)
            {/*
                case 2:
                    {
                        Character character = (Character)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
                        if (character == null) return;

                        Skill skill = character.GetWeaponSkill();
                        if (skill == null) return;

                        character.UltiDisabled();

                        bool indirection = false;
                        if (skill.SkillData.Projectile != null)
                        {
                            ProjectileData projectileData = DataTables.Get(DataType.Projectile).GetData<ProjectileData>(skill.SkillData.Projectile);
                            indirection = projectileData.Indirect;
                        }

                        if (!input.AutoAttack && !indirection)
                        {
                            character.ActivateSkill(false, input.X, input.Y);
                        }
                        else
                        {
                            character.ActivateSkill(false, input.X - character.GetX(), input.Y - character.GetY());
                        }

                        break;
                    }
                case 0:
                    {
                        Character character = (Character)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
                        if (character == null) return;

                        Skill skill = character.GetUltimateSkill();
                        if (skill == null) return;

                        if (!player.HasUlti()) return;
                        player.UseUlti();

                        character.UltiEnabled();

                        bool indirection = false;
                        if (skill.SkillData.Projectile != null)
                        {
                            ProjectileData projectileData = DataTables.Get(DataType.Projectile).GetData<ProjectileData>(skill.SkillData.Projectile);
                            indirection = projectileData.Indirect;
                        }

                        if (!input.AutoAttack && !indirection)
                        {
                            character.ActivateSkill(true, input.X, input.Y);
                        }
                        else
                        {
                            character.ActivateSkill(true, input.X - character.GetX(), input.Y - character.GetY());
                        }

                        break;
                    }
                    */
                case 100:
                    {
                        Character character = (Character)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
                        if (character == null) return;

                        character.MoveTo(input.X, input.Y);

                        break;
                    }
                    
                case 107:
                    {
                        Character character = (Character)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
                        character.UltiEnabled();
                        break;
                    }
                case 108:
                    {
                        Character character = (Character)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
                        character.UltiDisabled();
                        break;
                    }
                    
                default:
                    if (input.Type < 100 && input.Type >= 0)
                    {
                        Character character = (Character)m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
                        if (character == null) break;
                        //Console.WriteLine(input.Type);
                        SkillData skill = character.GetSkill(input.Type);
                        if (skill == null) break;
                        if (character.CharacterData.WeaponSkill == skill.Name)
                        {
                            //Console.WriteLine("weapon");
                            character.UltiDisabled();
                        }
                        else if (character.CharacterData.UltimateSkill == skill.Name)
                        {
                            //Console.WriteLine("ulti");
                            character.UltiEnabled();
                            player.UseUlti();
                        }
                        else break;

                        character.ActivateSkill(character.CharacterData.UltimateSkill == skill.Name, input.X - character.GetX(), input.Y - character.GetY());

                        break;
                    }
                    else
                    {
                        Debugger.Warning("Input is unhandled: " + input.Type);
                        break;
                    }
            }
        }

        public bool IsTileOnPoisonArea(int xTile, int yTile)
        {
            if (m_gameModeVariation != 6) return false;

            int tick = GetTicksGone();

            if (tick > 500)
            {
                int poisons = 0;
                poisons += (tick - 500) / 100;

                if (xTile <= poisons || xTile >= 59 - poisons || yTile <= poisons || yTile >= 59 - poisons)
                {
                    return true;
                }
            }
            return false;
        }

        private void HandleIncomingInputMessages()
        {
            while (m_inputQueue.Count > 0)
            {
                this.HandleClientInput(m_inputQueue.Dequeue());
            }
        }

        public void ExecuteOneTick()
        {
            try
            {
                this.HandleIncomingInputMessages();
                foreach (BattlePlayer player in GetPlayers())
                {
                    player.KillList.Clear();
                }

                if (this.CalculateIsGameOver())
                {
                    this.m_updateTimer.Dispose();
                    this.GameOver();
                    this.IsGameOver = true;
                }

                this.m_gameObjectManager.PreTick();
                this.Tick();
                this.m_time.IncreaseTick();
                this.SendVisionUpdateToPlayers();
            } catch (Exception e)
            {
                Console.WriteLine("Battle stopped with exception! Message: " + e.Message + " Trace: " + e.StackTrace);
                m_updateTimer.Dispose();
                IsGameOver = true;
            }
        }

        private void TickSpawnEventStuffDelayed()
        {
            if (m_gameModeVariation == 0)
            {
                if (GetTicksGone() % 100 == 0)
                {
                    int instanceId = DataTables.Get(18).GetInstanceId("Point");
                    Item gem = new Item(18, instanceId);
                    gem.SetPosition(3150, 4950, 0);
                    gem.SetAngle(GetRandomInt(0, 360));
                    m_gameObjectManager.AddGameObject(gem);
                }
            }
            else if (m_gameModeVariation == 4)
            {
                if (GetTicksGone() == 600 && !isArtifactSpawned)
                {
                    isArtifactSpawned = true;
                    int instanceId = DataTables.Get(18).GetInstanceId("Point");
                    Item gem = new Item(18, instanceId);
                    gem.SetPosition(3150, 4950, 0);
                    gem.SetAngle(GetRandomInt(0, 360));
                    m_gameObjectManager.AddGameObject(gem);
                }
            }
        }

        public void GameOver()
        {
            SendBattleEndToPlayers();
        }

        public void SendBattleEndToPlayers()
        {
            Random rand = new Random();

            foreach (BattlePlayer player in m_players)
            {
                if (player.SessionId < 0) continue;
                if (!player.IsAlive) continue;
                if (player.BattleRoyaleRank == -1) player.BattleRoyaleRank = 1;
                if (player.Avatar == null) continue;
                int rank = player.BattleRoyaleRank;
                player.Avatar.BattleId = -1;

                bool isVictory = m_winnerTeam == player.TeamIndex;

                BattleEndMessage message = new BattleEndMessage();
                Hero hero = player.Avatar.GetHero(player.CharacterId);


                if (m_gameModeVariation != 6)
                {
                    message.GameMode = 1;
                    message.IsPvP = true;
                    message.Players = m_players;
                    message.OwnPlayer = player;

                    if (m_winnerTeam == -1) // Draw
                    {
                        message.Result = 2;
                        message.TokensReward = 10;

                        message.TrophiesReward = 0;

                        player.Avatar.AddChips(10);

                        player.Home.TokenReward += 10;
                    }

                    if (isVictory)
                    {
                        message.Result = 0;
                        message.TokensReward = 20;

                        int trophiesReward = rand.Next(5, 8) + 1;
                        message.TrophiesReward = trophiesReward;

                        hero.AddTrophies(trophiesReward);
                        player.Avatar.AddChips(20);
                        player.Avatar.TrioWins++;

                        player.Home.TokenReward += 20;
                        player.Home.TrophiesReward = LogicMath.Max(player.Home.TrophiesReward + trophiesReward, 0);
                    }
                    else if (m_winnerTeam != -1)
                    {
                        message.Result = 1;
                        message.TokensReward = 10;

                        int trophiesReward = -2;
                        if (hero.Trophies < -trophiesReward) trophiesReward = -hero.Trophies;
                        message.TrophiesReward = trophiesReward;

                        hero.AddTrophies(trophiesReward);
                        player.Avatar.AddChips(10);

                        player.Home.TokenReward += 10;
                        player.Home.TrophiesReward = LogicMath.Max(player.Home.TrophiesReward + trophiesReward, 0);
                    }
                }
                else
                {
                    message.IsPvP = true;
                    message.GameMode = 2;
                    message.Result = player.BattleRoyaleRank;
                    message.Players = new List<BattlePlayer>();
                    message.Players.Add(player);
                    message.OwnPlayer = player;
                    int tokensReward = 40 / rank;
                    message.TokensReward = tokensReward;
                    if (rank > 5)
                    {
                        int trophiesReward = -(rank - 5);
                        if (hero.Trophies < -trophiesReward) trophiesReward = -hero.Trophies;
                        message.TrophiesReward = trophiesReward;

                        player.Avatar.AddChips(tokensReward);
                        player.Home.TokenReward += tokensReward;
                        hero.AddTrophies(message.TrophiesReward);
                    }
                    else
                    {
                        int trophiesReward = (5 - rank) * 2;
                        message.TrophiesReward = trophiesReward;

                        player.Avatar.AddChips(tokensReward);
                        player.Home.TokenReward += tokensReward;
                        player.Home.TrophiesReward += trophiesReward;
                        hero.AddTrophies(message.TrophiesReward);
                    }
                }

                if (player.Avatar == null) continue;
                if (player.GameListener == null) continue;
                player.GameListener.SendTCPMessage(message);
            }
        }

        public int GetTeamScore(int team)
        {
            int score = 0;
            foreach (BattlePlayer player in m_players)
            {
                if (player.TeamIndex == team) score += player.GetScore();
            }
            return score;
        }

        private bool CalculateIsGameOver()
        {
            int v4;
            if (m_ART_TEST && m_gameModeVariation != 5 || m_BATTLE_SIM)
            {
                return false;
            }
            int v3 = GetTicksGone();
            if (m_ART_TEST)
            {
                v4 = 99999999;
            }
            else
            {
                v4 = GameModeUtil.GetBattleTicks(m_gameModeVariation, this);
            }
            if (v3 < v4 - 1)
            {
                switch (m_gameModeVariation)
                {
                    case 2:
                        Character[] characters= m_gameObjectManager.GetCharacters();
                        foreach (Character character in characters)
                        {
                            if (!character.CharacterData.IsBase()) continue;
                            if (character.IsAlive()) continue;
                            if (character.GetIndex() / 16 == 0) m_winnerTeam = 1;
                            else m_winnerTeam = 0;
                            return true;
                        }
                        break;
                    case 3:
                        if (GetTicksGone() >= 20 * 120 + 120)
                        {
                            if (GetTeamScore(0) > GetTeamScore(1))
                            {
                                m_winnerTeam = 0;
                            }
                            else if (GetTeamScore(0) < GetTeamScore(1))
                            {
                                m_winnerTeam = 1;
                            }
                            else
                            {
                                m_winnerTeam = -1;
                            }
                            return true;
                        }
                        break;

                    case 0:
                        if (GetTeamScore(0) > GetTeamScore(1) && GetTeamScore(0) >= 10)
                        {
                            if (m_gemGrabCountdown == 0)
                            {
                                m_gemGrabCountdown = GetTicksGone() + 20 * 17;
                            }
                            else if (GetTicksGone() > m_gemGrabCountdown)
                            {
                                m_winnerTeam = 0;
                                return true;
                            }
                        }
                        else if (GetTeamScore(0) < GetTeamScore(1) && GetTeamScore(1) >= 10)
                        {
                            if (m_gemGrabCountdown == 0)
                            {
                                m_gemGrabCountdown = GetTicksGone() + 20 * 17;
                            }
                            else if (GetTicksGone() > m_gemGrabCountdown)
                            {
                                m_winnerTeam = 1;
                                return true;
                            }
                        }
                        else
                        {
                            m_gemGrabCountdown = 0;
                        }
                        break;
                    case 4:
                        if (GetTeamScore(0) > GetTeamScore(1) && GetTeamScore(0) >= 1)
                        {
                            if (m_gemGrabCountdown == 0)
                            {
                                m_gemGrabCountdown = GetTicksGone() + 20 * 30; // getTicks gone + ticks per sec * sec
                            }
                            else if (GetTicksGone() > m_gemGrabCountdown)
                            {
                                m_winnerTeam = 0;
                                return true;
                            }
                        }
                        else if (GetTeamScore(0) < GetTeamScore(1) && GetTeamScore(1) >= 1)
                        {
                            if (m_gemGrabCountdown == 0)
                            {
                                m_gemGrabCountdown = GetTicksGone() + 20 * 30;
                            }
                            else if (GetTicksGone() > m_gemGrabCountdown)
                            {
                                m_winnerTeam = 1;
                                return true;
                            }
                        }
                        else
                        {
                            m_gemGrabCountdown = 0;
                        }
                        break;
                    case 6:
                        if (m_playersAlive <= 1)
                        {
                            return true;
                        }
                        break;
                    default:
                        return false;
                }
                return false;
            }
            return true;
        }

        private void Tick()
        {
            m_gameObjectManager.Tick();
            TickSpawnEventStuffDelayed();
            m_tileMap.Tick(m_gameObjectManager);
            TickSpawnHeroes();
            UpdatePlayerStatus();
        }
        public void UpdatePlayerStatus()
        {
            ; //todooo
        }
        private void SendVisionUpdateToPlayers()
        {
            try
            {
                
            Parallel.ForEach(m_players, player =>
            {
                if (player.GameListener != null)
                {
                    BitStream visionBitStream = new BitStream(64);
                    m_gameObjectManager.Encode(visionBitStream, m_tileMap, player.OwnObjectId, player.PlayerIndex, player.TeamIndex);

                    VisionUpdateMessage visionUpdate = new VisionUpdateMessage();
                    visionUpdate.Tick = GetTicksGone();
                    visionUpdate.HandledInputs = player.LastHandledInput;
                    visionUpdate.Viewers = m_spectators.Count;
                    visionUpdate.VisionBitStream = visionBitStream;

                    player.GameListener.SendMessage(visionUpdate);

                    //Debugger.Print("Send!");
                }
            });

            BitStream spectateStream = new BitStream(64);
            m_gameObjectManager.Encode(spectateStream, m_tileMap, 0, -1, -1);

            Task.Run(() =>
            {
                foreach (LogicGameListener gameListener in m_spectators.Values.ToArray())
                {
                    VisionUpdateMessage visionUpdate = new VisionUpdateMessage();
                    visionUpdate.Tick = GetTicksGone();
                    visionUpdate.HandledInputs = gameListener.HandledInputs;
                    visionUpdate.Viewers = m_spectators.Count;
                    visionUpdate.VisionBitStream = spectateStream;

                    gameListener.SendMessage(visionUpdate);
                }
            });
            
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to send vision update: " + ex);
            }
        }

        public BattlePlayer[] GetPlayers()
        {
            return m_players.ToArray();
        }

        public int GetRandomInt(int min, int max)
        {
            return m_random.Rand(max - min) + min;
        }

        public int GetRandomInt(int max)
        {
            return m_random.Rand(max);
        }

        public int GetTicksGone()
        {
            return m_time.GetTick();
        }

        public int GetGameModeVariation()
        {
            return m_gameModeVariation;
        }

        public int GetPlayersCountWithGameModeVariation()
        {
            return m_playersCountWithGameModeVariation;
        }

        public int GetRandomSeed()
        {
            return m_randomSeed;
        }

        public LocationData Location
        {
            get
            {
                return DataTables.Get(DataType.Location).GetDataByGlobalId<LocationData>(m_locationId);
            }
        }

        public long Id { get; set; }
    }

    internal class isArtifactSpawned
    {
    }
}
