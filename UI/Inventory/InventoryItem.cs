using Godot;
using System;

public partial class InventoryItem : Resource
{
    public Texture2D texture;
    public int Amount;
    public Itemtype type;

    public bool IsEqual(InventoryItem other)
    {
        return this.type == other.type;
    }
}
