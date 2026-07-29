using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

[Tool]
[GlobalClass]
public partial class GeneratorV3 : Node2D
{
	[Export] public GeneratorData genData;
	[Export] public Godot.Collections.Array<GenerationStep> steps;
	[Export] public string LandDualMapPath;
	public TileMapLayer MainTileMapPrefab;

	[Export] public string EnviromentTileMapPath;
	public TileMapLayer EnvirometLayer;

	[Export] public Node2D ResorseNode; 

	[Export] public TextureRect FinalMap;
	[Export] public TextureRect HeightMap, HeatMap, MoistureMap;
	[Export] public TextureRect DebugLatitudeMask, DebugHeatFractal, DebugLatFractalMask;
	[Export] public TextureRect DebugMoistureFractal;


	public override void _Ready()
	{
		genData.ResetData();
		MainTileMapPrefab = (TileMapLayer)GD.Load<PackedScene>(LandDualMapPath).Instantiate();
		EnvirometLayer = (TileMapLayer)GD.Load<PackedScene>(EnviromentTileMapPath).Instantiate();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		//GetCell();
	}

	public async Task Generate(ProgressBar progress)
	{
		Stopwatch generation = Stopwatch.StartNew();
		Stopwatch resetData = Stopwatch.StartNew();
		Stopwatch ExecuteStep = Stopwatch.StartNew();

		progress.MaxValue = 2 + steps.Count;
		genData.ResetData();
		progress.Value++;
		await ToSignal(GetTree(), "process_frame");
		resetData.Stop();
		GD.Print($"GEN data Reseted in {resetData}");

		foreach (var step in steps)
		{
			step.Execute(genData);
			progress.Value++;
			await ToSignal(GetTree(), "process_frame");
		}
		ExecuteStep.Stop();
		GD.Print($"Every Step Executed in {ExecuteStep}");

		ResourceSaver.Save(genData, $"res://SavedWorlds/{genData.WorldName}.tres");

		GeneratorData dupl = ResourceLoader.Load<GeneratorData>($"res://SavedWorlds/{genData.WorldName}.tres");

		GenerateScene(dupl);

		ResourceSaver.Save(dupl.ToSimpleData(), String.Format("res://SavedWorlds/__SIMPLE{0}.tres", dupl.WorldName));

		progress.Value++;
		generation.Stop();
		GD.Print($"Generated in {generation}");
	}

	public void ClearTileMapTemlate(TileMapLayer map)
	{
		map.Clear();
	}

	public void preRender(GeneratorData genData)
	{
		HeightMap.Texture = ImageTexture.CreateFromImage(genData.HeightMap);
		HeatMap.Texture = ImageTexture.CreateFromImage(genData.HeatMap);
		MoistureMap.Texture = ImageTexture.CreateFromImage(genData.MoistureMap);

		DebugLatitudeMask.Texture = ImageTexture.CreateFromImage(genData.DebugLatitudeMask);
		DebugHeatFractal.Texture = ImageTexture.CreateFromImage(genData.DebugHeatFractal);
		DebugLatFractalMask.Texture = ImageTexture.CreateFromImage(genData.DebugLatFractal);

		DebugMoistureFractal.Texture = ImageTexture.CreateFromImage(genData.DebugMoistureFractal);

		Image finalRender = Image.CreateEmpty(genData.mapSize.X, genData.mapSize.Y, false, Image.Format.Rgba8);

		for (int chunk_x = 0; chunk_x < GenerationSettings.MAP_CHUNK_SIZE_X; chunk_x++)
		{
			for (int chunk_y = 0; chunk_y < GenerationSettings.MAP_CHUNK_SIZE_Y; chunk_y++)
			{
				ChunkData chunk = genData.ChunkMap[chunk_x][chunk_y];
				int local_x = 0;
				for (int x = chunk.rect.X; x < chunk.rect.Z; x++)
				{
					int local_y = 0;
					for (int y = chunk.rect.Y; y < chunk.rect.W; y++)
					{
						//GD.Print($"Coords: {x} {y}");
						finalRender.SetPixel(x, y, GenerationUtils.getTileTypeColor(chunk.Map[local_x][local_y].Type));
						local_y++;
					}
					local_x++;
				}
			}
		}
		FinalMap.Texture = ImageTexture.CreateFromImage(finalRender);
	}
	

