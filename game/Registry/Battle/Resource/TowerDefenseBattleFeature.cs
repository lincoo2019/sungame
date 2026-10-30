using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeature : TowerDefenseBattleComponentBase
{
	public override void Destroy()
	{
		base.Destroy();
	}

	public virtual Dictionary SaveFeature()
	{
		return new Dictionary();
	}

	public virtual void LoadFeature(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
	}

	public TowerDefenseBattleProcess GetProcess()
	{
		return control.process;
	}
}
