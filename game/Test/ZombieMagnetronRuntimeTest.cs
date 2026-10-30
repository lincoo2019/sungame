using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class ZombieMagnetronRuntimeTest : Node
{
	private const int ExpectedChecks = 59;

	private const string MagnetronScenePath = "res://Asset/Anime/Character/Zombie/Chapter9/Magnetron/Scene/TowerDefenseZombieMagnetron.tscn";

	private const string MagnetronPacketPath = "res://Asset/Anime/Character/Zombie/Chapter9/Magnetron/Packet/ZombieMagnetron.tres";

	private const string CaltropScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Caltrop/Scene/TowerDefensePlantCaltrop.tscn";

	private const string CaltropPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/Caltrop/Packet/PlantCaltrop.tres";

	private const string WallnutScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn";

	private const string WallnutPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres";

	private const string ShieldScenePath = "res://Asset/Anime/Character/Item/Sheild/Scene/TowerDefenseItemSheild.tscn";

	private const string ShieldPacketPath = "res://Asset/Anime/Character/Item/Sheild/Packet/ItemSheild.tres";

	private const int TerminalColumn = 8;

	private static readonly Vector2 ExpectedPullOriginLocalPosition = new Vector2(-48f, -20f);

	private int _checks;

	private int _failures;

	private ZombieMagnetronRuntimeControlStub _control;

	private TowerDefenseManager _manager;

	public override async void _Ready()
	{
		_manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = _manager?.currentControl;
		Vector2 previousGridBegin = _manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = _manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = _manager?.gridNum ?? Vector2I.Zero;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 9;
			try
			{
				Check(GodotObject.IsInstanceValid(_manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(_manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_00e6;
				}
				await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
				ResourceManager.Instance.RequireFullGameplayResourcesReady("ZombieMagnetronRuntimeTest");
				_control = new ZombieMagnetronRuntimeControlStub
				{
					Name = "ZombieMagnetronRuntimeControl",
					isGameRunning = false,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig()
				};
				AddChild(_control, forceReadableName: false, InternalMode.Disabled);
				_control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				_control.AddChild(_control.characterNode, forceReadableName: false, InternalMode.Disabled);
				_manager.currentControl = _control;
				_manager.gridBeginPos = Vector2.Zero;
				_manager.gridSize = new Vector2(100f, 76f);
				_manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, _manager.gridNum);
				mapFeature.control = _control;
				_control.featureDictionary[new StringName("Map")] = mapFeature;
				Check(TowerDefenseManager.HasGameplayAuthority, "The focused single-player fixture must own gameplay authority.");
				await RunPullEffectPresentationCase();
				await WaitFrames(3);
				_control.isGameRunning = false;
				await RunSpikeBatchCase();
				await RunShieldCase();
				await RunShieldBatchCase();
				await RunOrdinaryCrushControlCase();
				await RunUnscheduledTerminalShieldCase();
				await RunGlobalSpikePriorityCase();
				await RunOrdinaryDeathCleanupCase();
				goto end_IL_00b2;
				end_IL_00e6:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[ZombieMagnetronRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00b2;
			}
			return;
			end_IL_00b2:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			if (GodotObject.IsInstanceValid(_manager))
			{
				_manager.currentControl = previousControl;
				_manager.gridBeginPos = previousGridBegin;
				_manager.gridSize = previousGridSize;
				_manager.gridNum = previousGridNum;
			}
			mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(mapControl))
			{
				mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(_control) && !_control.IsQueuedForDeletion())
			{
				_control.QueueFree();
			}
			await WaitFrames(6);
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag = _failures == 0 && _checks == 59;
		GD.Print($"ZOMBIE_MAGNETRON_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task RunPullEffectPresentationCase()
	{
		TowerDefenseZombieMagnetron zombie = CreateCharacter<TowerDefenseZombieMagnetron>("res://Asset/Anime/Character/Zombie/Chapter9/Magnetron/Scene/TowerDefenseZombieMagnetron.tscn", "res://Asset/Anime/Character/Zombie/Chapter9/Magnetron/Packet/ZombieMagnetron.tres", new Vector2I(8, 5));
		TowerDefensePlant wallnut = CreateCharacter<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres", new Vector2I(6, 5));
		RegisterInCell(wallnut);
		await WaitFrames(5);
		Check(IsReady(zombie) && IsReady(wallnut) && GodotObject.IsInstanceValid(zombie.sprite), "The pull presentation fixture must initialize its production characters and sprite.");
		_control.isGameRunning = true;
		PlaceFrontAtColumn(zombie, 8, 5);
		zombie.sprite.ProcessMode = ProcessModeEnum.Always;
		zombie.WalkProcessing(10.0);
		Check(zombie.anchored && zombie.shooting && zombie.sprite.clip == "anim_shooting", "The pull presentation fixture must enter the authored shooting clip.");
		MagnetronPullEffect effect = await WaitForPullEffect(120);
		Check(GodotObject.IsInstanceValid(effect) && !zombie.shooting && zombie.pullRemaining > 0.0, "The authored shooting event must create the production pull effect.");
		zombie.sprite.ProcessMode = ProcessModeEnum.Disabled;
		await WaitFrames(1);
		Marker2D nodeOrNull = zombie.GetNodeOrNull<Marker2D>("SpriteGroup/TransformPoint/ZombieMagnetron/PullSlot/PullOrigin");
		Check(GodotObject.IsInstanceValid(nodeOrNull) && nodeOrNull.Position.IsEqualApprox(ExpectedPullOriginLocalPosition), "The pull origin marker must use the visually aligned local offset.");
		Check(GodotObject.IsInstanceValid(effect) && !effect.ZAsRelative && effect.ZIndex == 3000, "The complete pull effect must use the battlefield's absolute top presentation layer.");
		Sprite2D sprite2D = effect?.GetNodeOrNull<Sprite2D>("Line");
		Node2D node2D = effect?.GetNodeOrNull<Node2D>("PullL");
		Node2D node2D2 = effect?.GetNodeOrNull<Node2D>("PullR");
		Check(GodotObject.IsInstanceValid(sprite2D) && sprite2D.ZIndex == -1, "The beam must retain its internal ordering immediately below both endpoint caps.");
		Vector2 pullOriginPosition = zombie.GetPullOriginPosition();
		Vector2 logicalGlobalPosition = wallnut.GetLogicalGlobalPosition();
		Vector2 vector = logicalGlobalPosition - pullOriginPosition;
		Check(GodotObject.IsInstanceValid(node2D2) && node2D2.GlobalPosition.IsEqualApprox(pullOriginPosition), "The source cap must follow the aligned cannon-mouth origin.");
		Check(GodotObject.IsInstanceValid(node2D) && node2D.GlobalPosition.IsEqualApprox(logicalGlobalPosition), "The target cap must remain on the pulled plant's logical position.");
		Check(GodotObject.IsInstanceValid(sprite2D) && sprite2D.GlobalPosition.IsEqualApprox((pullOriginPosition + logicalGlobalPosition) * 0.5f), "The pull beam must remain centered between its endpoints.");
		Check(GodotObject.IsInstanceValid(sprite2D) && Mathf.IsZeroApprox(Mathf.AngleDifference(sprite2D.GlobalRotation, vector.Angle())), "The pull beam must remain aimed from the cannon mouth to the target.");
		Check(GodotObject.IsInstanceValid(sprite2D) && Mathf.IsEqualApprox(sprite2D.Scale.X, Math.Max(0.01f, vector.Length() / 162f)) && Mathf.IsEqualApprox(sprite2D.Scale.Y, 1f), "The pull beam must retain its distance-derived scale.");
		TowerDefenseManager.GetMapCell(wallnut.gridPos)?.characterList.Remove(wallnut);
		zombie.QueueFree();
		wallnut.QueueFree();
		await WaitFrames(3);
		Check(!GodotObject.IsInstanceValid(effect), "The pull effect must clean itself up after its source leaves the battle.");
	}

	private async Task RunSpikeBatchCase()
	{
		TowerDefenseZombieMagnetron zombie = CreateCharacter<TowerDefenseZombieMagnetron>("res://Asset/Anime/Character/Zombie/Chapter9/Magnetron/Scene/TowerDefenseZombieMagnetron.tscn", "res://Asset/Anime/Character/Zombie/Chapter9/Magnetron/Packet/ZombieMagnetron.tres", new Vector2I(8, 1));
		TowerDefensePlantCaltrop caltrop = CreateCharacter<TowerDefensePlantCaltrop>("res://Asset/Anime/Character/Plant/Chapter0/Caltrop/Scene/TowerDefensePlantCaltrop.tscn", "res://Asset/Anime/Character/Plant/Chapter0/Caltrop/Packet/PlantCaltrop.tres", new Vector2I(7, 1));
		TowerDefensePlant wallnut = CreateCharacter<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres", new Vector2I(6, 1));
		RegisterInCell(caltrop);
		RegisterInCell(wallnut);
		await WaitFrames(5);
		Check(IsReady(zombie) && IsReady(caltrop) && IsReady(wallnut), "The production Magnetron, Caltrop, and Wallnut scenes must initialize.");
		if (IsReady(zombie) && IsReady(caltrop) && IsReady(wallnut))
		{
			caltrop.instance.hitpoints = 900.0;
			double spikeHurt = caltrop.instance.spikeHurt;
			Check(Mathf.IsEqualApprox((float)spikeHurt, 300f), "The real Caltrop must expose its 300-point tire damage contract.");
			BeginPull(zombie, 1);
			Check(caltrop.gridPos == new Vector2I(8, 1) && wallnut.gridPos == new Vector2I(7, 1), "One pull must queue the Caltrop at the terminal and move its batch companion.");
			zombie.WalkProcessing(2.01);
			await WaitFrames(3);
			Check(zombie.die || zombie.nearDie, "A terminal Caltrop must burst the Magnetron before the completed crush runs.");
			Check(!caltrop.isDestroy && !caltrop.die && Mathf.IsEqualApprox((float)caltrop.instance.hitpoints, (float)(900.0 - spikeHurt)), "The Caltrop must take only spikeHurt and must not be smash-destroyed.");
			Check(!wallnut.isDestroy && !wallnut.die, "Every other plant in the interrupted pull batch must survive the tire burst.");
			Check(GetStateArrayCount(zombie.ExportVariantSave(), "pendingCrushes") == 0 && GetStateArrayCount(zombie.ExportNetworkSpecialState(), "moveDestinations") == 0, "A tire burst must clear pending crush and active pull state.");
			await WaitPhysicsSeconds(2.2);
			Check(!caltrop.isDestroy && !wallnut.isDestroy && caltrop.GetLogicalGlobalPosition().IsEqualApprox(TowerDefenseManager.GetMapCellPlantPos(new Vector2I(8, 1))) && wallnut.GetLogicalGlobalPosition().IsEqualApprox(TowerDefenseManager.GetMapCellPlantPos(new Vector2I(7, 1))), "Survivors must finish their already-issued pull tweens without a delayed crush.");
		}
	}

	private async Task RunShieldCase()
	{
		TowerDefenseZombieMagnetron zombie = CreateCharacter<TowerDefenseZombieMagnetron>("res://Asset/Anime/Character/Zombie/Chapter9/Magnetron/Scene/TowerDefenseZombieMagnetron.tscn", "res://Asset/Anime/Character/Zombie/Chapter9/Magnetron/Packet/ZombieMagnetron.tres", new Vector2I(8, 2));
		TowerDefensePlant protectedPlant = CreateCharacter<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres", new Vector2I(7, 2));
		TowerDefenseItemSheild shield = CreateCharacter<TowerDefenseItemSheild>("res://Asset/Anime/Character/Item/Sheild/Scene/TowerDefenseItemSheild.tscn", "res://Asset/Anime/Character/Item/Sheild/Packet/ItemSheild.tres", new Vector2I(8, 2));
		RegisterInCell(protectedPlant);
		RegisterInCell(shield);
		await WaitFrames(5);
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(new Vector2I(8, 2));
		Check(IsReady(zombie) && IsReady(protectedPlant) && IsReady(shield) && mapCell?.itemShield == shield, "The production shield must be registered on the terminal cell.");
		if (IsReady(zombie) && IsReady(protectedPlant) && IsReady(shield) && mapCell?.itemShield == shield)
		{
			double shieldHitpoints = shield.instance.hitpoints;
			protectedPlant.instance.hitpoints = 20000.0;
			BeginPull(zombie, 2);
			Check(protectedPlant.gridPos == new Vector2I(8, 2), "The shield fixture plant must enter the terminal pending-crush cell.");
			zombie.WalkProcessing(2.01);
			await WaitFrames(3);
			Check(zombie.die || zombie.nearDie, "A lethal shield block must deflate the Magnetron vehicle.");
			Check(!protectedPlant.isDestroy && !protectedPlant.die, "The plant protected by a real ItemSheild must survive the lethal crush.");
			Check(shield.instance.hitpoints < shieldHitpoints, "Blocking the Magnetron must consume one real shield layer.");
			Check(GetStateArrayCount(zombie.ExportVariantSave(), "pendingCrushes") == 0, "Shield deflation must cancel the batch pending-crush state.");
			await WaitPhysicsSeconds(2.2);
			Check(!protectedPlant.isDestroy && protectedPlant.GetLogicalGlobalPosition().IsEqualApprox(TowerDefenseManager.GetMapCellPlantPos(new Vector2I(8, 2))), "A shield survivor must finish the existing pull tween without a delayed crush.");
		}
	}

	private async Task RunShieldBatchCase()
	{
		TowerDefenseZombieMagnetron zombie = CreateCharacter<TowerDefenseZombieMagnetron>("res://Asset/Anime/Character/Zombie/Chapter9/Magnetron/Scene/TowerDefenseZombieMagnetron.tscn", "res://Asset/Anime/Character/Zombie/Chapter9/Magnetron/Packet/ZombieMagnetron.tres", new Vector2I(5, 2));
		TowerDefensePlant shieldedPlant = CreateCharacter<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres", new Vector2I(5, 2));
		TowerDefenseItemSheild shield = CreateCharacter<TowerDefenseItemSheild>("res://Asset/Anime/Character/Item/Sheild/Scene/TowerDefenseItemSheild.tscn", "res://Asset/Anime/Character/Item/Sheild/Packet/ItemSheild.tres", new Vector2I(5, 2));
		TowerDefensePlant companion = CreateCharacter<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres", new Vector2I(4, 2));
		RegisterInCell(shieldedPlant);
		RegisterInCell(shield);
		RegisterInCell(companion);
		await WaitFrames(5);
		Check(IsReady(zombie) && IsReady(shieldedPlant) && IsReady(shield) && IsReady(companion), "The shield-only multi-pending fixture must initialize.");
		if (IsReady(zombie) && IsReady(shieldedPlant) && IsReady(shield) && IsReady(companion))
		{
			shieldedPlant.instance.hitpoints = 20000.0;
			double shieldHitpoints = shield.instance.hitpoints;
			zombie.ImportVariantSave(new Dictionary
			{
				["pendingCrushes"] = new Array<Dictionary>
				{
					CreatePendingCrushState(shieldedPlant, new Vector2I(5, 2)),
					CreatePendingCrushState(companion, new Vector2I(4, 2))
				},
				["stopColumn"] = 5
			});
			zombie.WalkProcessing(0.0);
			await WaitFrames(3);
			Check(zombie.die || zombie.nearDie, "A lethal shield block must deflate the multi-pending Magnetron batch.");
			Check(!shieldedPlant.isDestroy && !companion.isDestroy, "Shield deflation must preserve every plant in the pending batch.");
			Check(shield.instance.hitpoints < shieldHitpoints, "The shield-only batch must consume exactly one available layer.");
			Check(GetStateArrayCount(zombie.ExportVariantSave(), "pendingCrushes") == 0, "Shield-only deflation must clear the entire pending batch.");
		}
	}

	private async Task RunOrdinaryCrushControlCase()
	{
		TowerDefenseZombieMagnetron zombie = CreateCharacter<TowerDefenseZombieMagnetron>("res://Asset/Anime/Character/Zombie/Chapter9/Magnetron/Scene/TowerDefenseZombieMagnetron.tscn", "res://Asset/Anime/Character/Zombie/Chapter9/Magnetron/Packet/ZombieMagnetron.tres", new Vector2I(8, 3));
		TowerDefensePlant wallnut = CreateCharacter<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres", new Vector2I(7, 3));
		RegisterInCell(wallnut);
		await WaitFrames(5);
		Check(IsReady(zombie) && IsReady(wallnut), "The ordinary crush control scenes must initialize.");
		if (IsReady(zombie) && IsReady(wallnut))
		{
			BeginPull(zombie, 3);
			zombie.WalkProcessing(2.01);
			await WaitFrames(3);
			Check(wallnut.isDestroy || wallnut.die, "A terminal plant without SPIKE or shield protection must still be crushed.");
			Check(!zombie.die && !zombie.nearDie, "An ordinary completed crush must not deflate the Magnetron.");
		}
	}

	private async Task RunUnscheduledTerminalShieldCase()
	{
		TowerDefenseZombieMagnetron zombie = CreateCharacter<TowerDefenseZombieMagnetron>("res://Asset/Anime/Character/Zombie/Chapter9/Magnetron/Scene/TowerDefenseZombieMagnetron.tscn", "res://Asset/Anime/Character/Zombie/Chapter9/Magnetron/Packet/ZombieMagnetron.tres", new Vector2I(8, 4));
		TowerDefensePlant protectedPlant = CreateCharacter<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres", new Vector2I(8, 4));
		TowerDefensePlant followupTarget = CreateCharacter<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres", new Vector2I(7, 4));
		TowerDefenseItemSheild shield = CreateCharacter<TowerDefenseItemSheild>("res://Asset/Anime/Character/Item/Sheild/Scene/TowerDefenseItemSheild.tscn", "res://Asset/Anime/Character/Item/Sheild/Packet/ItemSheild.tres", new Vector2I(8, 4));
		RegisterInCell(protectedPlant);
		RegisterInCell(followupTarget);
		RegisterInCell(shield);
		await WaitFrames(5);
		Check(IsReady(zombie) && IsReady(protectedPlant) && IsReady(followupTarget) && IsReady(shield), "The unscheduled terminal shield fixture must initialize.");
		if (IsReady(zombie) && IsReady(protectedPlant) && IsReady(followupTarget) && IsReady(shield))
		{
			_control.isGameRunning = true;
			PlaceFrontAtColumn(zombie, 8, 4);
			zombie.anchored = true;
			zombie.stopColumn = 8;
			zombie.WalkProcessing(10.0);
			await WaitFrames(3);
			Check(zombie.die || zombie.nearDie, "An unscheduled shield-protected terminal plant must deflate the Magnetron.");
			Check(!protectedPlant.isDestroy && !followupTarget.isDestroy, "Terminal shield deflation must leave both the protected plant and later target alive.");
			Check(!zombie.shooting && zombie.targetGrid == new Vector2I(-1, -1), "ProcessAnchored must stop immediately after unscheduled terminal deflation.");
		}
	}

	private async Task RunGlobalSpikePriorityCase()
	{
		TowerDefenseZombieMagnetron zombie = CreateCharacter<TowerDefenseZombieMagnetron>("res://Asset/Anime/Character/Zombie/Chapter9/Magnetron/Scene/TowerDefenseZombieMagnetron.tscn", "res://Asset/Anime/Character/Zombie/Chapter9/Magnetron/Packet/ZombieMagnetron.tres", new Vector2I(8, 5));
		TowerDefensePlant shieldedPlant = CreateCharacter<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres", new Vector2I(8, 5));
		TowerDefenseItemSheild shield = CreateCharacter<TowerDefenseItemSheild>("res://Asset/Anime/Character/Item/Sheild/Scene/TowerDefenseItemSheild.tscn", "res://Asset/Anime/Character/Item/Sheild/Packet/ItemSheild.tres", new Vector2I(8, 5));
		TowerDefensePlantCaltrop caltrop = CreateCharacter<TowerDefensePlantCaltrop>("res://Asset/Anime/Character/Plant/Chapter0/Caltrop/Scene/TowerDefensePlantCaltrop.tscn", "res://Asset/Anime/Character/Plant/Chapter0/Caltrop/Packet/PlantCaltrop.tres", new Vector2I(7, 5));
		RegisterInCell(shieldedPlant);
		RegisterInCell(shield);
		RegisterInCell(caltrop);
		await WaitFrames(5);
		Check(IsReady(zombie) && IsReady(shieldedPlant) && IsReady(shield) && IsReady(caltrop), "The cross-cell SPIKE-priority fixture must initialize.");
		if (IsReady(zombie) && IsReady(shieldedPlant) && IsReady(shield) && IsReady(caltrop))
		{
			caltrop.instance.hitpoints = 900.0;
			double shieldHitpoints = shield.instance.hitpoints;
			Array<Dictionary> array = new Array<Dictionary>
			{
				CreatePendingCrushState(shieldedPlant, new Vector2I(8, 5)),
				CreatePendingCrushState(caltrop, new Vector2I(7, 5))
			};
			zombie.ImportVariantSave(new Dictionary
			{
				["pendingCrushes"] = array,
				["stopColumn"] = 8
			});
			zombie.WalkProcessing(0.0);
			await WaitFrames(3);
			Check(zombie.die || zombie.nearDie, "A later SPIKE candidate must deflate the Magnetron before any shield check.");
			Check(!caltrop.isDestroy && Mathf.IsEqualApprox((float)caltrop.instance.hitpoints, 600f), "The global SPIKE candidate must receive spikeHurt instead of a smash.");
			Check(!shieldedPlant.isDestroy && Mathf.IsEqualApprox((float)shield.instance.hitpoints, (float)shieldHitpoints), "SPIKE priority must preserve the earlier shield candidate and leave its layer unused.");
			Check(GetStateArrayCount(zombie.ExportVariantSave(), "pendingCrushes") == 0, "Global SPIKE priority must cancel the entire restored pending batch.");
		}
	}

	private async Task RunOrdinaryDeathCleanupCase()
	{
		TowerDefenseZombieMagnetron zombie = CreateCharacter<TowerDefenseZombieMagnetron>("res://Asset/Anime/Character/Zombie/Chapter9/Magnetron/Scene/TowerDefenseZombieMagnetron.tscn", "res://Asset/Anime/Character/Zombie/Chapter9/Magnetron/Packet/ZombieMagnetron.tres", new Vector2I(5, 1));
		TowerDefensePlant wallnut = CreateCharacter<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres", new Vector2I(4, 1));
		RegisterInCell(wallnut);
		await WaitFrames(5);
		Check(IsReady(zombie) && IsReady(wallnut), "The ordinary death-cleanup fixture must initialize.");
		if (IsReady(zombie) && IsReady(wallnut))
		{
			BeginPull(zombie, 1, 5);
			Check(wallnut.gridPos == new Vector2I(5, 1) && !wallnut.isDestroy, "The ordinary death fixture must begin with a live pending crush.");
			zombie.Die();
			await WaitFrames(1);
			Check(zombie.die || zombie.nearDie, "The production Die entry must transition the ordinary cleanup fixture.");
			zombie.WalkProcessing(0.0);
			await WaitFrames(3);
			Check(wallnut.isDestroy || wallnut.die, "Non-deflation death must retain the existing FinishPendingCrushes contract.");
		}
	}

	private void BeginPull(TowerDefenseZombieMagnetron zombie, int row, int terminalColumn = 8)
	{
		_control.isGameRunning = true;
		PlaceFrontAtColumn(zombie, terminalColumn, row);
		zombie.WalkProcessing(10.0);
		Check(zombie.anchored && zombie.shooting, $"The row-{row} Magnetron must anchor and begin its production shooting state.");
		zombie.AnimeEvent("pull", "");
		Check(!zombie.shooting && Mathf.IsEqualApprox((float)zombie.pullRemaining, 2f), $"The row-{row} authored pull event must start the two-second pull.");
	}

	private static Dictionary CreatePendingCrushState(TowerDefensePlant plant, Vector2I grid)
	{
		return new Dictionary
		{
			["syncId"] = plant.syncId,
			["gridX"] = grid.X,
			["gridY"] = grid.Y,
			["nodeName"] = plant.Name.ToString().ValidateNodeName(),
			["saveKey"] = plant.packet?.saveKey ?? "",
			["remaining"] = 0.0
		};
	}

	private T CreateCharacter<T>(string scenePath, string packetPath, Vector2I grid) where T : TowerDefenseCharacter
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(scenePath, null, ResourceLoader.CacheMode.IgnoreDeep);
		T val = ((packedScene != null) ? packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled) : null);
		if (!GodotObject.IsInstanceValid(val))
		{
			return null;
		}
		val.packet = ResourceLoader.Load<TowerDefensePacketConfig>(packetPath, null, ResourceLoader.CacheMode.Ignore);
		val.inGame = !(val is TowerDefenseZombieMagnetron);
		val.editorPreviewMode = false;
		val.gridPos = grid;
		val.cell = ((val is TowerDefenseZombie) ? null : TowerDefenseManager.GetMapCell(grid));
		val.Position = _manager.GetMapCellPosCenter(grid);
		if (val is TowerDefenseZombieMagnetron towerDefenseZombieMagnetron)
		{
			towerDefenseZombieMagnetron.ProcessMode = ProcessModeEnum.Disabled;
		}
		_control.characterNode.AddChild(val, forceReadableName: false, InternalMode.Disabled);
		return val;
	}

	private static void RegisterInCell(TowerDefenseCharacter character)
	{
		TowerDefenseManager.GetMapCell(character.gridPos)?.CharacterPlant(character.packet, character, noLimit: true);
	}

	private static bool IsReady(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character) && GodotObject.IsInstanceValid(character.instance))
		{
			return GodotObject.IsInstanceValid(character.config);
		}
		return false;
	}

	private static int GetStateArrayCount(Dictionary state, string key)
	{
		return state.GetValueOrDefault(key, new Godot.Collections.Array()).AsGodotArray().Count;
	}

	private static void PlaceFrontAtColumn(TowerDefenseZombieMagnetron zombie, int column, int row)
	{
		Marker2D nodeOrNull = zombie.GetNodeOrNull<Marker2D>("FrontAnchor");
		float num = zombie.GetLogicalGlobalPosition().X + (nodeOrNull?.Position.X ?? 0f) * zombie.Scale.X;
		Vector2 logicalGlobalPosition = zombie.GetLogicalGlobalPosition();
		logicalGlobalPosition.X += TowerDefenseManager.GetMapCellPlantPos(new Vector2I(column, row)).X - num;
		zombie.SetLogicalGlobalPosition(logicalGlobalPosition);
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig
			{
				gridNum = gridNum
			}
		});
		towerDefenseBattleFeatureMap.PlantGridInit();
		for (int i = 1; i <= gridNum.X; i++)
		{
			for (int j = 1; j <= gridNum.Y; j++)
			{
				towerDefenseBattleFeatureMap.GetPlantGridCell(new Vector2I(i, j)).Init(new TowerDefenseCellConfig());
			}
		}
		return towerDefenseBattleFeatureMap;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task WaitPhysicsSeconds(double duration)
	{
		for (double elapsed = 0.0; elapsed < duration; elapsed += GetPhysicsProcessDeltaTime())
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task<MagnetronPullEffect> WaitForPullEffect(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			foreach (Node child in _control.characterNode.GetChildren())
			{
				if (child is MagnetronPullEffect result)
				{
					return result;
				}
			}
			await WaitFrames(1);
		}
		return null;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[ZombieMagnetronRuntimeTest] " + message);
		}
	}
}
