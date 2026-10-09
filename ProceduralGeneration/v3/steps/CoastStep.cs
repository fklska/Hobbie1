using Godot;
using System;

[GlobalClass]
public partial class CoastStep : GenerationStep
{
	[Export] public int ShallowWidth = 2;
	[Export] public int ShallowExtra = 2;
	[Export] public int BeachWidth = 1;
	[Export] public int BeachExtra = 1;
	[Export] public float Frequency = 0.06f;
	[Export] public int LakeShallowWidth = 2;

	public override void Execute(WorldGen gen)
	{
		if (!Enabled) return;
		FastNoiseLite noise = gen.Noise("coast", Frequency, 2);
		int[] fromLand = gen.Distance(i => gen.Surfaces[i] != WorldGen.Surface.Ocean, true, ShallowWidth + ShallowExtra);
		int[] fromOcean = gen.Distance(i => gen.Surfaces[i] == WorldGen.Surface.Ocean, true, BeachWidth + BeachExtra);
		int[] fromShore = gen.Distance(i => gen.Surfaces[i] == WorldGen.Surface.Land || gen.Surfaces[i] == WorldGen.Surface.River, true, LakeShallowWidth + 1);

		for (int i = 0; i < gen.Count; i++)
		{
			float n = Mathf.Clamp(noise.GetNoise2D(i % gen.Width, i / gen.Width) * 1.5f + 0.5f, 0f, 1f);
			if (gen.Surfaces[i] == WorldGen.Surface.Ocean)
			{
				if (fromLand[i] <= ShallowWidth + Mathf.RoundToInt(ShallowExtra * n)) gen.Biomes[i] = (byte)TileType.TropicWater;
			}
			else if (gen.Surfaces[i] == WorldGen.Surface.Lake)
			{
				if (fromShore[i] > LakeShallowWidth) gen.Biomes[i] = (byte)TileType.DeepWater;
			}
			else if (gen.Surfaces[i] == WorldGen.Surface.Land && fromOcean[i] <= BeachWidth + Mathf.RoundToInt(BeachExtra * n))
			{
				gen.Biomes[i] = (byte)TileType.Desert;
			}
		}
	}
}
