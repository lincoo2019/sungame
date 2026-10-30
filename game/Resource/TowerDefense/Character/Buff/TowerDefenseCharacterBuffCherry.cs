using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterBuffCherry : TowerDefenseCharacterBuffConfig
{
	private static PackedScene _CHERRY_BOMB_EXPLOSION;

	private static readonly Color CHERRY_COLOR = new Color(1f, 0.628f, 0.571f);

	[Export(PropertyHint.None, "")]
	public double time = 3.0;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	private static PackedScene CHERRY_BOMB_EXPLOSION => _CHERRY_BOMB_EXPLOSION ?? (_CHERRY_BOMB_EXPLOSION = GD.Load<PackedScene>("uid://cibtjjjomdxnh"));

	public override Color FrameMeshColorMultiplier => CHERRY_COLOR;

	public override void _Init()
	{
		key = "Cherry";
	}

	public override void Enter()
	{
		if ((character.instance.unUseBuffFlags & 0x400) != 0)
		{
			character.buff.DeleteBuff("Cherry");
		}
	}

	public override bool Step(double delta)
	{
		currentTime += delta;
		return currentTime >= time;
	}

	public override void StepReadOnlyClient(double delta)
	{
		Step(delta);
	}

	public override void Exit()
	{
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
		time = Mathf.Max(time, ((TowerDefenseCharacterBuffCherry)config).time + GD.RandRange(-0.2, 0.2));
		currentTime = 0.0;
	}

	public override void Destroy()
	{
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(CHERRY_BOMB_EXPLOSION, character.gridPos);
		characterNode.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, Node.InternalMode.Disabled);
		Vector2 pos = (towerDefenseEffectParticlesOnce.GlobalPosition = character.GetLogicalGlobalPosition());
		TowerDefenseCharacterEventExplodeHurt towerDefenseCharacterEventExplodeHurt = new TowerDefenseCharacterEventExplodeHurt();
		towerDefenseCharacterEventExplodeHurt.num = 100.0;
		TowerDefenseEnum.CHARACTER_CAMP camp = TowerDefenseEnum.CHARACTER_CAMP.ALL;
		if (character.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT)
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE;
		}
		if (character.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT;
		}
		AudioManager.Instance.AudioPlay("ExplodeCherrybomb");
		Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase> { towerDefenseCharacterEventExplodeHurt };
		Array<TowerDefenseCharacter> exclude = new Array<TowerDefenseCharacter>();
		TowerDefenseExplode.CreateExplode(pos, new Vector2(1.4f, 1.4f), eventList, exclude, camp, -1);
	}
}
