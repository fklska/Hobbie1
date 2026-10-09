using Godot;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

[GlobalClass]
public partial class GeneratorV3 : Node
{
	[Export] public Godot.Collections.Array<GenerationStep> steps;
	[Export] public Gradient HeightGradient;
	[Export] public Gradient HeatGradient;
	[Export] public Gradient MoistureGradient;

	[Signal] public delegate void StageChangedEventHandler(string stage, int index, int count);
	[Signal] public delegate void GenerationFinishedEventHandler(bool success, string worldName);
	[Signal] public delegate void WorldUpgradedEventHandler(string scenePath, bool success);

	public bool Busy { get; private set; }

	private Image[] layers = Array.Empty<Image>();

	public Image GetLayer(int index) => index >= 0 && index < layers.Length ? layers[index] : null;

	public async void StartGeneration(ProgressBar progress, string worldName, int seed, int size)
	{
		try
		{
			string name = await Generate(progress, worldName, seed, size);
			EmitSignal(SignalName.GenerationFinished, true, name);
		}
		catch (Exception e)
		{
			GD.PushError(e.ToString());
			EmitSignal(SignalName.GenerationFinished, false, "");
		}
	}

	public async Task<string> Generate(ProgressBar progress, string worldName, int seed, int size)
	{
		Busy = true;
		Stopwatch watch = Stopwatch.StartNew();
		string name = string.IsNullOrWhiteSpace(worldName) ? GenerationUtils.GenerateNameWorld() : worldName.Trim();
		int count = steps.Count + 1;
		int index = 0;
		if (progress != null)
		{
			progress.MaxValue = count;
			progress.Value = 0;
		}

		void Stage(string stage)
		{
			if (progress != null) progress.Value = index;
			EmitSignal(SignalName.StageChanged, stage, index, count);
			index++;
		}

		try
		{
			var gen = new WorldGen(size, size, seed);
			foreach (GenerationStep step in steps)
			{
				Stage(step.GetType().Name);
				await Task.Run(() => step.Execute(gen));
			}

			Stage("save");
			await Task.Run(() =>
			{
				WorldMap map = gen.ToMap(name);
				Image preview = map.Preview();
				layers = new[]
				{
					preview,
					gen.Layer(gen.Elevation, HeightGradient),
					gen.Layer(gen.Temperature, HeatGradient),
					gen.Layer(gen.Moisture, MoistureGradient),
				};
				Error error = WorldFiles.Save(map, name, preview);
				if (error != Error.Ok) throw new Exception($"Не удалось сохранить мир «{name}»: {error}");
			});
			if (progress != null) progress.Value = count;
		}
		finally
		{
			Busy = false;
		}
		GD.Print($"Generated {name} ({size}x{size}, seed {seed}) in {watch.Elapsed}");
		return name;
	}

	public bool NeedsUpgrade(string scenePath) => WorldFiles.NeedsUpgrade(scenePath);

	public async void UpgradeWorld(string scenePath)
	{
		bool ok;
		try
		{
			ok = await Task.Run(() => WorldFiles.Upgrade(scenePath));
		}
		catch (Exception e)
		{
			GD.PushError(e.ToString());
			ok = false;
		}
		EmitSignal(SignalName.WorldUpgraded, scenePath, ok);
	}

	public bool UpgradeWorldNow(string scenePath) => WorldFiles.Upgrade(scenePath);

	public void DeleteWorld(string scenePath) => WorldFiles.Delete(scenePath);
}
