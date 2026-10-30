using Godot;

[GlobalClass]
[Tool]
public partial class ShowHealthComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public PackedScene viewScene;

	[Export(PropertyHint.None, "")]
	public string textTemplate = "HP:{0}/{1}";

	[Export(PropertyHint.Range, "0,3,1")]
	public int decimalPlaces;

	[Export(PropertyHint.None, "")]
	public bool roundHitpoints = true;

	[Export(PropertyHint.None, "")]
	public bool showBody = true;

	[Export(PropertyHint.None, "")]
	public bool showSecondaryArmor = true;

	[Export(PropertyHint.None, "")]
	public bool showHelmet = true;

	[Export(PropertyHint.None, "")]
	public ShowHealthComponent.SecondaryArmorPriority secondaryArmorPriority;

	[Export(PropertyHint.None, "")]
	public bool keepScreenAligned = true;

	[Export(PropertyHint.None, "")]
	public int displayZIndex = 10;

	[Export(PropertyHint.None, "")]
	public Color shieldColor = new Color(0f, 0.992157f, 1f);

	[Export(PropertyHint.None, "")]
	public Color helmetColor = Colors.Yellow;

	[Export(PropertyHint.None, "")]
	public Color bodyColor = Colors.Red;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new ShowHealthComponent();
	}
}
