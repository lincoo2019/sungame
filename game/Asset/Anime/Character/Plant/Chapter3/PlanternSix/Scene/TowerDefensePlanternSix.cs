using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlanternSix : TowerDefensePlant
{
	private static readonly string TOWER_DEFENSE_PROJECTILE_EFFECT_PLANTERN_SIX = "uid://nbjvjnrei6vi";

	[Export(PropertyHint.None, "")]
	public double attackInterval = 1.5;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventAttackList = new Array<TowerDefenseCharacterEventBase>();

	private PointLight2D light;

	public double attackTimer;

	public int coldCheckInterval = 2;

	public bool over;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			light = GetNode<PointLight2D>("%Light");
			AudioManager.Instance.AudioPlay("Plantern");
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			base.BatchUpdate(delta);
			if ((ulong)((long)TowerDefenseProcessModeDispatch.CurrentPhysicsFrame + (long)randFreshIndex) % 30uL == 0L)
			{
				light.Visible = TowerDefenseManager.GetMapIsNight() && GameSaveManager.Instance.GetConfigValue("MapEffect").AsBool();
			}
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		base.sprite.timeScale = timeScale;
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		if (coldCheckInterval > 0)
		{
			coldCheckInterval--;
		}
		else
		{
			TowerDefenseExplode.CreateExplode(logicalGlobalPosition, new Vector2(1.3f, 1.3f), eventList, null, base.camp, -1);
			coldCheckInterval = 2;
		}
		if (attackTimer >= attackInterval)
		{
			TowerDefenseExplode.CreateExplode(logicalGlobalPosition, new Vector2(1.3f, 1.3f), eventAttackList, null, base.camp, instance.collisionFlags);
			attackTimer = 0.0;
		}
		else
		{
			attackTimer += delta;
		}
	}

	public override async void DestroySet()
	{
		if (!over)
		{
			over = true;
			TowerDefenseProjectileEffectBase towerDefenseProjectileEffectBase = (TowerDefenseProjectileEffectBase)GD.Load<PackedScene>(TOWER_DEFENSE_PROJECTILE_EFFECT_PLANTERN_SIX).Instantiate(PackedScene.GenEditState.Disabled);
			int collisionFlag = base.config?.collisionFlags ?? (-1);
			towerDefenseProjectileEffectBase.Init(base.gridPos, base.camp, collisionFlag, null, base.groundHeight);
			towerDefenseProjectileEffectBase.GlobalPosition = GetLogicalGlobalPosition();
			TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseProjectileEffectBase, forceReadableName: false, InternalMode.Disabled);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "attackInterval", attackInterval },
			{ "attackTimer", attackTimer },
			{ "coldCheckInterval", coldCheckInterval },
			{ "over", over }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		attackInterval = data.GetValueOrDefault("attackInterval", 1.5).AsDouble();
		attackTimer = data.GetValueOrDefault("attackTimer", 0.0).AsDouble();
		coldCheckInterval = data.GetValueOrDefault("coldCheckInterval", 2).AsInt32();
		over = data.GetValueOrDefault("over", false).AsBool();
	}
}
