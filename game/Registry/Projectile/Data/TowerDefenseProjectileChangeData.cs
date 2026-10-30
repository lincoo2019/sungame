using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
public partial class TowerDefenseProjectileChangeData : RefCounted
{
	public List<StringName> ChangeList = new List<StringName>();

	public Dictionary<StringName, StringName> ChangeToProjectile = new Dictionary<StringName, StringName>();

	public void AddChange(StringName projectileName, StringName toProjectileName)
	{
		ChangeToProjectile[projectileName] = toProjectileName;
		ChangeList.Add(projectileName);
	}

	public StringName GetChange(StringName projectileName)
	{
		if (!HasChange(projectileName))
		{
			return null;
		}
		return ChangeToProjectile[projectileName];
	}

	public bool HasChange(StringName projectileName)
	{
		return ChangeList.Contains(projectileName);
	}

	public bool HasChangeTarget(StringName projectileName)
	{
		return ChangeToProjectile.Values.Contains(projectileName);
	}

	public bool IsEmpty()
	{
		return ChangeList.Count == 0;
	}
}
