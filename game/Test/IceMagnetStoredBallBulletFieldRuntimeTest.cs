using System;
using System.Threading.Tasks;
using Godot;

public partial class IceMagnetStoredBallBulletFieldRuntimeTest : Node
{
	private const string IceMagnetScenePath = "res://Asset/Anime/Character/Plant/Chapter4/MagnetShroomIce/Scene/TowerDefensePlantMagnetShroomIce.tscn";

	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private static readonly Vector2 GridBegin = new Vector2(100f, 100f);

	private static readonly Vector2 GridSize = new Vector2(100f, 76f);

	private static readonly Vector2I GridNum = new Vector2I(9, 5);

	private static readonly Vector2I PlantGrid = new Vector2I(3, 3);

	private static readonly Vector2I ZombieGrid = new Vector2I(5, 3);

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		bool previousUseCharacterBatch = TowerDefenseCharacter.UseCharacterBatch;
		bool previousUseZombieBatch = TowerDefenseZombie.UseBatch;
		ProjectileUpdateManager projectileUpdateManager = null;
		ProcessModeEnum previousProjectileProcessMode = ProcessModeEnum.Inherit;
		IceMagnetStoredBallRuntimeControlStub control = null;
		TowerDefensePlantMagnetShroomIce iceMagnet = null;
		TowerDefenseCharacter zombie = null;
		BulletField bulletField = null;
		ResourceManager resources = ResourceManager.Instance;
		bool hadPreviousIceBallTrack = false;
		Resource previousIceBallTrack = null;
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(resources), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(resources))
				{
					throw new InvalidOperationException("A required manager autoload is unavailable.");
				}
				hadPreviousIceBallTrack = resources.PROJECTILE_CONFIG.TryGetValue("IceBallTrack", out previousIceBallTrack);
				resources.PROJECTILE_CONFIG["IceBallTrack"] = ResourceLoader.Load<TowerDefenseProjectileConfig>("res://Asset/Config/Projectile/Iceball/IceBallTrack.tres", null, ResourceLoader.CacheMode.Ignore);
				control = new IceMagnetStoredBallRuntimeControlStub
				{
					Name = "IceMagnetStoredBallRuntimeControl",
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
				manager.gridBeginPos = GridBegin;
				manager.gridSize = GridSize;
				manager.gridNum = GridNum;
				TowerDefenseProjectileRegistry.Init();
				TowerDefenseCharacter.UseCharacterBatch = false;
				TowerDefenseZombie.UseBatch = false;
				bulletField = BulletField.EnsureMountedOnCharacterNode();
				Check(GodotObject.IsInstanceValid(bulletField) && bulletField.GetParent() == control.characterNode, "The real BulletField must mount on the focused battle character node.");
				if (!GodotObject.IsInstanceValid(bulletField))
				{
					throw new InvalidOperationException("BulletField could not be mounted.");
				}
				projectileUpdateManager = ProjectileUpdateManager.Instance;
				if (GodotObject.IsInstanceValid(projectileUpdateManager))
				{
					previousProjectileProcessMode = projectileUpdateManager.ProcessMode;
					projectileUpdateManager.ProcessMode = ProcessModeEnum.Disabled;
				}
				bulletField.ClearActiveBullets();
				iceMagnet = InstantiateCharacter<TowerDefensePlantMagnetShroomIce>("res://Asset/Anime/Character/Plant/Chapter4/MagnetShroomIce/Scene/TowerDefensePlantMagnetShroomIce.tscn", control.characterNode, TowerDefenseEnum.CHARACTER_CAMP.PLANT, PlantGrid);
				await WaitFrames(4);
				Check(GodotObject.IsInstanceValid(iceMagnet) && iceMagnet.SceneFilePath == "res://Asset/Anime/Character/Plant/Chapter4/MagnetShroomIce/Scene/TowerDefensePlantMagnetShroomIce.tscn", "The regression must instantiate the real Ice Magnet-shroom scene.");
				if (!GodotObject.IsInstanceValid(iceMagnet))
				{
					throw new InvalidOperationException("Ice Magnet-shroom could not be instantiated.");
				}
				iceMagnet.ProcessMode = ProcessModeEnum.Disabled;
				IceMagnetStoredBallBulletFieldRuntimeTest iceMagnetStoredBallBulletFieldRuntimeTest = this;
				MagnetComponent magnetComponent = iceMagnet.magnetComponent;
				int condition;
				if (magnetComponent != null && !magnetComponent.IsReleased)
				{
					FireComponent fireComponent = iceMagnet.fireComponent;
					condition = ((fireComponent != null && !fireComponent.IsReleased) ? 1 : 0);
				}
				else
				{
					condition = 0;
				}
				iceMagnetStoredBallBulletFieldRuntimeTest.Check((byte)condition != 0, "The Ice Magnet-shroom must activate its authored Magnet and Fire resource runtimes.");
				TowerDefenseProjectileConfig projectileConfig = TowerDefenseManager.GetProjectileConfig("IceBallTrack");
				Check(GodotObject.IsInstanceValid(projectileConfig) && GodotObject.IsInstanceValid(projectileConfig.projectileScene), "The IceBallTrack registry entry and projectile scene must resolve.");
				int activeCount = bulletField.ActiveCount;
				iceMagnet.BreakDown(null);
				int bulletIndex = bulletField.LastSpawnedIndex;
				Check(bulletField.ActiveCount == activeCount + 1 && bulletIndex >= 0 && bulletField.IsBulletActive(bulletIndex), "Digesting one armor item must create one real BulletField ice ball.");
				if (!bulletField.IsBulletActive(bulletIndex))
				{
					throw new InvalidOperationException("Stored IceBallTrack did not spawn.");
				}
				ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(bulletIndex);
				IceMagnetStoredBallBulletFieldRuntimeTest iceMagnetStoredBallBulletFieldRuntimeTest2 = this;
				TowerDefenseProjectileConfig config = bulletDataRef.config;
				iceMagnetStoredBallBulletFieldRuntimeTest2.Check(config != null && config.projectileScene?.ResourcePath.EndsWith("/Asset/Config/Projectile/Iceball/Sprite/Iceball/ProjectileIceBall.tscn", StringComparison.Ordinal) == true && bulletDataRef.fireCharacter == iceMagnet && bulletDataRef.trackOpen, "The stored slot must retain the real IceBallTrack visual, owner, and tracking contract.");
				Check(bulletDataRef.externalControlled && bulletDataRef.hitOver && bulletDataRef.speed == 0f && bulletDataRef.vel.IsZeroApprox(), "The stored ice ball must pause built-in motion and collision while orbiting.");
				Vector2 pos = bulletDataRef.pos;
				iceMagnet.BatchUpdate(0.25);
				Check(bulletField.IsBulletActive(bulletIndex), "Without an enemy, the stored ice ball must remain active.");
				ref BulletData bulletDataRef2 = ref bulletField.GetBulletDataRef(bulletIndex);
				Check(bulletDataRef2.externalControlled && bulletDataRef2.hitOver && !bulletDataRef2.pos.IsEqualApprox(pos) && bulletDataRef2.pos.DistanceTo(iceMagnet.GlobalPosition) <= 51f, "Without an enemy, the stored ice ball must visibly orbit near the plant.");
				bulletField.Update(0.0, Engine.GetPhysicsFrames() + 101);
				MultiMeshInstance2D multiMeshInstance2D = FindProjectileRenderer(bulletField, PlantGrid.Y);
				float[] array = ((!GodotObject.IsInstanceValid(multiMeshInstance2D)) ? null : multiMeshInstance2D.Multimesh?.Buffer);
				IceMagnetStoredBallBulletFieldRuntimeTest iceMagnetStoredBallBulletFieldRuntimeTest3 = this;
				int condition2;
				if (GodotObject.IsInstanceValid(multiMeshInstance2D))
				{
					MultiMesh multimesh = multiMeshInstance2D.Multimesh;
					if (multimesh != null && multimesh.VisibleInstanceCount == 1)
					{
						condition2 = ((array != null && array.Length >= 16) ? 1 : 0);
						goto IL_06ac;
					}
				}
				condition2 = 0;
				goto IL_06ac;
				IL_06ac:
				iceMagnetStoredBallBulletFieldRuntimeTest3.Check((byte)condition2 != 0, "The stored ice ball must be published by the real BulletField renderer.");
				if (array != null && array.Length >= 16)
				{
					Check(new Vector2(array[3], array[7]).DistanceTo(bulletDataRef2.pos) < 0.1f, "The rendered IceBall transform must match the current orbit position.");
				}
				zombie = InstantiateCharacter<TowerDefenseCharacter>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", control.characterNode, TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE, ZombieGrid);
				await WaitFrames(4);
				Check(GodotObject.IsInstanceValid(zombie) && (zombie.targetRegistrationComponent?.canProjectileCheck ?? false), "The real normal zombie must register as an eligible projectile target.");
				if (!GodotObject.IsInstanceValid(zombie))
				{
					throw new InvalidOperationException("Normal zombie could not be instantiated.");
				}
				zombie.ProcessMode = ProcessModeEnum.Disabled;
				double hitpoints = zombie.instance.hitpoints;
				ref BulletData bulletDataRef3 = ref bulletField.GetBulletDataRef(bulletIndex);
				bulletDataRef3.pos = new Vector2(zombie.GlobalPosition.X, (float)((double)zombie.GlobalPosition.Y - bulletDataRef3.height));
				bulletField.Update(0.0, Engine.GetPhysicsFrames() + 102);
				Check(bulletField.IsBulletActive(bulletIndex) && Math.Abs(zombie.instance.hitpoints - hitpoints) < 0.001, "A stored hitOver ice ball must not damage or consume itself on an overlapping target.");
				Check(iceMagnet.fireComponent.CheckTrackTarget(bulletDataRef3.collisionFlags), "The production FireComponent target check must detect the newly registered zombie.");
				Vector2 pos2 = bulletDataRef3.pos;
				iceMagnet.BatchUpdate(0.016);
				Check(bulletField.IsBulletActive(bulletIndex), "Releasing the stored ice ball must keep the same BulletField slot alive.");
				ref BulletData bulletDataRef4 = ref bulletField.GetBulletDataRef(bulletIndex);
				Check(!bulletDataRef4.externalControlled && !bulletDataRef4.hitOver && bulletDataRef4.trackOpen && bulletDataRef4.checkAll && Math.Abs(bulletDataRef4.speed - 600f) < 0.001f && Math.Abs(bulletDataRef4.vel.Length() - 600f) < 0.001f, "An eligible target must restore tracking motion, collision, and the authored 600 release speed.");
				bulletField.Update(0.016, Engine.GetPhysicsFrames() + 103);
				Check(!bulletField.IsBulletActive(bulletIndex) || !bulletField.GetBulletDataRef(bulletIndex).pos.IsEqualApprox(pos2), "The released ice ball must resume BulletField movement or hit the nearby target.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[IceMagnetStoredBallBulletFieldRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(bulletField))
			{
				bulletField.ClearActiveBullets();
			}
			if (GodotObject.IsInstanceValid(projectileUpdateManager))
			{
				projectileUpdateManager.ProcessMode = previousProjectileProcessMode;
			}
			TowerDefenseCharacter.UseCharacterBatch = previousUseCharacterBatch;
			TowerDefenseZombie.UseBatch = previousUseZombieBatch;
			if (GodotObject.IsInstanceValid(iceMagnet))
			{
				iceMagnet.QueueFree();
			}
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(resources))
			{
				if (hadPreviousIceBallTrack)
				{
					resources.PROJECTILE_CONFIG["IceBallTrack"] = previousIceBallTrack;
				}
				else
				{
					resources.PROJECTILE_CONFIG.Remove("IceBallTrack");
				}
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitFrames(3);
		}
		bool flag = _failures == 0;
		GD.Print($"ICE_MAGNET_STORED_BALL_BULLET_FIELD_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static T InstantiateCharacter<T>(string path, Node parent, TowerDefenseEnum.CHARACTER_CAMP camp, Vector2I grid) where T : TowerDefenseCharacter
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		T val = ((packedScene != null) ? packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled) : null);
		if (!GodotObject.IsInstanceValid(val))
		{
			return null;
		}
		val.inGame = true;
		val.editorPreviewMode = false;
		val.camp = camp;
		val.gridPos = grid;
		val.GlobalPosition = GridBegin + new Vector2((float)(grid.X - 1) * GridSize.X, (float)(grid.Y - 1) * GridSize.Y);
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

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[IceMagnetStoredBallBulletFieldRuntimeTest] " + message);
		}
	}
}
