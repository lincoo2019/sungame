using Godot;

[GlobalClass]
public partial class CoinBank : Control
{
	private const string CoinBankFontPath = "res://Asset/Font/fzcq.ttf";

	private const long DebugMaximum = 999999999L;

	private const float HiddenAlphaThreshold = 0.001f;

	public NinePatchRect coinBankTexture;

	public Label coinNumLabel;

	private long _num;

	private double _numShow;

	private long _displayedNum = -9223372036854775808L;

	public double timer;

	[Export(PropertyHint.None, "")]
	public long num
	{
		get
		{
			return _num;
		}
		set
		{
			if (_num != value)
			{
				_num = value;
				numShow = value;
			}
		}
	}

	public double numShow
	{
		get
		{
			return _numShow;
		}
		set
		{
			_numShow = value;
			long num = (long)Mathf.Round(_numShow);
			if (coinNumLabel != null && num != _displayedNum)
			{
				_displayedNum = num;
				coinNumLabel.Text = num.ToString();
			}
		}
	}

	public override void _Ready()
	{
		coinBankTexture = GetNode<NinePatchRect>("%CoinBankTexture");
		coinNumLabel = GetNode<Label>("%CoinNumLabel");
		base.Modulate = new Color(base.Modulate, 0f);
		numShow = _num;
		CommandManager instance = CommandManager.Instance;
		if (instance != null && instance.debugCoinMax)
		{
			num = 999999999L;
		}
		SetPhysicsProcess(enable: false);
		LoadFontAfterFirstDraw();
	}

	private async void LoadFontAfterFirstDraw()
	{
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		if (IsInsideTree() && GodotObject.IsInstanceValid(coinNumLabel))
		{
			StartupLoadDiagnostics.Mark("deferred.coin_font.begin");
			FontFile fontFile = GD.Load<FontFile>("res://Asset/Font/fzcq.ttf");
			if (GodotObject.IsInstanceValid(fontFile))
			{
				coinNumLabel.AddThemeFontOverride("font", fontFile);
			}
			StartupLoadDiagnostics.Mark("deferred.coin_font.end");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (timer > 0.0)
		{
			timer = Mathf.Max(0.0, timer - delta);
			return;
		}
		Color modulate = base.Modulate;
		if (modulate.A <= 0.001f)
		{
			if (modulate.A > 0f)
			{
				base.Modulate = new Color(modulate, 0f);
			}
			SetPhysicsProcess(enable: false);
			return;
		}
		float num = Mathf.Lerp(modulate.A, 0f, (float)(5.0 * delta));
		if (num <= 0.001f)
		{
			num = 0f;
		}
		base.Modulate = new Color(modulate, num);
		if (num <= 0f)
		{
			SetPhysicsProcess(enable: false);
		}
	}

	public void ShowCoinBank(Vector2 pos = default(Vector2), bool still = false)
	{
		if (pos == default(Vector2))
		{
			pos = new Vector2(86f, 557f);
		}
		base.Position = pos;
		base.Modulate = new Color(base.Modulate);
		if (!still)
		{
			timer = 5.0;
			SetPhysicsProcess(enable: true);
		}
		else
		{
			timer = 10000000000.0;
			SetPhysicsProcess(enable: false);
		}
	}

	public void StartHide()
	{
		timer = 0.0;
		SetPhysicsProcess(enable: true);
	}

	public void AddNum(long _num)
	{
		num += _num;
	}

	public void UseCoin(long _num)
	{
		num -= _num;
	}

	public void SetNum(long _num)
	{
		num = _num;
	}
}
