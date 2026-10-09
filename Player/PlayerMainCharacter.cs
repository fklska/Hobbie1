using Godot;
using Godot1.Globals;
using System;
using System.Threading.Tasks;

public partial class PlayerMainCharacter : CharacterBody2D
{
	[Export] public Control InventoryManager;
	[Export] public Control HotBar;

	[ExportCategory("Stats")]
	[Export] public int AGILITY = 10;
	[Export] public int STRENCH = 10;
	[Export] public int INTELECT = 10;
	[Export] public float SPEED = 20;
	[Export] public ProgressBar ActionProgress;

	[ExportCategory("Combat")]
	[Export] public int MaxHealth = 100;
	[Export] public float AttackRange = 140;
	[Export] public float AttackCooldown = 0.5f;
	[Export] public float RegenDelay = 4f;
	[Export] public float RegenPerSecond = 4f;

	[ExportCategory("Render")]
	[Export] public AnimatedSprite2D anim;
	[Export] public CpuParticles2D miningParticle;

	public WorldScene WorldScene;
	public Vector2I lastChunkCell;
	public enum State { RUN, ATTACK, ACTION}

	public float Health;
	public bool IsDead;
	private double attackTimer;
	private double attackAnimTimer;
	private double sinceDamage;
	private Godot.Range hpBar;

	public override void _Ready()
	{
		WorldScene = GetTree().Root.GetNode<WorldScene>("Map");
		lastChunkCell = Utils.GetChunkCoords(GlobalPosition);
		hpBar = GetNode<Godot.Range>("PlayerUI/PlayerHealthBar/HBoxContainer/VBoxContainer/HPbar");
		Health = MaxHealth;
		UpdateHealthBar();
		AddToGroup("village");
	}

