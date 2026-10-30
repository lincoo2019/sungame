using System.Collections.Generic;
using Godot;

public partial class PeriodicProbeEvent : TowerDefenseCharacterEventBase
{
	public static readonly List<string> Order = new List<string>();

	private readonly string _name;

	public PeriodicProbeEvent(string name)
	{
		_name = name;
	}

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Order.Add(_name);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Order.Add(_name);
	}
}
