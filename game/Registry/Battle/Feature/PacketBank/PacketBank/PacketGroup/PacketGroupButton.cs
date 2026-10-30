using System;
using Godot;

[GlobalClass]
public partial class PacketGroupButton : NinePatchButtonBase
{
	public delegate void SaveGroupEventHandler(int _id);

	public delegate void LoadGroupEventHandler(int _id);

	public LineEdit nameChangeLine;

	[Export(PropertyHint.None, "")]
	public int id = 1;

	public bool pressSave;

	private TextureButton _loadButton;

	private TextureButton _saveButton;

	private SceneTreeTimer _loadDelayTimer;

	private Action _loadDelayHandler;

	public event SaveGroupEventHandler OnSaveGroup;

	public event LoadGroupEventHandler OnLoadGroup;

	public override void OnReady()
	{
		nameChangeLine = GetNode<LineEdit>("%NameChangeLine");
		_loadButton = GetNode<TextureButton>("%TextureButton");
		_saveButton = GetNode<TextureButton>("%SaveButton");
		_loadButton.Pressed += LoadButtonPressed;
		_saveButton.Pressed += SaveButtonPressed;
		nameChangeLine.EditingToggled += NameChangeOver;
		if (!Engine.IsEditorHint())
		{
			Callable.From(LoadSavedName).CallDeferred();
		}
	}

	private void LoadSavedName()
	{
		if (IsInsideTree() && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			if (TowerDefenseManager.Instance.IsIZMMode() || TowerDefenseManager.Instance.IsIZM2Mode())
			{
				labelText.Text = GameSaveManager.Instance.GetKeyValue($"ZombiePacketGroupName{id}").AsString();
			}
			else
			{
				labelText.Text = GameSaveManager.Instance.GetKeyValue($"PacketGroupName{id}").AsString();
			}
		}
	}

	public void SaveButtonPressed()
	{
		this.OnSaveGroup?.Invoke(id);
	}

	public void LoadButtonPressed()
	{
		if (!pressSave)
		{
			pressSave = true;
			ClearLoadDelay();
			_loadDelayTimer = GetTree().CreateTimer(0.2, processAlways: false);
			_loadDelayHandler = OnLoadDelayTimeout;
			_loadDelayTimer.Timeout += _loadDelayHandler;
		}
		else
		{
			pressSave = false;
			ClearLoadDelay();
			labelText.Visible = false;
			nameChangeLine.Visible = true;
			nameChangeLine.Text = labelText.Text;
			nameChangeLine.GrabFocus();
		}
	}

	private void OnLoadDelayTimeout()
	{
		ClearLoadDelay();
		if (IsInsideTree() && pressSave)
		{
			this.OnLoadGroup?.Invoke(id);
			pressSave = false;
		}
	}

	private void ClearLoadDelay()
	{
		if (GodotObject.IsInstanceValid(_loadDelayTimer) && _loadDelayHandler != null)
		{
			_loadDelayTimer.Timeout -= _loadDelayHandler;
		}
		_loadDelayTimer = null;
		_loadDelayHandler = null;
	}

	public void NameChangeOver(bool toggledOn)
	{
		if (!toggledOn)
		{
			labelText.Text = nameChangeLine.Text;
			if (TowerDefenseManager.Instance.IsIZMMode() || TowerDefenseManager.Instance.IsIZM2Mode())
			{
				GameSaveManager.Instance.SetKeyValue($"ZombiePacketGroupName{id}", labelText.Text);
			}
			else
			{
				GameSaveManager.Instance.SetKeyValue($"PacketGroupName{id}", labelText.Text);
			}
			GameSaveManager.Instance.Save();
			labelText.Visible = true;
			nameChangeLine.Visible = false;
		}
	}

	public override void _ExitTree()
	{
		ClearLoadDelay();
		pressSave = false;
		if (GodotObject.IsInstanceValid(_loadButton))
		{
			_loadButton.Pressed -= LoadButtonPressed;
		}
		if (GodotObject.IsInstanceValid(_saveButton))
		{
			_saveButton.Pressed -= SaveButtonPressed;
		}
		if (GodotObject.IsInstanceValid(nameChangeLine))
		{
			nameChangeLine.EditingToggled -= NameChangeOver;
		}
		this.OnSaveGroup = null;
		this.OnLoadGroup = null;
		_loadButton = null;
		_saveButton = null;
		nameChangeLine = null;
		base._ExitTree();
	}
}
