using Godot;

namespace PVZHE.ModEditor.Core;

public partial class XWEditorData : RefCounted
{
	private const string SavePath = "user://ModEditor/editor_data.cfg";

	private ConfigFile _config = new ConfigFile();

	private string _editedScenePath = "";

	public string EditedScenePath
	{
		get
		{
			return _editedScenePath;
		}
		set
		{
			_editedScenePath = value;
			_config.SetValue("editor", "edited_scene_path", value);
		}
	}

	public void Load()
	{
		if (_config.Load("user://ModEditor/editor_data.cfg") == Error.Ok)
		{
			_editedScenePath = (string)_config.GetValue("editor", "edited_scene_path", "");
		}
	}

	public void Save()
	{
		EnsureDirectory();
		_config.Save("user://ModEditor/editor_data.cfg");
	}

	public void SetValue(string section, string key, Variant value)
	{
		_config.SetValue(section, key, value);
	}

	public Variant GetValue(string section, string key, Variant defaultValue = default(Variant))
	{
		return _config.GetValue(section, key, defaultValue);
	}

	public bool HasSection(string section)
	{
		return _config.HasSection(section);
	}

	public bool HasSectionKey(string section, string key)
	{
		return _config.HasSectionKey(section, key);
	}

	private void EnsureDirectory()
	{
		if (!DirAccess.DirExistsAbsolute("user://ModEditor"))
		{
			DirAccess.MakeDirAbsolute("user://ModEditor");
		}
	}
}
