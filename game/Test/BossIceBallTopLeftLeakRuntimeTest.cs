using System;
using Godot;

public partial class BossIceBallTopLeftLeakRuntimeTest : Node
{
	private const string BossIceBallEffectPath = "res://Asset/Anime/Character/Zombie/Boss/Boss/Effect/IceBall/IceBall.tscn";

	private const string ResultMarker = "BOSS_ICE_BALL_TOP_LEFT_LEAK_RESULT";

	private static readonly Color BackgroundColor = new Color(0.015f, 0.015f, 0.015f);

	private static readonly Vector2 SpawnPosition = new Vector2(650f, 300f);

	private static readonly Rect2I TopLeftRegion = new Rect2I(0, 0, 250, 160);

	private static readonly Rect2I SpawnRegion = new Rect2I(430, 150, 440, 300);

	public override async void _Ready()
	{
		int exitCode = 2;
		TowerDefenseEffectParticlesOnce effect = null;
		try
		{
			_ = 1;
			try
			{
				ColorRect node = new ColorRect
				{
					Color = BackgroundColor,
					Position = Vector2.Zero,
					Size = new Vector2(900f, 540f),
					MouseFilter = Control.MouseFilterEnum.Ignore,
					ZIndex = -4096
				};
				AddChild(node, forceReadableName: false, InternalMode.Disabled);
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Boss/Boss/Effect/IceBall/IceBall.tscn", null, ResourceLoader.CacheMode.Ignore);
				if (!GodotObject.IsInstanceValid(packedScene))
				{
					throw new InvalidOperationException("Boss 冰球粒子场景不可用。");
				}
				effect = TowerDefenseManager.CreateEffectParticlesOnce(packedScene, new Vector2I(100, 3));
				AddChild(effect, forceReadableName: false, InternalMode.Disabled);
				effect.GlobalPosition = SpawnPosition;
				bool localCoordinates = effect.particles.LocalCoords;
				double lifetime = effect.particles.Lifetime;
				bool productionWorldParticles = !localCoordinates && Mathf.IsEqualApprox(lifetime, 1.5);
				int topLeftLeakFrames = 0;
				int spawnVisibleFrames = 0;
				int maximumTopLeftPixels = 0;
				int maximumSpawnPixels = 0;
				for (int frame = 0; frame < 120; frame++)
				{
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
					await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
					using Image image = GetViewport().GetTexture().GetImage();
					int num = CountForegroundPixels(image, TopLeftRegion);
					int num2 = CountForegroundPixels(image, SpawnRegion);
					maximumTopLeftPixels = Math.Max(maximumTopLeftPixels, num);
					maximumSpawnPixels = Math.Max(maximumSpawnPixels, num2);
					if (num > 0)
					{
						topLeftLeakFrames++;
					}
					if (num2 > 0)
					{
						spawnVisibleFrames++;
					}
				}
				bool flag = topLeftLeakFrames == 0 && spawnVisibleFrames > 0 && productionWorldParticles && string.Equals(RenderingServer.GetCurrentRenderingMethod(), "mobile", StringComparison.Ordinal);
				GD.Print($"{"BOSS_ICE_BALL_TOP_LEFT_LEAK_RESULT"} passed={flag} topLeftLeakFrames={topLeftLeakFrames} spawnVisibleFrames={spawnVisibleFrames} maximumTopLeftPixels={maximumTopLeftPixels} maximumSpawnPixels={maximumSpawnPixels} localCoords={localCoordinates} lifetime={lifetime:F1} renderer={RenderingServer.GetCurrentRenderingMethod()}");
				exitCode = ((!flag) ? 2 : 0);
			}
			catch (Exception value)
			{
				GD.PrintErr($"{"BOSS_ICE_BALL_TOP_LEFT_LEAK_RESULT"} exception={value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(effect))
			{
				effect.QueueFree();
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		GetTree().Quit(exitCode);
	}

	private static int CountForegroundPixels(Image image, Rect2I region)
	{
		int num = 0;
		int num2 = Math.Min(region.End.X, image.GetWidth());
		int num3 = Math.Min(region.End.Y, image.GetHeight());
		for (int i = Math.Max(0, region.Position.Y); i < num3; i++)
		{
			for (int j = Math.Max(0, region.Position.X); j < num2; j++)
			{
				Color pixel = image.GetPixel(j, i);
				if (Math.Abs(pixel.R - BackgroundColor.R) + Math.Abs(pixel.G - BackgroundColor.G) + Math.Abs(pixel.B - BackgroundColor.B) > 0.08f)
				{
					num++;
				}
			}
		}
		return num;
	}
}
