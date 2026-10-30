using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseBattleDependenceData : Resource
{
	public Array<StringName> FeatureNames = new Array<StringName>();

	public Array<StringName> ConfiguredOptionalFeatureNames = new Array<StringName>();

	public Array<StringName> RequiredFeatureNames => FeatureNames ?? (FeatureNames = new Array<StringName>());
}
