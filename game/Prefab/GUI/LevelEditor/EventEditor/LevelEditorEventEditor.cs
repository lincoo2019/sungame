using Godot;

[GlobalClass]
public partial class LevelEditorEventEditor : Control
{
	private LevelEventListContainer initLevelEventListContainer;

	private LevelEventListContainer readyLevelEventListContainer;

	private LevelEventListContainer startEventListContainer;

	private LevelEditorInspector inspector;

	public static LevelEditorEventEditor Instance;

	[Export(PropertyHint.None, "")]
	public TowerDefenseLevelConfig levelConfig { get; set; }

	public override void _Ready()
	{
		initLevelEventListContainer = GetNode<LevelEventListContainer>("%InitLevelEventListContainer");
		readyLevelEventListContainer = GetNode<LevelEventListContainer>("%ReadyLevelEventListContainer");
		startEventListContainer = GetNode<LevelEventListContainer>("%StartEventListContainer");
		inspector = GetNode<LevelEditorInspector>("%LevelEditorInspector");
		initLevelEventListContainer.OnChange += InitEventChange;
		readyLevelEventListContainer.OnChange += ReadyEventChange;
		startEventListContainer.OnChange += StartEventChange;
		Instance = this;
		base.VisibilityChanged += Save;
	}

	public void Clear()
	{
		initLevelEventListContainer.Clear();
		readyLevelEventListContainer.Clear();
		startEventListContainer.Clear();
		inspector.Clear();
	}

	public void Init(TowerDefenseLevelConfig _levelConfig)
	{
		Clear();
		levelConfig = _levelConfig;
		initLevelEventListContainer.Init(levelConfig.eventInit);
		readyLevelEventListContainer.Init(levelConfig.eventReady);
		startEventListContainer.Init(levelConfig.eventStart);
		inspector.Clear();
	}

	public void Save()
	{
		if (GodotObject.IsInstanceValid(levelConfig))
		{
			levelConfig.eventInit = initLevelEventListContainer.GetEventList();
			levelConfig.eventReady = readyLevelEventListContainer.GetEventList();
			levelConfig.eventStart = startEventListContainer.GetEventList();
		}
	}

	public void InitEventChange()
	{
		levelConfig.canExport = false;
		levelConfig.eventInit = initLevelEventListContainer.GetEventList();
		levelConfig.MarkEventDataEditedFromEditor();
		inspector.Clear();
	}

	public void ReadyEventChange()
	{
		levelConfig.canExport = false;
		levelConfig.eventReady = readyLevelEventListContainer.GetEventList();
		levelConfig.MarkEventDataEditedFromEditor();
		inspector.Clear();
	}

	public void StartEventChange()
	{
		levelConfig.canExport = false;
		levelConfig.eventStart = startEventListContainer.GetEventList();
		levelConfig.MarkEventDataEditedFromEditor();
		inspector.Clear();
	}
}
