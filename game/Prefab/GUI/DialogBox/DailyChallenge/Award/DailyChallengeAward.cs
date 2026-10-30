using Godot;
using Godot.Collections;

public partial class DailyChallengeAward : DialogBoxBase
{
	private static PackedScene _dailyChallengeAwardItem;

	public TextureProgressBar dailyChallengeAwardProgressBar;

	public VBoxContainer itemContainer;

	public Label textLabel;

	public int year = 2025;

	public int month = 1;

	public int finish;

	public int dayAll = 1;

	public Array currentAwardList;

	private static PackedScene DAILY_CHALLENGE_AWARD_ITEM => _dailyChallengeAwardItem ?? (_dailyChallengeAwardItem = GD.Load<PackedScene>("uid://b1cbbcykkioe0"));

	public override void _Ready()
	{
		base._Ready();
		dailyChallengeAwardProgressBar = GetNode<TextureProgressBar>("%DailyChallengeAwardProgressBar");
		itemContainer = GetNode<VBoxContainer>("%ItemContainer");
		textLabel = GetNode<Label>("%TextLabel");
		GetNode<TextureButton>("Layer/BackgroundTexture/CloseButton").Pressed += CloseButtonPressed;
		GetNode<BaseButton>("%BackButton").Pressed += CloseButtonPressed;
	}

	public void InitDialog(int _year, int _month, int _finish, int _dayAll)
	{
		year = _year;
		month = _month;
		finish = _finish;
		dayAll = _dayAll;
		textLabel.Text = $"{year}/{month}月奖励";
		Dictionary dictionary = ResourceManager.DAILY_LEVEL_AWARD.Data.AsGodotDictionary();
		string text = $"{year}-{month:D2}";
		if (dictionary.ContainsKey(text))
		{
			currentAwardList = (Array)dictionary[text];
		}
		else
		{
			currentAwardList = new Array();
		}
		((DailyChallengeAwardProgressBar)dailyChallengeAwardProgressBar).Init(currentAwardList, finish, dayAll);
		foreach (Variant currentAward in currentAwardList)
		{
			DailyChallengeAwardItem dailyChallengeAwardItem = DAILY_CHALLENGE_AWARD_ITEM.Instantiate<DailyChallengeAwardItem>(PackedScene.GenEditState.Disabled);
			itemContainer.AddChild(dailyChallengeAwardItem, forceReadableName: false, InternalMode.Disabled);
			dailyChallengeAwardItem.Init((Dictionary)currentAward, finish);
		}
	}

	public void CloseButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		CloseDialog();
	}
}
