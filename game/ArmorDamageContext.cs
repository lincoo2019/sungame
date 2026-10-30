using Godot;

public struct ArmorDamageContext(TowerDefenseArmorInstance armor, double damage, bool playSplatAudio, Vector2 velocity, bool createDamagePart, bool ignoreLimit)
{
	public TowerDefenseArmorInstance Armor = armor;

	public readonly double IncomingDamage = damage;

	public double Damage = damage;

	public double AppliedDamage = 0.0;

	public double RemainingDamage = damage;

	public bool PlaySplatAudio = playSplatAudio;

	public Vector2 Velocity = velocity;

	public bool CreateDamagePart = createDamagePart;

	public bool IgnoreLimit = ignoreLimit;

	public bool Cancel = false;
}
