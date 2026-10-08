using Godot;

[GlobalClass]
[Tool]
public partial class Slot : Panel
{
    [Export]
    public InventoryItem CurrentItem;

    [Export]
    public string Key = "";

    public static InventoryItem FlyingObj;

    protected TextureRect _itemSprite;
    protected Label _itemAmount;
    protected Label _slotNumberLabel;

    private bool _selected;
    private bool _hovered;

    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            RefreshStyle();
        }
    }

    public override void _Ready()
    {
        _itemSprite = GetNode<TextureRect>("%Icon");
        _itemAmount = GetNode<Label>("%Amount");
        _slotNumberLabel = GetNode<Label>("%Key");
        _slotNumberLabel.Text = Key;
        RefreshStyle();
    }

    public void Update(InventoryItem item)
    {
        CurrentItem = item;
        _itemAmount.Text = item.Amount > 1 ? item.Amount.ToString() : "";
        _itemSprite.Texture = item.texture;
        RefreshStyle();
    }

    public void ClearSlot()
    {
        CurrentItem = null;
        _itemAmount.Text = "";
        _itemSprite.Texture = null;
        RefreshStyle();
    }

    public bool IsEmpty()
    {
        return CurrentItem == null;
    }

    public void RefreshStyle()
    {
        ThemeTypeVariation = _selected ? "SlotPanelSelected" : _hovered ? "SlotPanelHover" : "SlotPanel";
        if (_itemSprite != null) _itemSprite.Modulate = IsEmpty() ? new Color(1, 1, 1, 0.5f) : Colors.White;
    }

    private void OnMouseEntered()
    {
        _hovered = true;
        RefreshStyle();
    }

    private void OnMouseExited()
    {
        _hovered = false;
        RefreshStyle();
    }

    private void OnGuiInput(InputEvent @event)
    {
        if (!@event.IsActionReleased("LeftMouseButton")) return;

        if (FlyingObj == null)
        {
            if (IsEmpty()) return;
            FlyingObj = CurrentItem;
            ClearSlot();
        }
        else if (IsEmpty())
        {
            Update(FlyingObj);
            FlyingObj = null;
            InventoryManager.DisableDisplay = true;
        }
    }
}
