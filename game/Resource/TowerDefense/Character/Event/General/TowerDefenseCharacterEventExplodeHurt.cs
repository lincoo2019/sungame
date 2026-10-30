using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterEventExplodeHurt : TowerDefenseCharacterEventBase
{
	private string _type = "Bomb";

	private TowerDefenseEnum.EXPLOSION_DAMAGE_KIND _damageKind = TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB;

	[Export(PropertyHint.None, "")]
	public double num = 1800.0;

	[Export(PropertyHint.None, "")]
	public bool burns = true;

	[Export(PropertyHint.Enum, "Bomb,Jala,Mine")]
	public string type
	{
		get
		{
			return _type;
		}
		set
		{
			_type = value ?? "Bomb";
			_damageKind = ResolveDamageKind(_type);
		}
	}

	internal TowerDefenseEnum.EXPLOSION_DAMAGE_KIND DamageKind => _damageKind;

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(pos, _damageKind, target, num);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(pos, _damageKind, target, num * delta);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(projectile.GlobalPosition, _damageKind, target, num);
	}

	public static void Run(Vector2 pos, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND damageKind, TowerDefenseCharacter target, double num = 20.0)
	{
		Vector2 velocity = new Vector2((float)((double)(target.GetLogicalGlobalPosition().X - pos.X) * GD.RandRange(3.0, 6.0)), -1000f);
		target.ExplodeHurt(num, damageKind, playSplatAudio: true, velocity);
		target.AttackDeal(null, "Explode", num);
	}

	private static TowerDefenseEnum.EXPLOSION_DAMAGE_KIND ResolveDamageKind(string type)
	{
		return type switch
		{
			"Bomb" => TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, 
			"Jala" => TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.JALA, 
			"Mine" => TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.MINE, 
			_ => TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.OTHER, 
		};
	}
}
