using System;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewPolevaulterPostJumpTwitchRuntimeTest : Node
{
	private const string PolevaulterScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Polevaulter/Scene/TowerDefenseZombiePolevaulter.tscn";

	private const string WallnutScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn";

	private static readonly Vector2I EncounterGrid = new Vector2I(4, 2);

	private static readonly Vector2 EncounterPosition = new Vector2(400f, 252f);

	private int _checks;

	private int _failures;

	private int _clipChecks;

	private int _clipFailures;

	private int _transformChecks;

	private int _transformFailures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		bool previousBackZombie = manager?.backZombie ?? false;
		bool previousUseBatch = TowerDefenseZombie.UseBatch;
		BugOverviewPolevaulterPostJumpTwitchControlStub control = null;
		TowerDefenseZombiePolevaulter polevaulter = null;
		TowerDefensePlantWallnut wallnut = null;
		PackedScene polevaulterScene = null;
		PackedScene wallnutScene = null;
		int walkStartCount = 0;
		try
		{
			_ = 5;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					return;
				}
				control = new BugOverviewPolevaulterPostJumpTwitchControlStub
				{
					Name = "PolevaulterPostJumpTwitchControl",
					isGameRunning = false,
					isInit = true
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				manager.gridBeginPos = new Vector2(0f, 100f);
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				manager.backZombie = false;
				TowerDefenseZombie.UseBatch = false;
				polevaulterScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Polevaulter/Scene/TowerDefenseZombiePolevaulter.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
				wallnutScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
				Check(GodotObject.IsInstanceValid(polevaulterScene) && GodotObject.IsInstanceValid(wallnutScene), "The production Polevaulter and Wall-nut scenes must load.");
				polevaulter = polevaulterScene?.Instantiate<TowerDefenseZombiePolevaulter>(PackedScene.GenEditState.Disabled);
				wallnut = wallnutScene?.Instantiate<TowerDefensePlantWallnut>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(polevaulter) && GodotObject.IsInstanceValid(wallnut), "The production Polevaulter and Wall-nut scenes must instantiate.");
				if (!GodotObject.IsInstanceValid(polevaulter) || !GodotObject.IsInstanceValid(wallnut))
				{
					return;
				}
				PrepareCharacter(wallnut, EncounterGrid, EncounterPosition);
				PrepareCharacter(polevaulter, EncounterGrid, EncounterPosition);
				control.characterNode.AddChild(wallnut, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(polevaulter, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(5);
				polevaulter.ProcessMode = ProcessModeEnum.Disabled;
				wallnut.ProcessMode = ProcessModeEnum.Disabled;
				PrepareCharacter(wallnut, EncounterGrid, EncounterPosition);
				PrepareCharacter(polevaulter, EncounterGrid, EncounterPosition);
				polevaulter.groundMoveComponent?.SetAlive(false);
				await WaitFrames(2);
				Check(polevaulter.SceneFilePath == "res://Asset/Anime/Character/Zombie/Chapter1/Polevaulter/Scene/TowerDefenseZombiePolevaulter.tscn" && wallnut.SceneFilePath == "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn" && polevaulter.config?.name == "ZombiePolevaulter" && wallnut.config?.name == "PlantWallnut", "The fixture must use the reported Chapter1 Polevaulter and a real Wall-nut.");
				Check((polevaulter.StateMachine?.IsInitialized ?? false) && (polevaulter.sprite?.HasClip("Jump") ?? false) && polevaulter.sprite.HasClip(polevaulter.walkAnimeClip), "The real Polevaulter state machine and authored Jump/Walk clips must be active.");
				AttackComponent attackComponent = polevaulter.componentManager?.GetRuntime<AttackComponent>("character.attack.1");
				BugOverviewPolevaulterPostJumpTwitchRuntimeTest bugOverviewPolevaulterPostJumpTwitchRuntimeTest = this;
				int condition;
				if (attackComponent != null && !attackComponent.IsReleased && attackComponent.checkTall)
				{
					GroundMoveComponent groundMoveComponent = polevaulter.groundMoveComponent;
					if (groundMoveComponent != null && !groundMoveComponent.IsReleased && polevaulter.HasHitBox && wallnut.HasHitBox && polevaulter.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
					{
						condition = ((wallnut.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT) ? 1 : 0);
						goto IL_0640;
					}
				}
				condition = 0;
				goto IL_0640;
				IL_0640:
				bugOverviewPolevaulterPostJumpTwitchRuntimeTest.Check((byte)condition != 0, "The authored jump targeter, movement source, camps, and collision bodies must be active.");
				BugOverviewPolevaulterPostJumpTwitchRuntimeTest bugOverviewPolevaulterPostJumpTwitchRuntimeTest2 = this;
				TowerDefenseCharacterInstance instance = wallnut.instance;
				bugOverviewPolevaulterPostJumpTwitchRuntimeTest2.Check(instance != null && instance.height < TowerDefenseEnum.CHARACTER_HEIGHT.TALL && polevaulter.gridPos == EncounterGrid && wallnut.gridPos == EncounterGrid, "The jump target must be a real short plant in the same encounter cell.");
				if (attackComponent == null || attackComponent.IsReleased)
				{
					goto end_IL_013f;
				}
				control.isGameRunning = true;
				attackComponent.target = wallnut;
				attackComponent.alive = true;
				attackComponent.timer = 0.0;
				attackComponent.checkIntrevalNow = 0;
				Check(attackComponent.HasAttackGridTargetCandidates() && attackComponent.CanAttack() && attackComponent.target == wallnut, "The real Polevaulter jump targeter must acquire the overlapping Wall-nut.");
				control.isGameRunning = false;
				polevaulter.sprite.OnAnimeStarted += TrackAnimationStart;
				bool flag = polevaulter.SendStateEvent("ToJump");
				CheckClip(flag && polevaulter.CurrentStateHandle?.StableId == "zombie.polevaulter.jump" && polevaulter.sprite.clip == "Jump", "The real state machine must enter the authored Jump clip.");
				polevaulter.AnimeEvent("check", default(Variant));
				polevaulter.AnimeEvent("jumpOver", default(Variant));
				float x = polevaulter.GlobalPosition.X;
				float outerScaleX = polevaulter.Scale.X;
				float transformScaleX = polevaulter.transformPoint.Scale.X;
				bool playbackBeforeLanding = polevaulter.sprite.playBack;
				float expectedLandingX = x - (float)((double)(outerScaleX * transformScaleX) * 148.0);
				polevaulter.AnimeCompleted("Jump");
				await WaitFrames(1);
				string settledClip = polevaulter.sprite.clip;
				string settledState = polevaulter.CurrentStateHandle?.StableId ?? string.Empty;
				float settledX = polevaulter.GlobalPosition.X;
				CheckClip(settledState == "zombie.walk" && settledClip == polevaulter.walkAnimeClip && walkStartCount == 1 && !polevaulter.jumpMove, "The first valid Jump completion must enter Walk exactly once.");
				CheckTransform(Mathf.IsEqualApprox(settledX, expectedLandingX), $"The first completion must apply exactly one authored 148px landing displacement; expected={expectedLandingX}, actual={settledX}.");
				CheckTransform(Mathf.IsEqualApprox(polevaulter.Scale.X, outerScaleX) && Mathf.IsEqualApprox(polevaulter.transformPoint.Scale.X, transformScaleX) && polevaulter.sprite.playBack == playbackBeforeLanding, "The valid landing must preserve the Polevaulter's authored facing.");
				await WaitFrames(8);
				CheckClip(polevaulter.CurrentStateHandle?.StableId == settledState && polevaulter.sprite.clip == settledClip && walkStartCount == 1, "The settled Walk clip must not restart while the old WalkEntered delay expires.");
				CheckTransform(Mathf.IsEqualApprox(polevaulter.GlobalPosition.X, settledX) && FacingMatches(polevaulter, outerScaleX, transformScaleX, playbackBeforeLanding), "The expired WalkEntered delay must not change the settled position or facing.");
				polevaulter.AnimeCompleted("Jump");
				CheckClip(polevaulter.CurrentStateHandle?.StableId == settledState && polevaulter.sprite.clip == settledClip && walkStartCount == 1, "A stale Jump completion must not re-enter or restart Walk.");
				CheckTransform(Mathf.IsEqualApprox(polevaulter.GlobalPosition.X, settledX) && FacingMatches(polevaulter, outerScaleX, transformScaleX, playbackBeforeLanding), "A stale Jump completion must not immediately apply a second movement or flip.");
				await WaitFrames(12);
				polevaulter.AnimeCompleted("Jump");
				await WaitFrames(2);
				CheckClip(polevaulter.CurrentStateHandle?.StableId == settledState && polevaulter.sprite.clip == settledClip && walkStartCount == 1, "Repeated late Jump completions must leave one uninterrupted Walk clip.");
				CheckTransform(Mathf.IsEqualApprox(polevaulter.GlobalPosition.X, settledX) && FacingMatches(polevaulter, outerScaleX, transformScaleX, playbackBeforeLanding), "Repeated late Jump completions must leave the settled transform unchanged.");
				goto end_IL_011c;
				end_IL_013f:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[{"BugOverviewPolevaulterPostJumpTwitchRuntimeTest"}] Unexpected exception: {value}");
				goto end_IL_011c;
			}
			return;
			end_IL_011c:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(polevaulter?.sprite))
			{
				polevaulter.sprite.OnAnimeStarted -= TrackAnimationStart;
			}
			if (GodotObject.IsInstanceValid(polevaulter))
			{
				polevaulter.QueueFree();
			}
			if (GodotObject.IsInstanceValid(wallnut))
			{
				wallnut.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
				manager.backZombie = previousBackZombie;
			}
			TowerDefenseZombie.UseBatch = previousUseBatch;
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitFrames(8);
			ObjectManager.Instance?.Clear();
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			polevaulterScene?.Dispose();
			wallnutScene?.Dispose();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			await WaitFrames(4);
		}
		bool flag2 = _clipFailures == 0 && _clipChecks == 5;
		bool flag3 = _transformFailures == 0 && _transformChecks == 5;
		bool flag4 = _failures == 0 && _checks == 18 && flag2 && flag3;
		GD.Print($"POLEVAULTER_POST_JUMP_CLIP_STABILITY_RESULT passed={flag2} checks={_clipChecks} failures={_clipFailures}");
		GD.Print($"POLEVAULTER_POST_JUMP_TRANSFORM_STABILITY_RESULT passed={flag3} checks={_transformChecks} failures={_transformFailures}");
		GD.Print($"BUG_OVERVIEW_POLEVAULTER_POST_JUMP_TWITCH_RESULT passed={flag4} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag4) ? 2 : 0);
		void TrackAnimationStart(string clip)
		{
			if (GodotObject.IsInstanceValid(polevaulter) && clip == polevaulter.walkAnimeClip)
			{
				walkStartCount++;
			}
		}
	}

	private static void PrepareCharacter(TowerDefenseCharacter character, Vector2I gridPos, Vector2 position)
	{
		character.editorPreviewMode = false;
		character.inGame = true;
		character.gridPos = gridPos;
		character.GlobalPosition = position;
	}

	private static bool FacingMatches(TowerDefenseZombiePolevaulter polevaulter, float outerScaleX, float transformScaleX, bool playback)
	{
		if (Mathf.IsEqualApprox(polevaulter.Scale.X, outerScaleX) && Mathf.IsEqualApprox(polevaulter.transformPoint.Scale.X, transformScaleX))
		{
			return polevaulter.sprite.playBack == playback;
		}
		return false;
	}

	private async Task WaitFrames(int count)
	{
		for (int index = 0; index < count; index++)
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
			GD.PushError("[BugOverviewPolevaulterPostJumpTwitchRuntimeTest] " + message);
		}
	}

	private void CheckClip(bool condition, string message)
	{
		_checks++;
		_clipChecks++;
		if (!condition)
		{
			_failures++;
			_clipFailures++;
			GD.PushError("[PolevaulterPostJumpClipStability] " + message);
		}
	}

	private void CheckTransform(bool condition, string message)
	{
		_checks++;
		_transformChecks++;
		if (!condition)
		{
			_failures++;
			_transformFailures++;
			GD.PushError("[PolevaulterPostJumpTransformStability] " + message);
		}
	}
}
