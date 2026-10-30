using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

public partial class BulletFieldAnimateTest : Node2D
{
	private sealed class TestDefinition
	{
		public int Id;

		public Vector2 Offset;

		public float FrameRate;

		public int FrameCount;
	}

	private static readonly string[] ProjectileScenePaths = new string[6] { "res://Asset/Config/Projectile/Pea/Sprite/FirePea/FirePea.tscn", "res://Asset/Config/Projectile/Pea/Sprite/IceFirePea/IceFirePea.tscn", "res://Asset/Config/Projectile/Pea/Sprite/MegaFirePea/MegaFirePea.tscn", "res://Asset/Config/Projectile/Star/FireStar/FireStar.tscn", "res://Asset/Config/Projectile/Pea/Sprite/WhiteFirePea/WhiteFirePea.tscn", "res://Asset/Config/Projectile/Puff/Sprite/HypnoPuff/HypnoPuff.tscn" };

	private readonly List<TestDefinition> _definitions = new List<TestDefinition>();

	private AnimateMultiMeshRenderer _renderer;

	private double _elapsed;

	private int _measuredFrames;

	private bool _screenshotSaved;

	private bool _finished;

	[Export(PropertyHint.None, "")]
	public int SpawnCount { get; set; } = 3000;

	[Export(PropertyHint.None, "")]
	public int ZBucketCount { get; set; } = 6;

	[Export(PropertyHint.None, "")]
	public int StressDefinitionIndex { get; set; } = -1;

	[Export(PropertyHint.None, "")]
	public float StressScale { get; set; } = 0.32f;

	[Export(PropertyHint.None, "")]
	public double WarmupSeconds { get; set; } = 2.0;

	[Export(PropertyHint.None, "")]
	public double MeasureSeconds { get; set; } = 4.0;

	[Export(PropertyHint.None, "")]
	public string ScreenshotPath { get; set; } = "res://TestResults/bulletfield-unified-animation.png";

	public override void _Ready()
	{
		ApplyCommandLineArguments();
		CreateBackgroundAndLabels();
		_renderer = new AnimateMultiMeshRenderer
		{
			Name = "BulletFieldUnifiedAnimateRenderer"
		};
		AddChild(_renderer, forceReadableName: false, InternalMode.Disabled);
		LoadDefinitionsAndReferences();
		if (_definitions.Count == 0)
		{
			GD.PrintErr("[BulletFieldAnimateTest] no compatible projectile animation definitions were loaded.");
			GetTree().Quit(1);
			return;
		}
		GD.Print($"[BulletFieldAnimateTest] ready definitions={_definitions.Count} spawn={SpawnCount} zBuckets={ZBucketCount} screenshot={ScreenshotPath}");
	}

	public override void _Process(double delta)
	{
		if (!_finished && _definitions.Count != 0)
		{
			_elapsed += delta;
			_renderer.SetAnimationTime(_elapsed);
			_renderer.BeginFrame();
			DrawReferenceBatchRow();
			DrawStressInstances();
			_renderer.EndFrame();
			if (_elapsed >= WarmupSeconds)
			{
				_measuredFrames++;
			}
			if (!_screenshotSaved && _elapsed >= WarmupSeconds + 0.5)
			{
				_screenshotSaved = CaptureScreenshot();
			}
			if (_elapsed >= WarmupSeconds + MeasureSeconds)
			{
				FinishTest();
			}
		}
	}

