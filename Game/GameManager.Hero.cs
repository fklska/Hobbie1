using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class GameManager
{
	[Signal] public delegate void HeroChangedEventHandler();

	public string HeroClass = "";
	public Dictionary<string, int> SkillRanks = new();

	private CanvasLayer heroHud;
	private ClassPicker classPicker;

	public HeroClasses.ClassInfo ClassInfo => HeroClasses.Get(HeroClass);

	public int Rank(string id) => SkillRanks.GetValueOrDefault(id);

	public int SkillPointsTotal => IsInstanceValid(Survival) ? Mathf.Max(1, Survival.Day) : 1;

	public int SkillPointsSpent => ClassInfo?.Skills.Sum(s => s.Costs.Take(Rank(s.Id)).Sum()) ?? 0;

	public int SkillPoints => Mathf.Max(0, SkillPointsTotal - SkillPointsSpent);

	public bool ChoosingClass => IsInstanceValid(classPicker);

	private void ResetHero()
	{
		HeroClass = "";
		SkillRanks.Clear();
	}

	private void StartHero()
	{
		heroHud = GD.Load<PackedScene>("res://Game/hero_hud.tscn").Instantiate<CanvasLayer>();
		GetTree().Root.AddChild(heroHud);
		if (!Restoring) OpenClassPicker();
	}

	private void OpenClassPicker()
	{
		if (IsInstanceValid(classPicker)) return;
		classPicker = new ClassPicker();
		GetTree().Root.AddChild(classPicker);
	}

	public void ChooseClass(string id)
	{
		HeroClasses.ClassInfo info = HeroClasses.Get(id);
		if (info == null) return;
		HeroClass = id;
		SkillRanks.Clear();
		SkillRanks[info.Innate] = 1;
		ApplyClass();
		if (IsInstanceValid(classPicker))
		{
			classPicker.QueueFree();
			classPicker = null;
		}
		GetTree().Paused = Ended;
		SoundManager.Instance.Play("skill_learn");
		Notify($"Класс: {info.Title.ToLower()}. Новое очко способностей каждый рассвет, дерево на K");
	}

	private void ApplyClass()
	{
		HeroClasses.ClassInfo info = ClassInfo;
		if (info == null || !IsInstanceValid(Player)) return;
		Player.anim.SpriteFrames = GD.Load<SpriteFrames>(info.Frames);
		Player.anim.Call("play_dir", "idle", Vector2.Zero, true);
		ApplyHeroStats();
		EmitSignal(SignalName.HeroChanged);
	}

	public string LearnLock(string id)
	{
		HeroClasses.Skill skill = HeroClasses.FindSkill(HeroClass, id);
		if (skill == null) return "Недоступно для этого класса";
		int rank = Rank(id);
		if (rank >= skill.MaxRank) return "Изучено полностью";
		if (skill.Requires != "" && Rank(skill.Requires) == 0)
			return $"Сначала изучите «{HeroClasses.FindSkill(HeroClass, skill.Requires).Title}»";
		if (SkillPoints < skill.Costs[rank]) return $"Нужно очков: {skill.Costs[rank]}";
		return "";
	}

	public bool LearnSkill(string id)
	{
		if (Ended) return false;
		string reason = LearnLock(id);
		if (reason != "")
		{
			Notify(reason);
			SoundManager.Instance.Play("error");
			return false;
		}
		SkillRanks[id] = Rank(id) + 1;
		SoundManager.Instance.Play("skill_learn");
		ApplyHeroStats();
		EmitSignal(SignalName.HeroChanged);
		return true;
	}

	public void OnSkillDawn()
	{
		if (ClassInfo == null) return;
		Notify($"Новое очко способностей: {SkillPoints}. Дерево на K");
		EmitSignal(SignalName.HeroChanged);
	}

	private Godot.Collections.Dictionary SaveHero()
	{
		var skills = new Godot.Collections.Dictionary();
		foreach (var (id, rank) in SkillRanks) skills[id] = rank;
		return new Godot.Collections.Dictionary { ["class"] = HeroClass, ["skills"] = skills };
	}

	private void LoadHero(Godot.Collections.Dictionary hero)
	{
		ResetHero();
		if (!hero.ContainsKey("class") || HeroClasses.Get(hero["class"].AsString()) == null)
		{
			OpenClassPicker();
			return;
		}
		HeroClass = hero["class"].AsString();
		if (hero.ContainsKey("skills"))
		{
			foreach (var (id, rank) in hero["skills"].AsGodotDictionary())
			{
				HeroClasses.Skill skill = HeroClasses.FindSkill(HeroClass, id.AsString());
				if (skill != null) SkillRanks[skill.Id] = Mathf.Clamp(rank.AsInt32(), 0, skill.MaxRank);
			}
		}
		SkillRanks[ClassInfo.Innate] = Mathf.Max(1, Rank(ClassInfo.Innate));
		ApplyClass();
	}
}
