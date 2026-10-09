using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class WaterMap
{
	public enum Kind : byte { Land, Shallow, Ocean }

	public const float FieldMaxDistance = 4f;
	private const int FlowCacheSize = 16;
	private const int MaxPendingFlows = 4;
	private const int LookAhead = 6;

	private static readonly int[] Dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
	private static readonly int[] Dy = { 0, 0, 1, -1, 1, -1, 1, -1 };

	public readonly int Width;
	public readonly int Height;

	private readonly Kind[] kinds;
	private readonly int[] regions;
	private readonly List<int> regionSizes = new();
	private readonly int mainRegion = -1;

	private readonly Dictionary<Vector2I, float[]> flows = new();
	private readonly Queue<Vector2I> flowOrder = new();
	private readonly HashSet<Vector2I> pending = new();

	public WaterMap(WorldMap map)
	{
		Width = map.Width;
		Height = map.Height;
		kinds = new Kind[Width * Height];
		for (int i = 0; i < kinds.Length; i++) kinds[i] = KindOf((TileType)map.Biomes[i]);

		regions = new int[kinds.Length];
		Array.Fill(regions, -1);
		var queue = new Queue<int>();
		for (int start = 0; start < kinds.Length; start++)
		{
			if (kinds[start] == Kind.Ocean || regions[start] >= 0) continue;
			int region = regionSizes.Count;
			int size = 0;
			regions[start] = region;
			queue.Enqueue(start);
			while (queue.Count > 0)
			{
				int i = queue.Dequeue();
				size++;
				int x = i % Width, y = i / Width;
				for (int k = 0; k < 4; k++)
				{
					int nx = x + Dx[k], ny = y + Dy[k];
					if (!IsWalkable(nx, ny)) continue;
					int j = ny * Width + nx;
					if (regions[j] >= 0) continue;
					regions[j] = region;
					queue.Enqueue(j);
				}
			}
			regionSizes.Add(size);
			if (mainRegion < 0 || size > regionSizes[mainRegion]) mainRegion = region;
		}
	}

	public static Kind KindOf(TileType type) => type switch
	{
		TileType.DeepWater => Kind.Ocean,
		TileType.TropicWater => Kind.Shallow,
		_ => Kind.Land,
	};

	public static Vector2I CellOf(Vector2 position) =>
		new(Mathf.FloorToInt(position.X / GenerationSettings.TILE_SIZE), Mathf.FloorToInt(position.Y / GenerationSettings.TILE_SIZE));

	public static Vector2 Center(Vector2I cell) => (cell * GenerationSettings.TILE_SIZE) + Vector2.One * GenerationSettings.TILE_SIZE / 2;

	public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

	public Kind KindAt(Vector2I cell) => InBounds(cell.X, cell.Y) ? kinds[cell.Y * Width + cell.X] : Kind.Ocean;

	public bool IsWalkable(int x, int y) => InBounds(x, y) && kinds[y * Width + x] != Kind.Ocean;

	public bool IsWalkable(Vector2I cell) => IsWalkable(cell.X, cell.Y);

	public Vector2I NearestWalkable(Vector2I cell, int region = -1, int maxRadius = 96)
	{
		for (int radius = 0; radius <= maxRadius; radius++)
		{
			Vector2I best = new(-1, -1);
			int bestDistance = int.MaxValue;
			for (int x = -radius; x <= radius; x++)
			{
				for (int y = -radius; y <= radius; y++)
				{
					if (Math.Max(Math.Abs(x), Math.Abs(y)) != radius) continue;
					Vector2I c = cell + new Vector2I(x, y);
					if (!IsWalkable(c) || (region >= 0 && regions[c.Y * Width + c.X] != region)) continue;
					int distance = x * x + y * y;
					if (distance < bestDistance)
					{
						bestDistance = distance;
						best = c;
					}
				}
			}
			if (best.X >= 0) return best;
		}
		return cell;
	}

	public Vector2 Walkable(Vector2 position)
	{
		Vector2I cell = CellOf(position);
		return IsWalkable(cell) ? position : Center(NearestWalkable(cell));
	}

	public Vector2 SafeSpawn(Vector2 position)
	{
		Vector2I cell = CellOf(position);
		if (mainRegion < 0) return position;
		if (IsWalkable(cell) && regionSizes[regions[cell.Y * Width + cell.X]] * 4 >= regionSizes[mainRegion]) return position;
		return Center(NearestWalkable(cell, mainRegion, Math.Max(Width, Height)));
	}

	public bool Blocked(Vector2 position, float margin) =>
		KindAt(CellOf(position)) == Kind.Ocean
		|| KindAt(CellOf(position + new Vector2(margin, 0))) == Kind.Ocean
		|| KindAt(CellOf(position - new Vector2(margin, 0))) == Kind.Ocean
		|| KindAt(CellOf(position + new Vector2(0, margin))) == Kind.Ocean
		|| KindAt(CellOf(position - new Vector2(0, margin))) == Kind.Ocean;

	public Vector2 Constrain(Vector2 position, Vector2 velocity, float delta, float margin)
	{
		if (velocity == Vector2.Zero || KindAt(CellOf(position)) == Kind.Ocean) return velocity;
		if (Blocked(position, margin)) margin = 0;
		if (!Blocked(position + velocity * delta, margin)) return velocity;
		Vector2 alongX = new(velocity.X, 0);
		Vector2 alongY = new(0, velocity.Y);
		bool canX = alongX != Vector2.Zero && !Blocked(position + alongX * delta, margin);
		bool canY = alongY != Vector2.Zero && !Blocked(position + alongY * delta, margin);
		if (canX && canY) return Mathf.Abs(velocity.X) >= Mathf.Abs(velocity.Y) ? alongX : alongY;
		if (canX) return alongX;
		if (canY) return alongY;
		return Vector2.Zero;
	}

	public bool LineCrossesOcean(Vector2 from, Vector2 to)
	{
		Vector2 a = from / GenerationSettings.TILE_SIZE;
		Vector2 b = to / GenerationSettings.TILE_SIZE;
		Vector2I cell = new(Mathf.FloorToInt(a.X), Mathf.FloorToInt(a.Y));
		Vector2I end = new(Mathf.FloorToInt(b.X), Mathf.FloorToInt(b.Y));
		Vector2 dir = b - a;
		Vector2I step = new(Math.Sign(dir.X), Math.Sign(dir.Y));
		float deltaX = dir.X == 0 ? float.MaxValue : Mathf.Abs(1f / dir.X);
		float deltaY = dir.Y == 0 ? float.MaxValue : Mathf.Abs(1f / dir.Y);
		float nextX = dir.X == 0 ? float.MaxValue : (dir.X > 0 ? cell.X + 1 - a.X : a.X - cell.X) * deltaX;
		float nextY = dir.Y == 0 ? float.MaxValue : (dir.Y > 0 ? cell.Y + 1 - a.Y : a.Y - cell.Y) * deltaY;

		for (int guard = 0; guard < Width + Height + 4; guard++)
		{
			if (KindAt(cell) == Kind.Ocean) return true;
			if (cell == end) return false;
			if (nextX < nextY)
			{
				if (nextX > 1f) return false;
				cell.X += step.X;
				nextX += deltaX;
			}
			else
			{
				if (nextY > 1f) return false;
				cell.Y += step.Y;
				nextY += deltaY;
			}
		}
		return false;
	}

	public Vector2 Steer(Vector2 from, Vector2 to)
	{
		Vector2 offset = to - from;
		if (offset.LengthSquared() < 1f) return Vector2.Zero;
		Vector2 direct = offset.Normalized();
		if (!LineCrossesOcean(from, to)) return direct;

		float[] flow = Flow(CellOf(to), false);
		Vector2I cell = CellOf(from);
		if (flow == null || !IsWalkable(cell) || float.IsInfinity(flow[cell.Y * Width + cell.X])) return direct;

		Vector2I aim = cell;
		for (int i = 0; i < LookAhead; i++)
		{
			Vector2I next = Downhill(flow, aim);
			if (next == aim) break;
			if (aim != cell && LineCrossesOcean(from, Center(next))) break;
			aim = next;
		}
		return aim == cell ? direct : (Center(aim) - from).Normalized();
	}

	public Vector2 ReachableNear(Vector2 point, Vector2 goal)
	{
		float[] flow = Flow(CellOf(goal), true);
		if (flow == null) return Walkable(point);
		Vector2I cell = CellOf(point);
		for (int radius = 0; radius <= 12; radius++)
		{
			for (int x = -radius; x <= radius; x++)
			{
				for (int y = -radius; y <= radius; y++)
				{
					if (Math.Max(Math.Abs(x), Math.Abs(y)) != radius) continue;
					Vector2I c = cell + new Vector2I(x, y);
					if (IsWalkable(c) && !float.IsInfinity(flow[c.Y * Width + c.X])) return radius == 0 ? point : Center(c);
				}
			}
		}
		Vector2 toGoal = goal - point;
		float length = toGoal.Length();
		for (float t = 0; t < length; t += GenerationSettings.TILE_SIZE)
		{
			Vector2I c = CellOf(point + toGoal / length * t);
			if (IsWalkable(c) && !float.IsInfinity(flow[c.Y * Width + c.X])) return Center(c);
		}
		return goal;
	}

	private Vector2I Downhill(float[] flow, Vector2I cell)
	{
		Vector2I best = cell;
		float bestValue = flow[cell.Y * Width + cell.X];
		for (int k = 0; k < 8; k++)
		{
			int nx = cell.X + Dx[k], ny = cell.Y + Dy[k];
			if (!CanStep(cell.X, cell.Y, Dx[k], Dy[k])) continue;
			float value = flow[ny * Width + nx];
			if (value < bestValue)
			{
				bestValue = value;
				best = new Vector2I(nx, ny);
			}
		}
		return best;
	}

	private bool CanStep(int x, int y, int dx, int dy) =>
		IsWalkable(x + dx, y + dy) && (dx == 0 || dy == 0 || (IsWalkable(x + dx, y) && IsWalkable(x, y + dy)));

	private float[] Flow(Vector2I target, bool wait)
	{
		Vector2I key = NearestWalkable(target);
		lock (flows)
		{
			if (flows.TryGetValue(key, out float[] cached)) return cached;
			if (!wait)
			{
				if (pending.Count < MaxPendingFlows && pending.Add(key)) Task.Run(() => Store(key, BuildFlow(key)));
				return null;
			}
		}
		float[] flow = BuildFlow(key);
		Store(key, flow);
		return flow;
	}

	private void Store(Vector2I key, float[] flow)
	{
		lock (flows)
		{
			pending.Remove(key);
			if (flows.ContainsKey(key)) return;
			flows[key] = flow;
			flowOrder.Enqueue(key);
			while (flowOrder.Count > FlowCacheSize) flows.Remove(flowOrder.Dequeue());
		}
	}

	private float[] BuildFlow(Vector2I goal)
	{
		var distance = new float[kinds.Length];
		Array.Fill(distance, float.PositiveInfinity);
		if (!IsWalkable(goal)) return distance;

		var queue = new PriorityQueue<int, float>();
		int start = goal.Y * Width + goal.X;
		distance[start] = 0;
		queue.Enqueue(start, 0);
		while (queue.TryDequeue(out int i, out float d))
		{
			if (d > distance[i]) continue;
			int x = i % Width, y = i / Width;
			for (int k = 0; k < 8; k++)
			{
				if (!CanStep(x, y, Dx[k], Dy[k])) continue;
				int j = (y + Dy[k]) * Width + x + Dx[k];
				float next = d + (k < 4 ? 1f : Mathf.Sqrt2);
				if (next >= distance[j]) continue;
				distance[j] = next;
				queue.Enqueue(j, next);
			}
		}
		return distance;
	}

	public Image ShoreField()
	{
		int r = Width * Height > 256 * 256 ? 2 : 4;
		int w = Width * r, h = Height * r;
		const double Far = 1e20;
		var field = new double[w * h];
		for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
				field[y * w + x] = kinds[(y / r) * Width + x / r] == Kind.Land ? 0 : Far;

		Parallel.For(0, h, y => Transform(field, y * w, 1, w));
		Parallel.For(0, w, x => Transform(field, x, w, h));

		var bytes = new byte[w * h];
		Parallel.For(0, h, y =>
		{
			for (int x = 0; x < w; x++)
			{
				float tiles = Mathf.Max(0f, ((float)Math.Sqrt(field[y * w + x]) - 0.5f) / r);
				bytes[y * w + x] = (byte)Mathf.RoundToInt(255f * (1f - Mathf.Min(tiles / FieldMaxDistance, 1f)));
			}
		});
		return Image.CreateFromData(w, h, false, Image.Format.R8, bytes);
	}

	private static void Transform(double[] field, int offset, int stride, int n)
	{
		var f = new double[n];
		var v = new int[n];
		var z = new double[n + 1];
		for (int i = 0; i < n; i++) f[i] = field[offset + i * stride];

		int k = 0;
		v[0] = 0;
		z[0] = double.NegativeInfinity;
		z[1] = double.PositiveInfinity;
		for (int q = 1; q < n; q++)
		{
			double s = Intersection(f, q, v[k]);
			while (s <= z[k])
			{
				k--;
				s = Intersection(f, q, v[k]);
			}
			k++;
			v[k] = q;
			z[k] = s;
			z[k + 1] = double.PositiveInfinity;
		}

		k = 0;
		for (int q = 0; q < n; q++)
		{
			while (z[k + 1] < q) k++;
			double d = q - v[k];
			field[offset + q * stride] = d * d + f[v[k]];
		}
	}

	private static double Intersection(double[] f, int q, int p) =>
		((f[q] + (double)q * q) - (f[p] + (double)p * p)) / (2.0 * q - 2.0 * p);
}
