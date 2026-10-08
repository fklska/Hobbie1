using Godot;
using System.Collections.Generic;

public partial class SoundManager : Node
{
	public static SoundManager Instance { get; private set; }

	private static readonly Dictionary<string, (float Db, float Pitch)> Sfx = new()
	{
		["chop"] = (-6f, 0.08f),
		["mine"] = (-8f, 0.08f),
		["pickup"] = (-9f, 0.04f),
		["swing"] = (-9f, 0.1f),
		["hit"] = (-5f, 0.1f),
		["hurt"] = (-3f, 0.06f),
		["build"] = (-4f, 0.03f),
		["ui_click"] = (-12f, 0.05f),
		["error"] = (-10f, 0f),
		["coin"] = (-8f, 0.05f),
		["forge"] = (-6f, 0.02f),
		["giant_roar"] = (-2f, 0.05f),
		["giant_smash"] = (-2f, 0.06f),
		["magic_beam"] = (-5f, 0.03f),
		["collapse"] = (-3f, 0.05f),
		["step"] = (-17f, 0.12f),
	};

	private static readonly HashSet<string> Loud = new() { "giant_roar" };

	private const float MusicDb = -9f;
	private const float SilentDb = -60f;
	private const double MinRepeat = 0.04;
	private const double StepInterval = 0.34;

	private readonly Dictionary<string, AudioStream> streams = new();
	private readonly Dictionary<string, double> lastPlayed = new();
	private readonly Dictionary<string, double> lastThrottled = new();
	private readonly List<AudioStreamPlayer> flat = new();
	private readonly List<AudioStreamPlayer2D> spatial = new();
	private int flatNext;
	private int spatialNext;
	private AudioStreamPlayer music;
	private AudioStreamPlayer jingle;
	private AudioStream victory;
	private AudioStream defeat;
	private Tween musicTween;
	private bool musicOn;
	private double stepTimer;

	public override void _Ready()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;

		foreach (string id in Sfx.Keys) streams[id] = GD.Load<AudioStream>($"res://Audio/sfx/{id}.wav");
		for (int i = 0; i < 10; i++) flat.Add(AddPlayer(new AudioStreamPlayer { Bus = "SFX" }));
		for (int i = 0; i < 24; i++) spatial.Add(AddPlayer(new AudioStreamPlayer2D { Bus = "SFX", Attenuation = 1.5f }));

		AudioStreamOggVorbis theme = GD.Load<AudioStreamOggVorbis>("res://Audio/music/village_theme.ogg");
		theme.Loop = true;
		music = AddPlayer(new AudioStreamPlayer { Bus = "Music", VolumeDb = SilentDb, Stream = theme });
		jingle = AddPlayer(new AudioStreamPlayer { Bus = "Music", VolumeDb = -4f });
		victory = GD.Load<AudioStream>("res://Audio/music/victory.ogg");
		defeat = GD.Load<AudioStream>("res://Audio/music/defeat.ogg");

		GameManager.Instance.GameEnded += OnGameEnded;
		GetTree().NodeAdded += AddClickSound;
		foreach (Node button in GetTree().Root.FindChildren("*", "BaseButton", true, false)) AddClickSound(button);
		FadeMusic(true);
	}

	public override void _Process(double delta)
	{
		if (!musicOn && !GameManager.Instance.Ended && !jingle.Playing) FadeMusic(true);

		PlayerMainCharacter player = GameManager.Instance.Player;
		if (!IsInstanceValid(player) || player.IsDead || GetTree().Paused || player.Velocity.LengthSquared() < 400f)
		{
			stepTimer = 0;
			return;
		}
		stepTimer -= delta;
		if (stepTimer > 0) return;
		stepTimer = StepInterval;
		Play("step");
	}

	public void Play(string id)
	{
		if (Take(id) is not AudioStream stream) return;
		AudioStreamPlayer player = flat[flatNext];
		flatNext = (flatNext + 1) % flat.Count;
		player.Stream = stream;
		player.VolumeDb = Sfx[id].Db;
		player.PitchScale = Pitch(id);
		player.Play();
	}

	public void PlayAt(string id, Vector2 position)
	{
		if (Take(id) is not AudioStream stream) return;
		AudioStreamPlayer2D player = spatial[spatialNext];
		spatialNext = (spatialNext + 1) % spatial.Count;
		player.GlobalPosition = position;
		player.MaxDistance = Loud.Contains(id) ? 4000f : 1600f;
		player.Stream = stream;
		player.VolumeDb = Sfx[id].Db;
		player.PitchScale = Pitch(id);
		player.Play();
	}

	public void PlayEvery(string id, Vector2 position, float interval)
	{
		double now = Now();
		if (now - lastThrottled.GetValueOrDefault(id, double.NegativeInfinity) < interval) return;
		lastThrottled[id] = now;
		PlayAt(id, position);
	}

	public string HarvestSound(ResorseType type) => GameManager.KindOf(type) == "wood" ? "chop" : "mine";

	private void OnGameEnded(bool won, string reason)
	{
		FadeMusic(false);
		jingle.Stream = won ? victory : defeat;
		jingle.Play();
	}

	private void FadeMusic(bool on)
	{
		musicOn = on;
		musicTween?.Kill();
		if (on && !music.Playing) music.Play();
		musicTween = CreateTween();
		musicTween.TweenProperty(music, "volume_db", on ? MusicDb : SilentDb, on ? 3.0 : 1.0);
		if (!on) musicTween.TweenCallback(Callable.From(music.Stop));
	}

	private void AddClickSound(Node node)
	{
		if (node is not BaseButton button || button.HasMeta("click_sound")) return;
		button.SetMeta("click_sound", true);
		button.Pressed += () => Play("ui_click");
	}

	private T AddPlayer<T>(T player) where T : Node
	{
		AddChild(player);
		return player;
	}

	private AudioStream Take(string id)
	{
		if (!streams.TryGetValue(id, out AudioStream stream)) return null;
		double now = Now();
		if (now - lastPlayed.GetValueOrDefault(id, double.NegativeInfinity) < MinRepeat) return null;
		lastPlayed[id] = now;
		return stream;
	}

	private static float Pitch(string id) => 1f + (float)GD.RandRange(-Sfx[id].Pitch, Sfx[id].Pitch);

	private static double Now() => Time.GetTicksMsec() / 1000.0;
}
