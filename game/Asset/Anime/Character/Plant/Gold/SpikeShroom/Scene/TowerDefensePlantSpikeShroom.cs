using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantSpikeShroom : TowerDefensePlant
{
	private const string SPIKE_SHROOM_SKIN_3_1 = "uid://setqd0doysia";

	private const string SPIKE_SHROOM_SKIN_3_2 = "uid://dtaq2xxqdn4l";

	private const string SPIKE_SHROOM_SKIN_3_3 = "uid://d345b8j5t8rim";

	private const string SPIKE_SHROOM_SKIN_4_1 = "uid://oupumw7ivoj2";

	private const string SPIKE_SHROOM_SKIN_4_2 = "uid://blsvhscujc3wy";

	private const string SPIKE_SHROOM_SKIN_4_3 = "uid://dab5sqapb1pxr";

	private const string SPIKE_SHROOM_SKIN_6_1 = "uid://b4c2b841mg3hk";

	private const string SPIKE_SHROOM_SKIN_6_2 = "uid://d3m54y244cyum";

	private const string SPIKE_SHROOM_SKIN_6_3 = "uid://d35grguid2x18";

	private static PackedScene _SPIKE_SHROOM_ATTACK_EFFECT;

	private static PackedScene _SPIKE_SHROOM_ATTACK_EFFECT_CUSTOM_0;

	private AttackComponent _attackComponent;

	private Marker2D _attackMarker;

	private double _fireInterval = 2.0;

	private static PackedScene SPIKE_SHROOM_ATTACK_EFFECT => _SPIKE_SHROOM_ATTACK_EFFECT ?? (_SPIKE_SHROOM_ATTACK_EFFECT = GD.Load<PackedScene>("uid://b7rfl7fns2fe2"));

	private static PackedScene SPIKE_SHROOM_ATTACK_EFFECT_CUSTOM_0 => _SPIKE_SHROOM_ATTACK_EFFECT_CUSTOM_0 ?? (_SPIKE_SHROOM_ATTACK_EFFECT_CUSTOM_0 = GD.Load<PackedScene>("uid://bac4csfsmibf0"));

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
				_attackComponent.attackInterval = _fireInterval;
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			_attackComponent.OnAttack += Attack;
			_attackMarker = GetNode<Marker2D>("%AttackMarker");
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

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		switch (damagePointName)
		{
		case "Damage0":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("SpikeShroom_skin3_1.png", "uid://setqd0doysia");
				base.sprite.SetAtlasReplace("SpikeShroom_skin4_1.png", "uid://oupumw7ivoj2");
				base.sprite.SetAtlasReplace("SpikeShroom_skin6_1.png", "uid://b4c2b841mg3hk");
			}
			break;
		case "Damage1":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("SpikeShroom_skin3_1.png", "uid://dtaq2xxqdn4l");
				base.sprite.SetAtlasReplace("SpikeShroom_skin4_1.png", "uid://blsvhscujc3wy");
				base.sprite.SetAtlasReplace("SpikeShroom_skin6_1.png", "uid://d3m54y244cyum");
			}
			break;
		case "Damage2":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("SpikeShroom_skin3_1.png", "uid://d345b8j5t8rim");
				base.sprite.SetAtlasReplace("SpikeShroom_skin4_1.png", "uid://dab5sqapb1pxr");
				base.sprite.SetAtlasReplace("SpikeShroom_skin6_1.png", "uid://d35grguid2x18");
			}
			break;
		}
	}

	public void Attack()
	{
		AudioManager.Instance.AudioPlay("Spike");
		_attackComponent.AttackEventExecute();
		PackedScene scene = SPIKE_SHROOM_ATTACK_EFFECT;
		if (base.currentCustom.Contains("Custom0"))
		{
			scene = SPIKE_SHROOM_ATTACK_EFFECT_CUSTOM_0;
		}
		TowerDefenseEffectSpriteOnce node = TowerDefenseManager.CreateEffectSpriteOnce(scene, base.gridPos, "Idle");
		_attackMarker.AddChild(node, forceReadableName: false, InternalMode.Disabled);
	}

	public override void AttackDeal(TowerDefenseCharacter character, string type, double num)
	{
		base.AttackDeal(character, type, num);
		if (GodotObject.IsInstanceValid(character))
		{
			if (type != "Eat")
			{
				character.Hurt(Mathf.Min(1000.0, num), playSplatAudio: false);
			}
			else
			{
				character.Hurt(Mathf.Min(80.0, num / 3.0), playSplatAudio: false);
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "fireInterval", fireInterval } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireInterval = (double)data.GetValueOrDefault("fireInterval", 2.0);
	}
}
