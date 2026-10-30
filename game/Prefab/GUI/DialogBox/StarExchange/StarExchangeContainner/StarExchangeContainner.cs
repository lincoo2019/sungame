using Godot;
using Godot.Collections;

[GlobalClass]
public partial class StarExchangeContainner : Control
{
	private static PackedScene _starExchangeItem;

	private TextureRect chapterTexture;

	private Label finishNumLabel;

	private HBoxContainer itemContainer;

	public Dictionary data;

	public int finishNum;

	private static PackedScene STAR_EXCHANGE_ITEM => _starExchangeItem ?? (_starExchangeItem = GD.Load<PackedScene>("uid://byuv6jflrtcul"));

	public override void _Ready()
	{
		chapterTexture = GetNode<TextureRect>("%ChapterTexture");
		finishNumLabel = GetNode<Label>("%FinishNumLabel");
		itemContainer = GetNode<HBoxContainer>("%ItemContainer");
	}

	public void Init(Dictionary _data)
	{
		data = _data;
		chapterTexture.Texture = GD.Load<Texture2D>(data["ChapterImage"].AsString());
		finishNum = TowerDefenseManager.Instance.GetLevelChapterFinishNum(data["LevelList"].AsString(), data["Chapter"].AsString());
		finishNumLabel.Text = $"x{finishNum}";
		foreach (Variant item in data["Award"].AsGodotArray())
		{
			AddItem(item.AsGodotDictionary());
		}
	}

	public void AddItem(Dictionary _data)
	{
		StarExchangeItem starExchangeItem = (StarExchangeItem)STAR_EXCHANGE_ITEM.Instantiate(PackedScene.GenEditState.Disabled);
		itemContainer.AddChild(starExchangeItem, forceReadableName: false, InternalMode.Disabled);
		starExchangeItem.Init(_data, finishNum);
	}
}
