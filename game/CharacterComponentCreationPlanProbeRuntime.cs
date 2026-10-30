using Godot.Collections;

public sealed class CharacterComponentCreationPlanProbeRuntime : CharacterComponentRuntime
{
	public int Value { get; private set; }

	public override Dictionary ExportComponentSave()
	{
		return new Dictionary { ["value"] = Value };
	}

	public override Dictionary SyncSerialize()
	{
		return ExportComponentSave();
	}

	public override void SyncDeserialize(Dictionary data)
	{
		if (data != null && data.ContainsKey("value"))
		{
			Value = data["value"].AsInt32();
		}
	}
}