	private void LoadDefinitionsAndReferences()
	{
		for (int i = 0; i < ProjectileScenePaths.Length; i++)
		{
			PackedScene packedScene = GD.Load<PackedScene>(ProjectileScenePaths[i]);
			if (packedScene != null && packedScene.Instantiate(PackedScene.GenEditState.Disabled) is AdobeAnimateSprite { clip: var clip } adobeAnimateSprite)
			{
				int num = _renderer.RegisterDefinition(adobeAnimateSprite.flashAnimeData, clip, 9);
				AnimateMultiMeshRenderer.Definition definition = _renderer.GetDefinition(num);
				if (definition == null || definition.frameMax <= 0)
				{
					adobeAnimateSprite.Free();
					continue;
				}
				TestDefinition testDefinition = new TestDefinition
				{
					Id = num,
					Offset = adobeAnimateSprite.offset,
					FrameRate = (float)Math.Max(0.0, definition.frameRate),
					FrameCount = definition.frameMax
				};
				_definitions.Add(testDefinition);
				AddChild(adobeAnimateSprite, forceReadableName: false, InternalMode.Disabled);
				adobeAnimateSprite.SetClip(clip);
				adobeAnimateSprite.pause = true;
				float num2 = ResolveReferenceFrame(testDefinition);
				adobeAnimateSprite.frameIndex = definition.clipStart + (int)Math.Floor(num2);
				adobeAnimateSprite.elapsedTimer = (double)num2 - Math.Floor(num2);
				adobeAnimateSprite.Position = new Vector2(125f + (float)i * 165f, 105f);
				adobeAnimateSprite.ZAsRelative = false;
				adobeAnimateSprite.ZIndex = 200;
			}
		}
	}

	private void DrawReferenceBatchRow()
	{
		for (int i = 0; i < _definitions.Count; i++)
		{
			TestDefinition testDefinition = _definitions[i];
			Vector2 origin = new Vector2(125f + (float)i * 165f, 220f) + testDefinition.Offset;
			_renderer.DrawInstanceAtZIndex(testDefinition.Id, 201, new Transform2D(0f, origin), ResolveReferenceFrame(testDefinition), Colors.White);
		}
	}

	private void DrawStressInstances()
	{
		if (SpawnCount > 0)
		{
			Vector2 size = GetViewportRect().Size;
			float num = 290f;
			float num2 = Math.Max(1f, size.Y - num);
			int num3 = Math.Max(1, Mathf.CeilToInt(Mathf.Sqrt((float)SpawnCount * size.X / num2)));
			int num4 = Math.Max(1, (SpawnCount + num3 - 1) / num3);
			float num5 = size.X / (float)num3;
			float num6 = num2 / (float)num4;
			for (int i = 0; i < SpawnCount; i++)
			{
				int index = ((StressDefinitionIndex >= 0) ? (StressDefinitionIndex % _definitions.Count) : (i % _definitions.Count));
				TestDefinition testDefinition = _definitions[index];
				int num7 = i % num3;
				int num8 = i / num3;
				Vector2 vector = new Vector2(((float)num7 + 0.5f) * num5, num + ((float)num8 + 0.5f) * num6);
				Vector2 vector2 = Vector2.One * Math.Max(0.01f, StressScale);
				Vector2 originPos = vector + testDefinition.Offset * vector2;
				Transform2D transform = new Transform2D(new Vector2(vector2.X, 0f), new Vector2(0f, vector2.Y), originPos);
				float frameFloat = (float)(_elapsed * (double)testDefinition.FrameRate + (double)i * 0.61803398875);
				_renderer.DrawInstanceAtZIndex(testDefinition.Id, num8 % Math.Max(1, ZBucketCount), transform, frameFloat, Colors.White);
			}
		}
	}

	private static float ResolveReferenceFrame(TestDefinition definition)
	{
		return (float)Math.Min(2, Math.Max(0, definition.FrameCount - 1)) + 0.25f;
	}

	private void CreateBackgroundAndLabels()
	{
		Vector2 size = GetViewportRect().Size;
		ColorRect node = new ColorRect
		{
			Color = new Color(0.025f, 0.055f, 0.04f),
			Size = size,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZAsRelative = false,
			ZIndex = -4096
		};
		AddChild(node, forceReadableName: false, InternalMode.Disabled);
		AddLabel("完整 Sprite 参考", new Vector2(18f, 18f));
		AddLabel("BulletField Compact Crowd", new Vector2(18f, 145f));
		AddLabel($"{SpawnCount} 个动态实例压力区", new Vector2(18f, 264f));
	}

