using System;
using System.Reflection;
using Godot;

public partial class BugOverviewFirePeaPortalExitRuntimeTest : Node
{
	private static readonly Vector2I DestinationGrid = new Vector2I(6, 4);

	private static readonly Vector2 DestinationPosition = new Vector2(600f, 304f);

	private int _checks;

	private int _failures;

	private CharacterAabbAreaComponent _captureArea;

	private ComponentManager _captureAreaManager;

	public override async void _Ready()
	{
		BulletField bulletField = null;
		FirePeaPortalExitZombiePortalStub portal = null;
		try
		{
			try
			{
				TowerDefenseProjectileRegistry.Init();
				bulletField = new BulletField
				{
					Name = "FirePeaPortalExitBulletField"
				};
				AddChild(bulletField, forceReadableName: false, InternalMode.Disabled);
				Check(BulletField.Instance == bulletField, "The runtime fixture must mount the live BulletField instance.");
				TowerDefenseProjectileConfig firePeaConfig = new TowerDefenseProjectileCreateData(new StringName("FirePea")).BuildConfig();
				Check(GodotObject.IsInstanceValid(firePeaConfig), "The canonical FirePea config must build from the live projectile registry.");
				if (!GodotObject.IsInstanceValid(firePeaConfig))
				{
					goto end_IL_003e;
				}
				Check(firePeaConfig.NameSN == new StringName("FirePea"), $"The regression must use FirePea, not a stand-in; got {firePeaConfig.NameSN}.");
				Check((firePeaConfig.damageFlags & 4) != 0, "The canonical projectile must retain its fire damage identity.");
				Check((firePeaConfig.fireMethodFlags & 1) != 0 && firePeaConfig.behaviors.Count == 0, "FirePea must remain a standard SHOOTER eligible for the BulletField struct path.");
				portal = CreatePortal();
				AddChild(portal, forceReadableName: false, InternalMode.Disabled);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				EnableCaptureArea(portal);
				VerifyZombiePortalRelease(bulletField, portal, firePeaConfig);
				goto end_IL_0035;
				end_IL_003e:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[FirePeaPortalExit] Unexpected exception: {value}");
				goto end_IL_0035;
			}
			return;
			end_IL_0035:;
		}
		finally
		{
			_captureArea?.Release();
			_captureArea = null;
			_captureAreaManager = null;
			if (GodotObject.IsInstanceValid(portal))
			{
				portal.QueueFree();
			}
			if (GodotObject.IsInstanceValid(bulletField))
			{
				bulletField.QueueFree();
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool flag = _failures == 0 && _checks == 17;
		GD.Print($"FIRE_PEA_PORTAL_EXIT_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void VerifyZombiePortalRelease(BulletField bulletField, FirePeaPortalExitZombiePortalStub portal, TowerDefenseProjectileConfig firePeaConfig)
	{
		int num = portal.ReleaseStoredProjectile(firePeaConfig, portal);
		Check(num >= 0, "Portal should release the real FirePea into BulletField.");
		if (num >= 0)
		{
			ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(num);
			Check(bulletDataRef.config == firePeaConfig, "Portal release must reuse the stored FirePea config without duplicating it.");
			Check(bulletDataRef.fireMethodFlags == 1 && bulletDataRef.checkAll && bulletDataRef.gridY == DestinationGrid.Y && bulletDataRef.gridPos.Y == DestinationGrid.Y, "Portal release must apply SHOOTER, cross-row collision, and destination lane state.");
			Check(bulletDataRef.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT && bulletDataRef.vel.X < 0f && bulletDataRef.pos.IsEqualApprox(DestinationPosition + new Vector2(-50f, 20f)) && bulletDataRef.flipX, "The production helper must apply non-hypnotized camp, direction, destination position, and flip.");
			Check(bulletDataRef.portalReleased, "Released portal bullets must not be recaptured.");
			BulletRenderMode renderMode = bulletDataRef.renderMode;
			bool condition = ((renderMode == BulletRenderMode.STATIC || renderMode == BulletRenderMode.ANIMATED_MESH) ? true : false);
			Check(condition, "Released portal bullets must use a MultiMesh render mode.");
			Check(GetTree().GetNodesInGroup("Projectile").Count == 0, "Portal release must not create TowerDefenseProjectile Nodes.");
			int count = portal.projectileConfigList.Count;
			portal.OnBulletIntersect(ref bulletDataRef, num);
			Check(bulletField.IsBulletActive(num) && bulletDataRef.portalReleased && portal.projectileConfigList.Count == count, "The real zombie portal must ignore a released BulletData item without destroying it.");
			TowerDefenseProjectileConfig towerDefenseProjectileConfig = new TowerDefenseProjectileCreateData(new StringName("FirePea"))
			{
				fireMethodFlags = 5
			}.BuildConfig();
			int num2 = bulletField.ChangeBulletData(num, towerDefenseProjectileConfig, null);
			Check(num2 >= 0, "Portal-released FirePea must survive a replacement ChangeBulletData path.");
			if (num2 >= 0)
			{
				ref BulletData bulletDataRef2 = ref bulletField.GetBulletDataRef(num2);
				Check(bulletDataRef2.config == towerDefenseProjectileConfig && bulletDataRef2.fireMethodFlags == towerDefenseProjectileConfig.fireMethodFlags, "The regression must exercise the replacement config rather than an in-place no-op.");
				Check(bulletDataRef2.portalReleased, "Replacement ChangeBulletData must preserve the portal release guard.");
				count = portal.projectileConfigList.Count;
				portal.OnBulletIntersect(ref bulletDataRef2, num2);
				Check(bulletField.IsBulletActive(num2) && bulletDataRef2.portalReleased && portal.projectileConfigList.Count == count, "A replacement of a released bullet must remain immune to zombie-portal recapture.");
			}
		}
	}

	private static FirePeaPortalExitZombiePortalStub CreatePortal()
	{
		return new FirePeaPortalExitZombiePortalStub
		{
			Name = "FirePeaZombiePortal",
			Position = DestinationPosition,
			Scale = Vector2.One,
			gridPos = DestinationGrid,
			camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE,
			instance = new TowerDefenseCharacterInstance
			{
				hypnoses = false
			}
		};
	}

	private void EnableCaptureArea(FirePeaPortalExitZombiePortalStub portal)
	{
		_captureAreaManager = new ComponentManager();
		_captureArea = new CharacterAabbAreaComponent();
		_captureArea.Bind(_captureAreaManager, portal, new CharacterAabbAreaComponentDefinition());
		_captureArea.Activate();
		FieldInfo? field = typeof(TowerDefenseZombiePortal).GetField("_checkArea", BindingFlags.Instance | BindingFlags.NonPublic);
		if (field == null)
		{
			throw new MissingFieldException(typeof(TowerDefenseZombiePortal).FullName, "_checkArea");
		}
		field.SetValue(portal, _captureArea);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[FirePeaPortalExit] " + message);
		}
	}
}
