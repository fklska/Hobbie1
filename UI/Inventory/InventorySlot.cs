using Godot;

public partial class InventorySlot : Slot
{
    public override string ToString()
    {
        return "InventorySlot #" + (GetIndex() + 1);
    }
}
