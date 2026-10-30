using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantStressShroom : TowerDefensePlant
{
	private const float AttackRangeGridCells = 6.5f;

	private const double BaseAttackDamage = 20.0;

	private const double AttackDamagePerLevel = 20.0;

	private const int MaxLevel = 4;

	private AttackComponent _attackComponent;

	private TowerDefenseCharacterEventHurt _hurtEvent;

	private GpuParticles2D _fireParticles;

	private GpuParticles2D _fireParticles2;

	private GpuParticles2D _fireParticles3;

	private GpuParticles2D _fireParticles4;

	private double _fireInterval = 2.0;

	[Export(PropertyHint.None, "")]
	public int level = 1;

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
			if (IsNodeReady() && _attackComponent != null)
			{
				AttackComponent attackComponent = _attackComponent;
				if (attackComponent != null && !attackComponent.IsReleased)
				{
					_attackComponent.attackInterval = (float)value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			_fireParticles = GetNode<GpuParticles2D>("%FireParticles");
			_fireParticles2 = GetNode<GpuParticles2D>("%FireParticles2");
			_fireParticles3 = GetNode<GpuParticles2D>("%FireParticles3");
			_fireParticles4 = GetNode<GpuParticles2D>("%FireParticles4");
			_attackComponent.OnAttack += Attack;
			_attackComponent.checkEachShape = true;
			DuplicateRuntimeMutableAttackResources();
			SetAttackAreaDimensions();
			SetAttackDamageForLevel(level);
			_fireParticles.ProcessMaterial = (Material)_fireParticles.ProcessMaterial.DuplicateDeep(Resource.DeepDuplicateMode.Internal);
			_fireParticles2.ProcessMaterial = _fireParticles.ProcessMaterial;
			_fireParticles3.ProcessMaterial = _fireParticles.ProcessMaterial;
			_fireParticles4.ProcessMaterial = _fireParticles.ProcessMaterial;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		AttackComponent attackComponent = _attackComponent;
		if (attackComponent != null && !attackComponent.IsReleased)
		{
			_attackComponent.OnAttack -= Attack;
		}
	}

	public void Attack()
	{
		_fireParticles.Restart();
		_fireParticles2.Restart();
		_fireParticles3.Restart();
		_fireParticles4.Restart();
		AudioManager.Instance.AudioPlay("Fume");
		_attackComponent.AttackEventExecute();
	}

	public override void Cover(TowerDefenseCharacter character)
	{
		if (character.config.name == "PlantStressShroom" && character is TowerDefensePlantStressShroom towerDefensePlantStressShroom)
		{
			level = Mathf.Clamp(towerDefensePlantStressShroom.level + 1, 1, 4);
			LevelSet(level);
			if (character.instance.wakeUp)
			{
				instance.wakeUp = true;
			}
		}
	}

	public void LevelSet(int lv)
	{
		lv = Mathf.Clamp(lv, 1, 4);
		SetAttackDamageForLevel(lv);
		switch (lv)
		{
		case 2:
			base.sprite.SetFliters(new Array { "Upgrade1_eyebrow", "Upgrade1_helmet", "Upgrade2_barrel" }, open: true);
			instance.hitpoints += 300.0;
			instance.hitpointsSave += 300.0;
			_attackComponent.attackEventName = "fire";
			_attackComponent.attackAnimeClips = "Fire2";
			_fireParticles.Scale *= 1.25f;
			((ParticleProcessMaterial)_fireParticles.ProcessMaterial).ScaleMax *= 1.25f;
			((ParticleProcessMaterial)_fireParticles.ProcessMaterial).ScaleMin *= 1.25f;
			idleAnimeClip = "Idle2";
			sleepAnimeClip = "Sleep2";
			break;
		case 3:
			base.sprite.SetFliters(new Array { "Upgrade1_eyebrow", "Upgrade1_eyebrow2", "Upgrade1_helmet", "Upgrade2_helmet", "Upgrade2_barrel", "Upgrade2_barrel2", "Upgrade2_face" }, open: true);
			instance.hitpoints += 600.0;
			instance.hitpointsSave += 600.0;
			_attackComponent.attackEventName = "fire";
			_attackComponent.attackAnimeClips = "Fire3";
			((ParticleProcessMaterial)_fireParticles.ProcessMaterial).ScaleMax *= 1.5f;
			((ParticleProcessMaterial)_fireParticles.ProcessMaterial).ScaleMin *= 1.5f;
			idleAnimeClip = "Idle3";
			sleepAnimeClip = "Sleep3";
			break;
		case 4:
			base.sprite.SetFliters(new Array { "Upgrade1_eyebrow", "Upgrade1_eyebrow2", "Upgrade1_helmet", "Upgrade2_helmet", "Upgrade2_barrel", "Upgrade2_barrel2", "Upgrade2_face" }, open: true);
			instance.hitpoints += 900.0;
			instance.hitpointsSave += 900.0;
			_attackComponent.attackEventName = "fire";
			_attackComponent.attackAnimeClips = "Fire4";
			((ParticleProcessMaterial)_fireParticles.ProcessMaterial).ScaleMax *= 1.75f;
			((ParticleProcessMaterial)_fireParticles.ProcessMaterial).ScaleMin *= 1.75f;
			idleAnimeClip = "Idle4";
			sleepAnimeClip = "Sleep4";
			break;
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["level"] = level,
			["fireInterval"] = fireInterval
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		level = ((!data.ContainsKey("level")) ? 1 : data["level"].AsInt32());
		if (level > 1)
		{
			LevelSetVisual(level);
		}
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 2.0);
	}

	public void LevelSetVisual(int lv)
	{
		lv = Mathf.Clamp(lv, 1, 4);
		SetAttackDamageForLevel(lv);
		switch (lv)
		{
		case 2:
			base.sprite.SetFliters(new Array { "Upgrade1_eyebrow", "Upgrade1_helmet", "Upgrade2_barrel" }, open: true);
			_attackComponent.attackEventName = "fire";
			_attackComponent.attackAnimeClips = "Fire2";
			_fireParticles.Scale *= 1.25f;
			((ParticleProcessMaterial)_fireParticles.ProcessMaterial).ScaleMax *= 1.25f;
			((ParticleProcessMaterial)_fireParticles.ProcessMaterial).ScaleMin *= 1.25f;
			idleAnimeClip = "Idle2";
			sleepAnimeClip = "Sleep2";
			break;
		case 3:
			base.sprite.SetFliters(new Array { "Upgrade1_eyebrow", "Upgrade1_eyebrow2", "Upgrade1_helmet", "Upgrade2_helmet", "Upgrade2_barrel", "Upgrade2_barrel2", "Upgrade2_face" }, open: true);
			_attackComponent.attackEventName = "fire";
			_attackComponent.attackAnimeClips = "Fire3";
			((ParticleProcessMaterial)_fireParticles.ProcessMaterial).ScaleMax *= 1.5f;
			((ParticleProcessMaterial)_fireParticles.ProcessMaterial).ScaleMin *= 1.5f;
			idleAnimeClip = "Idle3";
			sleepAnimeClip = "Sleep3";
			break;
		case 4:
			base.sprite.SetFliters(new Array { "Upgrade1_eyebrow", "Upgrade1_eyebrow2", "Upgrade1_helmet", "Upgrade2_helmet", "Upgrade2_barrel", "Upgrade2_barrel2", "Upgrade2_face" }, open: true);
			_attackComponent.attackEventName = "fire";
			_attackComponent.attackAnimeClips = "Fire4";
			((ParticleProcessMaterial)_fireParticles.ProcessMaterial).ScaleMax *= 1.75f;
			((ParticleProcessMaterial)_fireParticles.ProcessMaterial).ScaleMin *= 1.75f;
			idleAnimeClip = "Idle4";
			sleepAnimeClip = "Sleep4";
			break;
		}
	}

	private void DuplicateRuntimeMutableAttackResources()
	{
		if (_attackComponent.eventList.Count != 0 && _attackComponent.eventList[0] is TowerDefenseCharacterEventHurt towerDefenseCharacterEventHurt)
		{
			_hurtEvent = towerDefenseCharacterEventHurt.Duplicate(deep: true) as TowerDefenseCharacterEventHurt;
			if (_hurtEvent != null)
			{
				_attackComponent.eventList[0] = _hurtEvent;
				_attackComponent.RefreshEventExecutionCache();
			}
		}
	}

	private void SetAttackAreaDimensions()
	{
		AttackComponent attackComponent = _attackComponent;
		if (attackComponent != null && !attackComponent.IsReleased)
		{
			Vector2 mapGridSize = TowerDefenseManager.Instance.GetMapGridSize();
			float length = mapGridSize.X * 6.5f;
			float length2 = mapGridSize.Y * 6.5f;
			_attackComponent.SetCheckAreaSegmentLengthX(0, length);
			_attackComponent.SetCheckAreaSegmentLengthX(1, length2);
			_attackComponent.SetCheckAreaSegmentLengthX(2, length2);
			_attackComponent.SetCheckAreaSegmentLengthX(3, length);
			_attackComponent.SetCheckAreaSegmentLengthX(4, length2);
			_attackComponent.SetCheckAreaSegmentLengthX(5, length2);
		}
	}

	private void SetAttackDamageForLevel(int lv)
	{
		if (_hurtEvent == null)
		{
			_hurtEvent = ((_attackComponent.eventList.Count > 0) ? (_attackComponent.eventList[0] as TowerDefenseCharacterEventHurt) : null);
		}
		if (_hurtEvent != null)
		{
			_hurtEvent.num = GetAttackDamageForLevel(lv);
		}
	}

	private static double GetAttackDamageForLevel(int lv)
	{
		lv = Mathf.Clamp(lv, 1, 4);
		return 20.0 + (double)(lv - 1) * 20.0;
	}
}