	public override void _Process(double delta)
	{
		if (IsDead) return;
		UpdateChunks();
		RenderingServer.GlobalShaderParameterSet("hero_position", GlobalPosition);
		HandAction(delta);
		HandAttack(delta);
		Regenerate(delta);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsDead) return;
		Run();
		MoveAndSlide();
	}

	public void TakeDamage(int amount)
	{
		if (IsDead) return;
		Health = Math.Max(0, Health - amount);
		sinceDamage = 0;
		UpdateHealthBar();
		Modulate = new Color(1, 0.5f, 0.5f);
		CreateTween().TweenProperty(this, "modulate", Colors.White, 0.2f);
		if (Health <= 0) Die();
	}

	private void Die()
	{
		IsDead = true;
		Velocity = Vector2.Zero;
		DisableParticle(miningParticle);
		anim.Call("play_dir", "death", Vector2.Zero, true);
		GameManager.Instance.OnHeroDied();
	}

	public void SetMaxHealth(int value)
	{
		Health = Math.Clamp(Health + value - MaxHealth, 1, value);
		MaxHealth = value;
		UpdateHealthBar();
	}

	private void Regenerate(double delta)
	{
		sinceDamage += delta;
		if (sinceDamage < RegenDelay || Health >= MaxHealth) return;
		Health = Math.Min(MaxHealth, Health + (float)(RegenPerSecond * delta));
		UpdateHealthBar();
	}

	private void UpdateHealthBar()
	{
		hpBar.MaxValue = MaxHealth;
		hpBar.Value = Health;
	}

	public void Heal(float amount)
	{
		Health = Math.Min(MaxHealth, Health + amount);
		UpdateHealthBar();
	}

	public void Respawn(Vector2 at)
	{
		GlobalPosition = at;
		Velocity = Vector2.Zero;
		Health = MaxHealth;
		sinceDamage = 0;
		IsDead = false;
		UpdateHealthBar();
	}

	public void HandAttack(double delta)
	{
		attackTimer -= delta;
		attackAnimTimer -= delta;
		if (attackTimer > 0 || Grid.buildMode) return;
		if (!Input.IsActionJustPressed("attack") && !Input.IsActionJustPressed("RightMouseButton")) return;

		attackTimer = AttackCooldown;
		Vector2 aim = (GetGlobalMousePosition() - GlobalPosition).Normalized();
		attackAnimTimer = (float)anim.Call("action_length", "attack");
		anim.Call("play_dir", "attack", aim, true);
		SoundManager.Instance.Play("swing");

		int damage = GameManager.Instance.HeroDamage;
		foreach (Node node in GetTree().GetNodesInGroup("enemies"))
		{
			if (node is not Node2D enemy) continue;
			Vector2 toEnemy = enemy.GlobalPosition - GlobalPosition;
			if (toEnemy.Length() <= AttackRange && aim.Dot(toEnemy.Normalized()) > 0.2f)
			{
				GameManager.Instance.Damage(enemy, damage);
			}
		}
	}

	private Vector2I focusCell;
	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("LeftMouseButton"))
		{
			UpdateFocusCell();
		}
		if (@event.IsActionReleased("LeftMouseButton"))
		{
			DisableParticle(miningParticle);
			ActionProgress.Value = 0;
		}
	}

	public void Run()
	{
		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down").Normalized();
		if (direction != Vector2.Zero)
		{
			Velocity = direction * SPEED * AGILITY;
		}
		else
		{
			Velocity = Vector2.Zero;
		}

		if (attackAnimTimer > 0) return;
		if (direction != Vector2.Zero) anim.Call("play_dir", "walk", direction);
		else if (miningParticle.Emitting) anim.Call("play_dir", "work", miningParticle.GlobalPosition - GlobalPosition);
		else anim.Call("play_dir", "idle", Vector2.Zero);
	}

	public void UpdateChunks()
	{
		if (IsInstanceValid(WorldScene)) WorldScene.UpdateChunkAroundPlayer(GlobalPosition);
	}

	public void HandAction(double delta)
	{
		if (Input.IsMouseButtonPressed(MouseButton.Left) && !Grid.buildMode) 
		{
			Vector2 clickPos = GetGlobalMousePosition();
			Vector2I globalCell = Utils.GetGlobalCell(clickPos);

			if (IsActionInValidRadius(clickPos))
			{
				if (globalCell != focusCell)
				{
					UpdateFocusCell();
					return;
				}

				Vector2I chunk = Utils.GetChunkCoords(clickPos);
				Vector2I localCell = Utils.GetLocalCell(clickPos);

				Tile tile = WorldScene.getTile(chunk, localCell);

				if (tile.Resourse != ResorseType.None)
				{
					EnableParticle(miningParticle, clickPos);
					SoundManager.Instance.PlayEvery(SoundManager.Instance.HarvestSound(tile.Resourse), clickPos, 0.33f);
					ActionProgress.Value += delta * STRENCH * 10 * GameManager.Instance.ToolSpeed;

					if (ActionProgress.Value >= 100.0f)
					{
						HandActionResult(globalCell);
					}
				}
				else
				{
					DisableParticle(miningParticle);
					ActionProgress.Value = 0;
				}
			}
		}
	}

	public void HandActionResult(Vector2I gobalCell)
	{
		GameManager.Instance.AddHarvest(WorldScene.HarvestTile(gobalCell));
		SoundManager.Instance.Play("pickup");
		ActionProgress.Value = 0;
	}

	public void EnableParticle(CpuParticles2D particle, Vector2 pos)
	{
		particle.GlobalPosition = pos;
		particle.Emitting = true;
	}

	public void DisableParticle(CpuParticles2D particle)
	{
		particle.Emitting = false;
	}

	public const int ActionRadius = 200;
	public const int SquareActionRadius = ActionRadius * ActionRadius;

	public bool IsActionInValidRadius(Vector2 coords)
	{
		if (GlobalPosition.DistanceSquaredTo(coords) > SquareActionRadius) return false;

		if (coords.X < 0 || coords.Y < 0 || coords > WorldScene.GeneratorData.mapSize * GenerationSettings.TILE_SIZE)
		{
			GD.PrintErr("OutOfMap");
			return false;
		}
		return true;
	}

	public void UpdateFocusCell()
	{
		focusCell = Utils.GetGlobalCell(GetGlobalMousePosition());
		ActionProgress.Value = 0;
	}
}
