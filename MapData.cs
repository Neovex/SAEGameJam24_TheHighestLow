using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using SFML.System;
using BlackCoat;
using BlackCoat.Collision;
using BlackCoat.Collision.Shapes;

namespace LowHigh
{
    class MapData
    {
        public Vector2i MapSize { get; private set; }
        public Vector2i TileSize { get; private set; }
        public Layer[] Layer { get; private set; }
        public MapObject[] MapObjects { get; private set; }

        public void Load(Core core, string file)
        {
            // Load Collisions
            var map = new List<(CollisionType type, RectangleCollisionShape collision)>();
            var root = XElement.Load(file);
            MapObjects = root.Elements("objectgroup")
                             .SelectMany(l => l.Elements("object")
                             .Select(o => new MapObject()
                             {
                                 Type = ParseCollisionType(o.Attribute("type")?.Value),
                                 Shape = new RectangleCollisionShape(core.CollisionSystem,
                                     new Vector2f((float)o.Attribute("x"), (float)o.Attribute("y")),
                                     new Vector2f(((float?)o.Attribute("width")) ?? 10, ((float?)o.Attribute("height")) ?? 1))
                             })).ToArray();


            // Load Tilemap Layer
            MapSize = new Vector2i((int)root.Attribute("width"), (int)root.Attribute("height"));
            TileSize = new Vector2i((int)root.Attribute("tilewidth"), (int)root.Attribute("tileheight"));
            var columns = (int)root.Element("tileset").Attribute("columns");

            Layer = root.Elements("layer").Select(l => new Layer()
            {
                Tiles = l.Element("data").Value.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                         .SelectMany((line, y) => line.Trim().Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(gidRaw => (Success: int.TryParse(gidRaw, out int gid), Gid: gid))
                                .Where(exp => exp.Success)
                                .Select((exp, x) => new Tile()
                                {
                                    Position = new Vector2f(x * TileSize.X, y * TileSize.Y),
                                    Coordinates = new Vector2i((exp.Gid - 1) % columns * TileSize.X,
                                                                (exp.Gid - 1) / columns * TileSize.Y)
                                })
                        ).ToArray(),
                Data = (string)l.Attribute("class")
            }).ToArray();
        }

        private static CollisionType ParseCollisionType(string type)
        {
            if (type == null) return CollisionType.Normal;
            return (CollisionType)Enum.Parse(typeof(CollisionType), type, true);
        }
    }

    class Layer
    {
        public Tile[] Tiles { get; set; }
        public string Data { get; set; }
    }

    class Tile
    {
        public Vector2f Position { get; set; }
        public Vector2i Coordinates { get; set; }
    }

    class MapObject : ICollisionShape
    {
        public CollisionType Type { get; set; }
        public RectangleCollisionShape Shape { get; set; }

        public Geometry CollisionGeometry => Shape.CollisionGeometry;

        public bool CollidesWith(Vector2f point) => Shape.CollidesWith(point);
        public bool CollidesWith(ICollisionShape other) => Shape.CollidesWith(other);
    }

    enum CollisionType
    {
        Normal,
        Portal,
        Start,
        Shroom,
        Damage
    }
}
