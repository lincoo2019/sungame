using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class BugDepartmentHypnotizedPeaZombieDirectionRuntimeTest : Node
{
	private const string PeaZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/PeaShooterSingle/TowerDefenseZombieNormalPeaShooterSingle.tscn";

	private const string PlantTargetScenePath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Scene/TowerDefensePlantPeaShooterSingle.tscn";

	private const string ZombieTargetScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string FireNutZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Firenut/TowerDefenseZombieNormalFireNut.tscn";

	private const string MapControlScenePath = "res://Registry/Battle/Feature/Map/Control/TowerDefenseMapControl.tscn";

	private static readonly Vector2I NormalShooterGrid = new Vector2I(7, 2);

	private static readonly Vector2I PlantTargetGrid = new Vector2I(3, 2);

	private static readonly Vector2I HypnotizedShooterGrid = new Vector2I(3, 4);

	private static readonly Vector2I ZombieTargetGrid = new Vector2I(7, 4);

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		ProcessModeEnum previousProjectileProcessMode = ProjectileUpdateManager.Instance?.ProcessMode ?? ProcessModeEnum.Inherit;
		BugDepartmentHypnotizedPeaZombieDirectionRuntimeControlStub control = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		BulletField bulletField = null;
		try
		{
			_ = 3;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ObjectManager.Instance), "ObjectManager autoload must be available for the real Node-projectile path.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ObjectManager.Instance))
				{
					goto end_IL_0114;
				}
				TowerDefenseProjectileRegistry.Init();
				if (GodotObject.IsInstanceValid(ProjectileUpdateManager.Instance))
				{
					ProjectileUpdateManager.Instance.ProcessMode = ProcessModeEnum.Disabled;
				}
				control = new BugDepartmentHypnotizedPeaZombieDirectionRuntimeControlStub
				{
					Name = "HypnotizedPeaZombieDirectionRuntimeControl",
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
				manager.gridBeginPos = new Vector2(0f, 100f);
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				TowerDefenseMapControl mapControl = ResourceLoader.Load<PackedScene>("res://Registry/Battle/Feature/Map/Control/TowerDefenseMapControl.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseMapControl>(PackedScene.GenEditState.Disabled);
				if (!GodotObject.IsInstanceValid(mapControl))
				{
					throw new InvalidOperationException("The production TowerDefenseMapControl scene must instantiate.");
				}
				mapControl.Name = "ProjectileDirectionMapControl";
				control.AddChild(mapControl, forceReadableName: false, InternalMode.Disabled);
				mapFeature = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
				{
					control = control,
					mapControl = mapControl,
					config = new TowerDefenseMapConfig
					{
						gridNum = manager.gridNum,
						gridSize = manager.gridSize
					},
					rect = new Rect2(-200f, -200f, 1400f, 900f),
					groundRect = new Rect2(manager.gridBeginPos, new Vector2((float)manager.gridNum.X * manager.gridSize.X, (float)manager.gridNum.Y * manager.gridSize.Y))
				});
				control.featureDictionary[new StringName("Map")] = mapFeature;
				TowerDefenseZombieNormalPeaShooterSingle normalShooter = Instantiate<TowerDefenseZombieNormalPeaShooterSingle>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/PeaShooterSingle/TowerDefenseZombieNormalPeaShooterSingle.tscn");
				TowerDefenseZombieNormalPeaShooterSingle hypnotizedShooter = Instantiate<TowerDefenseZombieNormalPeaShooterSingle>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/PeaShooterSingle/TowerDefenseZombieNormalPeaShooterSingle.tscn");
				TowerDefenseCharacter plantTarget = Instantiate<TowerDefenseCharacter>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Scene/TowerDefensePlantPeaShooterSingle.tscn");
				TowerDefenseCharacter zombieTarget = Instantiate<TowerDefenseCharacter>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
				TowerDefenseZombie fireNutZombie = Instantiate<TowerDefenseZombie>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Firenut/TowerDefenseZombieNormalFireNut.tscn");
				Check(GodotObject.IsInstanceValid(normalShooter) && GodotObject.IsInstanceValid(hypnotizedShooter) && GodotObject.IsInstanceValid(plantTarget) && GodotObject.IsInstanceValid(zombieTarget) && GodotObject.IsInstanceValid(fireNutZombie) && normalShooter.config?.name == "ZombieNormalPeaShooterSingle" && hypnotizedShooter.config?.name == "ZombieNormalPeaShooterSingle", "The fixture must instantiate two real Pea Zombie scenes, the real FireNut Zombie, and real opposing targets.");
				if (!GodotObject.IsInstanceValid(normalShooter) || !GodotObject.IsInstanceValid(hypnotizedShooter) || !GodotObject.IsInstanceValid(plantTarget) || !GodotObject.IsInstanceValid(zombieTarget) || !GodotObject.IsInstanceValid(fireNutZombie))
				{
					goto end_IL_0114;
				}
				control.characterNode.AddChild(normalShooter, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(hypnotizedShooter, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(plantTarget, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(zombieTarget, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(fireNutZombie, forceReadableName: false, InternalMode.Disabled);
				Place(normalShooter, NormalShooterGrid);
				Place(plantTarget, PlantTargetGrid);
				Place(hypnotizedShooter, HypnotizedShooterGrid);
				Place(zombieTarget, ZombieTargetGrid);
				Place(fireNutZombie, new Vector2I(5, 2));
				await WaitFrames(5);
				hypnotizedShooter.Hypnoses();
				await WaitFrames(3);
				plantTarget.ProcessMode = ProcessModeEnum.Disabled;
				zombieTarget.ProcessMode = ProcessModeEnum.Disabled;
				normalShooter.ProcessMode = ProcessModeEnum.Disabled;
				hypnotizedShooter.ProcessMode = ProcessModeEnum.Disabled;
				fireNutZombie.ProcessMode = ProcessModeEnum.Disabled;
				FireComponent fireComponent = normalShooter.componentManager?.GetRuntime<FireComponent>("character.fire");
				FireComponent hypnotizedFire = hypnotizedShooter.componentManager?.GetRuntime<FireComponent>("character.fire");
				ChangeProjectileComponent fireNutChange = fireNutZombie.componentManager?.GetRuntime<ChangeProjectileComponent>();
				Check(fireComponent != null && !fireComponent.IsReleased && hypnotizedFire != null && !hypnotizedFire.IsReleased, "Both real Pea Zombie scenes must expose their resource-backed FireComponent.");
				if (fireComponent == null || fireComponent.IsReleased || hypnotizedFire == null || hypnotizedFire.IsReleased)
				{
					goto end_IL_0114;
				}
				BugDepartmentHypnotizedPeaZombieDirectionRuntimeTest bugDepartmentHypnotizedPeaZombieDirectionRuntimeTest = this;
				int condition;
				if (fireNutChange != null && !fireNutChange.IsReleased && fireNutChange.Alive && fireNutChange.changeName == new StringName("Fire"))
				{
					TowerDefenseCharacter parent = fireNutChange.parent;
					condition = ((parent != null && parent.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE) ? 1 : 0);
				}
				else
				{
					condition = 0;
				}
				bugDepartmentHypnotizedPeaZombieDirectionRuntimeTest.Check((byte)condition != 0, "The real FireNut Zombie must expose its active zombie-camp Fire projectile-change zone.");
				if (fireNutChange == null || fireNutChange.IsReleased || !fireNutChange.Alive)
				{
					goto end_IL_0114;
				}
				FireComponentFireProjectileConfig fireComponentFireProjectileConfig = ((fireComponent.fireProjectileList.Count > 0) ? fireComponent.fireProjectileList[0] : null);
				Check(GodotObject.IsInstanceValid(fireComponentFireProjectileConfig) && Mathf.IsEqualApprox(fireComponentFireProjectileConfig.speed, -300f) && fireComponentFireProjectileConfig.projectileFlip, "The real Pea Zombie definition must retain its authored leftward speed and visual flip.");
				Check(normalShooter.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE && !normalShooter.instance.hypnoses && normalShooter.Scale.X > 0f, "The normal control must remain a right-facing zombie combatant.");
				Check(hypnotizedShooter.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT && hypnotizedShooter.instance.hypnoses && hypnotizedShooter.Scale.X < 0f, "The reported Pea Zombie must become a left-facing hypnotized plant-camp combatant.");
				Check(normalShooter.CanTarget(plantTarget) && hypnotizedShooter.CanTarget(zombieTarget), "Normal and hypnotized controls must target the opposing real character on their firing side.");
				Check(fireComponent.fireEventName.Split('&', StringSplitOptions.RemoveEmptyEntries).Length != 0 && hypnotizedFire.fireEventName.Split('&', StringSplitOptions.RemoveEmptyEntries).Length != 0, "Both controls must retain the real animation fire-event binding.");
				Check(fireComponent.checkRayResources.Count == 1 && hypnotizedFire.checkRayResources.Count == 1, "Both controls must retain the authored Pea Zombie targeting ray.");
				PrepareFire(fireComponent);
				PrepareFire(hypnotizedFire);
				control.isGameRunning = true;
				bulletField = BulletField.EnsureMountedOnCharacterNode();
				Check(GodotObject.IsInstanceValid(bulletField), "The production BulletField must mount for the default projectile path.");
				if (!GodotObject.IsInstanceValid(bulletField))
				{
					goto end_IL_0114;
				}
				bulletField.ClearActiveBullets();
				DispatchRealFireEvent(fireComponent);
				int lastSpawnedIndex = bulletField.LastSpawnedIndex;
				Check(lastSpawnedIndex >= 0 && bulletField.IsBulletActive(lastSpawnedIndex), "The normal real animation fire event must create one BulletField projectile.");
				if (lastSpawnedIndex >= 0 && bulletField.IsBulletActive(lastSpawnedIndex))
				{
					ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(lastSpawnedIndex);
					Check(bulletDataRef.vel.X < 0f && bulletDataRef.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE && bulletDataRef.flipX, "The normal BulletField pea must travel left, retain zombie camp, and remain flipped.");
					Vector2 vel = bulletDataRef.vel;
					float fireDirX = bulletDataRef.fireDirX;
					StringName projectileChange = TowerDefenseProjectileRegistry.GetProjectileChange(bulletDataRef.config.NameSN, new StringName("Fire"));
					Check(projectileChange == new StringName("FirePea"), $"The production projectile registry must route the real Zombie-fired {bulletDataRef.config.NameSN} through Fire into FirePea; target={projectileChange}.");
					fireNutChange.BindBulletField(bulletField);
					fireNutChange.OnBulletIntersect(ref bulletDataRef, lastSpawnedIndex);
					int num = ((bulletField.IsBulletActive(lastSpawnedIndex) && bulletField.GetBulletDataRef(lastSpawnedIndex).config?.NameSN == new StringName("FirePea")) ? lastSpawnedIndex : bulletField.LastSpawnedIndex);
					bool flag = num >= 0 && bulletField.IsBulletActive(num) && bulletField.GetBulletDataRef(num).config?.NameSN == new StringName("FirePea");
					Check(flag, "The real Zombie-fired pea crossing the real FireNut Zombie must become FirePea.");
					if (flag)
					{
						ref BulletData bulletDataRef2 = ref bulletField.GetBulletDataRef(num);
						Check(bulletDataRef2.vel.IsEqualApprox(vel) && bulletDataRef2.vel.X < 0f && Mathf.IsEqualApprox(bulletDataRef2.fireDirX, fireDirX) && bulletDataRef2.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE, "Passing fire must preserve the Zombie pea's leftward velocity, fire direction, and camp.");
						CheckVisualDirection(bulletField, num, "普通植物僵尸过火炬");
					}
					else
					{
						Check(condition: false, "The transformed FirePea data must remain inspectable in the production BulletField.");
					}
				}
				else
				{
					Check(condition: false, "The normal BulletField pea data must be inspectable.");
				}
				bulletField.ClearActiveBullets();
				DispatchRealFireEvent(hypnotizedFire);
				int lastSpawnedIndex2 = bulletField.LastSpawnedIndex;
				Check(lastSpawnedIndex2 >= 0 && bulletField.IsBulletActive(lastSpawnedIndex2), "The hypnotized real animation fire event must create one BulletField projectile.");
				if (lastSpawnedIndex2 >= 0 && bulletField.IsBulletActive(lastSpawnedIndex2))
				{
					ref BulletData bulletDataRef3 = ref bulletField.GetBulletDataRef(lastSpawnedIndex2);
					Check(bulletDataRef3.vel.X > 0f && bulletDataRef3.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT && !bulletDataRef3.flipX, "The hypnotized BulletField pea must travel right, use plant camp, and reverse the normal flip.");
				}
				else
				{
					Check(condition: false, "The hypnotized BulletField pea data must be inspectable.");
				}
				RunFireVisualDirectionCases(bulletField, fireComponent, fireNutChange);
				await CheckRenderedFireDirection(bulletField, fireComponent, fireNutChange, control, mapControl);
				fireNutZombie.Hypnoses();
				RunFireVisualDirectionCases(bulletField, hypnotizedFire, fireNutChange);
				await CheckRenderedFireDirection(bulletField, hypnotizedFire, fireNutChange, control, mapControl);
				goto end_IL_00f9;
				end_IL_0114:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[HypnotizedPeaZombieDirection] Unexpected exception: {value}");
				goto end_IL_00f9;
			}
			return;
			end_IL_00f9:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(bulletField))
			{
				bulletField.ClearActiveBullets();
				bulletField.Free();
			}
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			if (GodotObject.IsInstanceValid(ProjectileUpdateManager.Instance))
			{
				ProjectileUpdateManager.Instance.ProcessMode = previousProjectileProcessMode;
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitFrames(8);
		}
		int num2 = ((DisplayServer.GetName() == "headless") ? 92 : 96);
		bool flag2 = _failures == 0 && _checks == num2;
		GD.Print($"HYPNOTIZED_PEA_ZOMBIE_DIRECTION_RESULT version=3 passed={flag2} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag2) ? 2 : 0);
	}

	private void RunFireVisualDirectionCases(BulletField field, FireComponent fire, ChangeProjectileComponent torch)
	{
		FireComponentProjectileSingle fireComponentProjectileSingle = (FireComponentProjectileSingle)fire.fireCheckList[0].projectile;
		FireComponentFireProjectileConfig fireComponentFireProjectileConfig = fire.fireProjectileList[0];
		string projectileName = fireComponentProjectileSingle.projectileName;
		float dir = fireComponentFireProjectileConfig.dir;
		torch.parent.gridPos = fire.parent.gridPos;
		try
		{
			string[] array = new string[2] { "Pea", "FirePea" };
			foreach (string text in array)
			{
				float[] array2 = new float[3] { 0f, 30f, -30f };
				foreach (float num in array2)
				{
					field.ClearActiveBullets();
					fireComponentProjectileSingle.projectileName = text;
					fireComponentFireProjectileConfig.dir = num;
					DispatchRealFireEvent(fire);
					int num2 = field.LastSpawnedIndex;
					Check(num2 >= 0 && field.IsBulletActive(num2), $"{text}/{num} 正式发射必须生成子弹。");
					if (num2 >= 0 && field.IsBulletActive(num2))
					{
						ref BulletData bulletDataRef = ref field.GetBulletDataRef(num2);
						Vector2 vel = bulletDataRef.vel;
						TowerDefenseEnum.CHARACTER_CAMP camp = bulletDataRef.camp;
						if (text == "Pea")
						{
							torch.OnBulletIntersect(ref bulletDataRef, num2);
						}
						Check(bulletDataRef.config.NameSN == new StringName("FirePea") && bulletDataRef.vel.IsEqualApprox(vel) && bulletDataRef.camp == camp, $"{camp}/{text}/{num} 火球必须保留原速度和阵营。");
						CheckVisualDirection(field, num2, $"{camp}/{text}/{num}");
						string[] array3 = new string[2] { "IceFirePea", "MegaFirePea" };
						foreach (string text2 in array3)
						{
							TowerDefenseProjectileConfig newConfig = new TowerDefenseProjectileCreateData(text2).BuildConfig();
							num2 = field.ChangeBulletData(num2, newConfig, torch.parent);
							CheckVisualDirection(field, num2, $"{camp}/{text}/{num}/{text2}");
						}
						TowerDefenseProjectileConfig towerDefenseProjectileConfig = new TowerDefenseProjectileCreateData("FirePea").BuildConfig();
						towerDefenseProjectileConfig.fireMethodFlags |= 4;
						num2 = field.ChangeBulletData(num2, towerDefenseProjectileConfig, torch.parent);
						CheckVisualDirection(field, num2, $"{camp}/{text}/{num}/replacement");
					}
				}
			}
		}
		finally
		{
			fireComponentProjectileSingle.projectileName = projectileName;
			fireComponentFireProjectileConfig.dir = dir;
			field.ClearActiveBullets();
		}
	}

	private void CheckVisualDirection(BulletField field, int index, string scenario)
	{
		MethodInfo? method = typeof(BulletField).GetMethod("BuildAnimatedMeshTransform", BindingFlags.Instance | BindingFlags.NonPublic);
		BulletData bulletDataRef = field.GetBulletDataRef(index);
		float num = ((Transform2D)method.Invoke(field, new object[1] { bulletDataRef })).X.Normalized().Dot(bulletDataRef.vel.Normalized());
		Check(num > 0.999f, $"{scenario} 火球贴图必须朝飞行方向: alignment={num:F3}, velocity={bulletDataRef.vel}, flip={bulletDataRef.flipX}, bodyRotation={bulletDataRef.rotation:F3}, spriteRotation={bulletDataRef.spriteRotation:F3}");
	}

	private async Task CheckRenderedFireDirection(BulletField field, FireComponent fire, ChangeProjectileComponent torch, TowerDefenseControlNew control, TowerDefenseMapControl mapControl)
	{
		if (DisplayServer.GetName() == "headless")
		{
			return;
		}
		foreach (Node child in control.characterNode.GetChildren())
		{
			if (child is TowerDefenseCharacter towerDefenseCharacter)
			{
				towerDefenseCharacter.Visible = false;
				towerDefenseCharacter.shadowComponent?.SetShadowVisible(visible: false);
			}
		}
		mapControl.Visible = false;
		ColorRect background = new ColorRect
		{
			Color = Colors.Black,
			Size = new Vector2(1080f, 600f),
			ZIndex = -4096,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		AddChild(background, forceReadableName: false, InternalMode.Disabled);
		field.ClearActiveBullets();
		DispatchRealFireEvent(fire);
		int lastSpawnedIndex = field.LastSpawnedIndex;
		ConvertPixelBullet(field, torch, lastSpawnedIndex);
		BulletData actual = field.GetBulletDataRef(lastSpawnedIndex);
		int index = field.TrySpawnFromConfig(actual.config, new Vector2(720f, 300f), actual.vel, actual.speed, null, actual.camp, actual.gridPos, actual.gridY, new Rect2(-2000f, -2000f, 4000f, 4000f));
		PreparePixelBullet(field, lastSpawnedIndex, new Vector2(300f, 300f));
		PreparePixelBullet(field, index, new Vector2(720f, 300f));
		field.Update(0.0, Engine.GetPhysicsFrames());
		await WaitFrames(6);
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		using Image image = GetViewport().GetTexture().GetImage();
		int num = 0;
		int num2 = 0;
		for (int i = 220; i < 380; i++)
		{
			for (int j = 200; j < 400; j++)
			{
				Color pixel = image.GetPixel(j + 420, i);
				Color pixel2 = image.GetPixel(j, i);
				if (Math.Max(pixel.R, Math.Max(pixel.G, pixel.B)) > 0.08f)
				{
					num++;
				}
				if (Math.Max(Math.Abs(pixel.R - pixel2.R), Math.Max(Math.Abs(pixel.G - pixel2.G), Math.Abs(pixel.B - pixel2.B))) > 0.04f)
				{
					num2++;
				}
			}
		}
		Check(num > 100, $"{actual.camp} 参考火球必须在真实视口中可见: {num}");
		Check(num2 < Math.Max(5, num / 100), $"{actual.camp} 过火炬后的火球画面方向必须与参考相同: different={num2}, visible={num}");
		string text = ProjectSettings.GlobalizePath($"user://PlantZombieFireDirection-{actual.camp}.png");
		image.SavePng(text);
		GD.Print($"PLANT_ZOMBIE_FIRE_DIRECTION_PIXELS camp={actual.camp} visible={num} different={num2} capture={text}");
		field.ClearActiveBullets();
		background.QueueFree();
	}

	private static void ConvertPixelBullet(BulletField field, ChangeProjectileComponent torch, int index)
	{
		torch.OnBulletIntersect(ref field.GetBulletDataRef(index), index);
	}

	private static void PreparePixelBullet(BulletField field, int index, Vector2 position)
	{
		ref BulletData bulletDataRef = ref field.GetBulletDataRef(index);
		bulletDataRef.pos = position;
		bulletDataRef.externalControlled = true;
		bulletDataRef.animElapsedTimer = 0f;
		bulletDataRef.animFrameRate = 0.0;
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

	private static void PrepareFire(FireComponent fire)
	{
		fire.alive = true;
		fire.timer = 0f;
		fire.checkInterval = 0;
		fire.currentFireNum = 0;
		fire.runningCheck = ((fire.fireCheckList.Count > 0) ? fire.fireCheckList[0] : null);
		fire.runningCheckId = ((fire.runningCheck == null) ? (-1) : 0);
	}

	private static void DispatchRealFireEvent(FireComponent fire)
	{
		string command = (string.IsNullOrWhiteSpace(fire.fireEventName) ? "Fire" : fire.fireEventName.Split('&', StringSplitOptions.RemoveEmptyEntries)[0]);
		bool checkUse = fire.checkUse;
		try
		{
			fire.checkUse = false;
			fire.AnimeEvent(command, default(Variant));
		}
		finally
		{
			fire.checkUse = checkUse;
		}
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
			GD.PushError("[HypnotizedPeaZombieDirection] " + message);
		}
	}
}
