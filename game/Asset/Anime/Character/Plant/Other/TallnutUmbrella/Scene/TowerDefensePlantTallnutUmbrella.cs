using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantTallnutUmbrella : TowerDefensePlant
{
	private BlockComponent _blockComponent;

	private bool _isBlocking;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_blockComponent = componentManager.GetRuntime<BlockComponent>();
			if (_blockComponent != null)
			{
				_blockComponent.OnBlock += BlockCharacter;
				_blockComponent.SetCheckRectangleSize(TowerDefenseManager.Instance.GetMapGridSize() * 3.5f);
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (_blockComponent != null)
		{
			_blockComponent.OnBlock -= BlockCharacter;
		}
		_blockComponent = null;
	}

	public void BlockCharacter()
	{
		if (!_isBlocking)
		{
			_isBlocking = true;
			base.itemLayer = TowerDefenseEnum.LAYER_GROUNDITEM.EFFECT;
			base.sprite.SetAnimation("Block", loop: false, 0.1);
			base.sprite.AddAnimation("Idle", 0.0);
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Block")
		{
			_isBlocking = false;
			base.itemLayer = TowerDefenseEnum.LAYER_GROUNDITEM.PLANT;
		}
	}
}
