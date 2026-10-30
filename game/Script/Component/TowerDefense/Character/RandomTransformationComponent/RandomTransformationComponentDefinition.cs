using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class RandomTransformationComponentDefinition : CharacterComponentDefinition
{
	[Export(PropertyHint.None, "")]
	public Array<RandomTransformationComponentPacketBankConfig> includePacketBankList { get; set; } = new Array<RandomTransformationComponentPacketBankConfig>();

	[Export(PropertyHint.None, "")]
	public Array<RandomTransformationComponentPacketBankConfig> excludePacketBankList { get; set; } = new Array<RandomTransformationComponentPacketBankConfig>();

	[Export(PropertyHint.None, "")]
	public Array<string> includePacketNameList { get; set; } = new Array<string>();

	[Export(PropertyHint.None, "")]
	public Array<string> excludePacketNameList { get; set; } = new Array<string>();

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new RandomTransformationComponent();
	}
}
