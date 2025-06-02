using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class GeneratorV3 : Node2D
{
    [Export] public GeneratorData genData;
    [Export] public Godot.Collections.Array<GenerationStep> steps;
    [Export] public Gradient grad;
    public Image map_render = new Image();
    public TextureRect MapTexture;

    public override void _Ready()
    {
        base._Ready();
        MapTexture = GetNode<TextureRect>("TextureRect");
        genData.ResetData();
        foreach (var step in steps)
        {
            step.Execute(genData);
        }

        preRender(genData);
    }

    public void preRender(GeneratorData genData)
    {
        map_render = Image.CreateEmpty(genData.mapSize.X, genData.mapSize.Y, false, Image.Format.Rgba8);
        Color color = new Color();

        for (int x = 0; x < genData.mapSize.X; x++)
        {
            for (int y = 0; y < genData.mapSize.Y; y++)
            {
                color = grad.Sample(genData.LandMapHeights[x, y]);
                map_render.SetPixel(x, y, color);
            }
        }

        MapTexture.Texture = ImageTexture.CreateFromImage(map_render);
    }
}
