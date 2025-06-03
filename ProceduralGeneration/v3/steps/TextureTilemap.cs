using Godot;
using System;

[GlobalClass]
[Tool]
public partial class TextureTilemap : TileMapLayer
{
    [Export] public CompressedTexture2D texture;
}
