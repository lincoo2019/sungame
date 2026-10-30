using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantMarigoldG : TowerDefensePlant
{
	private FireComponent _fireComponent;

	private bool _fireBurstRunning;

	private double _fireInterval = 6.0;

	private int _fireNum = 8;

	private string _skinName = "Default";

	[Export(PropertyHint.None, "")]
	public double fireInterval
	{
		get
		{
			return _fireInterval;
		}
		set
		{
			_fireInterval = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireInterval = (float)value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int fireNum
	{
		get
		{
			return _fireNum;
		}
		set
		{
			_fireNum = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireNum = value;
				}
			}
		}
	}

	public string skinName
	{
		get
		{
			return _skinName;
		}
		set
		{
			_skinName = value;
			if (!IsNodeReady() || _fireComponent == null)
			{
				return;
			}
			FireComponent fireComponent = _fireComponent;
			if (fireComponent == null || fireComponent.IsReleased || _fireComponent.fireCheckList.Count <= 0)
			{
				return;
			}
			foreach (FireComponentCheckConfig fireCheck in _fireComponent.fireCheckList)
			{
				if (fireCheck != null)
				{
					ApplySkinNameToProjectile(fireCheck.projectile, value);
				}
			}
		}
	}

	private static void ApplySkinNameToProjectile(FireComponentProjectileResource projectile, string value)
	{
		if (projectile == null)
		{
			return;
		}
		if (projectile is FireComponentProjectileSingle fireComponentProjectileSingle)
		{
			if (fireComponentProjectileSingle.projectileData != null)
			{
				fireComponentProjectileSingle.projectileData.skinName = value;
			}
		}
		else
		{
			if (!(projectile is FireComponentProjectileWeight fireComponentProjectileWeight))
			{
				return;
			}
			if (fireComponentProjectileWeight.projectileData != null)
			{
				fireComponentProjectileWeight.projectileData.skinName = value;
			}
			foreach (FireComponentProjectileWeightItem item in fireComponentProjectileWeight.projectileWeight)
			{
				if (item != null)
				{
					ApplySkinNameToProjectile(item.projectileResource, value);
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			if (base.currentCustom.Contains("Custom0"))
			{
				skinName = "Sycee";
			}
		}
	}

	public override void OnCustomSwitched(string customKey)
	{
		if (customKey == "Custom0")
		{
			skinName = "Sycee";
		}
		else
		{
			skinName = "Default";
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		base.sprite.timeScale = timeScale;
		if (!_fireBurstRunning && _fireComponent.CanFireByData(_fireComponent.fireCheckList[0].projectile.GetProjetile()))
		{
			_fireComponent.Refresh();
			TowerDefenseProjectileCreateData projetile = _fireComponent.fireCheckList[0].projectile.GetProjetile();
			_fireBurstRunning = true;
			FireBurstAsync(base.CurrentStateHandle, projetile);
		}
	}

	private async Task FireBurstAsync(StateHandle stateHandle, TowerDefenseProjectileCreateData projectileData)
	{
		try
		{
			for (int i = 0; i < fireNum; i++)
			{
				if (_fireComponent?.IsReleased ?? true)
				{
					break;
				}
				if (stateHandle == null || !stateHandle.IsActive)
				{
					break;
				}
				Vector2 vector = Vector2.FromAngle(Mathf.DegToRad(360f / (float)fireNum * (float)i));
				BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
				{
					spawnTweenOffset = vector * 50f,
					spawnTweenDuration = 0.5f,
					spawnTweenEase = Tween.EaseType.Out,
					spawnTweenTrans = Tween.TransitionType.Quart
				};
				_fireComponent.CreateProjectileByData(0, new Vector2(600f, 0f), projectileData, -1, base.camp, Vector2.Zero, overrides);
				bool flag = i + 1 < fireNum;
				if (flag)
				{
					flag = !(await WaitForStateDelayAsync(stateHandle, 0.1));
				}
				if (flag)
				{
					break;
				}
			}
		}
		finally
		{
			_fireBurstRunning = false;
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["fireNum"] = fireNum,
			["skinName"] = skinName,
			["fireInterval"] = fireInterval
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = (data.ContainsKey("fireNum") ? data["fireNum"].AsInt32() : 8);
		skinName = (data.ContainsKey("skinName") ? data["skinName"].AsString() : "Default");
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 6.0);
	}
}
