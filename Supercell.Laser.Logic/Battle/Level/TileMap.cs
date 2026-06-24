namespace Supercell.Laser.Logic.Battle.Level
{
    using System;
    using System.Collections.Generic;
    using Supercell.Laser.Logic.Battle.Objects;
    using Supercell.Laser.Titan.Math;

    public class TileMap
    {
        public GameObjectManager gameObjectManager;
        public readonly int Width, Height;
        private readonly Tile[,] Tiles;

        public int LogicWidth => TileToLogic(Width);
        public int LogicHeight => TileToLogic(Height);
        
        public List<Tile> SpawnPoints;
        public List<Tile> SpawnPointsTeam1;
        public List<Tile> SpawnPointsTeam2;
        public List<Tile> LootBoxes;
        public List<Tile> PoisonBarrels;
        public Tile Safe;
        public string SafeName;
        public TileMap(int width, int height, string data)
        {
            Width = width;
            Height = height;
            SpawnPoints = new List<Tile>();
            SpawnPointsTeam1 = new List<Tile>();
            SpawnPointsTeam2 = new List<Tile>();
            LootBoxes = new List<Tile>();
            PoisonBarrels = new List<Tile>();
            char[] chars = data.ToCharArray();
            int idx = 0;

            Tiles = new Tile[Height, Width];

            for (int i = 0; i < Height; i++)
            {
                for (int j = 0; j < Width; j++)
                {
                    Tiles[i, j] = new Tile(chars[idx], TileToLogic(j), TileToLogic(i));
                    if (chars[idx] == '1')
                    {
                        SpawnPoints.Add(Tiles[i, j]);
                        SpawnPointsTeam1.Add(Tiles[i, j]);
                    }
                    else if (chars[idx] == '2')
                    {
                        SpawnPoints.Add(Tiles[i, j]);
                        SpawnPointsTeam2.Add(Tiles[i, j]);
                    }
                    else if (chars[idx] == '4')
                    {
                        LootBoxes.Add(Tiles[i, j]);
                    }
                    else if (chars[idx] == '5')
                    {
                        PoisonBarrels.Add(Tiles[i, j]);
                    }
                    else if (chars[idx] == '6')
                    {
                        Safe = Tiles[i, j];
                        SafeName = "Safe1";
                    }
                    else if (chars[idx] == '7')
                    {
                        Safe = Tiles[i, j];
                        SafeName = "Safe2";
                    }
                    else if (chars[idx] == '8')
                    {
                        Safe = Tiles[i, j];
                        SafeName = "Safe3";
                    }
                    else if (chars[idx] == '9')
                    {
                        Safe = Tiles[i, j];
                        SafeName = "Safe4";
                    }
                    
                    idx++;
                }
            }
        }

        public Tile GetTile(int x, int y, bool isTile = false)
        {
            if (!isTile)
            {
                x = LogicToTile(x);
                y = LogicToTile(y);
            }

            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                return Tiles[y, x];
            }

            return null;
        }
        public LogicVector2 GetPosition()
        {
            int centerX = TileToLogic(Width / 2);
            int centerY = TileToLogic(Height / 2);
            return new LogicVector2(centerX, centerY);
        }
        public static int LogicToTile(int logicValue)
        {
            return logicValue / 300;
        }

        public static int TileToLogic(int tile)
        {
            return 300 * tile + 150;
        }
        public void Tick(GameObjectManager GameObjectManager)
        {
            ; //todo or smth
        }

        internal Tile[,] GetTiles()
        {
            return Tiles;
        }
    }
}
