using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Godot;

public partial class RichwoodChestBulletFieldRuntimeTest : Node
{
	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		BulletField bulletField = null;
		try
		{
			try
			{
				TowerDefenseProjectileRegistry.Init();
				bulletField = new BulletField
				{
					Name = "RichwoodChestBulletField"
				};
				AddChild(bulletField, forceReadableName: false, InternalMode.Disabled);
				TowerDefenseProjectileConfig towerDefenseProjectileConfig = new TowerDefenseProjectileCreateData(new StringName("Chest")).BuildConfig();
				TowerDefenseProjectileConfig towerDefenseProjectileConfig2 = new TowerDefenseProjectileCreateData(new StringName("Pea")).BuildConfig();
				Check(GodotObject.IsInstanceValid(towerDefenseProjectileConfig) && GodotObject.IsInstanceValid(towerDefenseProjectileConfig.projectileScene), "The canonical Chest projectile and its scene must load from the live registry.");
				Check(GodotObject.IsInstanceValid(towerDefenseProjectileConfig2), "The captured projectile config must load from the live registry.");
				if (!GodotObject.IsInstanceValid(towerDefenseProjectileConfig) || !GodotObject.IsInstanceValid(towerDefenseProjectileConfig2))
				{
					goto end_IL_0024;
				}
				TowerDefenseCharacterEventExplodeProjectileFromMetaData towerDefenseCharacterEventExplodeProjectileFromMetaData = null;
				foreach (TowerDefenseCharacterEventBase hitTargetEvent in towerDefenseProjectileConfig.hitTargetEventList)
				{
					if (hitTargetEvent is TowerDefenseCharacterEventExplodeProjectileFromMetaData towerDefenseCharacterEventExplodeProjectileFromMetaData2)
					{
						towerDefenseCharacterEventExplodeProjectileFromMetaData = towerDefenseCharacterEventExplodeProjectileFromMetaData2;
						break;
					}
				}
				Check(GodotObject.IsInstanceValid(towerDefenseCharacterEventExplodeProjectileFromMetaData), "The canonical Chest projectile must retain its metadata scatter hit event.");
				if (!GodotObject.IsInstanceValid(towerDefenseCharacterEventExplodeProjectileFromMetaData))
				{
					goto end_IL_0024;
				}
				int num = towerDefenseProjectileConfig2.fireMethodFlags | 8;
				BulletFieldStoredProjectile[] array = new BulletFieldStoredProjectile[6];
				for (int i = 0; i < 6; i++)
				{
					array[i] = new BulletFieldStoredProjectile(towerDefenseProjectileConfig2, num);
				}
				List<int> list = new List<int>();
				bulletField.OnBulletSpawned += list.Add;
				int num2 = bulletField.TrySpawnFromConfig(towerDefenseProjectileConfig, new Vector2(240f, 240f), new Vector2(600f, 0f), 600.0, null, TowerDefenseEnum.CHARACTER_CAMP.PLANT, new Vector2I(3, 3), 3, new Rect2(-2000f, -2000f, 4000f, 4000f), null, 0.0, 0.0, 1, -1f, checkHeight: false, checkAll: false, useFall: false, useGravity: false, 1.5f, 0f, 0f, new BulletFieldSpawnOverrides
				{
					gridYOverride = 3,
					eventProjectiles = array
				});
				Check(num2 >= 0 && bulletField.ActiveCount == 1, "The real Chest projectile must spawn into BulletField.");
				if (num2 < 0)
				{
					goto end_IL_0024;
				}
				IDictionary eventProjectileDictionary = GetEventProjectileDictionary(bulletField);
				Check(eventProjectileDictionary.Count == 1 && eventProjectileDictionary.Contains(num2), "BulletField must retain metadata only for the Chest slot.");
				TowerDefenseProjectile towerDefenseProjectile = FillAndGetEventProxy(bulletField, num2);
				RichwoodChestBulletFieldRuntimeTest richwoodChestBulletFieldRuntimeTest = this;
				int condition;
				if (GodotObject.IsInstanceValid(towerDefenseProjectile))
				{
					BulletFieldStoredProjectile[] eventProjectiles = towerDefenseProjectile.eventProjectiles;
					condition = ((eventProjectiles != null && eventProjectiles.Length == 6) ? 1 : 0);
				}
				else
				{
					condition = 0;
				}
				richwoodChestBulletFieldRuntimeTest.Check((byte)condition != 0, "The shared event proxy must receive all six captured projectile configs.");
				if (!GodotObject.IsInstanceValid(towerDefenseProjectile))
				{
					goto end_IL_0024;
				}
				int activeCount = bulletField.ActiveCount;
				towerDefenseCharacterEventExplodeProjectileFromMetaData.ExecuteProject(towerDefenseProjectile, null);
				Check(bulletField.ActiveCount == activeCount + 6, "The real Chest metadata hit event must emit all six stored projectiles.");
				Check(list.Count == 7, "The Chest plus six released projectiles must all use BulletField slots.");
				Check(GetTree().GetNodesInGroup("Projectile").Count == 0, "Richwood output must not restore per-projectile Nodes.");
				for (int j = 1; j < list.Count; j++)
				{
					ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(list[j]);
					Check(bulletDataRef.active && bulletDataRef.config.NameSN == towerDefenseProjectileConfig2.NameSN && bulletDataRef.fireMethodFlags == num && bulletDataRef.checkAll && bulletDataRef.vel.LengthSquared() > 0f, $"Released projectile {j} must preserve its config and radial BulletField motion.");
				}
				bulletField.ClearActiveBullets();
				Check(bulletField.ActiveCount == 0 && eventProjectileDictionary.Count == 0, "Despawn must release the Chest metadata sidecar.");
				goto end_IL_0024_2;
				end_IL_0024:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[RichwoodChestBulletField] Unexpected exception: {value}");
				goto end_IL_0024_2;
			}
			end_IL_0024_2:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(bulletField))
			{
				bulletField.QueueFree();
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			bool flag = _failures == 0 && _checks == 16;
			GD.Print($"RICHWOOD_CHEST_BULLET_FIELD_RESULT passed={flag} checks={_checks} failures={_failures}");
			GetTree().Quit((!flag) ? 2 : 0);
		}
	}

	private static IDictionary GetEventProjectileDictionary(BulletField bulletField)
	{
		return (typeof(BulletField).GetField("_eventProjectilesByIndex", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(bulletField) as IDictionary) ?? throw new MissingFieldException(typeof(BulletField).FullName, "_eventProjectilesByIndex");
	}

	private static TowerDefenseProjectile FillAndGetEventProxy(BulletField bulletField, int index)
	{
		MethodInfo? method = typeof(BulletField).GetMethod("FillEventProxy", BindingFlags.Instance | BindingFlags.NonPublic);
		if (method == null)
		{
			throw new MissingMethodException(typeof(BulletField).FullName, "FillEventProxy");
		}
		BulletData bulletDataRef = bulletField.GetBulletDataRef(index);
		method.Invoke(bulletField, new object[2] { index, bulletDataRef });
		return (typeof(BulletField).GetField("_eventProxy", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as TowerDefenseProjectile) ?? throw new MissingFieldException(typeof(BulletField).FullName, "_eventProxy");
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[RichwoodChestBulletField] " + message);
		}
	}
}
