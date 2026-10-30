using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewSnowShroomTargetMagnetRuntimeTest : Node
{
	private const string SnowShroomScenePath = "res://Asset/Anime/Character/Plant/Chapter2/SnowShroom/Scene/TowerDefensePlantSnowShroom.tscn";

	private const string TargetMagnetScenePath = "res://Asset/Anime/Character/GraveStone/TargetMagnet/Scene/TowerDefenseGraveStoneTargetMagnet.tscn";

	private const string SwardProjectileScenePath = "res://Asset/Config/Projectile/Sword/Sprite/IceSword/ProjectileIceSword.tscn";

	private static readonly Vector2I EncounterGrid = new Vector2I(4, 2);

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		bool previousMultiplayerMode = Global.Instance?.isMultiplayerMode ?? false;
		bool previousIsHost = MultiPlayerManager.Instance?.isHost ?? false;
		SnowShroomTargetMagnetRuntimeControlStub control = null;
		TowerDefensePlantSnowShroom snowShroom = null;
		TowerDefenseGraveStoneTargetMagnet targetMagnet = null;
		BulletField bulletField = null;
		try
		{
			_ = 2;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					throw new InvalidOperationException("TowerDefenseManager autoload is unavailable.");
				}
				control = new SnowShroomTargetMagnetRuntimeControlStub
				{
					Name = "SnowShroomTargetMagnetRuntimeControl",
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
				manager.gridBeginPos = Vector2.Zero;
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				TowerDefenseProjectileRegistry.Init();
				bulletField = BulletField.EnsureMountedOnCharacterNode();
				Check(GodotObject.IsInstanceValid(bulletField) && BulletField.Instance == bulletField && bulletField.GetParent() == control.characterNode, "The scenario must use the real BulletField mounted on the battle character node.");
				if (!GodotObject.IsInstanceValid(bulletField))
				{
					throw new InvalidOperationException("BulletField could not be mounted.");
				}
				snowShroom = InstantiateCharacter<TowerDefensePlantSnowShroom>("res://Asset/Anime/Character/Plant/Chapter2/SnowShroom/Scene/TowerDefensePlantSnowShroom.tscn", control.characterNode, TowerDefenseEnum.CHARACTER_CAMP.PLANT, new Vector2(250f, 200f), new Array<string> { "Custom0" });
				targetMagnet = InstantiateCharacter<TowerDefenseGraveStoneTargetMagnet>("res://Asset/Anime/Character/GraveStone/TargetMagnet/Scene/TowerDefenseGraveStoneTargetMagnet.tscn", control.characterNode, TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE, new Vector2(450f, 200f));
				await WaitFrames(3);
				Check(GodotObject.IsInstanceValid(snowShroom) && GodotObject.IsInstanceValid(targetMagnet), "The regression must instantiate the real Snow-shroom and Target Magnet gravestone scenes.");
				if (!GodotObject.IsInstanceValid(snowShroom) || !GodotObject.IsInstanceValid(targetMagnet))
				{
					throw new InvalidOperationException("The real scenario actors could not be instantiated.");
				}
				Check(snowShroom.SceneFilePath == "res://Asset/Anime/Character/Plant/Chapter2/SnowShroom/Scene/TowerDefensePlantSnowShroom.tscn" && targetMagnet.SceneFilePath == "res://Asset/Anime/Character/GraveStone/TargetMagnet/Scene/TowerDefenseGraveStoneTargetMagnet.tscn", "Both actors must retain their authored scene paths.");
				Check(snowShroom.currentCustom.Contains("Custom0") && snowShroom.skinName == "Sward", "Snow-shroom Custom0 must select the live Sward projectile skin.");
				ChangeProjectileStateComponent changeProjectileStateComponent = targetMagnet.componentManager?.GetRuntime<ChangeProjectileStateComponent>();
				Check(changeProjectileStateComponent != null && !changeProjectileStateComponent.IsReleased && changeProjectileStateComponent.Alive && changeProjectileStateComponent.target == targetMagnet, "The real Target Magnet ChangeProjectileStateComponent must be alive and target its owner.");
				BugOverviewSnowShroomTargetMagnetRuntimeTest bugOverviewSnowShroomTargetMagnetRuntimeTest = this;
				AabbShape2DResource aabbShape2DResource = changeProjectileStateComponent?.checkShape;
				bugOverviewSnowShroomTargetMagnetRuntimeTest.Check(aabbShape2DResource != null && aabbShape2DResource.Enabled && changeProjectileStateComponent.checkShape.Geometry is RectangleShape2D { Size: var size } && size.IsEqualApprox(manager.GetMapGridSize() * new Vector2(3f, 3f)), "The Target Magnet must enable its authored three-by-three projectile zone.");
				if (changeProjectileStateComponent == null || changeProjectileStateComponent.IsReleased)
				{
					throw new InvalidOperationException("Target Magnet projectile-state runtime is unavailable.");
				}
				TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData(snowShroom.projectileName)
				{
					skinName = snowShroom.skinName,
					baseDamage = 50.0,
					damageFlags = 2,
					collisionFlags = 35
				};
				TowerDefenseProjectileConfig towerDefenseProjectileConfig = towerDefenseProjectileCreateData.BuildConfig();
				PackedScene projectileSkinProjectileScene = TowerDefenseProjectileRegistry.GetProjectileSkinProjectileScene(new StringName("SnowPea"), new StringName("Sward"));
				Check(towerDefenseProjectileConfig?.skinName == new StringName("Sward") && projectileSkinProjectileScene?.ResourcePath == "res://Asset/Config/Projectile/Sword/Sprite/IceSword/ProjectileIceSword.tscn" && towerDefenseProjectileConfig.projectileScene?.ResourcePath == "res://Asset/Config/Projectile/Sword/Sprite/IceSword/ProjectileIceSword.tscn", "The real Custom0 source data must resolve the long IceSword Sward scene.");
				if (towerDefenseProjectileConfig == null)
				{
					throw new InvalidOperationException("The Sward projectile config could not be built.");
				}
				Vector2 pos = targetMagnet.GlobalPosition + new Vector2(0f, 110f);
				BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
				{
					useFall = true,
					gridYOverride = EncounterGrid.Y,
					zOverride = 110.0,
					ySpeedOverride = 400.0
				};
				int num = bulletField.TrySpawnFromConfig(towerDefenseProjectileConfig, pos, new Vector2(100f, 0f), 600.0, snowShroom, snowShroom.camp, EncounterGrid, EncounterGrid.Y, new Rect2(-10000f, -10000f, 20000f, 20000f), null, 0.0, 0.0, towerDefenseProjectileCreateData.collisionFlags, -1f, checkHeight: false, checkAll: false, useFall: false, useGravity: false, 1.5f, 0f, 0f, overrides);
				Check(num >= 0, "Snow-shroom's real Sward projectile must spawn through BulletField.");
				if (num < 0)
				{
					throw new InvalidOperationException("The real Sward projectile could not be spawned.");
				}
				ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(num);
				Check(bulletDataRef.active && bulletDataRef.renderMode == BulletRenderMode.STATIC && bulletDataRef.useFall && !bulletDataRef.trackOpen && bulletDataRef.config?.projectileScene?.ResourcePath == "res://Asset/Config/Projectile/Sword/Sprite/IceSword/ProjectileIceSword.tscn", "The spawned Sward must begin as the real static falling IceSword bullet.");
				changeProjectileStateComponent.UpdateRect();
				Check(bulletField.IsBulletIntersectingRect(num, changeProjectileStateComponent.WorldRect), "The falling IceSword collision box must enter the real Target Magnet zone.");
				Vector2 vel = bulletDataRef.vel;
				double ySpeed = bulletDataRef.ySpeed;
				bulletField.Update(0.0, 1uL);
				ref BulletData bulletDataRef2 = ref bulletField.GetBulletDataRef(num);
				Check(bulletDataRef2.trackOpen && bulletDataRef2.target == targetMagnet && bulletDataRef2.magneticTarget == targetMagnet, "The real Target Magnet must mark the intersecting projectile as tracking itself.");
				Check(bulletDataRef2.useFall && bulletDataRef2.ySpeed == ySpeed && bulletDataRef2.vel.IsEqualApprox(vel), "Magnet conversion must preserve the active fall trajectory.");
				Check((bulletDataRef2.fireMethodFlags & 0x20) != 0 && bulletDataRef2.gridY == 10 && bulletDataRef2.checkAll, "Target Magnet must still apply its authored tracking state and cross-row contract.");
				bulletField.Update(0.016, 2uL);
				ref BulletData bulletDataRef3 = ref bulletField.GetBulletDataRef(num);
				Check(bulletDataRef3.active && bulletDataRef3.useFall && bulletDataRef3.trackOpen && bulletDataRef3.vel.IsEqualApprox(vel) && bulletDataRef3.ySpeed > ySpeed, "BulletField must continue fall physics without replacing the sword velocity with horizontal tracking motion.");
				MultiMeshInstance2D multiMeshInstance2D = FindProjectileRenderer(bulletField, bulletDataRef3.gridY);
				float[] array = ((!GodotObject.IsInstanceValid(multiMeshInstance2D)) ? null : multiMeshInstance2D.Multimesh?.Buffer);
				BugOverviewSnowShroomTargetMagnetRuntimeTest bugOverviewSnowShroomTargetMagnetRuntimeTest2 = this;
				int condition;
				if (GodotObject.IsInstanceValid(multiMeshInstance2D))
				{
					MultiMesh multimesh = multiMeshInstance2D.Multimesh;
					if (multimesh != null && multimesh.VisibleInstanceCount == 1 && multiMeshInstance2D.Multimesh.InstanceCount >= 1)
					{
						condition = ((array != null && array.Length >= 16) ? 1 : 0);
						goto IL_08fd;
					}
				}
				condition = 0;
				goto IL_08fd;
				IL_08fd:
				bugOverviewSnowShroomTargetMagnetRuntimeTest2.Check((byte)condition != 0, "The real Sward projectile must be published by the static BulletField renderer.");
				if (GodotObject.IsInstanceValid(multiMeshInstance2D))
				{
					MultiMesh multimesh2 = multiMeshInstance2D.Multimesh;
					if (multimesh2 != null && multimesh2.VisibleInstanceCount == 1 && array != null && array.Length >= 16)
					{
						Vector2 vector = new Vector2(array[0], array[4]);
						float num2 = vector.Angle();
						float num3 = new Vector2(bulletDataRef3.vel.X, (float)bulletDataRef3.ySpeed).Angle();
						Check(Mathf.Abs(Mathf.AngleDifference(num2, num3)) < 0.05f, $"A magnetized falling Sward must render along its vertical fall velocity; actual={num2}, expected={num3}.");
						Check(Mathf.Abs(vector.Y) > Mathf.Abs(vector.X) * 2f, "The long Sward must remain visibly downward instead of snapping into a horizontal sword.");
					}
				}
				bulletField.ClearActiveBullets();
				Global.Instance.isMultiplayerMode = true;
				MultiPlayerManager.Instance.isHost = false;
				snowShroom.CreateProjectile();
				await WaitFrames(3);
				Check(bulletField.ActiveCount == 0, "A remote Snow-shroom client must not create authoritative rain projectiles.");
				TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = await WaitForColdVisual(control.characterNode, EncounterGrid, 9.0);
				Check(GodotObject.IsInstanceValid(towerDefenseEffectParticlesOnce), "A remote Snow-shroom client must still create the cold visual after the rain duration.");
				Check(GodotObject.IsInstanceValid(towerDefenseEffectParticlesOnce?.particles) && towerDefenseEffectParticlesOnce.gridPos == EncounterGrid && towerDefenseEffectParticlesOnce.GlobalPosition.IsEqualApprox(TowerDefenseManager.GetMapCellPlantPos(EncounterGrid)), "The remote cold visual must mount the live snowflake particles at the Snow-shroom grid position.");
				Check(targetMagnet.buff?.BuffGet("Frozen") == null, "The presentation-only remote branch must not apply authoritative Frozen gameplay state.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewSnowShroomTargetMagnetRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(bulletField))
			{
				bulletField.ClearActiveBullets();
			}
			if (GodotObject.IsInstanceValid(snowShroom))
			{
				snowShroom.QueueFree();
			}
			if (GodotObject.IsInstanceValid(targetMagnet))
			{
				targetMagnet.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(Global.Instance))
			{
				Global.Instance.isMultiplayerMode = previousMultiplayerMode;
			}
			if (GodotObject.IsInstanceValid(MultiPlayerManager.Instance))
			{
				MultiPlayerManager.Instance.isHost = previousIsHost;
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 22;
		GD.Print($"SNOW_SHROOM_TARGET_MAGNET_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static T InstantiateCharacter<T>(string path, Node parent, TowerDefenseEnum.CHARACTER_CAMP camp, Vector2 position, Array<string> customs = null) where T : TowerDefenseCharacter
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		T val = ((packedScene != null) ? packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled) : null);
		if (!GodotObject.IsInstanceValid(val))
		{
			return null;
		}
		val.inGame = false;
		val.editorPreviewMode = true;
		val.camp = camp;
		val.gridPos = EncounterGrid;
		val.GlobalPosition = position;
		if (customs != null)
		{
			val.currentCustom = customs;
		}
		parent.AddChild(val, forceReadableName: false, InternalMode.Disabled);
		return val;
	}

	private static MultiMeshInstance2D FindProjectileRenderer(BulletField bulletField, int gridY)
	{
		int num = gridY * 15 + 9;
		foreach (Node child in bulletField.GetChildren())
		{
			if (child is MultiMeshInstance2D multiMeshInstance2D && multiMeshInstance2D.ZIndex == num)
			{
				MultiMesh multimesh = multiMeshInstance2D.Multimesh;
				if (multimesh != null && multimesh.VisibleInstanceCount > 0)
				{
					return multiMeshInstance2D;
				}
			}
		}
		return null;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task<TowerDefenseEffectParticlesOnce> WaitForColdVisual(Node parent, Vector2I expectedGrid, double timeoutSeconds)
	{
		ulong deadline = Time.GetTicksMsec() + (ulong)Math.Ceiling(Math.Max(0.0, timeoutSeconds) * 1000.0);
		do
		{
			foreach (Node child in parent.GetChildren())
			{
				if (child is TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce && towerDefenseEffectParticlesOnce.gridPos == expectedGrid)
				{
					return towerDefenseEffectParticlesOnce;
				}
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		while (Time.GetTicksMsec() < deadline);
		return null;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewSnowShroomTargetMagnetRuntimeTest] " + message);
		}
	}
}
