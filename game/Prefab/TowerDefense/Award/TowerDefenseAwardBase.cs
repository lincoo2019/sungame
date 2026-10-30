using Godot;

[GlobalClass]
public partial class TowerDefenseAwardBase : Node2D
{
	private AwardRay awardRay;

	private Sprite2D awardPickupGlow;

	private Sprite2D downArrow;

	private Button button;

	public bool press;

	public override void _Ready()
	{
		awardRay = GetNode<AwardRay>("%AwardRay");
		awardPickupGlow = GetNode<Sprite2D>("%AwardPickupGlow");
		downArrow = GetNode<Sprite2D>("%DownArrow");
		button = GetNode<Button>("%Button");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!press)
		{
			Vector2 screenCenterPosition = GetViewport().GetCamera2D().GetScreenCenterPosition();
			base.GlobalPosition = base.GlobalPosition.Lerp(screenCenterPosition, (float)(delta * 0.5));
			base.GlobalScale = base.GlobalScale.Lerp(Vector2.One * 1.5f, (float)(delta * 0.5));
		}
	}

	public virtual void Pressed()
	{
	}

	public virtual void Init(string value)
	{
	}

	protected void TrySelectNextLevelFromLevelChoose()
	{
		Global instance = Global.Instance;
		if (instance != null && !(instance.enterLevelMode != "LevelChoose") && !(instance.currentLevelChoose == "TryLevel") && instance.currentChapterId >= 0 && instance.currentLevelId >= 0 && TowerDefenseManager.Instance != null)
		{
			TowerDefenseManager.Instance.SetNextLevel(instance.currentLevelChoose, instance.currentChapterId, instance.currentLevelId);
		}
	}
}
