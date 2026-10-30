using Godot;

[GlobalClass]
public partial class NpcBase : Node2D
{
	public delegate void NpcReadyEventHandler();

	public delegate void TalkNextEventHandler();

	private Label _talkLabel;

	private Tween _talkTween;

	private bool _waitingForEntry;

	private bool _finishing;

	protected TextureRect talkrubble;

	[Export(PropertyHint.None, "")]
	public AdobeAnimateSprite sprite;

	[Export(PropertyHint.None, "")]
	public NpcAnimationConfig animationConfig;

	public bool canPress;

	public event NpcReadyEventHandler OnNpcReady;

	public event TalkNextEventHandler OnTalkNext;

	public void EmitNpcReady()
	{
		this.OnNpcReady?.Invoke();
	}

	public override void _Ready()
	{
		_talkLabel = GetNode<Label>("%TalkLabel");
		talkrubble = GetNode<TextureRect>("%TalkBubble");
		sprite.OnAnimeCompleted += AnimeCompleted;
		_waitingForEntry = !string.IsNullOrEmpty(animationConfig?.ResolveAnimation(animationConfig.enterAnimation));
		if (_waitingForEntry)
		{
			PlayAnimation(animationConfig.enterAnimation);
			return;
		}
		PlayAnimation(animationConfig?.idleAnimation);
		EmitNpcReady();
	}

	public override void _ExitTree()
	{
		_talkTween?.Kill();
		_talkTween = null;
		if (GodotObject.IsInstanceValid(sprite))
		{
			sprite.OnAnimeCompleted -= AnimeCompleted;
		}
	}

	public override void _Input(InputEvent _event)
	{
		if (canPress && Input.IsActionJustPressed("Press"))
		{
			this.OnTalkNext?.Invoke();
			canPress = false;
		}
	}

	public virtual void AnimeCompleted(string clip)
	{
		if (IsLeaveAnimation(clip))
		{
			QueueFree();
		}
		else if (!_finishing && animationConfig != null)
		{
			bool flag = _waitingForEntry && clip == animationConfig.ResolveAnimation(animationConfig.enterAnimation);
			if (flag)
			{
				_waitingForEntry = false;
			}
			if (animationConfig.nextAnimations.TryGetValue(clip, out var value))
			{
				PlayAnimation(value);
			}
			else if (flag)
			{
				PlayAnimation(animationConfig.idleAnimation);
			}
			if (flag)
			{
				EmitNpcReady();
			}
		}
	}

	public bool IsLeaveAnimation(string animation)
	{
		return animationConfig?.IsLeaveAnimation(animation) ?? false;
	}

	private void PlayAnimation(string animation, bool? loopOverride = null)
	{
		string text = animationConfig?.ResolveAnimation(animation) ?? animation;
		if (!string.IsNullOrEmpty(text))
		{
			sprite.SetAnimation(text, loopOverride ?? animationConfig?.GetLoop(text) ?? true, animationConfig?.GetBlendTime(text) ?? 0.0);
		}
	}

	public virtual void Talk(string text, string animeClip, string audio)
	{
		if (_finishing)
		{
			return;
		}
		if (IsLeaveAnimation(animeClip))
		{
			Finish();
			return;
		}
		canPress = false;
		_talkTween?.Kill();
		AudioManager.Instance.AudioPlay(audio);
		PlayAnimation(animeClip);
		_talkLabel.Text = text;
		talkrubble.Visible = true;
		_talkTween = CreateTween();
		_talkTween.SetEase(Tween.EaseType.InOut);
		_talkTween.SetTrans(Tween.TransitionType.Quart);
		_talkTween.TweenProperty(talkrubble, "scale", Vector2.One, 0.5).From(Vector2.Zero);
		_talkTween.TweenCallback(Callable.From(() => canPress = !_finishing && IsInsideTree()));
	}

	public virtual void Hand(NpcTalkHandConfig hand)
	{
	}

	public virtual void Finish()
	{
		if (!_finishing)
		{
			_finishing = true;
			_waitingForEntry = false;
			canPress = false;
			_talkTween?.Kill();
			_talkTween = null;
			talkrubble.Visible = false;
			if (string.IsNullOrEmpty(animationConfig?.ResolveAnimation(animationConfig.leaveAnimation)))
			{
				QueueFree();
			}
			else
			{
				PlayAnimation(animationConfig.leaveAnimation, false);
			}
		}
	}
}