	private void AddLabel(string text, Vector2 position)
	{
		Label label = new Label
		{
			Text = text,
			Position = position,
			ZAsRelative = false,
			ZIndex = 4096
		};
		label.AddThemeFontSizeOverride("font_size", 22);
		AddChild(label, forceReadableName: false, InternalMode.Disabled);
	}

	private bool CaptureScreenshot()
	{
		if (string.IsNullOrWhiteSpace(ScreenshotPath))
		{
			return true;
		}
		Image image = GetViewport().GetTexture().GetImage();
		if (image == null || image.IsEmpty())
		{
			return false;
		}
		string text = ScreenshotPath.Replace('\\', '/');
		string text2 = (text.StartsWith("res://", StringComparison.OrdinalIgnoreCase) ? ProjectSettings.GlobalizePath(text) : text);
		Error error = DirAccess.MakeDirRecursiveAbsolute(text2.GetBaseDir());
		if (error != Error.Ok && error != Error.AlreadyExists)
		{
			return false;
		}
		Error num = image.SavePng(text2);
		if (num == Error.Ok)
		{
			GD.Print($"[BulletFieldAnimateTest] screenshot saved path={text} size={image.GetWidth()}x{image.GetHeight()}");
		}
		return num == Error.Ok;
	}

	private void FinishTest()
	{
		_finished = true;
		double num = Math.Max(0.0001, _elapsed - WarmupSeconds);
		double num2 = (double)_measuredFrames / num;
		int num3 = SpawnCount + _definitions.Count;
		int visibleInstanceCountForTest = _renderer.GetVisibleInstanceCountForTest();
		int unifiedBucketCountForTest = _renderer.GetUnifiedBucketCountForTest();
		bool flag = _screenshotSaved && visibleInstanceCountForTest == num3 && unifiedBucketCountForTest > 0 && num2 > 0.0;
		GD.Print($"[BulletFieldAnimateTestResult] spawn={SpawnCount} definitions={_definitions.Count} visible={visibleInstanceCountForTest}/{num3} buckets={unifiedBucketCountForTest} averageFps={num2:F2} screenshotSaved={_screenshotSaved} passed={flag}");
		GetTree().Quit((!flag) ? 1 : 0);
	}

	private void ApplyCommandLineArguments()
	{
		string[] cmdlineUserArgs = OS.GetCmdlineUserArgs();
		foreach (string text in cmdlineUserArgs)
		{
			int value2;
			int value3;
			double value4;
			double value5;
			if (TryReadInt(text, "--bullet-test-spawn=", out var value))
			{
				SpawnCount = Math.Max(0, value);
			}
			else if (TryReadInt(text, "--bullet-test-z-buckets=", out value2))
			{
				ZBucketCount = Math.Max(1, value2);
			}
			else if (TryReadInt(text, "--bullet-test-definition=", out value3))
			{
				StressDefinitionIndex = value3;
			}
			else if (TryReadDouble(text, "--bullet-test-warmup=", out value4))
			{
				WarmupSeconds = Math.Max(0.0, value4);
			}
			else if (TryReadDouble(text, "--bullet-test-measure=", out value5))
			{
				MeasureSeconds = Math.Max(0.1, value5);
			}
			else if (text.StartsWith("--bullet-test-screenshot=", StringComparison.OrdinalIgnoreCase))
			{
				string text2 = text;
				int length = "--bullet-test-screenshot=".Length;
				ScreenshotPath = text2.Substring(length, text2.Length - length);
			}
		}
	}

	private static bool TryReadInt(string arg, string prefix, out int value)
	{
		value = 0;
		if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			int length = prefix.Length;
			return int.TryParse(arg.Substring(length, arg.Length - length), out value);
		}
		return false;
	}

	private static bool TryReadDouble(string arg, string prefix, out double value)
	{
		value = 0.0;
		if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			int length = prefix.Length;
			return double.TryParse(arg.Substring(length, arg.Length - length), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
		}
		return false;
	}
}
