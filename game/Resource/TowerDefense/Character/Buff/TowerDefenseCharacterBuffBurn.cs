using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffBurn : TowerDefenseCharacterBuffConfig
{
	[Export(PropertyHint.None, "")]
	public double time = 3.0;

	[Export(PropertyHint.None, "")]
	public double dpsAttack = 100.0;

	[Export(PropertyHint.Enum, "Particles,Sprite")]
	public string splatSceneType = "Particles";

	[Export(PropertyHint.None, "")]
	public PackedScene splatScene;

	[Export(PropertyHint.None, "")]
	public double splatInterval = 1.0;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	[Export(PropertyHint.None, "")]
	public double splatTime;

	public override void _Init()
	{
		key = "Burn";
	}

	public override void Enter()
	{
		splatTime = splatInterval;
	}

	public override bool Step(double delta)
	{
		if (character.IsDie())
		{
			return true;
		}
		currentTime += delta;
		if (StepSplatVisual(delta))
		{
			character.buff.ApplyFireHit();
		}
		character.FlagHurt(dpsAttack * delta, 6, playSplatAudio: false);
		if (!(currentTime > time))
		{
			return character.IsDie();
		}
		return true;
	}

	public override void StepReadOnlyClient(double delta)
	{
		if (!character.IsDie())
		{
			currentTime += delta;
			StepSplatVisual(delta);
		}
	}

	private bool StepSplatVisual(double delta)
	{
		if (splatScene == null)
		{
			return false;
		}
		splatTime += delta;
		if (splatTime <= splatInterval)
		{
			return false;
		}
		string text = splatSceneType;
		Node2D node2D = ((text == "Particles") ? TowerDefenseManager.CreateEffectParticlesOnce(splatScene, character.gridPos) : ((!(text == "Sprite")) ? ((TowerDefenseEffectBase)TowerDefenseManager.CreateEffectParticlesOnce(splatScene, character.gridPos)) : ((TowerDefenseEffectBase)TowerDefenseManager.CreateEffectSpriteOnce(splatScene, character.gridPos))));
		TowerDefenseManager.GetCharacterNode().AddChild(node2D, forceReadableName: false, Node.InternalMode.Disabled);
		node2D.GlobalPosition = character.GetLogicalGlobalPosition();
		splatTime = 0.0;
		return true;
	}

	public override void Exit()
	{
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
		if (config is TowerDefenseCharacterBuffBurn towerDefenseCharacterBuffBurn)
		{
			time = Mathf.Max(time, towerDefenseCharacterBuffBurn.time);
			dpsAttack = towerDefenseCharacterBuffBurn.dpsAttack;
			splatSceneType = towerDefenseCharacterBuffBurn.splatSceneType;
			splatScene = towerDefenseCharacterBuffBurn.splatScene;
			splatInterval = towerDefenseCharacterBuffBurn.splatInterval;
			splatTime = Mathf.Min(splatTime, splatInterval);
			currentTime = 0.0;
		}
	}
}
