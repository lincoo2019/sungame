using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantBowlingFirenut : TowerDefensePlantBowlingBase, INetworkSpawnStateReceiver
{
	private string _spawnDamagePoint = "";

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			return;
		}
		if (base.config.customData != null)
		{
			string text = GameSaveManager.Instance.GetTowerDefensePacketValue("PlantFirenut").GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary()
				.GetValueOrDefault("Custom", "")
				.AsString();
			if (text != "")
			{
				base.currentCustom = new Array<string> { text };
			}
		}
		ApplySpawnDamagePoint();
	}

	public void ImportNetworkSpawnState(Dictionary data)
	{
		ApplyNetworkDamagePoint(data);
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		return new Dictionary { ["damagePoint"] = _spawnDamagePoint };
	}

	public override void ImportNetworkSpecialState(Dictionary data)
	{
		ApplyNetworkDamagePoint(data);
	}

	public override int GetNetworkSpecialStateRevision()
	{
		return (!(_spawnDamagePoint == "")) ? 1 : 0;
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { ["spawnDamagePoint"] = _spawnDamagePoint };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		_spawnDamagePoint = data?.GetValueOrDefault("spawnDamagePoint", _spawnDamagePoint).AsString() ?? _spawnDamagePoint;
		if (IsNodeReady())
		{
			ApplySpawnDamagePoint();
		}
	}

	private void ApplyNetworkDamagePoint(Dictionary data)
	{
		_spawnDamagePoint = data?.GetValueOrDefault("damagePoint", "").AsString() ?? "";
		if (IsNodeReady())
		{
			ApplySpawnDamagePoint();
		}
	}

	private void ApplySpawnDamagePoint()
	{
		if (!(_spawnDamagePoint == "") && GodotObject.IsInstanceValid(base.config?.damagePointData) && GodotObject.IsInstanceValid(base.sprite))
		{
			base.config.damagePointData.SetDamagePointFliters(base.sprite, _spawnDamagePoint);
		}
	}
}
