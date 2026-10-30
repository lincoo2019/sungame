using Godot;
using Godot.Collections;

public partial class DailyChallengeAwardProgressBar : TextureProgressBar
{
	private static Texture2D _rewardProgressTick;

	public Control tickNode;

	public Array awardList;

	public int finish;

	public int dayAll = 30;

	private static Texture2D REWARD_PROGRESS_TICK => _rewardProgressTick ?? (_rewardProgressTick = GD.Load<Texture2D>("res://Asset/Texture/GUI/DailyChallenge/Award/RewardProgressTick.png"));

	public override void _Ready()
	{
		tickNode = GetNode<Control>("%TickNode");
	}

	public void Init(Array _awardList, int _finish, int _dayAll)
	{
		awardList = _awardList;
		finish = _finish;
		dayAll = _dayAll;
		base.MaxValue = dayAll;
		base.Value = finish;
		foreach (Variant _award in _awardList)
		{
			Dictionary dictionary = (Dictionary)_award;
			TextureRect textureRect = new TextureRect();
			textureRect.Texture = REWARD_PROGRESS_TICK;
			Array array = (Array)dictionary["ConditionArg"];
			textureRect.Position = new Vector2((float)(int)array[0] / (float)dayAll * base.Size.X, 5f);
			tickNode.AddChild(textureRect, forceReadableName: false, InternalMode.Disabled);
			TextureRect textureRect2 = new TextureRect();
			textureRect2.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
			textureRect2.Texture = GD.Load<Texture2D>((string)dictionary["Icon"]);
			textureRect2.Size = new Vector2(54f, 54f);
			textureRect2.Position = -new Vector2(27f, 27f) - new Vector2(0f, 35f);
			textureRect.AddChild(textureRect2, forceReadableName: false, InternalMode.Disabled);
			Label label = new Label();
			label.Text = ((int)array[0]).ToString();
			label.AddThemeConstantOverride("outline_size", 5);
			label.Position = new Vector2(0f, 30f);
			textureRect.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		}
	}
}
