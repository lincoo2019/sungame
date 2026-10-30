using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewNormalZombieWaterEntryStabilityRuntimeTest : Node
{
	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string ResultMarker = "NORMAL_ZOMBIE_WATER_ENTRY_STABILITY_RESULT";

	private readonly List<string> _failures = new List<string>();

	private int _checks;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		bool previousUseBatch = TowerDefenseZombie.UseBatch;
		BugOverviewNormalZombieWaterEntryControlStub control = null;
		TowerDefenseZombieNormal zombie = null;
		AdobeAnimateSprite.AnimeStartedEventHandler startedHandler = null;
		int swimAnimationStarts = 0;
		try
		{
			_ = 3;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Require(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload is unavailable.");
				control = new BugOverviewNormalZombieWaterEntryControlStub
				{
					Name = "NormalZombieWaterEntryControl",
					isGameRunning = true,
					isInit = true
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				TowerDefenseZombie.UseBatch = false;
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(5, 2)
				};
				towerDefenseCellInstance.Init(new TowerDefenseCellConfig());
				TowerDefenseCellConfig towerDefenseCellConfig = new TowerDefenseCellConfig();
				towerDefenseCellConfig.gridType.Add(TowerDefenseEnum.PLANTGRIDTYPE.WATER);
				TowerDefenseCellInstance waterCell = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(5, 2)
				};
				waterCell.Init(towerDefenseCellConfig);
				Check(!towerDefenseCellInstance.isWater && waterCell.isWater, "The fixture must prepare real land and WATER cell instances for environment detection.");
				zombie = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.IgnoreDeep)?.Instantiate<TowerDefenseZombieNormal>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(zombie) && zombie.SceneFilePath == "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn" && zombie.config?.name == "ZombieNormal", "The fixture must instantiate the reported real ordinary-zombie scene.");
				Require(GodotObject.IsInstanceValid(zombie), "The real ordinary-zombie scene failed to instantiate.");
				zombie.inGame = true;
				zombie.editorPreviewMode = false;
				zombie.gridPos = towerDefenseCellInstance.gridPos;
				zombie.cell = towerDefenseCellInstance;
				zombie.cellPercentage = 0.75;
				zombie.GlobalPosition = new Vector2(800f, 180f);
				control.characterNode.AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(5);
				BugOverviewNormalZombieWaterEntryStabilityRuntimeTest bugOverviewNormalZombieWaterEntryStabilityRuntimeTest = this;
				int condition;
				if (zombie.HasValidRuntimeConfiguration && GodotObject.IsInstanceValid(zombie.sprite))
				{
					SwimComponent swimComponent = zombie.swimComponent;
					if (swimComponent != null && !swimComponent.IsReleased)
					{
						WaterInteractionComponent waterInteractionComponent = zombie.waterInteractionComponent;
						if (waterInteractionComponent != null && !waterInteractionComponent.IsReleased)
						{
							GroundHeightComponent groundHeightComponent = zombie.groundHeightComponent;
							condition = ((groundHeightComponent != null && !groundHeightComponent.IsReleased && groundHeightComponent.detectWater) ? 1 : 0);
							goto IL_0405;
						}
					}
				}
				condition = 0;
				goto IL_0405;
				IL_0490:
				int condition2;
				Require((byte)condition2 != 0, "The ordinary-zombie water runtimes did not initialize.");
				Check(zombie.walkAnimeClip == "Walk1&Walk2" && zombie.swimAnimeClip == "Swim", "The scene must retain its authored distinct land and water clips.");
				zombie.inSwimPlay = false;
				zombie.Walk();
				await WaitFrames(3);
				bool flag = Array.IndexOf(zombie.walkAnimeClip.Split('&', StringSplitOptions.RemoveEmptyEntries), zombie.sprite.clip) >= 0;
				Check(!zombie.inWater && flag, $"The real zombie must begin on one authored land-walk clip; configured={zombie.walkAnimeClip}, actual={zombie.sprite.clip}.");
				startedHandler = delegate(string clip)
				{
					if (clip == zombie.swimAnimeClip)
					{
						swimAnimationStarts++;
					}
				};
				zombie.sprite.OnAnimeStarted += startedHandler;
				zombie.cell = waterCell;
				zombie.groundHeightComponent.DetectEnvironment();
				await WaitFrames(4);
				Check(zombie.inWater && zombie.sprite.clip == zombie.swimAnimeClip && Mathf.IsEqualApprox((float)zombie.groundHeight, (float)(0.0 - zombie.waterHeight)), $"Entering water must switch once to Swim and lower the real zombie; clip={zombie.sprite.clip}, ground={zombie.groundHeight}, waterHeight={zombie.waterHeight}.");
				Check(zombie.CurrentStateHandle?.StableId == "zombie.walk", "Water entry must remain in the production walk state; state=" + zombie.CurrentStateHandle?.StableId + ".");
				int backwardsJumps = 0;
				int previousFrame = zombie.sprite.frameIndex;
				HashSet<int> seenFrames = new HashSet<int> { previousFrame };
				for (int sample = 0; sample < 30; sample++)
				{
					zombie.groundHeightComponent.DetectEnvironment();
					zombie.groundHeightComponent.BatchUpdate(1.0 / 60.0);
					if (sample % 10 == 0)
					{
						zombie.InWaterDiscardSet();
					}
					await WaitPhysicsFrames(1);
					Check(zombie.sprite.clip == zombie.swimAnimeClip, $"Water synchronization must keep the Swim clip at sample {sample}; actual={zombie.sprite.clip}.");
					int frameIndex = zombie.sprite.frameIndex;
					seenFrames.Add(frameIndex);
					if (frameIndex < previousFrame)
					{
						backwardsJumps++;
					}
					previousFrame = frameIndex;
				}
				int count = seenFrames.Count;
				Check(count >= 4, $"The real Swim animation must continue advancing instead of freezing; distinctFrames={count}.");
				Check(backwardsJumps == 0, $"The short Swim sample cannot naturally loop and must never restart/twitch; backwardsJumps={backwardsJumps}.");
				Check(swimAnimationStarts == 1, $"Continuous real water-cell detection must start Swim exactly once; starts={swimAnimationStarts}.");
				Check(zombie.waterInteractionComponent.isInWater, "The periodic real discard refresh must retain the water visual state.");
				goto end_IL_00a2;
				IL_0405:
				bugOverviewNormalZombieWaterEntryStabilityRuntimeTest.Check((byte)condition != 0, "The real zombie must initialize its authored Sprite, Swim, WaterInteraction, and water-detecting GroundHeight runtimes.");
				if (GodotObject.IsInstanceValid(zombie.sprite))
				{
					SwimComponent swimComponent = zombie.swimComponent;
					if (swimComponent != null && !swimComponent.IsReleased)
					{
						WaterInteractionComponent waterInteractionComponent = zombie.waterInteractionComponent;
						if (waterInteractionComponent != null && !waterInteractionComponent.IsReleased)
						{
							GroundHeightComponent groundHeightComponent = zombie.groundHeightComponent;
							condition2 = ((groundHeightComponent != null && !groundHeightComponent.IsReleased && groundHeightComponent.detectWater) ? 1 : 0);
							goto IL_0490;
						}
					}
				}
				condition2 = 0;
				goto IL_0490;
				end_IL_00a2:;
			}
			catch (Exception value)
			{
				_failures.Add($"Unexpected runtime exception: {value}");
			}
		}
		finally
		{
			TowerDefenseZombie.UseBatch = previousUseBatch;
			if (GodotObject.IsInstanceValid(zombie?.sprite) && startedHandler != null)
			{
				zombie.sprite.OnAnimeStarted -= startedHandler;
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
			}
			if (GodotObject.IsInstanceValid(zombie) && !zombie.IsQueuedForDeletion())
			{
				zombie.QueueFree();
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			await WaitFrames(4);
		}
		Finish();
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}

	private async Task WaitFrames(int count)
	{
		for (int index = 0; index < count; index++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task WaitPhysicsFrames(int count)
	{
		for (int index = 0; index < count; index++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures.Add(message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PushError("[BugOverviewNormalZombieWaterEntryStabilityRuntimeTest] " + failure);
		}
		bool flag = _failures.Count == 0 && _checks == 42;
		GD.Print($"{"NORMAL_ZOMBIE_WATER_ENTRY_STABILITY_RESULT"} passed={flag} checks={_checks} failures={_failures.Count}");
		GetTree().Quit((!flag) ? 2 : 0);
	}
}
