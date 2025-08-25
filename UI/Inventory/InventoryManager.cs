using Godot;
using System.Collections.Generic;

public partial class InventoryManager : Control
{
    public Godot.Collections.Array<Slot> Slots;
    public Dictionary<Itemtype, Slot> ItemSlotMap = new();

    public Sprite2D DisplayFlyingObj;

    public static bool DisableDisplay = false;

    public override void _Ready()
    {
        DisplayFlyingObj = GetNode<Sprite2D>("FlyingObj");
        InitializeSlots();
    }

    public void InitializeSlots()
    {
        foreach (Slot slot in GetNode<GridContainer>("GridContainer").GetChildren())
        {
            Slots.Add(slot);
        }
    }

    public override void _Process(double delta)
    {
        if (Slot.FlyingObj != null)
        {
            DisplayFlyingObj.Texture = Slot.FlyingObj.texture;
            DisplayFlyingObj.GetChild<Label>(0).Text = Slot.FlyingObj.Amount.ToString();
            DisplayFlyingObj.GlobalPosition = GetGlobalMousePosition();
        }

        if (DisableDisplay)
        {
            DisableDisplayFlyObj();
        }
    }

    public void UpdateSlots()
    {
        foreach (Slot slot in Slots)
        {
            slot.Update(slot.CurrentItem);
        }
    }

    public void DisableDisplayFlyObj()
    {
        DisplayFlyingObj.Texture = null;
        DisplayFlyingObj.GetChild<Label>(0).Text = "";
        DisplayFlyingObj.GlobalPosition = Vector2.Zero;
        DisableDisplay = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("inventory"))
        {
            Visible = !Visible;
        }
    }
}