using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantQueenSunFlower : TowerDefensePlant
{
	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> allEventList = new Array<TowerDefenseCharacterEventBase>();

	private static readonly Array<TowerDefenseCharacter> _emptyCharacterList = new Array<TowerDefenseCharacter>();

	private AttackComponent _attackComponent;

	private ProduceComponent _produceComponent;

	private FireComponent _fireComponent;

	[Export(PropertyHint.None, "")]
	public double attackInterval = 3.0;

	[Export(PropertyHint.None, "")]
	public double fireInterval = 3.0;

	[Export(PropertyHint.None, "")]
	public int fireNum = 6;

	private double _produceInterval = 25.0;

	private int _sunNum = 150;

	public int currentFireNum;

	public double attackTimer;

	private string _projectileName = "FirePea";

	private string _skinName = "Default";

	[Export(PropertyHint.None, "")]
	public double produceInterval
	{
		get
		{
			return _produceInterval;
		}
		set
		{
			_produceInterval = value;
			if (IsNodeReady() && _produceComponent != null)
			{
				_produceComponent.produceInterval = (float)_produceInterval;
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int sunNum
	{
		get
		{
			return _sunNum;
		}
		set
		{
			_sunNum = value;
			if (IsNodeReady() && _produceComponent != null)
			{
				_produceComponent.num = _sunNum;
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public string projectileName
	{
		get
		{
			return _projectileName;
		}
		set
		{
			_projectileName = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileName = _projectileName;
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
			if (IsNodeReady() && _fireComponent != null)
			{
				((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileData.skinName = _skinName;
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			_produceComponent = componentManager.GetRuntime<ProduceComponent>();
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			if (base.currentCustom.Contains("Custom0"))
			{
				skinName = "Note";
			}
			_attackComponent.SetCheckAreaRectangleSize(0, TowerDefenseManager.Instance.GetMapGridSize() * 2.75f);
			_fireComponent.fireInterval = (float)fireInterval;
		}
	}

	public override void OnCustomSwitched(string customKey)
	{
		if (customKey == "Custom0")
		{
			skinName = "Note";
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
		Vector2 pos = default(Vector2);
		bool flag = false;
		if (Engine.GetPhysicsFrames() % 2 == 0L)
		{
			pos = GetLogicalGlobalPosition();
			flag = true;
			TowerDefenseExplode.CreateExplode(pos, new Vector2(1.3f, 1.3f), allEventList, _emptyCharacterList, TowerDefenseEnum.CHARACTER_CAMP.ALL, -1);
		}
		if (attackTimer >= attackInterval)
		{
			if (_attackComponent.CanAttack())
			{
				if (!flag)
				{
					pos = GetLogicalGlobalPosition();
				}
				TowerDefenseExplode.CreateExplode(pos, new Vector2(1.3f, 1.3f), eventList, _emptyCharacterList, base.camp, instance.collisionFlags);
				attackTimer = 0.0;
			}
		}
		else
		{
			attackTimer += delta;
		}
		if (_fireComponent.CanFireByData(_fireComponent.fireCheckList[0].projectile.GetProjetile()))
		{
			_fireComponent.Refresh();
			for (int i = 0; i < fireNum; i++)
			{
				Vector2 vector = Vector2.FromAngle((float)Mathf.DegToRad(360.0 / (double)fireNum * (double)i));
				BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
				{
					spawnTweenOffset = vector * 50f,
					spawnTweenDuration = 0.5f,
					spawnTweenEase = Tween.EaseType.Out,
					spawnTweenTrans = Tween.TransitionType.Quart
				};
				_fireComponent.CreateProjectileByData(0, new Vector2(600f, 0f), _fireComponent.fireCheckList[0].projectile.GetProjetile(), -1, base.camp, Vector2.Zero, overrides);
			}
		}
	}

	protected internal override void OnHypnosisStateChanged()
	{
		base.OnHypnosisStateChanged();
		_produceComponent.produceType = (instance.hypnoses ? "BrainSun" : "Sun");
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "attackInterval", attackInterval },
			{ "fireNum", fireNum },
			{ "produceInterval", produceInterval },
			{ "sunNum", sunNum },
			{ "currentFireNum", currentFireNum },
			{ "attackTimer", attackTimer },
			{ "projectileName", projectileName },
			{ "skinName", skinName },
			{ "fireInterval", fireInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		attackInterval = (double)data.GetValueOrDefault("attackInterval", 3.0);
		fireNum = (int)data.GetValueOrDefault("fireNum", 6);
		produceInterval = (double)data.GetValueOrDefault("produceInterval", 25.0);
		sunNum = (int)data.GetValueOrDefault("sunNum", 150);
		currentFireNum = (int)data.GetValueOrDefault("currentFireNum", 0);
		attackTimer = (double)data.GetValueOrDefault("attackTimer", 0.0);
		projectileName = (string)data.GetValueOrDefault("projectileName", "FirePea");
		skinName = (string)data.GetValueOrDefault("skinName", "Default");
		fireInterval = (double)data.GetValueOrDefault("fireInterval", 3.0);
	}
}
