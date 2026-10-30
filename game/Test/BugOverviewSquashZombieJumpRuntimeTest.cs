using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewSquashZombieJumpRuntimeTest : Node
{
	private const string SquashScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Squash/TowerDefenseZombieNormalSquash.tscn";

	private const string TargetScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn";

	private static readonly Vector2I SquashGrid = new Vector2I(5, 2);

	private int _checks;

	private int _failures;

	private bool _verifyRendering;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		SquashZombieJumpRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 6;
			try
			{
				_verifyRendering = System.Array.Exists(OS.GetCmdlineUserArgs(), (string argument) => argument == "--verify-rendering");
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					throw new InvalidOperationException("TowerDefenseManager autoload is unavailable.");
				}
				control = new SquashZombieJumpRuntimeControlStub
				{
					Name = "SquashZombieJumpRuntimeControl",
					isGameRunning = false,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig()
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				manager.gridBeginPos = new Vector2(100f, 200f);
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				TowerDefenseZombieNormalSquash squash = LoadCharacter<TowerDefenseZombieNormalSquash>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Squash/TowerDefenseZombieNormalSquash.tscn");
				Check(GodotObject.IsInstanceValid(squash) && squash.config?.name == "ZombieNormalSquash", "The regression must instantiate the real Squash zombie scene and config.");
				if (!GodotObject.IsInstanceValid(squash))
				{
					throw new InvalidOperationException("The real Squash zombie scene could not be instantiated.");
				}
				squash.editorPreviewMode = false;
				squash.inGame = true;
				squash.gridPos = SquashGrid;
				squash.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(SquashGrid);
				control.characterNode.AddChild(squash, forceReadableName: false, InternalMode.Disabled);
				Vector2 targetPosition = squash.GetLogicalGlobalPosition() + new Vector2(-50f, 0f);
				TowerDefensePlantWallnut target = LoadCharacter<TowerDefensePlantWallnut>("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn");
				target.editorPreviewMode = false;
				target.inGame = true;
				target.gridPos = new Vector2I(4, 2);
				target.GlobalPosition = targetPosition;
				control.characterNode.AddChild(target, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(8);
				Check(target.config?.name == "PlantWallnut" && !target.die, "The landing target must be a living plant from the real Wallnut scene.");
				double initialHealth = target.instance.hitpoints;
				AdobeAnimateSpriteBase squashHead = (squash.sprite as ZombieNormalSquashSprite)?.head;
				Check(GodotObject.IsInstanceValid(squashHead) && squashHead.HasClip("JumpUp") && squashHead.HasClip("JumpDown"), "The real nested Squash visual must expose both jump clips.");
				if (!GodotObject.IsInstanceValid(squashHead))
				{
					throw new InvalidOperationException("The real nested Squash visual is unavailable.");
				}
				AdobeAnimateGpuRenderGraphDefinition graph;
				AdobeAnimateSprite[] ownerSprites;
				string failureReason;
				bool flag = AdobeAnimateGpuRenderGraphBuilder.TryBuild(squash.sprite, out graph, out ownerSprites, out failureReason);
				Check(flag && System.Array.Exists(ownerSprites, (AdobeAnimateSprite owner) => owner == squashHead), $"The GPU render graph must own the nested Squash visual; reason={failureReason}, owners={((ownerSprites != null) ? ownerSprites.Length : 0)}, slots={(graph?.RenderSlots?.Length).GetValueOrDefault()}.");
				double initialOffsetX = squashHead.offset.X;
				double initialOffsetY = squashHead.offset.Y;
				double maximumOffsetX = initialOffsetX;
				double minimumOffsetY = initialOffsetY;
				ulong initialPoseRevision = squashHead.ManagedPoseRevision;
				ulong maximumPoseRevision = initialPoseRevision;
				Vector2 initialScale = squashHead.Scale;
				Vector2 initialPixels = Vector2.Zero;
				float minimumPixelY = 3.4028235E+38f;
				float minimumPixelX = 3.4028235E+38f;
				if (_verifyRendering)
				{
					initialPixels = await CaptureFrame("before");
				}
				squash.StartSquashJump(targetPosition);
				Check(!squashHead.IsRenderedByParentSpriteForRender(), "The jumping Squash must stop using the body's cached head attachment.");
				Check(squashHead.Scale.IsEqualApprox(initialScale), "Detaching the mirrored Squash must preserve its scale and upright rotation convention.");
				bool sawJumpUp = false;
				bool sawJumpDown = false;
				bool airborneTargetIntact = true;
				for (int frame = 0; frame < 120; frame++)
				{
					if (squash.over)
					{
						break;
					}
					await WaitFrames(1);
					maximumOffsetX = Math.Max(maximumOffsetX, squashHead.offset.X);
					minimumOffsetY = Math.Min(minimumOffsetY, squashHead.offset.Y);
					maximumPoseRevision = Math.Max(maximumPoseRevision, squashHead.ManagedPoseRevision);
					sawJumpUp |= squashHead.clip == "JumpUp";
					sawJumpDown |= squashHead.clip == "JumpDown";
					if (squashHead.clip == "JumpUp")
					{
						airborneTargetIntact &= !squash.over && Math.Abs(target.instance.hitpoints - initialHealth) < 0.001;
					}
					if (_verifyRendering && frame % 3 == 0)
					{
						Vector2 vector = await CaptureFrame($"jump-{frame:D2}");
						minimumPixelY = Math.Min(minimumPixelY, vector.Y);
						minimumPixelX = Math.Min(minimumPixelX, vector.X);
					}
				}
				Check(maximumOffsetX > initialOffsetX + 15.0, $"The Squash must travel toward the target; initialX={initialOffsetX:F3}, maximumX={maximumOffsetX:F3}.");
				Check(minimumOffsetY < initialOffsetY - 40.0, $"The Squash must rise before smashing; initialY={initialOffsetY:F3}, minimumY={minimumOffsetY:F3}.");
				Check(maximumPoseRevision > initialPoseRevision + 2, $"The complete offset tween must republish changing GPU poses; initialRevision={initialPoseRevision}, maximumRevision={maximumPoseRevision}.");
				Check(sawJumpUp && sawJumpDown && airborneTargetIntact, "The full jump must keep the target unharmed until the landing animation.");
				Check(squash.over, "The real animation completion callback must reach the smash impact.");
				await WaitFrames(3);
				Check(target.die || target.instance.hitpoints < initialHealth, $"The real landing event must damage the target plant; before={initialHealth}, after={target.instance.hitpoints}.");
				if (_verifyRendering)
				{
					Vector2 value = await CaptureFrame("landed");
					Check(minimumPixelY < initialPixels.Y - 35f, $"Rendered Squash pixels must rise above the head; initial={initialPixels}, minimumY={minimumPixelY:F2}.");
					float num = Math.Max(15f, (initialPixels.X - targetPosition.X) * 0.5f);
					Check(minimumPixelX < initialPixels.X - num, $"Rendered Squash pixels must cover at least half the horizontal distance to the plant; initialX={initialPixels.X:F2}, targetX={targetPosition.X:F2}, minimumX={minimumPixelX:F2}.");
					Check(value.Y > initialPixels.Y + 35f && Math.Abs(value.X - targetPosition.X) < 15f, $"Rendered Squash pixels must land on the target plant below the original head position; target={targetPosition}, initialY={initialPixels.Y:F2}, landed={value}.");
					GD.Print($"SQUASH_ZOMBIE_JUMP_VISUAL initial={initialPixels} apexY={minimumPixelY:F2} leftmostX={minimumPixelX:F2} landed={value} renderer={RenderingServer.GetCurrentRenderingMethod()}");
				}
				await WaitFrames(40);
			}
			catch (Exception value2)
			{
				_failures++;
				GD.PushError($"[BugOverviewSquashZombieJumpRuntimeTest] Unexpected exception: {value2}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(mapControl))
			{
				mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitFrames(3);
		}
		bool flag2 = _failures == 0 && _checks == (_verifyRendering ? 16 : 13);
		GD.Print($"SQUASH_ZOMBIE_JUMP_RESULT passed={flag2} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag2) ? 2 : 0);
	}

	private async Task<Vector2> CaptureFrame(string label)
	{
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		using Image image = GetViewport().GetTexture().GetImage();
		string environment = OS.GetEnvironment("SQUASH_JUMP_CAPTURE_DIR");
		if (!string.IsNullOrWhiteSpace(environment))
		{
			Directory.CreateDirectory(environment);
			image.SavePng(Path.Combine(environment, label + ".png"));
		}
		image.Convert(Image.Format.Rgba8);
		byte[] data = image.GetData();
		int width = image.GetWidth();
		long num = 0L;
		long num2 = 0L;
		int num3 = 0;
		for (int i = 0; i < data.Length / 4; i++)
		{
			int num4 = i * 4;
			int num5 = data[num4];
			int num6 = data[num4 + 1];
			int num7 = data[num4 + 2];
			if (num5 >= 70 && num6 >= 110 && !((float)num6 < (float)num5 * 1.1f) && !((float)num7 > (float)num6 * 0.75f))
			{
				num += i % width;
				num2 += i / width;
				num3++;
			}
		}
		if (num3 < 100)
		{
			throw new InvalidOperationException($"The rendered Squash disappeared in frame {label}; pixels={num3}.");
		}
		return new Vector2((float)num / (float)num3, (float)num2 / (float)num3);
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig()
		});
		towerDefenseBattleFeatureMap.plantGrid.Resize(gridNum.X + 1);
		for (int i = 0; i <= gridNum.X; i++)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			array.Resize(gridNum.Y + 1);
			for (int j = 1; j <= gridNum.Y; j++)
			{
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(i, j)
				};
				towerDefenseCellInstance.Init(new TowerDefenseCellConfig());
				array[j] = towerDefenseCellInstance;
			}
			towerDefenseBattleFeatureMap.plantGrid[i] = array;
		}
		towerDefenseBattleFeatureMap.iceCapList.Resize(gridNum.Y + 1);
		return towerDefenseBattleFeatureMap;
	}

	private static T LoadCharacter<T>(string path) where T : TowerDefenseCharacter
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		if (packedScene == null)
		{
			return null;
		}
		return packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled);
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewSquashZombieJumpRuntimeTest] " + message);
		}
	}
}
