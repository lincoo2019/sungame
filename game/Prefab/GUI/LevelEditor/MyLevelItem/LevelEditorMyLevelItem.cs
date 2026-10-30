using Godot;

public partial class LevelEditorMyLevelItem : Control
{
	public delegate void SelectEventHandler(string uid);

	public delegate void DeleteEventHandler(string uid);

	private const string PATH = "user://Csharp/Diy";

	private static Texture2D _izIcon;

	private static Texture2D _iz2Icon;

	private static Texture2D _vrIcon;

	private Label _nameLabel;

	private TextureRect _mapTexture;

	private TextureRect _iconTexture;

	public string uid;

	public TowerDefenseLevelConfig levelConfig;

	private static Texture2D IZ_ICON => _izIcon ?? (_izIcon = GD.Load<Texture2D>("uid://ciyj1718ypbih"));

	private static Texture2D IZ2_ICON => _iz2Icon ?? (_iz2Icon = GD.Load<Texture2D>("uid://b8rsot26eb7od"));

	private static Texture2D Vr_ICON => _vrIcon ?? (_vrIcon = GD.Load<Texture2D>("uid://24j6iw1ww08b"));

	public event SelectEventHandler OnSelect;

	public event DeleteEventHandler OnDelete;

	public override void _Ready()
	{
		GetNode<TextureButton>("SelectButton").Pressed += SelectButtonPressed;
		GetNode<TextureButton>("DeleteButton").Pressed += DeleteButtonPressed;
	}

	public void Init(string _uid)
	{
		uid = _uid;
		_nameLabel = GetNode<Label>("%NameLabel");
		_mapTexture = GetNode<TextureRect>("%MapTexture");
		_iconTexture = GetNode<TextureRect>("%IconTexture");
		string text = "user://Csharp/Diy/" + uid + ".tres";
		if ((LevelEditorStage.MigrateDiyLevelTresIfNeeded(text) ? ResourceLoader.Load(text, "", ResourceLoader.CacheMode.Ignore) : GD.Load(text)) is TowerDefenseLevelConfig towerDefenseLevelConfig)
		{
			levelConfig = towerDefenseLevelConfig;
			_nameLabel.Text = levelConfig.levelName;
			TowerDefenseMapConfig mapConfig = TowerDefenseManager.Instance.GetMapConfig(levelConfig.map);
			TowerDefenseMapConfig.ApplyMapPreviewTexture(_mapTexture, mapConfig?.GetMapThumbnail());
			switch (levelConfig.finishMethod)
			{
			case TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE:
				_iconTexture.Visible = false;
				break;
			case TowerDefenseEnum.LEVEL_FINISH_METHOD.VASE:
				_iconTexture.Visible = true;
				_iconTexture.Texture = Vr_ICON;
				break;
			case TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM:
				_iconTexture.Visible = true;
				_iconTexture.Texture = IZ_ICON;
				break;
			case TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM2:
				_iconTexture.Visible = true;
				_iconTexture.Texture = IZ2_ICON;
				break;
			case TowerDefenseEnum.LEVEL_FINISH_METHOD.QUIZ:
				break;
			}
		}
	}

	public void DeleteButtonPressed()
	{
		DialogBoxMyLevelDelete obj = (DialogBoxMyLevelDelete)DialogManager.Instance.DialogCreate("MyLevelDelete");
		string _uid = uid;
		obj.OnPressDelete += delegate
		{
			this.OnDelete?.Invoke(_uid);
			QueueFree();
		};
	}

	public void SelectButtonPressed()
	{
		this.OnSelect?.Invoke(uid);
	}
}
