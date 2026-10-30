using Godot;

[GlobalClass]
public partial class TutorialEnum : RefCounted
{
	public static TutorialConditionConfig GetCondition(string conditionName)
	{
		if (!(conditionName == "CheckCharacterNum"))
		{
			if (conditionName == "CheckSunCollect")
			{
				return new TutorialConditionCheckSunCollect();
			}
			return null;
		}
		return new TutorialConditionCheckCharaterNum();
	}
}
