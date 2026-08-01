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

	[ExportCategory("Render")]
	[Export] public AnimationPlayer anim;
	[Export] public CpuParticles2D miningParticle;

	public WorldScene WorldScene;
	public Vector2I lastChunkCell;
	public enum State { RUN, ATTACK, ACTION}

	public override void _Ready()
	{
		WorldScene = GetTree().Root.GetNode<WorldScene>("Map");
		lastChunkCell = Utils.GetChunkCoords(GlobalPosition);
	}

	public override void _Process(double delta)
	{
		UpdateChunks();
		SetEnviromentAlphaShader();
		HandAction(delta);
	}

	public override void _PhysicsProcess(double delta)
	{
		Run();
		MoveAndSlide();
	}

	public void SetEnviromentAlphaShader()
	{
		ShaderMaterial EnvShader = (ShaderMaterial)WorldScene.EnviromentLayer.Material;
		EnvShader.SetShaderParameter("player_pos", GlobalPosition);
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

		anim.Play(GetANimByDir(direction));
	}

	public void UpdateChunks()
	{
		if (IsInstanceValid(WorldScene)) WorldScene.UpdateChunkAroundPlayer(GlobalPosition);
	}

	public void HandAction(double delta)
	{
		if (Input.IsMouseButtonPressed(MouseButton.Left)) 
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
					ActionProgress.Value += delta * STRENCH * 10;

					if (ActionProgress.Value >= 100.0f)
					{
						HandActionResult(globalCell, chunk, localCell);
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

	public void HandActionResult(Vector2I gobalCell, Vector2I chunk, Vector2I LocalCell)
	{
		WorldScene.EnviromentLayer.EraseCell(gobalCell);
		WorldScene.GeneratorData.ChunkMap[chunk.X][chunk.Y].Map[LocalCell.X][LocalCell.Y].Resourse = ResorseType.None;
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

	public string GetANimByDir(Vector2 dir)
	{
		switch (dir)
		{
			case Vector2(0, 0):
				return "idleStatic";

			case Vector2(0, 1):
				return "runDown";

			case Vector2(1, 0):
				return "runRight";

			case Vector2(-1, 0):
				return "runLeft";

			case Vector2(0, -1):
				return "runUp";

			default:
				//GD.PrintErr("Unxepected Dir");
				return "idleStatic";
		}
	}
}
