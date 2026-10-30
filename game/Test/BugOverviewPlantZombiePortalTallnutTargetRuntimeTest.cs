using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewPlantZombiePortalTallnutTargetRuntimeTest : Node
{
	private const string PlantZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/PeaShooterSingle/TowerDefenseZombieNormalPeaShooterSingle.tscn";

	private const string PortalTallnutScenePath = "res://Asset/Anime/Character/Plant/Star/PorTallnut/Scene/TowerDefensePlantPorTallnut.tscn";

	private static readonly Vector2I PortalGrid = new Vector2I(3, 3);

	private static readonly Vector2I PartnerGrid = new Vector2I(2, 4);

	private static readonly Vector2I ZombieGrid = new Vector2I(7, 3);

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		PlantZombiePortalTallnutTargetRuntimeControlStub control = null;
		TowerDefensePlantPorTallnut portal = null;
		TowerDefensePlantPorTallnut partner = null;
		try
		{
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					goto end_IL_00c6;
				}
				control = new PlantZombiePortalTallnutTargetRuntimeControlStub
				{
					Name = "PlantZombiePortalTallnutTargetRuntimeControl",
					isGameRunning = true,
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
				manager.gridBeginPos = Vector2.Zero;
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				TowerDefenseZombieNormalPeaShooterSingle plantZombie = Instantiate<TowerDefenseZombieNormalPeaShooterSingle>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/PeaShooterSingle/TowerDefenseZombieNormalPeaShooterSingle.tscn");
				portal = Instantiate<TowerDefensePlantPorTallnut>("res://Asset/Anime/Character/Plant/Star/PorTallnut/Scene/TowerDefensePlantPorTallnut.tscn");
				partner = Instantiate<TowerDefensePlantPorTallnut>("res://Asset/Anime/Character/Plant/Star/PorTallnut/Scene/TowerDefensePlantPorTallnut.tscn");
				Check(GodotObject.IsInstanceValid(plantZombie) && plantZombie.config?.name == "ZombieNormalPeaShooterSingle", "The fixture must instantiate the real Peashooter plant-zombie scene.");
				Check(GodotObject.IsInstanceValid(portal) && GodotObject.IsInstanceValid(partner) && portal.config?.name == "PlantPorTallnut" && partner.config?.name == "PlantPorTallnut", "The fixture must instantiate two real Portal Tall Nut scenes.");
				if (!GodotObject.IsInstanceValid(plantZombie) || !GodotObject.IsInstanceValid(portal) || !GodotObject.IsInstanceValid(partner))
				{
					goto end_IL_00c6;
				}
				control.characterNode.AddChild(portal, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(partner, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(plantZombie, forceReadableName: false, InternalMode.Disabled);
				Place(portal, PortalGrid);
				Place(partner, PartnerGrid);
				Place(plantZombie, ZombieGrid);
				await WaitFrames(5);
				portal.ProcessMode = ProcessModeEnum.Disabled;
				partner.ProcessMode = ProcessModeEnum.Disabled;
				plantZombie.ProcessMode = ProcessModeEnum.Disabled;
				Check(GetTree().GetNodeCountInGroup("PorTallnut") == 2, "The real portal pair must be discoverable by the production portal group.");
				portal.OpenIdleEntered();
				partner.OpenIdleEntered();
				portal.UpdateRect();
				partner.UpdateRect();
				Check(portal.open && portal.WorldRect.HasArea(), "The reported Portal Tall Nut must be fully open with an active transport zone.");
				Check(portal.instance.canBeCollection, "A fully open Portal Tall Nut must remain targetable instead of reusing collection eligibility as portal state.");
				FireComponent fireComponent = plantZombie.componentManager?.GetRuntime<FireComponent>("character.fire");
				Check(fireComponent != null && !fireComponent.IsReleased, "The real Peashooter plant-zombie FireComponent must be active.");
				if (fireComponent == null || fireComponent.IsReleased)
				{
					goto end_IL_00c6;
				}
				Check(plantZombie.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE && portal.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT && plantZombie.CanTarget(portal), "The real plant-zombie and Portal Tall Nut must remain opposing combatants.");
				Check((plantZombie.instance.collisionFlags & portal.instance.maskFlags) != 0 && (portal.targetRegistrationComponent?.canProjectileCheck ?? false) && portal.HasHitBox, "The real Portal Tall Nut must retain compatible collision, projectile-check, and hit-box data.");
				Check(AabbShapeUtil.SegmentIntersectsRect(plantZombie.GlobalPosition, plantZombie.GlobalPosition + new Vector2(-2000f, 0f), portal.WorldHitRect, out var _), "The plant-zombie's authored backward fire ray must intersect the only same-row plant target.");
				int num = 0;
				foreach (TowerDefenseCharacter characterLine in manager.GetCharacterLineList(ZombieGrid.Y, fliterGraveStone: false))
				{
					if (GodotObject.IsInstanceValid(characterLine) && characterLine.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT)
					{
						num++;
					}
				}
				Check(num == 1, $"Portal Tall Nut must be the only plant ahead on the reported row; count={num}.");
				Check(plantZombie.GlobalPosition.X > portal.GlobalPosition.X && Mathf.IsEqualApprox(plantZombie.GlobalPosition.Y, portal.GlobalPosition.Y), $"The real plant-zombie must be positioned behind the only same-row portal target; zombie={plantZombie.GlobalPosition}, portal={portal.GlobalPosition}.");
				Check(manager.characterRegistry.HasAttackGridCharacters(PortalGrid.Y, PortalGrid.X, PortalGrid.X, includeAllLineCheck: false, plantZombie.camp), "The attack-grid index must expose the Portal Tall Nut to zombie attackers.");
				BugOverviewPlantZombiePortalTallnutTargetRuntimeTest bugOverviewPlantZombiePortalTallnutTargetRuntimeTest = this;
				Array<AabbRay2DResource> checkRayResources = fireComponent.checkRayResources;
				bugOverviewPlantZombiePortalTallnutTargetRuntimeTest.Check(checkRayResources != null && checkRayResources.Count == 1, "The real Peashooter plant-zombie must retain its authored backward fire ray.");
				goto end_IL_00bd;
				end_IL_00c6:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[PlantZombiePortalTallnutTarget] Unexpected exception: {value}");
				goto end_IL_00bd;
			}
			return;
			end_IL_00bd:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(portal))
			{
				portal.OpenIdleExited();
			}
			if (GodotObject.IsInstanceValid(partner))
			{
				partner.OpenIdleExited();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 14;
		GD.Print($"PLANT_ZOMBIE_PORTAL_TALLNUT_TARGET_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static T Instantiate<T>(string path) where T : Node
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		if (packedScene == null)
		{
			return null;
		}
		return packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled);
	}

	private static void Place(TowerDefenseCharacter character, Vector2I grid)
	{
		character.inGame = true;
		character.editorPreviewMode = false;
		character.gridPos = grid;
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		character.GlobalPosition = instance.gridBeginPos + new Vector2((float)grid.X * instance.gridSize.X, (float)grid.Y * instance.gridSize.Y);
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
			GD.PushError("[PlantZombiePortalTallnutTarget] " + message);
		}
	}
}
