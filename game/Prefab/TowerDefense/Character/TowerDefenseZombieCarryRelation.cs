using Godot;
using Godot.Collections;

public static class TowerDefenseZombieCarryRelation
{
	public const string SaveKey = "carryCharacterNodeName";

	public static void Export(Dictionary data, TowerDefenseCharacter carryCharacter)
	{
		if (data != null && GodotObject.IsInstanceValid(carryCharacter))
		{
			data["carryCharacterNodeName"] = carryCharacter.Name;
		}
	}

	public static TowerDefenseZombie Resolve(Dictionary data)
	{
		if (data == null)
		{
			return null;
		}
		string text = data.GetValueOrDefault("carryCharacterNodeName", "").AsString();
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(characterNode))
		{
			return null;
		}
		return characterNode.GetNodeOrNull<TowerDefenseZombie>(new NodePath(text));
	}

	public static bool Attach(TowerDefenseZombie carrier, ref TowerDefenseCharacter carryCharacter, TowerDefenseZombie carryZombie, double carryHeight)
	{
		if (!GodotObject.IsInstanceValid(carrier) || !GodotObject.IsInstanceValid(carryZombie))
		{
			return false;
		}
		if (GodotObject.IsInstanceValid(carryCharacter) && carryCharacter != carryZombie)
		{
			return false;
		}
		if (GodotObject.IsInstanceValid(carryZombie.riderCarryOwner) && carryZombie.riderCarryOwner != carrier)
		{
			return false;
		}
		carryZombie.riderCarryOwner = carrier;
		carryCharacter = carryZombie;
		carryCharacter.SetDeferred("isPause", true);
		carryCharacter.groundHeight = carryHeight;
		carryCharacter.z = carryHeight;
		return true;
	}

	public static bool Release(TowerDefenseZombie carrier, ref TowerDefenseCharacter carryCharacter, double carrierGroundHeight)
	{
		TowerDefenseCharacter towerDefenseCharacter = carryCharacter;
		carryCharacter = null;
		if (!GodotObject.IsInstanceValid(carrier) || !(towerDefenseCharacter is TowerDefenseZombie towerDefenseZombie) || !GodotObject.IsInstanceValid(towerDefenseZombie) || towerDefenseZombie.riderCarryOwner != carrier)
		{
			return false;
		}
		towerDefenseZombie.riderCarryOwner = null;
		towerDefenseCharacter.isGround = false;
		towerDefenseCharacter.groundHeight = carrierGroundHeight;
		towerDefenseZombie.isPause = false;
		towerDefenseZombie.SetDeferred("isPause", false);
		towerDefenseZombie.CallDeferred("WalkReady");
		return true;
	}
}
