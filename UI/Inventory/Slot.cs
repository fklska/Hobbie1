using Godot;
using System.Reflection;

[GlobalClass]
[Tool]
public partial class Slot : Panel
{
    [Export]
    public InventoryItem CurrentItem;

    protected TextureRect _itemSprite;
    protected Label _itemAmount;
    protected Label _slotNumberLabel;

    public static InventoryItem FlyingObj;

    private readonly Color _emptyStyle = Color.Color8(255, 255, 255, 215);
    private readonly Color _defaultStyle = Color.Color8(255, 255, 255, 255);
    private readonly Color _selectedColor = Color.Color8(255, 255, 145, 255);

    public enum State
    {
        Empty,
        Fill,
        Selected
    }

    public State CurrentState = State.Empty;

    public override void _Process(double delta)
    {
        //StateMachine();
    }

    public override void _Ready()
    {
        _itemSprite = GetNode<TextureRect>("CenterContainer/item_display");
        _itemAmount = GetNode<Label>("CenterContainer/item_display/item_amount");
        _slotNumberLabel = GetNode<Label>("Label");
    }

    public void Update(InventoryItem item)
    {
        CurrentItem = item;
        _itemAmount.Text = item.Amount.ToString();
        _itemSprite.Texture = item.texture;

        RefreshState();
    }

    public void ClearSlot()
    {
        CurrentItem = null;
        _itemAmount.Text = "";
        _itemSprite.Texture = null;

        RefreshState();
    }

    public void StateMachine()
    {
        switch (CurrentState)
        {
            case State.Empty:
                SetEmptyStyle();
                break;
            case State.Fill:
                SetDefaultStyle();
                break;
            case State.Selected:
                SetSelectedStyle();
                break;
        }
    }

    public void RefreshState()
    {
        if (IsEmpty())
        {
            CurrentState = State.Empty;
        }
        else if (CurrentState == State.Selected)
        {
            CurrentState = State.Selected;
        }
        else
        {
            CurrentState = State.Fill;
        }
    }

    public bool IsEmpty()
    {
        return CurrentItem == null;
    }

    public void SetEmptyStyle()
    {
        Modulate = _emptyStyle;
    }

    public void SetDefaultStyle()
    {
        Modulate = _defaultStyle;
    }

    public void SetSelectedStyle()
    {
        Modulate = _selectedColor;
    }

    private void OnMouseEntered()
    {
        CurrentState = State.Selected;
    }

    private void OnMouseExited()
    {
        if (IsEmpty())
        {
            CurrentState = State.Empty;
            return;
        }
        CurrentState = State.Fill;
    }

    private void OnGuiInput(InputEvent @event)
    {
        if (@event.IsActionPressed("RightMouseButton"))
        {
            GD.Print(this);
        }

        if (@event.IsActionReleased("LeftMouseButton"))
        {
            if (FlyingObj == null) // Take Item
            {
                FlyingObj = CurrentItem;
                ClearSlot();
                StateMachine();
            }
            else // Put Item
            {
                if (IsEmpty())
                {
                    CurrentItem = FlyingObj;
                    Update(FlyingObj);
                    StateMachine();
                    FlyingObj = null;
                    InventoryManager.DisableDisplay = true;
                }
                else
                {
                    if (CurrentItem.IsEqual(FlyingObj))
                    {
                        GD.Print("Stack");
                    }
                    else
                    {
                        GD.Print("Slot zanyat!");
                    }
                }
            }
        }
    }
}