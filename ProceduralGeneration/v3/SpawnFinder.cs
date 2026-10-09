using Godot;
using System.Collections.Generic;

public static class SpawnFinder
{
	public const int Clearance = 5;

	private static readonly int[] Dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
	private static readonly int[] Dy = { 0, 0, 1, -1, 1, -1, 1, -1 };

	public static bool IsPreferred(TileType type) => type == TileType.RegularForest || type == TileType.Savanna;

	public static bool IsRough(TileType type) => type == TileType.StoneMountain || type == TileType.Snow;

	public static Vector2I Find(int width, int height, byte[] biomes, Vector2I target)
	{
		int count = width * height;
		var blocked = new bool[count];
		for (int i = 0; i < count; i++)
		{
			TileType type = (TileType)biomes[i];
			blocked[i] = type == TileType.None || WorldMap.IsWater(type);
		}

		int[] component = LargestComponent(width, height, blocked);
		var rough = new bool[count];
		for (int i = 0; i < count; i++) rough[i] = blocked[i] || IsRough((TileType)biomes[i]);
		int[] clearance = Clearances(width, height, rough);

		for (int tier = 0; tier < 3; tier++)
		{
			int best = -1;
			long bestDistance = long.MaxValue;
			for (int i = 0; i < count; i++)
			{
				if (component[i] == 0) continue;
				TileType type = (TileType)biomes[i];
				if (tier == 0 && (clearance[i] < Clearance || !IsPreferred(type))) continue;
				if (tier == 1 && clearance[i] < 3) continue;
				long dx = i % width - target.X, dy = i / width - target.Y;
				long distance = dx * dx + dy * dy;
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = i;
				}
			}
			if (best >= 0) return new Vector2I(best % width, best / width);
		}
		return target;
	}

	public static int[] LargestComponent(int width, int height, bool[] blocked)
	{
		int count = width * height;
		var label = new int[count];
		var queue = new Queue<int>();
		int bestLabel = 0, bestSize = 0, next = 0;
		for (int start = 0; start < count; start++)
		{
			if (blocked[start] || label[start] != 0) continue;
			next++;
			int size = 0;
			label[start] = next;
			queue.Enqueue(start);
			while (queue.Count > 0)
			{
				int i = queue.Dequeue();
				size++;
				int x = i % width, y = i / width;
				for (int k = 0; k < 4; k++)
				{
					int nx = x + Dx[k], ny = y + Dy[k];
					if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
					int j = ny * width + nx;
					if (blocked[j] || label[j] != 0) continue;
					label[j] = next;
					queue.Enqueue(j);
				}
			}
			if (size > bestSize)
			{
				bestSize = size;
				bestLabel = next;
			}
		}
		var result = new int[count];
		for (int i = 0; i < count; i++) result[i] = bestLabel != 0 && label[i] == bestLabel ? 1 : 0;
		return result;
	}

	public static int[] Clearances(int width, int height, bool[] blocked)
	{
		int count = width * height;
		var distance = new int[count];
		var queue = new Queue<int>();
		for (int i = 0; i < count; i++)
		{
			int x = i % width, y = i / width;
			if (blocked[i])
			{
				distance[i] = 0;
				queue.Enqueue(i);
			}
			else distance[i] = Mathf.Min(Mathf.Min(x, y), Mathf.Min(width - 1 - x, height - 1 - y)) + 1;
		}
		while (queue.Count > 0)
		{
			int i = queue.Dequeue();
			int x = i % width, y = i / width;
			for (int k = 0; k < 8; k++)
			{
				int nx = x + Dx[k], ny = y + Dy[k];
				if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
				int j = ny * width + nx;
				if (distance[j] <= distance[i] + 1) continue;
				distance[j] = distance[i] + 1;
				queue.Enqueue(j);
			}
		}
		return distance;
	}
}
