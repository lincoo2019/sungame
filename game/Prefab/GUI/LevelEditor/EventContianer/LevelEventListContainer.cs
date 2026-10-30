using Godot;
using Godot.Collections;

public partial class LevelEventListContainer : ScrollContainer
{
	public delegate void ChangeEventHandler();

	private static PackedScene _levelEventContianerScene;

	private VBoxContainer eventContainer;

	public bool isInit;

	private static PackedScene LevelEventContianerScene => _levelEventContianerScene ?? (_levelEventContianerScene = GD.Load<PackedScene>("uid://b617wjctwpjyw"));

	[Export(PropertyHint.None, "")]
	public LevelEditorInspector inspector { get; set; }

	public event ChangeEventHandler OnChange;

	public override void _Ready()
	{
		eventContainer = GetNode<VBoxContainer>("%EventContainer");
		GetNode<MainButton>("VboxContainer/AddEventButton").Pressed += delegate
		{
			AddEvent();
		};
	}

	public void Init(Array<TowerDefenseLevelEventBase> eventList)
	{
		isInit = true;
		Clear();
		foreach (TowerDefenseLevelEventBase @event in eventList)
		{
			AddEvent(@event);
		}
		isInit = false;
	}

	public void Clear()
	{
		foreach (Node child in eventContainer.GetChildren())
		{
			child.QueueFree();
		}
	}

	public void AddEvent(TowerDefenseLevelEventBase eventConfig = null)
	{
		LevelEventContianer levelEventContianer = LevelEventContianerScene.Instantiate<LevelEventContianer>(PackedScene.GenEditState.Disabled);
		eventContainer.AddChild(levelEventContianer, forceReadableName: false, InternalMode.Disabled);
		levelEventContianer.inspector = inspector;
		levelEventContianer.OnChange += HandleChange;
		if (eventConfig != null)
		{
			levelEventContianer.Init(eventConfig);
		}
		else
		{
			levelEventContianer.Init(TowerDefenseLevelEventRegistry.Create("TipsPlay"));
		}
		HandleChange();
	}

	public void HandleChange()
	{
		if (!isInit)
		{
			this.OnChange?.Invoke();
		}
	}

	public Array<TowerDefenseLevelEventBase> GetEventList()
	{
		Array<TowerDefenseLevelEventBase> array = new Array<TowerDefenseLevelEventBase>();
		foreach (Node child in eventContainer.GetChildren())
		{
			if (child is LevelEventContianer levelEventContianer)
			{
				array.Add(levelEventContianer.eventConfig);
			}
		}
		return array;
	}
}
