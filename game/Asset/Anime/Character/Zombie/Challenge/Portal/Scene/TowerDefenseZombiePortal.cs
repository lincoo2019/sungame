using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombiePortal : TowerDefenseZombie, IProjectileZone
{
	private readonly struct PortalProjectileReleaseContext(BulletField bulletField, Vector2 spawnPosition, Vector2I destinationGrid, Rect2 projectileMapRect, double groundHeight, bool hypnoses, TowerDefenseEnum.CHARACTER_CAMP outputCamp, BulletFieldSpawnOverrides overrides)
	{
		public readonly BulletField BulletField = bulletField;

		public readonly Vector2 SpawnPosition = spawnPosition;

		public readonly Vector2I DestinationGrid = destinationGrid;

		public readonly Rect2 ProjectileMapRect = projectileMapRect;

		public readonly double GroundHeight = groundHeight;

		public readonly bool Hypnoses = hypnoses;

		public readonly TowerDefenseEnum.CHARACTER_CAMP OutputCamp = outputCamp;

		public readonly BulletFieldSpawnOverrides Overrides = overrides;
	}

	public bool halfHp;

	public bool isAttack;

	public double timer;

	public double time = 20.0;

	public bool over;

	public bool audioPlay;

	[Export(PropertyHint.None, "")]
	public double absorbDuration = 0.26;

	[Export(PropertyHint.None, "")]
	public double absorbSpin = 13.0;

	[Export(PropertyHint.None, "")]
	public double absorbCurveSign = -1.0;

	[Export(PropertyHint.None, "")]
	public double absorbCurveRatio = 0.18;

	public List<TowerDefenseProjectileConfig> projectileConfigList = new List<TowerDefenseProjectileConfig>();

	private CharacterAabbAreaComponent _checkArea;

	private Rect2 _zoneRect;

	public Rect2 WorldRect => _zoneRect;

	public int GridY => base.gridPos.Y;

	public int RowSpan => 0;

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			return;
		}
		_checkArea = componentManager.GetRuntime<CharacterAabbAreaComponent>("character.aabb_area.0");
		_checkArea?.SetEnabled(!over);
		base.sprite.SetFliters((Array?)new Array<string> { "light1", "light2", "light3" }, open: true);
		if ((double)GD.Randf() > 0.5)
		{
			base.sprite.SetFliter("anim_tongue", open: true);
		}
		ConfigureWaterLineVisualLayers("Zombie_duckytube", "Zombie_whitewater", "Zombie_whitewater2");
		if (!over)
		{
			AddToGroup("ZombiePortalGroup");
			if (BulletField.Instance != null)
			{
				BulletField.Instance.RegisterZone(this);
			}
		}
	}

	public override void _ExitTree()
	{
		if (!Engine.IsEditorHint() && BulletField.Instance != null)
		{
			BulletField.Instance.UnregisterZone(this);
		}
		base._ExitTree();
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (Engine.IsEditorHint() || !GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) || !TowerDefenseManager.CurrentControl.isGameRunning || !base.IsInsideComponentBattlefield)
		{
			return;
		}
		if (!audioPlay)
		{
			AudioManager.Instance.AudioPlay("Portal");
			audioPlay = true;
		}
		if (!over && !base.sprite.pause)
		{
			if (timer < time)
			{
				timer += delta * timeScale;
			}
			else
			{
				over = true;
				if (nearDie || die)
				{
					return;
				}
				AudioManager.Instance.AudioPlay("Portal");
				ToPortal();
			}
		}
		if (TowerDefenseManager.Instance.IsGameRunning())
		{
			_ = inGame;
		}
	}

	public override void AttackEntered()
	{
		base.AttackEntered();
		isAttack = true;
		if (over)
		{
			base.sprite.SetFliters((Array?)new Array<string> { "light1", "light2", "light3" }, open: false);
		}
		if (HasShield())
		{
			base.sprite.SetFliters((Array?)new Array<string> { "Zombie_outerarm_upper" }, open: true);
			if (!halfHp)
			{
				base.sprite.SetFliters((Array?)new Array<string> { "Zombie_outerarm_hand", "Zombie_outerarm_lower" }, open: true);
			}
		}
	}

	public override void AttackExited()
	{
		base.AttackExited();
		isAttack = false;
		if (HasShield())
		{
			base.sprite.SetFliters((Array?)new Array<string> { "Zombie_outerarm_upper", "Zombie_outerarm_hand", "Zombie_outerarm_lower" }, open: false);
		}
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		if (damagePointName == "Arm")
		{
			halfHp = true;
			base.sprite.SetFliters((Array?)new Array<string> { "Zombie_outerarm_upper" }, open: true);
		}
	}

	public override void ArmorDamagePointReach(string armorName, int stage)
	{
		base.ArmorDamagePointReach(armorName, stage);
		if (armorName == "Portal" && over)
		{
			base.sprite.SetFliters((Array?)new Array<string> { "light1", "light2", "light3" }, open: false);
		}
		if (isAttack && HasShield() && stage > 0)
		{
			base.sprite.SetFliters((Array?)new Array<string> { "Zombie_outerarm_upper" }, open: true);
			if (!halfHp)
			{
				base.sprite.SetFliters((Array?)new Array<string> { "Zombie_outerarm_hand", "Zombie_outerarm_lower" }, open: true);
			}
		}
	}

	public void UpdateRect()
	{
		if (!base.IsInsideComponentBattlefield || over || _checkArea == null || !_checkArea.TryGetWorldRect(out _zoneRect))
		{
			_zoneRect = new Rect2(Vector2.Zero, Vector2.Zero);
		}
	}

	public void OnBulletIntersect(ref BulletData b, int index)
	{
		if (!base.IsInsideComponentBattlefield || b.portalReleased || b.absorbOpen || over)
		{
			return;
		}
		CharacterAabbAreaComponent checkArea = _checkArea;
		if (checkArea != null && checkArea.Enabled && b.active && b.config != null && !b.over && b.camp != base.camp && (b.fireMethodFlags & 2) == 0)
		{
			projectileConfigList.Add(b.config);
			if (BulletField.Instance != null && !BulletField.Instance.BeginAbsorb(index, GetAbsorbTargetPosition(), (float)absorbDuration, (float)absorbSpin, (float)absorbCurveSign, (float)absorbCurveRatio))
			{
				BulletField.Instance.Despawn(index);
			}
		}
	}

	private Vector2 GetAbsorbTargetPosition()
	{
		if (base.damagePartSlot.TryGetValue("Portal", out var value))
		{
			Node2D nodeOrNull = GetNodeOrNull<Node2D>(value.AsString());
			if (GodotObject.IsInstanceValid(nodeOrNull))
			{
				return GetLogicalGlobalPosition(nodeOrNull);
			}
		}
		return _zoneRect.GetCenter();
	}

	public void OnProjectileIntersect(TowerDefenseProjectile projectile)
	{
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (armorName == "Portal")
		{
			over = true;
			ToPortal();
		}
	}

	public override void DieEntered()
	{
		base.DieEntered();
		over = true;
		ToPortal();
	}

	public void ToPortal()
	{
		if (BulletField.Instance != null)
		{
			BulletField.Instance.UnregisterZone(this);
		}
		_zoneRect = new Rect2(Vector2.Zero, Vector2.Zero);
		_checkArea?.SetEnabled(enabled: false);
		RemoveFromGroup("ZombiePortalGroup");
		if (!nearDie && !die && GetHasArmor("Portal"))
		{
			if (base.inWater)
			{
				base.sprite.SetAnimation("WaterClose", loop: false);
			}
			else
			{
				base.sprite.SetAnimation("Close", loop: false);
			}
		}
		else
		{
			Portal();
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Close" || clip == "WaterClose")
		{
			Portal();
		}
	}

	public void Portal()
	{
		if (!nearDie && !die)
		{
			if (isAttack)
			{
				if (base.inWater && base.attackWaterAnimeClip != "")
				{
					base.sprite.SetAnimation(base.attackWaterAnimeClip, loop: true, 0.2);
				}
				else
				{
					base.sprite.SetAnimation(base.attackAnimeClip, loop: true, 0.2);
				}
			}
			else
			{
				base.sprite.SetAnimation(base.walkAnimeClip, loop: true, 0.2);
			}
		}
		base.sprite.SetFliters((Array?)new Array<string> { "light1", "light2", "light3" }, open: false);
		Array<Node> nodesInGroup = GetTree().GetNodesInGroup("ZombiePortalGroup");
		if (nodesInGroup.Count == 0)
		{
			return;
		}
		Node destination = nodesInGroup[GD.RandRange(0, nodesInGroup.Count - 1)];
		if (projectileConfigList.Count == 0)
		{
			return;
		}
		if (!TryPrepareProjectileRelease(destination, out var context))
		{
			projectileConfigList.Clear();
			return;
		}
		for (int i = 0; i < projectileConfigList.Count; i++)
		{
			TowerDefenseProjectileConfig towerDefenseProjectileConfig = projectileConfigList[i];
			if (towerDefenseProjectileConfig != null)
			{
				ReleaseStoredProjectilePrepared(towerDefenseProjectileConfig, in context);
			}
		}
		projectileConfigList.Clear();
	}

	internal int ReleaseStoredProjectile(TowerDefenseProjectileConfig storedProjectileConfig, GodotObject destination)
	{
		if (!TryPrepareProjectileRelease(destination, out var context))
		{
			return -1;
		}
		return ReleaseStoredProjectilePrepared(storedProjectileConfig, in context);
	}

	private bool TryPrepareProjectileRelease(GodotObject destination, out PortalProjectileReleaseContext context)
	{
		context = default(PortalProjectileReleaseContext);
		if (!(destination is TowerDefenseCharacter towerDefenseCharacter) || !GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			GD.PushError("[BulletField:E_PORTAL_DESTINATION_INVALID] zombie portal destination is unavailable");
			return false;
		}
		if (!GodotObject.IsInstanceValid(instance))
		{
			GD.PushError("[BulletField:E_PORTAL_SOURCE_INVALID] zombie portal source instance is unavailable");
			return false;
		}
		BulletField bulletField = BulletField.EnsureMountedOnCharacterNode();
		if (!GodotObject.IsInstanceValid(bulletField))
		{
			GD.PushError("[BulletField:E_FIELD_UNAVAILABLE] zombie portal could not mount BulletField");
			return false;
		}
		bool hypnoses = instance.hypnoses;
		Vector2I destinationGrid = towerDefenseCharacter.gridPos;
		BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
		{
			checkAllOverride = true,
			fireMethodFlagsOverride = 1,
			flipXOverride = (towerDefenseCharacter.Scale.X > 0f),
			portalReleasedOverride = true,
			gridYOverride = destinationGrid.Y
		};
		Vector2 spawnPosition = towerDefenseCharacter.GetLogicalGlobalPosition() + new Vector2(hypnoses ? 50f : (-50f), 20f);
		double num = GetGroundHeight(GetLogicalGlobalPosition().Y);
		TowerDefenseEnum.CHARACTER_CAMP outputCamp = (hypnoses ? TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE : TowerDefenseEnum.CHARACTER_CAMP.PLANT);
		context = new PortalProjectileReleaseContext(bulletField, spawnPosition, destinationGrid, FireComponent.ComputeProjectileMapRect(), num, hypnoses, outputCamp, overrides);
		return true;
	}

	private static int ReleaseStoredProjectilePrepared(TowerDefenseProjectileConfig storedProjectileConfig, in PortalProjectileReleaseContext context)
	{
		float num = (context.Hypnoses ? ((float)GD.RandRange(250, 500)) : ((float)GD.RandRange(-500, -250)));
		Vector2 vector = new Vector2(num, 0f);
		double height = context.GroundHeight + (double)GD.RandRange(0, 70);
		return context.BulletField.TrySpawnFromConfig(storedProjectileConfig, context.SpawnPosition, vector, Mathf.Abs(num), null, context.OutputCamp, context.DestinationGrid, context.DestinationGrid.Y, context.ProjectileMapRect, null, collisionFlagsOverride: storedProjectileConfig?.collisionFlags ?? (-1), height: height, groundHeight: context.GroundHeight, fireLength: -1f, checkHeight: false, checkAll: false, useFall: false, useGravity: false, gravityScale: 1.5f, yOffsetTarget: 0f, yOffsetDuration: 0f, overrides: context.Overrides);
	}

	public override void Hypnoses(double time = -1.0, bool canFliter = true, TowerDefenseCharacterBuffHypnoses hypnosesConfig = null)
	{
		base.Hypnoses(time, canFliter, hypnosesConfig);
		projectileConfigList.Clear();
	}

	public override Dictionary ExportVariantSave()
	{
		Array array = new Array();
		for (int i = 0; i < projectileConfigList.Count; i++)
		{
			TowerDefenseProjectileConfig towerDefenseProjectileConfig = projectileConfigList[i];
			if (GodotObject.IsInstanceValid(towerDefenseProjectileConfig) && !string.IsNullOrEmpty(towerDefenseProjectileConfig.name))
			{
				array.Add(towerDefenseProjectileConfig.name);
			}
		}
		return new Dictionary
		{
			["timer"] = timer,
			["time"] = time,
			["over"] = over,
			["audioPlay"] = audioPlay,
			["projectileConfigNames"] = array
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		timer = data.GetValueOrDefault("timer", 0.0).AsDouble();
		time = data.GetValueOrDefault("time", 20.0).AsDouble();
		over = data.GetValueOrDefault("over", false).AsBool();
		audioPlay = data.GetValueOrDefault("audioPlay", false).AsBool();
		_checkArea?.SetEnabled(!over);
		if (over)
		{
			if (BulletField.Instance != null)
			{
				BulletField.Instance.UnregisterZone(this);
			}
			RemoveFromGroup("ZombiePortalGroup");
		}
		projectileConfigList.Clear();
		foreach (Variant item in data.GetValueOrDefault("projectileConfigNames", new Array()).AsGodotArray())
		{
			string text = item.AsString();
			if (!string.IsNullOrEmpty(text))
			{
				TowerDefenseProjectileConfig projectileConfig = TowerDefenseManager.GetProjectileConfig(text);
				if (GodotObject.IsInstanceValid(projectileConfig))
				{
					projectileConfigList.Add(projectileConfig);
				}
			}
		}
	}
}
