using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerMainCharacter
{
	private const string SkillsDir = "res://Game/Skills/";
	private const float IgniteRange = 450f;
	private const float MeteorRange = 650f;

	private static readonly Dictionary<string, PackedScene> scenes = new();

	private readonly Dictionary<string, double> cooldowns = new();
	private double castTimer;
	private float castLength;
	private Action castDone;
	private double whirlTimer;
	private double whirlTick;
	private float whirlInterval;
	private int whirlHits;
	private int whirlDamage;
	private int whirlFacing;
	private Node2D whirlFx;
	private int comboStep;
	private double shakeTimer;
	private float shakeTime;
	private float shakeStrength;
	private Camera2D camera;

	public bool Casting => castTimer > 0;

	private float MoveScale => whirlTimer > 0 ? 0.6f : 1f;

	private static GameManager Game => GameManager.Instance;

	public double CooldownLeft(string id) => cooldowns.GetValueOrDefault(id);

	public float CooldownOf(HeroClasses.Skill skill)
	{
		float time = skill.Cooldown;
		if (skill.Id == "meteor" && Game.Rank("meteor") >= 2) time = 20f;
		if (Game.HeroClass == HeroClasses.Mage) time *= 1f - 0.1f * Game.Rank("focus");
		return time;
	}

	private float FirePower => 1f + 0.15f * Game.Rank("heat");

	private void HandSkills(double delta)
	{
		foreach (string id in cooldowns.Keys.ToList())
		{
			cooldowns[id] -= delta;
			if (cooldowns[id] <= 0) cooldowns.Remove(id);
		}
		UpdateShake(delta);
		if (castTimer > 0)
		{
			castTimer -= delta;
			ActionProgress.Value = ActionProgress.MaxValue * (1 - Mathf.Max(0, castTimer) / castLength);
			if (castTimer <= 0)
			{
				ActionProgress.Value = 0;
				Action done = castDone;
				castDone = null;
				done?.Invoke();
			}
		}
		if (whirlTimer > 0) UpdateWhirl(delta);
		BondRegen(delta);

		if (Grid.buildMode || Game.Ended) return;
		for (int i = 0; i < HeroClasses.SlotActions.Length; i++)
			if (Input.IsActionJustPressed(HeroClasses.SlotActions[i])) UseSlot(i, false);
	}

	public void ResetSkills()
	{
		castTimer = 0;
		castDone = null;
		StopWhirl();
		anim.SpeedScale = 1;
		ActionProgress.Value = 0;
	}

	public bool UseSlot(int slot, bool autoAim)
	{
		HeroClasses.Skill skill = Game.ClassInfo?.Skills.FirstOrDefault(s => s.Slot == slot);
		if (skill == null || IsDead || Game.Ended) return false;
		int rank = Game.Rank(skill.Id);
		if (rank == 0)
		{
			Game.Notify($"«{skill.Title}» ещё не изучено: откройте способности (K)");
			return false;
		}
		if (Casting || whirlTimer > 0 || CooldownLeft(skill.Id) > 0) return false;
		Node2D focus = autoAim ? NearestEnemy(GlobalPosition, skill.Id == "meteor" ? MeteorRange : IgniteRange) : NearestEnemy(GetGlobalMousePosition(), 70f);
		Vector2 target = autoAim ? focus?.GlobalPosition ?? GlobalPosition + FacingVector() * 200 : GetGlobalMousePosition();
		bool used = skill.Id switch
		{
			"heavy" => Heavy(rank),
			"whirl" => Whirl(rank),
			"quake" => Quake(rank),
			"ignite" => Ignite(rank, target, focus),
			"meteor" => Meteor(rank, target),
			"crow" or "wolf" or "bear" => Summon(skill.Id, rank),
			_ => false,
		};
		if (used) cooldowns[skill.Id] = CooldownOf(skill);
		return used;
	}

	private Node2D NearestEnemy(Vector2 point, float range) =>
		Enemies().Where(e => e.GlobalPosition.DistanceTo(point) <= range && e.GlobalPosition.DistanceTo(GlobalPosition) <= IgniteRange + 200)
			.OrderBy(e => e.GlobalPosition.DistanceSquaredTo(point)).FirstOrDefault();

	private Vector2 FacingVector() => Vector2.FromAngle((int)anim.Get("facing") * Mathf.Pi / 4);

	private Vector2 AimAt(Vector2 point)
	{
		Vector2 dir = point - GlobalPosition;
		return dir.LengthSquared() > 1 ? dir.Normalized() : FacingVector();
	}

	private void BeginCast(float time, string animation, Vector2 dir, Action done)
	{
		castTimer = castLength = time;
		castDone = done;
		attackAnimTimer = time;
		anim.SpeedScale = 1;
		Velocity = Vector2.Zero;
		DisableParticle(miningParticle);
		anim.Call("play_dir", animation, dir, true);
	}

	private void PlayAction(string animation, Vector2 dir, float speed = 1f)
	{
		anim.SpeedScale = speed;
		attackAnimTimer = (float)anim.Call("action_length", animation) / speed;
		anim.Call("play_dir", animation, dir, true);
	}

	private record Swing(float Interval, int Damage, float Range, float Arc, float AnimSpeed);

	private Swing NextSwing()
	{
		int damage = Game.HeroDamage;
		HeroClasses.ClassInfo info = Game.ClassInfo;
		if (info?.Id != HeroClasses.Warrior)
			return new Swing(AttackCooldown, Mathf.Max(1, Mathf.RoundToInt(damage * (info?.AttackScale ?? 1f))), AttackRange, 0.2f, 1f);
		int rank = Mathf.Max(1, Game.Rank("combo"));
		float share = 0.35f + 0.1f * rank;
		float interval = 0.27f - 0.02f * rank;
		comboStep = (comboStep + 1) % 3;
		if (comboStep == 0) return new Swing(interval, Mathf.Max(1, Mathf.RoundToInt(damage * share * 2)), AttackRange + 30, -0.2f, 2f);
		return new Swing(interval, Mathf.Max(1, Mathf.RoundToInt(damage * share)), AttackRange, 0.2f, 2f);
	}

	private bool AttackHeld() => Game.HeroClass == HeroClasses.Warrior
		? Input.IsActionPressed("attack") || Input.IsActionPressed("RightMouseButton")
		: Input.IsActionJustPressed("attack") || Input.IsActionJustPressed("RightMouseButton");

	private static IEnumerable<Node2D> Enemies(SceneTree tree) =>
		tree.GetNodesInGroup("enemies").OfType<Node2D>().Where(e => IsInstanceValid(e) && e.Get("hp").AsInt32() > 0);

	private IEnumerable<Node2D> Enemies() => Enemies(GetTree());

	private int HitArea(Vector2 center, float radius, Vector2 aim, float minDot, int damage, float knock, float stun)
	{
		int hits = 0;
		foreach (Node2D enemy in Enemies().ToList())
		{
			Vector2 to = enemy.GlobalPosition - center;
			float reach = radius + HitRadius(enemy);
			if (to.Length() > reach) continue;
			if (minDot > -1 && to.LengthSquared() > 1 && aim.Dot(to.Normalized()) < minDot) continue;
			Game.Damage(enemy, damage);
			hits++;
			if (knock > 0) Knockback(enemy, (to.LengthSquared() > 1 ? to.Normalized() : aim) * knock);
			if (stun > 0) ApplyEffect("stun.gd", enemy, stun);
		}
		return hits;
	}

	private static float HitRadius(Node2D enemy)
	{
		Variant r = enemy.Get("hit_radius");
		return r.VariantType == Variant.Type.Nil ? 16f : r.AsSingle();
	}

	private void Knockback(Node2D enemy, Vector2 push)
	{
		if (enemy.Get("mob_id").VariantType == Variant.Type.Nil || enemy.Get("hp").AsInt32() <= 0) return;
		Vector2 to = enemy.GlobalPosition + push;
		if (IsInstanceValid(WorldScene) && WorldScene.IsOcean(to)) return;
		enemy.CreateTween().TweenProperty(enemy, "global_position", to, 0.15f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
	}

	public static void ApplyEffect(string script, Node2D target, params Variant[] args)
	{
		GDScript effect = GD.Load<GDScript>(SkillsDir + script);
		effect.Call("apply", new Variant[] { target }.Concat(args).ToArray());
	}

	private static Node Spawn(string path)
	{
		if (!scenes.TryGetValue(path, out PackedScene scene)) scenes[path] = scene = GD.Load<PackedScene>(path);
		return scene.Instantiate();
	}

	public Node2D Fx(string animation, Vector2 at, float scale = 1f, int z = 3)
	{
		Node2D fx = (Node2D)Spawn(SkillsDir + "spell_fx.tscn");
		fx.GlobalPosition = at;
		fx.Scale = Vector2.One * scale;
		fx.ZIndex = z;
		(Game.EntitiesRoot ?? GetParent()).AddChild(fx);
		fx.Call("play_fx", animation);
		return fx;
	}

	public void Shake(float strength, float time)
	{
		if (strength < shakeStrength * (float)(shakeTimer / Mathf.Max(shakeTime, 0.01f))) return;
		shakeStrength = strength;
		shakeTimer = shakeTime = time;
	}

	private void UpdateShake(double delta)
	{
		camera ??= GetNodeOrNull<Camera2D>("Camera2D");
		if (camera == null) return;
		if (shakeTimer <= 0)
		{
			camera.Offset = Vector2.Zero;
			return;
		}
		shakeTimer -= delta;
		float k = shakeStrength * (float)Mathf.Max(0, shakeTimer / shakeTime);
		camera.Offset = new Vector2((float)GD.RandRange(-k, k), (float)GD.RandRange(-k, k));
	}

	private bool Heavy(int rank)
	{
		Vector2 aim = AimAt(GetGlobalMousePosition());
		BeginCast(0.9f, "heavy_windup", aim, () =>
		{
			PlayAction("heavy", aim);
			int damage = Mathf.RoundToInt(Game.HeroDamage * (2f + 0.5f * rank));
			HitArea(GlobalPosition, 175f, aim, 0.34f, damage, 70f, 0f);
			Fx("shockwave", GlobalPosition + aim * 70, 0.7f, 1);
			Shake(6, 0.25f);
			SoundManager.Instance.PlayAt("slam", GlobalPosition);
		});
		return true;
	}

	private bool Quake(int rank)
	{
		BeginCast(0.6f, "heavy_windup", FacingVector(), () =>
		{
			PlayAction("heavy", FacingVector());
			int damage = Mathf.RoundToInt(Game.HeroDamage * (rank >= 2 ? 2.6f : 2f));
			HitArea(GlobalPosition, 210f, Vector2.Zero, -2f, damage, 80f, rank >= 2 ? 2.2f : 1.5f);
			Fx("shockwave", GlobalPosition, 1.9f, 1);
			Shake(11, 0.45f);
			SoundManager.Instance.PlayAt("slam", GlobalPosition);
			SoundManager.Instance.PlayAt("giant_smash", GlobalPosition);
		});
		return true;
	}

	private bool Whirl(int rank)
	{
		whirlHits = rank >= 2 ? 6 : 4;
		whirlInterval = 0.3f;
		whirlTimer = whirlHits * whirlInterval;
		whirlTick = 0;
		whirlDamage = Mathf.Max(1, Mathf.RoundToInt(Game.HeroDamage * (rank >= 2 ? 0.7f : 0.6f)));
		whirlFx = Fx("whirl", GlobalPosition, 1.1f);
		whirlFx.Reparent(this);
		whirlFx.Position = new Vector2(0, -8);
		return true;
	}

	private void UpdateWhirl(double delta)
	{
		whirlTimer -= delta;
		whirlTick -= delta;
		whirlFacing = (int)(whirlTimer / 0.07) % 8;
		anim.SpeedScale = 3;
		attackAnimTimer = 0.1;
		anim.Call("play_dir", "attack", Vector2.FromAngle(-whirlFacing * Mathf.Pi / 4), false);
		if (whirlTick <= 0 && whirlHits > 0)
		{
			whirlTick = whirlInterval;
			whirlHits--;
			HitArea(GlobalPosition, 150f, Vector2.Zero, -2f, whirlDamage, 18f, 0f);
			SoundManager.Instance.PlayAt("swing", GlobalPosition);
		}
		if (whirlTimer <= 0) StopWhirl();
	}

	private void StopWhirl()
	{
		whirlTimer = 0;
		whirlHits = 0;
		if (IsInstanceValid(whirlFx)) whirlFx.QueueFree();
		whirlFx = null;
		anim.SpeedScale = 1;
	}

	private bool Ignite(int rank, Vector2 target, Node2D focus)
	{
		Vector2 aim = AimAt(target);
		target = GlobalPosition + (target - GlobalPosition).LimitLength(IgniteRange);
		SoundManager.Instance.PlayAt("fire_cast", GlobalPosition);
		BeginCast(0.25f, "cast", aim, () =>
		{
			Node2D ball = (Node2D)Spawn(SkillsDir + "fireball.tscn");
			(Game.EntitiesRoot ?? GetParent()).AddChild(ball);
			float hd = Game.HeroDamage * FirePower;
			ball.Call("launch", GlobalPosition + aim * 18 + new Vector2(0, -36), target,
				Mathf.Max(1, Mathf.RoundToInt(hd * 0.6f)), 80f, hd * 0.4f, 3f + rank, PyreRadius(), focus);
			PlayAction("cast", aim);
		});
		return true;
	}

	private float PyreRadius() => Game.Rank("pyre") switch { 0 => 0f, 1 => 110f, _ => 170f };

	private bool Meteor(int rank, Vector2 target)
	{
		Vector2 aim = AimAt(target);
		target = GlobalPosition + (target - GlobalPosition).LimitLength(MeteorRange);
		if (IsInstanceValid(WorldScene) && WorldScene.IsOcean(target)) target = WorldScene.Walkable(target);
		BeginCast(0.6f, "cast", aim, () =>
		{
			Node2D meteor = (Node2D)Spawn(SkillsDir + "meteor.tscn");
			(Game.EntitiesRoot ?? GetParent()).AddChild(meteor);
			float hd = Game.HeroDamage * FirePower;
			int damage = Mathf.RoundToInt(hd * (rank >= 2 ? 4f : 3f) + (rank >= 2 ? 90 : 60) * FirePower);
			meteor.Call("drop", target, damage, 140f, hd * 0.4f, 3f + Mathf.Max(1, Game.Rank("ignite")), PyreRadius());
		});
		return true;
	}

	private static readonly Dictionary<string, string> SummonScenes = new()
	{
		["crow"] = "res://AI/Summons/crow.tscn",
		["wolf"] = "res://AI/Summons/spirit_wolf.tscn",
		["bear"] = "res://AI/Summons/bear.tscn",
	};

	private bool Summon(string id, int rank)
	{
		Vector2 aim = FacingVector();
		BeginCast(0.5f, "cast", aim, () =>
		{
			foreach (Node old in GetTree().GetNodesInGroup("summon_" + id)) old.Call("dismiss");
			float wild = 1f + 0.2f * Game.Rank("wild");
			float life = 1f + 0.3f * Game.Rank("bond");
			int hd = Game.HeroDamage;
			(int count, float hp, float damage, float lifetime) = id switch
			{
				"crow" => (rank, 20f, 2 + 0.25f * hd, 30f),
				"wolf" => (rank, 70f, 4 + 0.45f * hd, 45f),
				_ => (1, rank >= 2 ? 360f : 260f, 10 + hd, rank >= 2 ? 60f : 45f),
			};
			for (int i = 0; i < count; i++)
			{
				Vector2 at = GlobalPosition + Vector2.FromAngle(aim.Angle() + (i - (count - 1) / 2f) * 0.9f) * 70;
				if (IsInstanceValid(WorldScene)) at = WorldScene.Walkable(at);
				Node2D beast = (Node2D)Spawn(SummonScenes[id]);
				beast.GlobalPosition = at;
				(Game.EntitiesRoot ?? GetParent()).AddChild(beast);
				beast.Call("setup", Mathf.RoundToInt(hp * wild), Mathf.Max(1, Mathf.RoundToInt(damage * wild)), lifetime * life, i);
				Fx("summon", at, id == "bear" ? 1.4f : 1f, 1);
			}
			SoundManager.Instance.PlayAt("summon", GlobalPosition);
			SoundManager.Instance.PlayAt(id == "crow" ? "crow" : "growl", GlobalPosition);
			PlayAction("cast", aim);
		});
		return true;
	}

	private void BondRegen(double delta)
	{
		int bond = Game.Rank("bond");
		if (bond == 0 || Health >= MaxHealth || GetTree().GetNodeCountInGroup("summons") == 0) return;
		Heal((float)(2 * bond * delta));
	}
}
