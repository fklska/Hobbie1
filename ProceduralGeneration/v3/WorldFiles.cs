using Godot;

public static class WorldFiles
{
	public const string Dir = GenerationSettings.SAVED_WORLDS_DIR;
	public const string WorldScene = "res://ProceduralGeneration/v3/world.tscn";
	public const string WorldSceneUid = "uid://b0g027vyndma1";

	public static string DataPath(string name) => Dir + name + WorldMap.Extension;

	public static string PreviewPath(string name) => Dir + name + ".png";

	public static string ScenePath(string name) => Dir + name + ".tscn";

	public static string SimplePath(string name) => Dir + "__SIMPLE" + name + ".tres";

	public static string LegacyPath(string name) => Dir + name + ".tres";

	public static string NameOf(string scenePath) => scenePath.GetFile().GetBaseName();

	public static bool NeedsUpgrade(string scenePath)
	{
		string name = NameOf(scenePath);
		return !FileAccess.FileExists(DataPath(name)) && FileAccess.FileExists(LegacyPath(name));
	}

	public static Error Save(WorldMap map, string name, Image preview)
	{
		DirAccess.MakeDirRecursiveAbsolute(Dir);
		Error error = map.Save(DataPath(name));
		if (error != Error.Ok) return error;
		preview.SavePng(PreviewPath(name));
		error = WriteScene(name);
		if (error != Error.Ok) return error;
		var simple = new SimpleGeneratorData
		{
			mapSize = map.Size,
			SpawnPoint = (Vector2I)map.SpawnPosition,
			WorldName = map.Name,
			seed = map.Seed,
			fullDataPath = DataPath(name),
			PreviewPath = PreviewPath(name),
		};
		return ResourceSaver.Save(simple, SimplePath(name));
	}

	private static Error WriteScene(string name)
	{
		using FileAccess file = FileAccess.Open(ScenePath(name), FileAccess.ModeFlags.Write);
		if (file == null) return FileAccess.GetOpenError();
		file.StoreString(
			"[gd_scene load_steps=2 format=3]\n\n" +
			$"[ext_resource type=\"PackedScene\" uid=\"{WorldSceneUid}\" path=\"{WorldScene}\" id=\"1\"]\n\n" +
			"[node name=\"Map\" instance=ExtResource(\"1\")]\n" +
			$"WorldPath = \"{DataPath(name).CEscape()}\"\n");
		return Error.Ok;
	}

	public static bool Upgrade(string scenePath)
	{
		string name = NameOf(scenePath);
		var legacy = ResourceLoader.Load<GeneratorData>(LegacyPath(name), null, ResourceLoader.CacheMode.Ignore);
		if (legacy?.ChunkMap == null) return false;
		WorldMap map = WorldMap.FromLegacy(legacy);
		if (string.IsNullOrEmpty(map.Name)) map.Name = name;
		if (Save(map, name, map.Preview()) != Error.Ok) return false;
		WorldMap check = WorldMap.Load(DataPath(name));
		if (check == null || check.Width != map.Width || check.Height != map.Height) return false;
		DirAccess.RemoveAbsolute(LegacyPath(name));
		return true;
	}

	public static void Delete(string scenePath)
	{
		string name = NameOf(scenePath);
		foreach (string path in new[] { DataPath(name), PreviewPath(name), ScenePath(name), SimplePath(name), LegacyPath(name) })
			if (FileAccess.FileExists(path)) DirAccess.RemoveAbsolute(path);
	}
}
