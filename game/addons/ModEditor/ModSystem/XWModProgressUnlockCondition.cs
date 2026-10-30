using Godot;

namespace PVZHE.ModEditor.ModSystem;

[GlobalClass]
public partial class XWModProgressUnlockCondition : UnlockConditionBaseConfig
{
	[Export(PropertyHint.None, "")]
	public string modId = "";

	[Export(PropertyHint.None, "")]
	public string catalogKey = "";

	[Export(PropertyHint.None, "")]
	public string levelSaveKey = "";

	[Export(PropertyHint.Range, "1,1000,1")]
	public int requiredFinishCount = 1;

	public override bool Check()
	{
		if (!string.IsNullOrWhiteSpace(modId) && !string.IsNullOrWhiteSpace(catalogKey) && !string.IsNullOrWhiteSpace(levelSaveKey))
		{
			return XWModPlayerProgressService.FinishCount(new XWModLevelIdentity(modId, catalogKey, levelSaveKey, "Normal")) >= requiredFinishCount;
		}
		return false;
	}
}