	public void MapRender(GeneratorData genData, TileMapLayer map, Node2D ResourseRootNode, Node2D owner)
	{
		ClearTileMapTemlate(map);

		for (int x = 0; x < genData.mapSize.X; x++)
		{
			for (int y = 0; y < genData.mapSize.Y; y++)
			{
				//map.SetCell(new Vector2I(x, y), GenerationUtils.getTileTypeAtlas(genData.Map[x][y].Type), new Vector2I(2, 1), 0);

				ResorseType currType = ResorseType.None; //genData.Map[x][y].Resourse;
				if (currType != ResorseType.None)
				{
					Node2D prefab = new Node2D(); //(Node2D)GenerationUtils.getResorsePrefabByType(genData.Map[x][y].Resourse).Instantiate();
					Vector2 offset = new Vector2I(GD.RandRange(-50, 50), GD.RandRange(-50, 50));
					prefab.Position = new Vector2(x * GenerationUtils.TILE_SIZE, y * GenerationUtils.TILE_SIZE); //+ offset;
					ResourseRootNode.AddChild(prefab);
					prefab.Owner = owner;
				}
			}
		}
	}

	public void GenerateScene(GeneratorData genData)
	{
		var PackedScene = new PackedScene();
		WorldScene Map = (WorldScene)GenerationUtils.SetUpWorldNode("Map", genData);
		TileMapLayer MainMap = (TileMapLayer)GenerationUtils.SetNode2d("DualMap", MainTileMapPrefab, Map);
		TileMapLayer EnvMap = (TileMapLayer)GenerationUtils.SetNode2d("EnviromentLayer", EnvirometLayer, Map);

		Node2D Enviroment = GenerationUtils.SetNode2d("Enviroment", Map);

		//MapRender(genData, MainMap, Enviroment, Map);

		PackedScene.Pack(Map);

		ResourceSaver.Save(PackedScene, $"res://SavedWorlds/{Map.WorldName}.tscn");
	}

	public override void _Input(InputEvent @event)
	{
		/*if(!Engine.IsEditorHint())
		{
			if (@event.IsActionPressed("LeftMouseButton"))
			{
				LoadChunk(GenerationUtils.PixelToChunkCoord(GetGlobalMousePosition()), MainTileMapPrefab, ResorseNode, MainTileMapPrefab);
			}

			if (@event.IsActionPressed("RightMouseButton"))
			{
				UnloadChunk(GenerationUtils.PixelToChunkCoord(GetGlobalMousePosition()), MainTileMapPrefab);
			}
		}
		
		if (@event.IsActionPressed("LeftMouseButton"))
		{
			Vector2I coords = MainTileMapPrefab.LocalToMap(GetGlobalMousePosition());
			float heightValue = genData.Map[coords.X, coords.Y].heightValue;
			float heatValue = genData.Map[coords.X, coords.Y].heatValue;
			float moistureValue = genData.Map[coords.X, coords.Y].moistureValue;
			GD.Print(String.Format("Coords: {0};\nHeight: {1};\nHeat: {2};\nMoisture: {3};\n", [coords, heightValue, heatValue, moistureValue]));
		}

		if (@event.IsActionPressed("DEBUG"))
		{
			GD.Print("WORK");
			foreach (var step in steps)
			{
				step.Execute(genData);
			}
			_Ready();
		}
		*/

	}

	[Export] public bool DebugInfo;
	public Vector2I lastcell = Vector2I.Zero;
	public void GetCell()
	{
		Vector2 mouseCoor = GetGlobalMousePosition();
		Vector2I cell = MainTileMapPrefab.LocalToMap(mouseCoor);
		if (cell.X < genData.mapSize.X && cell.Y < genData.mapSize.Y && DebugInfo && cell != lastcell)
		{
			lastcell = cell;
			//float height = genData.Map[cell.X][cell.Y].heightValue;
			//float heat = genData.Map[cell.X][cell.Y].heatValue;
			//float moisture = genData.Map[cell.X][cell.Y].moistureValue;
			//TileType tileType = genData.Map[cell.X][cell.Y].Type;
			//GD.Print(String.Format("Coords: {0};\nHeight: {1};\nHeat: {2};\nMoisture: {3};\nBiome: {4};\n", [cell, height, heat, moisture, tileType]));
		}
	}
}
