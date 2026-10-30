using System;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewPogoTallnutLowFpsRuntimeTest : Node
{
	private const string PogoScenePath = "res://Asset/Anime/Character/Zombie/Chapter4/Pogo/Scene/TowerDefenseZombiePogo.tscn";

	private const string TallnutScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Tallnut/Scene/TowerDefensePlantTallnut.tscn";

	private const string WallnutScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn";

	private const int LowPhysicsTicks = 30;

	private const float ZombieStartX = 480f;

	private const float PlantX = 450f;

	private const float JumpDestinationX = 370f;

	private static readonly Vector2I TallEncounterGrid = new Vector2I(5, 1);

	private static readonly Vector2I ShortEncounterGrid = new Vector2I(5, 3);

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		bool previousBackZombie = manager?.backZombie ?? false;
		int previousPhysicsTicks = Engine.PhysicsTicksPerSecond;
		int previousMaxFps = Engine.MaxFps;
		BugOverviewPogoTallnutLowFpsControlStub control = null;
		TowerDefenseZombiePogo tallPogo = null;
		TowerDefenseZombiePogo shortPogo = null;
		TowerDefensePlantTallnut tallnut = null;
		TowerDefensePlantWallnut wallnut = null;
		PackedScene pogoScene = null;
		PackedScene tallnutScene = null;
		PackedScene wallnutScene = null;
		try
		{
			_ = 2;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					return;
				}
				Engine.PhysicsTicksPerSecond = 30;
				Engine.MaxFps = 30;
				control = new BugOverviewPogoTallnutLowFpsControlStub
				{
					Name = "PogoTallnutLowFpsControl",
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
				manager.gridBeginPos = new Vector2(0f, 100f);
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				manager.backZombie = false;
				pogoScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter4/Pogo/Scene/TowerDefenseZombiePogo.tscn", null, ResourceLoader.CacheMode.Ignore);
				tallnutScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/Tallnut/Scene/TowerDefensePlantTallnut.tscn", null, ResourceLoader.CacheMode.Ignore);
				wallnutScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(pogoScene), "The real Pogo zombie scene must load.");
				Check(GodotObject.IsInstanceValid(tallnutScene), "The real Tallnut scene must load.");
				Check(GodotObject.IsInstanceValid(wallnutScene), "The real Wallnut control scene must load.");
				tallPogo = pogoScene?.Instantiate<TowerDefenseZombiePogo>(PackedScene.GenEditState.Disabled);
				shortPogo = pogoScene?.Instantiate<TowerDefenseZombiePogo>(PackedScene.GenEditState.Disabled);
				tallnut = tallnutScene?.Instantiate<TowerDefensePlantTallnut>(PackedScene.GenEditState.Disabled);
				wallnut = wallnutScene?.Instantiate<TowerDefensePlantWallnut>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(tallPogo) && GodotObject.IsInstanceValid(shortPogo), "The real Pogo scene must instantiate both jump variants.");
				Check(GodotObject.IsInstanceValid(tallnut) && GodotObject.IsInstanceValid(wallnut), "The real Tallnut and Wallnut scenes must instantiate.");
				if (!GodotObject.IsInstanceValid(tallPogo) || !GodotObject.IsInstanceValid(shortPogo) || !GodotObject.IsInstanceValid(tallnut) || !GodotObject.IsInstanceValid(wallnut))
				{
					return;
				}
				Vector2 fixtureCellCenter = GetFixtureCellCenter(manager, TallEncounterGrid);
				Vector2 fixtureCellCenter2 = GetFixtureCellCenter(manager, ShortEncounterGrid);
				PrepareCharacter(tallnut, TallEncounterGrid, new Vector2(450f, fixtureCellCenter.Y));
				PrepareCharacter(tallPogo, TallEncounterGrid, new Vector2(480f, fixtureCellCenter.Y));
				PrepareCharacter(wallnut, ShortEncounterGrid, new Vector2(450f, fixtureCellCenter2.Y));
				PrepareCharacter(shortPogo, ShortEncounterGrid, new Vector2(480f, fixtureCellCenter2.Y));
				control.characterNode.AddChild(tallnut, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(tallPogo, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(wallnut, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(shortPogo, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(3);
				AttackComponent attackComponent = tallPogo.componentManager?.GetRuntime<AttackComponent>("character.attack.1");
				AttackComponent shortJumpAttack = shortPogo.componentManager?.GetRuntime<AttackComponent>("character.attack.1");
				Check(Engine.PhysicsTicksPerSecond == 30 && Engine.PhysicsTicksPerSecond <= 40, $"The regression must actually run at no more than 40 physics ticks; got {Engine.PhysicsTicksPerSecond}.");
				Check(tallPogo.config?.name == "ZombiePogo" && (tallPogo.sprite?.HasClip("Pogo") ?? false) && (tallPogo.instance?.ArmorHas("Pogo") ?? false), "The fixture must use the authored armed Pogo zombie.");
				BugOverviewPogoTallnutLowFpsRuntimeTest bugOverviewPogoTallnutLowFpsRuntimeTest = this;
				int condition;
				if (tallnut.config?.name == "PlantTallnut")
				{
					TowerDefenseCharacterInstance instance = tallnut.instance;
					if (instance != null && instance.height >= TowerDefenseEnum.CHARACTER_HEIGHT.TALL && wallnut.config?.name == "PlantWallnut")
					{
						TowerDefenseCharacterInstance instance2 = wallnut.instance;
						condition = ((instance2 != null && instance2.height < TowerDefenseEnum.CHARACTER_HEIGHT.TALL) ? 1 : 0);
						goto IL_067b;
					}
				}
				condition = 0;
				goto IL_067b;
				IL_067b:
				bugOverviewPogoTallnutLowFpsRuntimeTest.Check((byte)condition != 0, "The real Tallnut must be tall and the real Wallnut must remain the short control.");
				BugOverviewPogoTallnutLowFpsRuntimeTest bugOverviewPogoTallnutLowFpsRuntimeTest2 = this;
				int condition2;
				if (attackComponent != null && !attackComponent.IsReleased)
				{
					if (shortJumpAttack != null && !shortJumpAttack.IsReleased && attackComponent.checkTall)
					{
						condition2 = (shortJumpAttack.checkTall ? 1 : 0);
						goto IL_06c1;
					}
				}
				condition2 = 0;
				goto IL_06c1;
				IL_06c1:
				bugOverviewPogoTallnutLowFpsRuntimeTest2.Check((byte)condition2 != 0, "Both real Pogo jump-target components must be active with tall-first targeting.");
				Check(tallPogo.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE && tallnut.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT && tallPogo.HasHitBox && tallnut.HasHitBox && wallnut.HasHitBox && tallPogo.gridPos == TallEncounterGrid && tallnut.gridPos == TallEncounterGrid && shortPogo.gridPos == ShortEncounterGrid && wallnut.gridPos == ShortEncounterGrid, "The real opposing camps, collision bodies, and authored encounter grids must be registered.");
				ArmLowFpsJump(tallPogo, attackComponent, tallnut);
				Check(attackComponent.HasAttackGridTargetCandidates() && attackComponent.CanAttack() && attackComponent.target == tallnut, "The real low-FPS jump check must acquire the overlapping Tallnut.");
				tallPogo.Land();
				tallPogo.PogoProcessing(1.0 / 30.0);
				float tallInterceptX = tallPogo.GlobalPosition.X;
				Check(!tallPogo.hasPogo && !tallPogo.isJump && !tallPogo.pogoPlant, "Tallnut interception must immediately end the Pogo jump state.");
				BugOverviewPogoTallnutLowFpsRuntimeTest bugOverviewPogoTallnutLowFpsRuntimeTest3 = this;
				TowerDefenseCharacterInstance instance3 = tallPogo.instance;
				bugOverviewPogoTallnutLowFpsRuntimeTest3.Check(instance3 != null && !instance3.ArmorHas("Pogo"), "Tallnut interception must drop the real pogo armor.");
				await WaitFrames(24);
				Check(tallPogo.GlobalPosition.X > 430f, $"The cancelled low-FPS jump must not carry Pogo behind Tallnut; x={tallPogo.GlobalPosition.X}.");
				Check(Math.Abs(tallPogo.GlobalPosition.X - tallInterceptX) < 40f, $"After interception only normal close-range behavior may move Pogo, not the old 110px Tween; x={tallInterceptX}->{tallPogo.GlobalPosition.X}.");
				ArmLowFpsJump(shortPogo, shortJumpAttack, wallnut);
				Check(shortJumpAttack.HasAttackGridTargetCandidates() && shortJumpAttack.CanAttack() && shortJumpAttack.target == wallnut, "The real low-FPS jump check must acquire the overlapping short Wallnut control.");
				shortPogo.Land();
				shortPogo.PogoProcessing(1.0 / 30.0);
				Check(shortPogo.hasPogo && (shortPogo.instance?.ArmorHas("Pogo") ?? false), "A short Wallnut must not remove the pogo tool or cancel the normal jump.");
				await WaitFrames(24);
				Check(shortPogo.hasPogo && shortPogo.GlobalPosition.X < 420f, $"The short-plant control must complete its real low-FPS jump; x={shortPogo.GlobalPosition.X}.");
				Check(!shortPogo.pogoPlant && !shortPogo.isJump, "The completed short-plant jump must settle its asynchronous jump state normally.");
				goto end_IL_0108;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewPogoTallnutLowFpsRuntimeTest] Unexpected exception: {value}");
				goto end_IL_0108;
			}
			end_IL_0108:;
		}
		finally
		{
			AudioManager.Instance?.AudioStopAll();
			if (GodotObject.IsInstanceValid(tallPogo))
			{
				tallPogo.QueueFree();
			}
			if (GodotObject.IsInstanceValid(shortPogo))
			{
				shortPogo.QueueFree();
			}
			if (GodotObject.IsInstanceValid(tallnut))
			{
				tallnut.QueueFree();
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
			Engine.PhysicsTicksPerSecond = previousPhysicsTicks;
			Engine.MaxFps = previousMaxFps;
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitFrames(8);
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			pogoScene?.Dispose();
			tallnutScene?.Dispose();
			wallnutScene?.Dispose();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag = _failures == 0 && _checks == 20;
		GD.Print($"POGO_TALLNUT_LOW_FPS_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static void PrepareCharacter(TowerDefenseCharacter character, Vector2I gridPos, Vector2 position)
	{
		character.editorPreviewMode = false;
		character.inGame = true;
		character.gridPos = gridPos;
		character.GlobalPosition = position;
	}

	private static Vector2 GetFixtureCellCenter(TowerDefenseManager manager, Vector2I gridPos)
	{
		return manager.gridBeginPos + new Vector2(((float)gridPos.X - 0.5f) * manager.gridSize.X, ((float)gridPos.Y - 0.5f) * manager.gridSize.Y);
	}

	private static void ArmLowFpsJump(TowerDefenseZombiePogo pogo, AttackComponent jumpAttack, TowerDefensePlant target)
	{
		jumpAttack.target = target;
		jumpAttack.alive = true;
		jumpAttack.timer = 0.0;
		jumpAttack.checkIntrevalNow = 0;
		pogo.sprite.SetAnimation("Pogo");
		pogo.pogoPlant = true;
		pogo.isJump = false;
		pogo.jumpWait = 0;
		pogo.jumpToPos = 370.0;
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
			GD.PushError("[BugOverviewPogoTallnutLowFpsRuntimeTest] " + message);
		}
	}
}
