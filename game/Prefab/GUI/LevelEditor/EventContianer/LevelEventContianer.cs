using Godot;

public partial class LevelEventContianer : PanelContainer
{
	public delegate void ChangeEventHandler();

	private OptionButton eventOptionButton;

	public LevelEditorInspector inspector;

	public TowerDefenseLevelEventBase eventConfig;

	public event ChangeEventHandler OnChange;

	public override void _Ready()
	{
		eventOptionButton = GetNode<OptionButton>("%EventOptionButton");
		LevelEditorDropdown.Configure(eventOptionButton);
		foreach (TowerDefenseLevelEventDefinition definition in TowerDefenseLevelEventRegistry.Definitions)
		{
			int itemCount = eventOptionButton.ItemCount;
			eventOptionButton.AddItem(definition.DisplayKey);
			eventOptionButton.SetItemMetadata(itemCount, definition.Id);
		}
		GetNode<MainButton>("VBoxContainer/HBoxContainer/UpButton").Pressed += UpButtonPressed;
		GetNode<MainButton>("VBoxContainer/HBoxContainer/DeleteButton").Pressed += DeleteButtonPressed;
		GetNode<MainButton>("VBoxContainer/HBoxContainer/SettingButton").Pressed += SettingButtonPressed;
		eventOptionButton.ItemSelected += EventOptionButtonItemSelected;
	}

	public void Init(TowerDefenseLevelEventBase _eventConfig)
	{
		eventConfig = _eventConfig;
		eventOptionButton.Selected = (TowerDefenseLevelEventRegistry.TryGetDefinition(eventConfig, out var definition) ? FindOptionButtonIndex(definition.Id) : (-1));
	}

	public void UpButtonPressed()
	{
		if (GetIndex() > 0)
		{
			GetParent().MoveChild(this, GetIndex() - 1);
		}
		this.OnChange?.Invoke();
	}

	public void DeleteButtonPressed()
	{
		GetParent()?.RemoveChild(this);
		QueueFree();
		this.OnChange?.Invoke();
	}

	public void SettingButtonPressed()
	{
		this.OnChange?.Invoke();
		if (eventOptionButton.Selected != -1)
		{
			inspector.Init(eventConfig, eventConfig.GetProperty());
		}
	}

	public void EventOptionButtonItemSelected(long index)
	{
		if (index >= 0 && index < eventOptionButton.ItemCount)
		{
			TowerDefenseLevelEventBase instance = TowerDefenseLevelEventRegistry.Create(eventOptionButton.GetItemMetadata((int)index).AsString());
			if (GodotObject.IsInstanceValid(instance))
			{
				eventConfig = instance;
				SettingButtonPressed();
			}
		}
	}

	private int FindOptionButtonIndex(string eventId)
	{
		for (int i = 0; i < eventOptionButton.ItemCount; i++)
		{
			if (eventOptionButton.GetItemMetadata(i).AsString() == eventId)
			{
				return i;
			}
		}
		return -1;
	}
}
