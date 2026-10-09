using Godot;
using System;
using System.Collections.Generic;

public class TerrainRules
{
	public static readonly Vector2I BaseTile = new(2, 1);

	private static readonly TileSet.CellNeighbor[] Corners =
	{
		TileSet.CellNeighbor.TopLeftCorner,
		TileSet.CellNeighbor.TopRightCorner,
		TileSet.CellNeighbor.BottomLeftCorner,
		TileSet.CellNeighbor.BottomRightCorner,
	};

	private static readonly Dictionary<TileSet, TerrainRules> cache = new();

	private readonly int span;
	private readonly int[] table;
	public readonly int[] SourceOf = new int[256];
	public readonly int[] TerrainOf = new int[256];

	private class Node
	{
		public readonly Dictionary<int, Node> Next = new();
		public int Tile = -1;
	}

	public static TerrainRules For(TileSet tileSet)
	{
		if (tileSet == null || tileSet.GetSourceCount() == 0) return null;
		lock (cache)
		{
			if (!cache.TryGetValue(tileSet, out TerrainRules rules)) cache[tileSet] = rules = new TerrainRules(tileSet);
			return rules;
		}
	}

	private TerrainRules(TileSet tileSet)
	{
		var root = new Node();
		int maxTerrain = 0;
		for (int s = 0; s < tileSet.GetSourceCount(); s++)
		{
			int sid = tileSet.GetSourceId(s);
			if (tileSet.GetSource(sid) is not TileSetAtlasSource atlas) continue;
			Vector2I grid = atlas.GetAtlasGridSize();
			for (int y = 0; y < grid.Y; y++)
			{
				for (int x = 0; x < grid.X; x++)
				{
					var coords = new Vector2I(x, y);
					if (!atlas.HasTile(coords)) continue;
					TileData data = atlas.GetTileData(coords, 0);
					if (data == null || data.TerrainSet != 0) continue;
					maxTerrain = Math.Max(maxTerrain, data.Terrain);
					var bits = new int[4];
					bool any = false, all = true;
					for (int k = 0; k < 4; k++)
					{
						bits[k] = data.GetTerrainPeeringBit(Corners[k]);
						any |= bits[k] != -1;
						all &= bits[k] != -1;
						maxTerrain = Math.Max(maxTerrain, bits[k]);
					}
					if (!any || !all) continue;
					Node node = root;
					foreach (int bit in bits)
					{
						if (!node.Next.TryGetValue(bit, out Node next)) node.Next[bit] = next = new Node();
						node = next;
					}
					node.Tile = Pack(sid, coords);
				}
			}
		}

		span = maxTerrain + 2;
		table = new int[span * span * span * span];
		var corners = new int[4];
		for (int index = 0; index < table.Length; index++)
		{
			int rest = index;
			for (int k = 3; k >= 0; k--)
			{
				corners[k] = rest % span - 1;
				rest /= span;
			}
			table[index] = Apply(root, corners);
		}

		Array.Fill(SourceOf, -1);
		Array.Fill(TerrainOf, -1);
		foreach (TileType type in Enum.GetValues<TileType>())
		{
			if (type == TileType.None) continue;
			int sid = GenerationUtils.getTileTypeAtlas(type);
			if (!tileSet.HasSource(sid) || tileSet.GetSource(sid) is not TileSetAtlasSource atlas || !atlas.HasTile(BaseTile)) continue;
			SourceOf[(int)type] = sid;
			TerrainOf[(int)type] = atlas.GetTileData(BaseTile, 0).Terrain;
		}
	}

	private static int Apply(Node root, int[] corners)
	{
		if (corners[0] == -1 && corners[1] == -1 && corners[2] == -1 && corners[3] == -1) return -1;
		Node node = root;
		foreach (int corner in corners)
		{
			int terrain = corner == -1 ? 0 : corner;
			if (!node.Next.TryGetValue(terrain, out Node next) && !node.Next.TryGetValue(0, out next)) return -1;
			node = next;
		}
		return node.Tile;
	}

	public static int Pack(int sid, Vector2I coords) => (sid << 16) | (coords.X << 8) | coords.Y;

	public static int SourceOfTile(int tile) => tile >> 16;

	public static Vector2I CoordsOf(int tile) => new((tile >> 8) & 0xff, tile & 0xff);

	public int Edge(int topLeft, int topRight, int bottomLeft, int bottomRight)
	{
		int index = (((topLeft + 1) * span + topRight + 1) * span + bottomLeft + 1) * span + bottomRight + 1;
		return table[index];
	}
}
