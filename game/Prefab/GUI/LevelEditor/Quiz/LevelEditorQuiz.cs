using Godot;

public partial class LevelEditorQuiz : Control
{
	private const string DisabledSuperBigMapName = "FrontlawnSuperBig";

	private const string DisabledSuperBigNightMapName = "FrontlawnSuperBigNight";

	private static PackedScene _levelEditorQuizLevelItem;

	private DragMenu _quizDragMenu;

	private MainButton _mapChooseButton;

	private ReferenceRect _quizMapChooseRect;

	public bool mapChooseReady;

	public bool mapChooseOver;

	public double currentChoose;

	private static PackedScene LEVEL_EDITOR_QUIZ_LEVEL_ITEM => _levelEditorQuizLevelItem ?? (_levelEditorQuizLevelItem = GD.Load<PackedScene>("uid://3rogaxqqkxn2"));

	public override void _Ready()
	{
		_quizDragMenu = GetNode<DragMenu>("%QuizDragMenu");
		_mapChooseButton = GetNode<MainButton>("%MapChooseButton");
		_quizMapChooseRect = GetNode<ReferenceRect>("%QuizMapChooseRect");
		_mapChooseButton.Pressed += MapChooseButtonPressed;
		foreach (string key in ResourceManager.Instance.MAPS.Keys)
		{
			if (!(key == "FrontlawnSuperBig") && !(key == "FrontlawnSuperBigNight"))
			{
				LevelEditorQuizLevelItem levelEditorQuizLevelItem = LEVEL_EDITOR_QUIZ_LEVEL_ITEM.Instantiate<LevelEditorQuizLevelItem>(PackedScene.GenEditState.Disabled);
				_quizDragMenu.AddChild(levelEditorQuizLevelItem, forceReadableName: false, InternalMode.Disabled);
				levelEditorQuizLevelItem.Init(key);
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (mapChooseReady)
		{
			currentChoose = Mathf.PosMod(currentChoose + delta * (double)GD.Randf() * 50.0, _quizDragMenu.GetChildCount());
			_quizDragMenu.currentIndex = (int)Mathf.Floor(currentChoose);
		}
	}

	public void Enter()
	{
		TowerDefenseLevelConfig towerDefenseLevelConfig = GD.Load<TowerDefenseLevelConfig>("uid://ccdkb1f5papjm").Duplicate(deep: true) as TowerDefenseLevelConfig;
		towerDefenseLevelConfig.Init();
		LevelEditorQuizLevelItem levelEditorQuizLevelItem = _quizDragMenu.GetChild(_quizDragMenu.currentIndex) as LevelEditorQuizLevelItem;
		towerDefenseLevelConfig.map = levelEditorQuizLevelItem.map;
		towerDefenseLevelConfig.featureData["Map"]["MapName"] = towerDefenseLevelConfig.map;
		TowerDefenseManager.Instance.currentLevelConfig = towerDefenseLevelConfig;
		SceneManager.Instance.ChangeScene("TowerDefense");
	}

	public async void MapChooseButtonPressed()
	{
		_mapChooseButton.Visible = false;
		_quizMapChooseRect.Visible = true;
		_quizDragMenu.alive = false;
		mapChooseOver = true;
		mapChooseReady = true;
		await ToSignal(GetTree().CreateTimer(GD.RandRange(3.0, 5.0), processAlways: false), SceneTreeTimer.SignalName.Timeout);
		mapChooseReady = false;
		await ToSignal(GetTree().CreateTimer(1.0, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		Enter();
	}
}
