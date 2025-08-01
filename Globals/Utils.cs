using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Godot1.Globals
{
    internal static class Utils
    {
        public static Vector2I GetLocalCell(Vector2 coords)
        {
            int nx = (int)coords.X % (GenerationSettings.CHUNK_SIZE * GenerationSettings.TILE_SIZE);
            int ny = (int)coords.Y % (GenerationSettings.CHUNK_SIZE * GenerationSettings.TILE_SIZE);

            return new Vector2I(nx, ny);
        }

        public static Vector2I GetChunkCoords(Vector2 coords)
        {
            int nx = (int)coords.X / (GenerationSettings.CHUNK_SIZE * GenerationSettings.TILE_SIZE);
            int ny = (int)coords.Y / (GenerationSettings.CHUNK_SIZE * GenerationSettings.TILE_SIZE);

            return new Vector2I(nx, ny);
        }
    }
}
