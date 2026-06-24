namespace Supercell.Laser.Logic.Battle.Objects
{
    using System;
    using Supercell.Laser.Logic.Battle.Level;
    using Supercell.Laser.Logic.Battle.Structures;
    using Supercell.Laser.Logic.Data.Helper;
    using Supercell.Laser.Logic.Helper;
    using Supercell.Laser.Titan.DataStream;

    public class GameObjectManager
    {
        private Queue<GameObject> AddObjects;
        private Queue<GameObject> RemoveObjects;
        private BattleMode Battle;
        private List<GameObject> GameObjects;
        public int GameModeVariation;
        private int ObjectCounter;
        public TileMap m_tileMap;

        public GameObjectManager(BattleMode battle)
        {
            Battle = battle;
            GameObjects = new List<GameObject>();

            AddObjects = new Queue<GameObject>();
            RemoveObjects = new Queue<GameObject>();
        }

        public GameObject[] GetGameObjects()
        {
            return GameObjects.ToArray();
        }

        public BattleMode GetBattle()
        {
            return Battle;
        }

        public void PreTick()
        {
            foreach (GameObject gameObject in GameObjects)
            {
                if (gameObject.ShouldDestruct())
                {
                    gameObject.OnDestruct();
                    RemoveGameObject(gameObject);
                }
                else
                {
                    gameObject.PreTick();
                }
            }

            while (AddObjects.Count > 0)
            {
                GameObjects.Add(AddObjects.Dequeue());
            }

            while (RemoveObjects.Count > 0)
            {
                GameObjects.Remove(RemoveObjects.Dequeue());
            }
        }

        public void Tick()
        {
            foreach (GameObject gameObject in GameObjects)
            {
                gameObject.Tick();
            }
        }

        public void AddGameObject(GameObject gameObject)
        {
            gameObject.AttachGameObjectManager(this, GlobalId.CreateGlobalId(gameObject.GetObjectType(), ObjectCounter++));
            AddObjects.Enqueue(gameObject);
        }

        public void RemoveGameObject(GameObject gameObject)
        {
            RemoveObjects.Enqueue(gameObject);
        }

        public GameObject GetGameObjectByID(int globalId)
        {
            return GameObjects.Find(obj => obj.GetGlobalID() == globalId);
        }

        public List<GameObject> GetVisibleGameObjects(int teamIndex)
        {
            List<GameObject> objects = new List<GameObject>();

            foreach (GameObject obj in GameObjects)
            {
                if (obj.GetFadeCounter() > 0 || obj.GetIndex() / 16 == teamIndex)
                {
                    objects.Add(obj);
                }
            }

            return objects;
        }

        public void Encode(BitStream bitStream, TileMap tileMap, int ownObjectGlobalId, int playerIndex, int teamIndex)
        {
            m_tileMap = tileMap;
            BattlePlayer[] players = Battle.GetPlayers();
            List<GameObject> visibleGameObjects = GetVisibleGameObjects(teamIndex);

            GameModeVariation = Battle.GetGameModeVariation();
            bitStream.WritePositiveInt(ownObjectGlobalId, 21);

            bitStream.WritePositiveInt(GetBattle().GetBattleMode().m_gemGrabCountdown, 15);
            

            bitStream.WriteBoolean(false);

            if (GameModeVariation != 6)
            {
                bitStream.WritePositiveInt(0, 5); // 0xa820a8
                bitStream.WritePositiveInt(0, 6); // 0xa820b4
                bitStream.WritePositiveInt(tileMap.Width - 1, 5); // 0xa820c0
                bitStream.WritePositiveInt(tileMap.Height - 1, 6); // 0xa820d0

            }
            else
            {
                bitStream.WritePositiveInt(0, 6); // 0xa820a8
                bitStream.WritePositiveInt(0, 7); // 0xa820b4
                bitStream.WritePositiveInt(tileMap.Width - 1, 6); // 0xa820c0
                bitStream.WritePositiveInt(tileMap.Height - 1, 7); // 0xa820d0

            }

            for (int i = 0; i < tileMap.Width; i++)
            {
                for (int j = 0; j < tileMap.Height; j++)
                {
                    var tile = tileMap.GetTile(i, j, true);
                    if (tile.Data.RespawnSeconds > 0 || tile.Data.IsDestructible)
                    {
                        bitStream.WriteBoolean(tile.IsDestructed());
                    }
                }
            }
            for (int i = 0; i < players.Length; i++)
            {
                bitStream.WritePositiveInt(0, 1);
                bitStream.WriteBoolean(players[i].HasUlti());
                if (i == playerIndex)
                {
                    bitStream.WritePositiveInt(players[i].GetUltiCharge(), 10);
                }
            }

            switch (GameModeVariation)
            {
                case 6:
                    bitStream.WritePositiveInt(Battle.GetPlayersAliveCountForBattleRoyale(), 4);
                    break;
                case 2:
                    int hp1 = 0;
                    int hp2 = 0;
                    
                    foreach (Character character in GetCharacters())
                    {
                        if (!character.CharacterData.IsBase()) continue;
                        if (!character.IsAlive()) continue;
                        if (character.GetIndex() / 16 == 0) hp1 = character.GetHitpointPercentage();
                        else hp2 = character.GetHitpointPercentage();
                    }
                    bitStream.WritePositiveIntMax127(hp1);
                    bitStream.WritePositiveIntMax127(hp2);
                    break;
                case 3:
                    bitStream.WritePositiveInt(100, 3);
                    bitStream.WritePositiveInt(100, 3);
                    break;
            }

            for (int i = 0; i < players.Length; i++)
            {
                bitStream.WritePositiveInt(players[i].GetScore(), 6);
                bitStream.WritePositiveIntMax3(players[i].KillList.Count);
                for (int j = 0; j < players[i].KillList.Count; j++)
                {
                    bitStream.WritePositiveIntMax7(1);
                    bitStream.WritePositiveIntMax15(players[i].KillList[j].BountyStarsEarned);
                }
                
            }

           // bitStream.WritePositiveIntMax7(0);

            bitStream.WritePositiveInt(visibleGameObjects.Count, 7);

            foreach (GameObject gameObject in visibleGameObjects)
            {
                ByteStreamHelper.WriteDataReference(bitStream, gameObject.GetDataId());
            }

            foreach (GameObject gameObject in visibleGameObjects)
            {
                ByteStreamHelper.WriteObjectRunning(bitStream, gameObject.GetGlobalID());
            }

            foreach (GameObject gameObject in visibleGameObjects)
            {
                gameObject.Encode(bitStream, gameObject.GetGlobalID() == ownObjectGlobalId, teamIndex);
            }
        }
        public Character[] GetCharacters()
        {
            List<Character> objects = new List<Character>();

            foreach (GameObject obj in GameObjects)
            {
                if (obj.GetObjectType() == 1)
                {
                    objects.Add((Character)obj);
                }
            }
            foreach (GameObject obj in AddObjects)
            {
                if (obj.GetObjectType() == 1)
                {
                    objects.Add((Character)obj);
                }
            }
            return objects.ToArray();
        }
    }
}
