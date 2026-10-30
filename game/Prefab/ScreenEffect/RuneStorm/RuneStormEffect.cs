using Godot;
using Godot.Collections;

[GlobalClass]
public partial class RuneStormEffect : ColorRect
{
	private const string ColorParameter = "runeColor";

	private const string SizeParameter = "effectSize";

	private const string TimerParameter = "timer";

	private const string FadeParameter = "fade";

	private const string TextureCountParameter = "runeTextureCount";

	private const string SeedParameter = "seed";

	private const string SeedStateKey = "textureSeed";

	private static readonly string[] TextureParameters = new string[3] { "runeTexture0", "runeTexture1", "runeTexture2" };

	private const double FadeInDuration = 0.5;

	private const double FadeOutDuration = 0.7;

	private const string RuneTexturePath = "res://Asset/Texture/ScreenEffect/RuneStorm/RuneStorm";

	[Export(PropertyHint.None, "")]
	public double damagePercentPerSecond = 0.05;

	[Export(PropertyHint.None, "")]
	public double healPerSecond = 50.0;

	private double _timer;

	private double _seed = -1.0;

	private double _elapsed;

	private double _tickAccumulator;

	private double _refreshAccumulator;

	private double _duration = 5.0;

	private TowerDefenseRuneMagicColor _color;

	public void Setup(TowerDefenseRuneMagicColor color, double duration)
	{
		_color = color;
		_duration = Mathf.Max(0.1, duration);
		_seed = GD.RandRange(0.0, 1000.0);
		ApplyTexture();
		ApplyColor();
	}

	public override void _Ready()
	{
		ApplyTexture();
		ApplyColor();
		ApplyFade();
		ApplyViewportSize();
	}

	public Dictionary SaveState()
	{
		return new Dictionary
		{
			["color"] = TowerDefenseRuneMagicUtil.ToKey(_color),
			["duration"] = _duration,
			["elapsed"] = _elapsed,
			["timer"] = _timer,
			["tickAccumulator"] = _tickAccumulator,
			["refreshAccumulator"] = _refreshAccumulator,
			["textureSeed"] = _seed,
			["damagePercentPerSecond"] = damagePercentPerSecond,
			["healPerSecond"] = healPerSecond
		};
	}

	public void LoadState(Dictionary state)
	{
		TowerDefenseRuneMagicUtil.TryParse(state["color"].AsString(), out _color);
		_duration = Mathf.Max(0.1, state["duration"].AsDouble());
		_elapsed = Mathf.Clamp(state["elapsed"].AsDouble(), 0.0, _duration);
		_timer = state.GetValueOrDefault("timer", _elapsed).AsDouble();
		_tickAccumulator = Mathf.Clamp(state.GetValueOrDefault("tickAccumulator", 0.0).AsDouble(), 0.0, 1.0);
		_refreshAccumulator = Mathf.Clamp(state.GetValueOrDefault("refreshAccumulator", 0.0).AsDouble(), 0.0, 0.1);
		_seed = state.GetValueOrDefault("textureSeed", -1.0).AsDouble();
		damagePercentPerSecond = state.GetValueOrDefault("damagePercentPerSecond", damagePercentPerSecond).AsDouble();
		healPerSecond = state.GetValueOrDefault("healPerSecond", healPerSecond).AsDouble();
		ApplyTexture();
		ApplyColor();
		ApplyFade();
	}

	public override void _PhysicsProcess(double delta)
	{
		double num = Mathf.Clamp(delta, 0.0, _duration - _elapsed);
		_timer += num;
		_elapsed += num;
		ApplyFade();
		if (base.Material is ShaderMaterial shaderMaterial)
		{
			shaderMaterial.SetShaderParameter("timer", _timer);
		}
		_refreshAccumulator += num;
		if (_refreshAccumulator >= 0.1 && _elapsed < _duration)
		{
			_refreshAccumulator = 0.0;
			TowerDefenseRuneMagicUtil.ApplyStormContinuous(_color, _duration - _elapsed);
		}
		_tickAccumulator += num;
		while (_tickAccumulator >= 0.999999999)
		{
			_tickAccumulator -= 1.0;
			TowerDefenseRuneMagicUtil.ApplyStormPerSecond(_color, damagePercentPerSecond, healPerSecond);
		}
		if (_elapsed >= _duration - 1E-09)
		{
			QueueFree();
		}
	}

	private void ApplyTexture()
	{
		if (!(base.Material is ShaderMaterial shaderMaterial))
		{
			return;
		}
		for (int i = 0; i < TextureParameters.Length; i++)
		{
			Texture2D texture2D = GD.Load<Texture2D>("res://Asset/Texture/ScreenEffect/RuneStorm/RuneStorm" + (i + 1) + ".png");
			if (GodotObject.IsInstanceValid(texture2D))
			{
				shaderMaterial.SetShaderParameter(TextureParameters[i], texture2D);
			}
		}
		shaderMaterial.SetShaderParameter("runeTextureCount", TextureParameters.Length);
		if (_seed < 0.0)
		{
			_seed = GD.RandRange(0.0, 1000.0);
		}
		shaderMaterial.SetShaderParameter("seed", (float)_seed);
	}

	private void ApplyColor()
	{
		if (base.Material is ShaderMaterial shaderMaterial)
		{
			shaderMaterial.SetShaderParameter("runeColor", TowerDefenseRuneMagicUtil.ToStormTint(_color));
		}
	}

	private void ApplyFade()
	{
		if (base.Material is ShaderMaterial shaderMaterial)
		{
			shaderMaterial.SetShaderParameter("fade", (float)ComputeFade());
		}
	}

	private double ComputeFade()
	{
		double num = Mathf.Min(0.5, _duration * 0.5);
		double num2 = Mathf.Min(0.7, _duration * 0.5);
		if (num > 0.0 && _elapsed < num)
		{
			return Mathf.Clamp(_elapsed / num, 0.0, 1.0);
		}
		double num3 = _duration - _elapsed;
		if (num2 > 0.0 && num3 < num2)
		{
			return Mathf.Clamp(num3 / num2, 0.0, 1.0);
		}
		return 1.0;
	}

	private void ApplyViewportSize()
	{
		if (base.Material is ShaderMaterial shaderMaterial)
		{
			Viewport viewport = GetViewport();
			if (GodotObject.IsInstanceValid(viewport))
			{
				shaderMaterial.SetShaderParameter("effectSize", viewport.GetVisibleRect().Size);
			}
		}
	}
}
