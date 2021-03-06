using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.IO;
using Microsoft.Xna.Framework;

namespace TileGameLib
{
    public class TileLayer
    {
        #region Fields

        int[,] tiles;
        Texture2D texture;
        int tileWidth, tileHeight;

        List<int> blockedTiles = new List<int>();
        List<int> slowTiles = new List<int>();
        List<int> coverTiles = new List<int>();
        List<int> castleTiles = new List<int>();

        public Pathfinder Pathfind;
        public EntityManager Entities;

        #endregion

        #region Initialization

        public TileLayer(int tileWidth, int tileHeight)
        {
            this.tileWidth = tileWidth;
            this.tileHeight = tileHeight;
        }

        public void LoadContent(ContentManager content, string textureFilename, string layoutFilename)
        {
            texture = content.Load<Texture2D>("Tilesets/" + textureFilename);
            tiles = Utility.FromCSV("Content/Levels/" + layoutFilename);
        }

        public void SetBlockedTiles(params Int32[] blockedTiles)
        {
            this.blockedTiles.Clear();

            foreach (Int32 tile in blockedTiles)
                this.blockedTiles.Add(tile);
        }

        public void SetSlowTiles(params Int32[] slowTiles)
        {
            this.slowTiles.Clear();

            foreach (Int32 tile in slowTiles)
                this.slowTiles.Add(tile);
        }

        public void SetCoverTiles(params Int32[] coverTiles)
        {
            this.coverTiles.Clear();

            foreach (Int32 tile in coverTiles)
                this.coverTiles.Add(tile);
        }

        public void SetCastleTiles(params Int32[] castleTiles)
        {
            this.castleTiles.Clear();

            foreach (Int32 tile in castleTiles)
                this.castleTiles.Add(tile);
        }

        public static TileLayer FromFile(ContentManager content, string filename)
        {
            TileLayer layer;

            using (StreamReader sr = new StreamReader("Content/Levels/" + filename))
            {
                int width = -1, height = -1;

                if (sr.ReadLine().Equals("[Width]"))
                    width = int.Parse(sr.ReadLine());
                if (sr.ReadLine().Equals("[Height]"))
                    height = int.Parse(sr.ReadLine());

                if (width == -1 || height == -1)
                    throw new ArgumentException("File format incorrect.");

                layer = new TileLayer(width, height);

                string textureFilename = "", layoutFilename = "";

                if (sr.ReadLine().Equals("[Texture]"))
                    textureFilename = sr.ReadLine();
                if (sr.ReadLine().Equals("[Layout]"))
                    layoutFilename = sr.ReadLine();

                if (string.IsNullOrEmpty(textureFilename) || string.IsNullOrEmpty(layoutFilename))
                    throw new ArgumentException("File format incorrect.");

                layer.LoadContent(content, textureFilename, "Maps/" + layoutFilename);

                if (sr.ReadLine().Equals("[Blocked Tiles]"))
                {
                    string next;
                    List<Int32> tiles = new List<Int32>();
                    while ((next = sr.ReadLine()) != "[Slow Tiles]" && next != null)
                    {
                        tiles.Add(Int32.Parse(next));
                    }
                    layer.SetBlockedTiles(tiles.ToArray());
                    tiles.Clear();

                    if (next == "[Slow Tiles]")
                    {
                        while ((next = sr.ReadLine()) != "[Cover Tiles]" && next != null)
                        {
                            tiles.Add(Int32.Parse(next));
                        }
                        layer.SetSlowTiles(tiles.ToArray());
                        tiles.Clear();
                    }

                    if (next == "[Cover Tiles]")
                    {
                        while ((next = sr.ReadLine()) != "[Castle Tiles]" && next != null)
                        {
                            tiles.Add(Int32.Parse(next));
                        }
                        layer.SetCoverTiles(tiles.ToArray());
                        tiles.Clear();
                    }

                    if (next == "[Castle Tiles]")
                    {
                        while ((next = sr.ReadLine()) != "[Entities]")
                        {
                            tiles.Add(Int32.Parse(next));
                        }
                        layer.SetCastleTiles(tiles.ToArray());
                        tiles.Clear();
                    }
                }
                else
                {
                    throw new ArgumentException("Improper file format.");
                }

                layer.Entities = new EntityManager(layer, sr, content);

                sr.Close();
            }

            return layer;
        }

        #endregion

        #region Properties

        public int WidthInPixels()
        {
            return tiles.GetLength(0) * tileWidth;
        }

        public int HeightInPixels()
        {
            return tiles.GetLength(1) * tileHeight;
        }

        public int Width()
        {
            return tiles.GetLength(0);
        }

        public int Height()
        {
            return tiles.GetLength(1);
        }

        public int TileWidth
        {
            get { return tileWidth; }
        }

        public int TileHeight
        {
            get { return tileHeight; }
        }

        public bool IsPassable(int x, int y)
        {
            return !blockedTiles.Contains(GetTile(x, y));
        }

        public bool IsPassable(Point loc)
        {
            return IsPassable(loc.X, loc.Y);
        }

        public bool IsSlowTile(int x, int y)
        {
            return slowTiles.Contains(GetTile(x, y));
        }

        public bool IsSlowTile(Point loc)
        {
            return IsSlowTile(loc.X, loc.Y);
        }

        public bool IsCoverTile(int x, int y)
        {
            return coverTiles.Contains(GetTile(x, y));
        }

        public bool IsCoverTile(Point loc)
        {
            return IsCoverTile(loc.X, loc.Y);
        }

        public bool IsCastleTile(int x, int y)
        {
            return castleTiles.Contains(GetTile(x, y));
        }

        public bool IsCastleTile(Point loc)
        {
            return IsCastleTile(loc.X, loc.Y);
        }
        
        #endregion

        #region Map Manipulation

        public void SetTile(int x, int y, uint value)
        {
            tiles[x, y] = (int)value;
        }

        public void SetTile(Point loc, uint value)
        {
            SetTile(loc.X, loc.Y, value);
        }

        public int GetTile(int x, int y)
        {
            return tiles[x, y];
        }

        public int GetTile(Point loc)
        {
            return GetTile(loc.X, loc.Y);
        }

        #endregion

        #region Helpers

        Rectangle tileSource(int tile)
        {
            int tilesPerRow = texture.Width / tileWidth;
            int tilesPerColumn = texture.Height / tileHeight;

            Point coords = new Point(--tile % tilesPerColumn, tile / tilesPerRow);

            return new Rectangle(coords.X * tileWidth, coords.Y * tileHeight, tileWidth, tileHeight);
        }

        Rectangle tileDestination(int x, int y)
        {
            return new Rectangle(x * tileWidth, y * tileHeight, tileWidth, tileHeight);
        }

        #endregion

        #region Draw

        public void Draw(SpriteBatch spriteBatch)
        {
            for (int x = 0; x < tiles.GetLength(0); ++x)
            {
                for (int y = 0; y < tiles.GetLength(1); ++y)
                {
                    if (tiles[x, y] != -1)
                        spriteBatch.Draw(texture, tileDestination(x, y), tileSource(tiles[x, y]), Color.White);
                }
            }
        }

        #endregion
    }
}
