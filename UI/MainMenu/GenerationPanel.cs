// ï»¿
using Godot;
using GodotPlugins.Game;
using System;

[Tool]
[GlobalClass]
public partial class GenerationPanel : Control
{
    [ExportGroup("Рендер")]
    [Export] public TextureRect heightMap, heatMap, moistureMap, biomeMap;

    [Export] public SpinBox seedLabel;
    [Export] public ProgressBar progressBar;

    [Export] public Label statusLabel;

    public GeneratorV3 generator;

    public override void _Ready()
    {
        base._Ready();
        generator = (GeneratorV3)GetTree().Root.GetNode("GENERATOR");
        seedLabel.Value = generator.genData.seed;
    }

    public void _on_h_slider_value_changed(int value)
    {
        GenerationSettings.MapSize = new Vector2I(128 * value, 128 * value);
    }

    public async void _on_generate_button_down()
    {
        ClearPreRender();
        statusLabel.Text = "Генерация началась!";
        await generator.Generate(progressBar);
        statusLabel.Text = "Мир сгенерирован успешно!";
        PreRender();
        GetParent<Control>().Set("reloadWorlds", true);
    }

    public void _on_new_seed_button_down()
    {
        generator.genData.GenerateNewSeed();
        seedLabel.Value = generator.genData.seed;
    }

    public void _on_back_to_menu_button_down()
    {
        Control parent = GetParent<Control>();
        if (IsInstanceValid(parent))
        {
            parent.GetNode<Control>("MainButtons").Visible = true;
        }
        Visible = false;
    }

    public void PreRender()
    {
        heightMap.Texture = ImageTexture.CreateFromImage(generator.genData.HeightMap);
        heatMap.Texture = ImageTexture.CreateFromImage(generator.genData.HeatMap);
        moistureMap.Texture = ImageTexture.CreateFromImage(generator.genData.MoistureMap);
        biomeMap.Texture = ImageTexture.CreateFromImage(generator.genData.BiomeMap);
    }

    public void ClearPreRender()
    {
        progressBar.Value = 0;
        heightMap.Texture = null;
        heatMap.Texture = null;
        moistureMap.Texture = null;
        biomeMap.Texture = null;
    }

}
