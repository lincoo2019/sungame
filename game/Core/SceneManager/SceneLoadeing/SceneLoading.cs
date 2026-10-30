using System.Linq;
using Godot;

[GlobalClass]
public partial class SceneLoading : CanvasLayer
{
	public delegate void EnterEventHandler();

	public Control background;

	public AdobeAnimateSpriteBase stinky;

	public Label label;

	public Label statusLabel;

	public int pointNum;

	private string _statusText = "正在加载场景";

	public event EnterEventHandler OnEnter;

	public override void _Ready()
	{
		background = GetNode<Control>("%Background");
		stinky = GetNode<AdobeAnimateSpriteBase>("%Stinky");
		Control node = GetNode<Control>("%StinkyRenderMount");
		stinky.forceLocalRender = true;
		stinky.SetRenderClipControl(node);
		stinky.OnAnimeCompleted += AnimeCompleted;
		label = GetNode<Label>("%Label");
		statusLabel = GetNode<Label>("%StatusLabel");
		statusLabel.Text = _statusText;
		EnterScene();
		stinky.SetAnimation("Out", loop: false);
		GetNode<Timer>("Timer").Timeout += Timeout;
	}

	public void SetStatus(string status)
	{
		if (!string.IsNullOrWhiteSpace(status))
		{
			_statusText = status;
			if (GodotObject.IsInstanceValid(statusLabel))
			{
				statusLabel.Text = status;
			}
		}
	}

	public async void EnterScene()
	{
		Tween tween = CreateTween();
		tween.SetParallel();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Back);
		tween.TweenProperty(stinky, "scale", new Vector2(1f, 1f), 0.5).From(new Vector2(2f, 0f));
		tween.TweenProperty(background, "modulate:a", 1.0, 0.5).From(0.0);
		await ToSignal(tween, Tween.SignalName.Finished);
		this.OnEnter?.Invoke();
	}

	public async void Exit()
	{
		Tween tween = CreateTween();
		tween.SetParallel();
		tween.SetEase(Tween.EaseType.In);
		tween.SetTrans(Tween.TransitionType.Back);
		tween.TweenProperty(stinky, "scale", new Vector2(0f, 0f), 1.0).From(new Vector2(1f, 1f));
		tween.TweenProperty(background, "modulate:a", 0.0, 0.75).From(1.0);
		await ToSignal(tween, Tween.SignalName.Finished);
		QueueFree();
	}

	public void AnimeCompleted(string clip)
	{
		switch (clip)
		{
		case "In":
			stinky.SetAnimation("Out", loop: false);
			break;
		case "Out":
			stinky.SetAnimation("Crawl", loop: false);
			break;
		case "Crawl":
			stinky.SetAnimation("In", loop: false);
			break;
		}
	}

	public void Timeout()
	{
		pointNum = (pointNum + 1) % 7;
		label.Text = string.Concat("加载中", string.Concat(Enumerable.Repeat(" .", pointNum)));
	}
}
