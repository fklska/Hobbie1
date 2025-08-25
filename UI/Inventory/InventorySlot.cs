using Godot;
using System.Collections.Generic;

public partial class InventorySlot : Slot
{
    public static int SlotAmount = 1;
    public static List<InventorySlot> Slots = new List<InventorySlot>();

    public int CurrentSlotNumber = 0;

    public InventorySlot()
    {
        CurrentSlotNumber = SlotAmount;
        SlotAmount++;
        Slots.Add(this);
    }

    public override void _Ready()
    {
        base._Ready();
        _slotNumberLabel = GetNode<Label>("Label");
        _slotNumberLabel.Text = CurrentSlotNumber.ToString();
    }

    public override string ToString()
    {
        return "InventorySlot #" + CurrentSlotNumber.ToString();
    }
}