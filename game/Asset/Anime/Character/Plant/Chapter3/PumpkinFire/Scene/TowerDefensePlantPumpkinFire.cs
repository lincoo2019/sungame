using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantPumpkinFire : TowerDefensePlant
{
	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> allEventList = new Array<TowerDefenseCharacterEventBase>();

	private PointLight2D light;

	public int coldCheckInterval = 2;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			light = GetNode<PointLight2D>("%Light");
			if (!inGame && !editorMapPreviewMode)
			{
				((PumpkinFireSprite)base.sprite).back.ZIndex = 0;
			}
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			base.BatchUpdate(delta);
			if ((ulong)((long)TowerDefenseProcessModeDispatch.CurrentPhysicsFrame + (long)randFreshIndex) % 30uL == 0L)
			{
				light.Visible = TowerDefenseManager.GetMapIsNight() && GameSaveManager.Instance.GetConfigValue("MapEffect").AsBool();
			}
			if (coldCheckInterval > 0)
			{
				coldCheckInterval--;
				return;
			}
			TowerDefenseExplode.CreateExplode(GetLogicalGlobalPosition(), new Vector2(0.25f, 0.25f), allEventList, null, TowerDefenseEnum.CHARACTER_CAMP.ALL, -1);
			coldCheckInterval = 2;
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "coldCheckInterval", coldCheckInterval } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		coldCheckInterval = data.GetValueOrDefault("coldCheckInterval", 2).AsInt32();
	}
}
