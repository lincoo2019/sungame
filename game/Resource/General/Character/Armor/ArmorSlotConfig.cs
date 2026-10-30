using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class ArmorSlotConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string armorName = "";

	[Export(PropertyHint.Enum, "Media,Sprite")]
	public string replaceMethod = "Media";

	[Export(PropertyHint.None, "")]
	public StringName replaceMediaName = "";

	[Export(PropertyHint.None, "")]
	public NodePath slotPath = new NodePath();

	[Export(PropertyHint.None, "")]
	public Vector2 offset = Vector2.Zero;

	[Export(PropertyHint.None, "")]
	public double rotation;

	[Export(PropertyHint.None, "")]
	public Vector2 scale = Vector2.One;

	[Export(PropertyHint.Range, "0,2,0.05,or_greater")]
	public float alphaMultiplier = 1f;

	private string _openFliter = "";

	private string _closeFliter = "";

	private string _destroyFliter = "";

	[Export(PropertyHint.None, "")]
	public double damagePoint = -1.0;

	[ExportCategory("Behavior")]
	[Export(PropertyHint.None, "")]
	public Array<StringName> behaviorIds = new Array<StringName>();

	[Export(PropertyHint.None, "")]
	public Array<ArmorBehaviorDefinition> behaviors = new Array<ArmorBehaviorDefinition>();

	[Export(PropertyHint.MultilineText, "")]
	public string openFliter
	{
		get
		{
			return _openFliter;
		}
		set
		{
			_openFliter = value;
			EmitChanged();
		}
	}

	[Export(PropertyHint.MultilineText, "")]
	public string closeFliter
	{
		get
		{
			return _closeFliter;
		}
		set
		{
			_closeFliter = value;
			EmitChanged();
		}
	}

	[Export(PropertyHint.MultilineText, "")]
	public string destroyFliter
	{
		get
		{
			return _destroyFliter;
		}
		set
		{
			_destroyFliter = value;
			EmitChanged();
		}
	}
}
