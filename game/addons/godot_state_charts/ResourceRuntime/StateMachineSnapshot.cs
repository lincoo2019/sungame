using Godot;
using Godot.Collections;

[GlobalClass]
[Tool]
public partial class StateMachineSnapshot : Resource
{
	[Export(PropertyHint.None, "")]
	public string DefinitionId { get; set; } = string.Empty;

	[Export(PropertyHint.None, "")]
	public int SchemaVersion { get; set; }

	[Export(PropertyHint.None, "")]
	public string ContentHash { get; set; } = string.Empty;

	[Export(PropertyHint.None, "")]
	public Array<string> ActiveStateIds { get; set; } = new Array<string>();

	[Export(PropertyHint.None, "")]
	public Array<string> HistoryStateIds { get; set; } = new Array<string>();

	[Export(PropertyHint.None, "")]
	public Array<string> PendingTransitionIds { get; set; } = new Array<string>();

	[Export(PropertyHint.None, "")]
	public Dictionary<string, double> PendingDelayRemaining { get; set; } = new Dictionary<string, double>();

	[Export(PropertyHint.None, "")]
	public Dictionary<StringName, Variant> ExpressionProperties { get; set; } = new Dictionary<StringName, Variant>();

	[Export(PropertyHint.None, "")]
	public long Revision { get; set; }

	public static bool IsSupportedExpressionVariant(Variant.Type type)
	{
		if ((ulong)type <= 22uL)
		{
			switch ((int)type)
			{
			case 0:
				return true;
			case 1:
				return true;
			case 2:
				return true;
			case 3:
				return true;
			case 4:
				return true;
			case 21:
				return true;
			case 22:
				return true;
			case 5:
				return true;
			case 6:
				return true;
			case 7:
				return true;
			case 8:
				return true;
			case 9:
				return true;
			case 10:
				return true;
			case 11:
				return true;
			case 12:
				return true;
			case 13:
				return true;
			case 14:
				return true;
			case 15:
				return true;
			case 16:
				return true;
			case 17:
				return true;
			case 18:
				return true;
			case 19:
				return true;
			case 20:
				return true;
			}
		}
		return false;
	}
}
