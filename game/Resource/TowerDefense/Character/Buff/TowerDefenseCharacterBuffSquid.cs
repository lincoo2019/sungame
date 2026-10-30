using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffSquid : TowerDefenseCharacterBuffConfig
{
	private static readonly Color SQUID_COLOR = new Color(0.184f, 0.184f, 0.184f);

	[Export(PropertyHint.None, "")]
	public double time = 15.0;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	public override Color FrameMeshColorMultiplier => SQUID_COLOR;

	public override bool MayModifyIncomingDamage => true;

	public override void _Init()
	{
		key = "Squid";
	}

	public override void Enter()
	{
		if ((character.instance.unUseBuffFlags & 0x800) != 0)
		{
			character.buff.DeleteBuff("Squid");
		}
		else
		{
			character.buff.AddBuff(new TowerDefenseCharacterBuffNormallHit());
		}
	}

	public override bool Step(double delta)
	{
		character.buff.AddBuff(new TowerDefenseCharacterBuffNormallHit());
		character.timeScale = Mathf.Min(character.timeScale, 0.5);
		currentTime += delta;
		return currentTime >= time;
	}

	public override void StepReadOnlyClient(double delta)
	{
		character.timeScale = Mathf.Min(character.timeScale, 0.5);
		currentTime += delta;
	}

	public override void Exit()
	{
	}

	public override double SetAttackNum(double num)
	{
		return num * 2.0;
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
		TowerDefenseCharacterBuffSquid towerDefenseCharacterBuffSquid = config as TowerDefenseCharacterBuffSquid;
		time = Mathf.Max(time, towerDefenseCharacterBuffSquid.time + GD.RandRange(-0.2, 0.2));
		currentTime = 0.0;
	}
}
