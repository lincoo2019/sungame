using Godot;

public partial class LevelEditorQuizLevelItem : Control
{
	private Label _nameLabel;

	private TextureRect _mapTexture;

	public string map;

	public TowerDefenseLevelConfig levelConfig;

	public void Init(string _map)
	{
		_nameLabel = GetNode<Label>("%NameLabel");
		_mapTexture = GetNode<TextureRect>("%MapTexture");
		map = _map;
		TowerDefenseMapConfig mapConfig = TowerDefenseManager.Instance.GetMapConfig(map);
		TowerDefenseMapConfig.ApplyMapPreviewTexture(_mapTexture, mapConfig?.GetMapThumbnail());
		_nameLabel.Text = mapConfig?.translate ?? "";
	}
}
