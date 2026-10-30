using Godot;
using Godot.Collections;
using PVZHE.ModEditor.ModSystem;

[GlobalClass]
public partial class DragMenuSelectItemChapter : DragMenuSelectItem
{
	private static Texture2D _silverMedal;

	private static Texture2D _goldMedal;

	private static Texture2D _dimondMedal;

	private static Texture2D _moreGameTrophy;

	private Sprite2D cupSprite;

	private static Texture2D SILVER_MEDAL => _silverMedal ?? (_silverMedal = GD.Load<Texture2D>("uid://bch4iu18n3i44"));

	private static Texture2D GOLD_MEDAL => _goldMedal ?? (_goldMedal = GD.Load<Texture2D>("uid://dqgpqtvy8kej3"));

	private static Texture2D DIMOND_MEDAL => _dimondMedal ?? (_dimondMedal = GD.Load<Texture2D>("uid://biqnbmrdvhih7"));

	private static Texture2D MORE_GAME_TROPHY => _moreGameTrophy ?? (_moreGameTrophy = GD.Load<Texture2D>("uid://d3l1vjo8lnm8t"));

	public override void _Ready()
	{
		base._Ready();
		cupSprite = GetNode<Sprite2D>("%CupSprite");
	}

	public void Init(Dictionary chapter, XWModLevelIdentity identity = null)
	{
		bool flag = true;
		bool flag2 = true;
		bool flag3 = true;
		Array array = chapter["Level"].AsGodotArray();
		if (array.Count <= 0)
		{
			flag = false;
			flag2 = false;
			flag3 = false;
		}
		for (int i = 0; i < array.Count; i++)
		{
			string text = array[i].AsGodotDictionary()["SaveKey"].AsString();
			Dictionary dictionary = ((identity == null) ? GameSaveManager.Instance.GetLevelValue(text) : XWModPlayerProgressService.GetLevel(identity with
			{
				LevelSaveKey = text
			}));
			Dictionary dictionary2 = dictionary.GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary();
			if (!dictionary.GetValueOrDefault("Difficult", false).AsBool() && !dictionary.GetValueOrDefault("Ultimate", false).AsBool())
			{
				flag = false;
			}
			if (!dictionary.GetValueOrDefault("Mower", false).AsBool())
			{
				flag2 = false;
			}
			if (dictionary2.GetValueOrDefault("Finish", 0).AsInt32() <= 0)
			{
				flag3 = false;
			}
		}
		if (Global.Instance.currentLevelChoose != "Puzzle" && Global.Instance.currentLevelChoose != "MiniGames")
		{
			if (flag3)
			{
				cupSprite.Texture = SILVER_MEDAL;
			}
			if (flag2)
			{
				cupSprite.Texture = GOLD_MEDAL;
			}
			if (flag)
			{
				cupSprite.Texture = DIMOND_MEDAL;
			}
		}
		else if (flag3 || flag2 || flag)
		{
			cupSprite.Texture = MORE_GAME_TROPHY;
		}
	}
}
