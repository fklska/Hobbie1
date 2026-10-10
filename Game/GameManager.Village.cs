using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class GameManager
{
	public Dictionary<string, int> Researched = new();
	public Dictionary<string, float> ResearchEnds = new();

	private VillageHud villageHud;
	private string legacyProfession = Economy.StartProfession;

	private void ResetVillage()
	{
		LoadResearch(new());
		legacyProfession = Economy.StartProfession;
	}

	private void StartVillage()
	{
		villageHud = new VillageHud { Name = "VillageHud" };
		GetTree().Root.AddChild(villageHud);
	}

	public void OpenHire() => villageHud?.ShowHire();

	public float Now => IsInstanceValid(Survival) ? Survival.Day + Survival.TimeOfDay : 0f;

	public int ProfessionCount(string profession) => Workers.Count(w => IsInstanceValid(w) && w.Get("profession").AsString() == profession);

	public Godot.Collections.Dictionary GetProfession(string profession)
	{
		Economy.Profession info = Economy.Professions.GetValueOrDefault(profession) ?? Economy.Professions[Economy.StartProfession];
		return new() { ["title"] = info.Title, ["kinds"] = info.Kinds, ["frames"] = info.Frames, ["building"] = info.Building };
	}

	public float ResearchBonus(string building) => Economy.Researches.TryGetValue(building, out Economy.Research[] list)
		? list.Take(Researched.GetValueOrDefault(building)).Sum(r => r.Bonus)
		: 0f;

	public float JobSpeed(string profession)
	{
		Economy.Profession info = Economy.Professions.GetValueOrDefault(profession);
		return WorkSpeed * (1f + (info == null ? 0f : ResearchBonus(info.Building)));
	}

	private IEnumerable<Node2D> NodesOf(string id) => Placed.Where(p => p.Id == id && IsInstanceValid(p.Node)).Select(p => p.Node);

	private Vector2 Nearest(IEnumerable<Node2D> buildings, Vector2 from, Vector2 fallback)
	{
		Node2D best = buildings.OrderBy(b => BuildingCenter(b).DistanceSquaredTo(from)).FirstOrDefault();
		return best == null ? fallback : BuildingCenter(best);
	}

	public Vector2 DropOffPoint(string profession, Vector2 from)
	{
		IEnumerable<Node2D> points = Economy.Professions.TryGetValue(profession, out Economy.Profession info) ? NodesOf(info.Building) : Enumerable.Empty<Node2D>();
		if (IsInstanceValid(TownHall)) points = points.Append(TownHall);
		return Nearest(points, from, from);
	}

	public Vector2 WorkCenter(string profession, Vector2 from)
	{
		Vector2 hall = IsInstanceValid(TownHall) ? BuildingCenter(TownHall) : from;
		return Economy.Professions.TryGetValue(profession, out Economy.Profession info) ? Nearest(NodesOf(info.Building), from, hall) : hall;
	}

	public string HireLock()
	{
		if (!IsInstanceValid(TownHall)) return "Нет центра поселения";
		if (Workers.Count >= PopulationCap) return "Нет места: постройте или улучшите жильё";
		return "";
	}

	public void Hire(string profession)
	{
		if (Ended || !Economy.Professions.TryGetValue(profession, out Economy.Profession info)) return;
		string lockReason = HireLock();
		if (lockReason != "")
		{
			SoundManager.Instance.Play("error");
			Notify(lockReason);
			return;
		}
		if (!TrySpend(info.Cost)) return;
		SpawnWorker(profession, BuildingCenter(TownHall) + Vector2.FromAngle(GD.Randf() * Mathf.Tau) * 96f);
		SoundManager.Instance.Play("coin");
		Notify($"Нанят: {info.Title.ToLower()}");
		EmitSignal(SignalName.ProgressChanged);
	}

	private Node2D SpawnWorker(string profession, Vector2 position)
	{
		Node2D worker = GD.Load<PackedScene>("res://AI/Village/worker.tscn").Instantiate<Node2D>();
		worker.Position = position;
		worker.Set("profession", Economy.Professions.ContainsKey(profession) ? profession : Economy.StartProfession);
		EntitiesRoot.AddChild(worker);
		Workers.Add(worker);
		worker.TreeExiting += () => OnWorkerGone(worker);
		return worker;
	}

	private void OnWorkerGone(Node2D worker)
	{
		if (!Workers.Remove(worker)) return;
		EmitSignal(SignalName.ProgressChanged);
	}

	public Economy.Research NextResearch(string building)
	{
		if (!Economy.Researches.TryGetValue(building, out Economy.Research[] list)) return null;
		int done = Researched.GetValueOrDefault(building);
		return done < list.Length ? list[done] : null;
	}

	public bool Researching(string building) => ResearchEnds.ContainsKey(building);

	public float ResearchLeft(string building) => ResearchEnds.TryGetValue(building, out float end) ? Mathf.Max(0f, end - Now) : 0f;

	public string ResearchLock(string building)
	{
		Economy.Research next = NextResearch(building);
		if (next == null) return "Все улучшения изучены";
		if (Researching(building)) return "Улучшение уже идёт";
		if (!HasBuilding(building)) return "Сначала постройте";
		int need = Researched.GetValueOrDefault(building) + 1;
		if (Levels[building] < need) return $"Нужно: {Economy.Buildings[building].Levels[need - 1].Title} (ур. {need})";
		return "";
	}

	public void StartResearch(string building)
	{
		if (Ended) return;
		string lockReason = ResearchLock(building);
		if (lockReason != "")
		{
			Notify(lockReason);
			return;
		}
		Economy.Research next = NextResearch(building);
		if (!TrySpend(next.Cost)) return;
		ResearchEnds[building] = Now + next.Days;
		SoundManager.Instance.Play("build");
		Notify($"Началось улучшение «{next.Title}»: {Economy.DaysText(next.Days)}");
		EmitSignal(SignalName.ProgressChanged);
	}

	private void CheckResearch()
	{
		if (Ended) return;
		foreach (string building in ResearchEnds.Where(r => Now >= r.Value).Select(r => r.Key).ToList())
		{
			Economy.Research done = NextResearch(building);
			ResearchEnds.Remove(building);
			Researched[building]++;
			SoundManager.Instance.Play("skill_learn");
			Notify($"Улучшение готово: {done.Title}. {Economy.Professions.Values.First(p => p.Building == building).Plural} работают быстрее");
			EmitSignal(SignalName.ProgressChanged);
		}
	}

	private Godot.Collections.Dictionary SaveResearch()
	{
		Godot.Collections.Dictionary done = new(), ends = new();
		foreach (var (id, count) in Researched) done[id] = count;
		foreach (var (id, end) in ResearchEnds) ends[id] = end;
		return new() { ["done"] = done, ["ends"] = ends };
	}

	private void LoadResearch(Godot.Collections.Dictionary data)
	{
		foreach (string id in Economy.Researches.Keys) Researched[id] = 0;
		ResearchEnds.Clear();
		if (data.ContainsKey("done"))
			foreach (var (id, count) in data["done"].AsGodotDictionary())
				if (Economy.Researches.TryGetValue(id.AsString(), out Economy.Research[] list)) Researched[id.AsString()] = Mathf.Clamp(count.AsInt32(), 0, list.Length);
		if (data.ContainsKey("ends"))
			foreach (var (id, end) in data["ends"].AsGodotDictionary())
				if (NextResearch(id.AsString()) != null) ResearchEnds[id.AsString()] = end.AsSingle();
	}
}
