using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.Core;

public partial class XWEditorSettings : RefCounted
{
	[Signal]
	public delegate void SettingsChangedEventHandler();

	private const string SavePath = "user://ModEditor/editor_settings.cfg";

	private readonly ConfigFile _config = new ConfigFile();

	private readonly Dictionary<string, Variant> _cache = new Dictionary<string, Variant>();

	public void Load()
	{
		if (_config.Load("user://ModEditor/editor_settings.cfg") != Error.Ok)
		{
			return;
		}
		_cache.Clear();
		string[] sections = _config.GetSections();
		foreach (string text in sections)
		{
			string[] sectionKeys = _config.GetSectionKeys(text);
			foreach (string text2 in sectionKeys)
			{
				string key = text + "/" + text2;
				_cache[key] = _config.GetValue(text, text2);
			}
		}
	}

	public void Save()
	{
		EnsureDirectory();
		_config.Save("user://ModEditor/editor_settings.cfg");
	}

	public void RegisterSetting(string name, Variant defaultValue)
	{
		if (!_cache.ContainsKey(name))
		{
			_cache[name] = defaultValue;
			string[] array = name.Split('/', 2);
			if (array.Length == 2)
			{
				_config.SetValue(array[0], array[1], defaultValue);
			}
			else
			{
				_config.SetValue("editor", name, defaultValue);
			}
		}
	}

	public Variant GetSetting(string name)
	{
		if (!_cache.TryGetValue(name, out var value))
		{
			return default(Variant);
		}
		return value;
	}

	public T GetSetting<[MustBeVariant] T>(string name, T defaultValue = default(T))
	{
		if (!_cache.TryGetValue(name, out var value))
		{
			return defaultValue;
		}
		try
		{
			return value.As<T>();
		}
		catch
		{
			return defaultValue;
		}
	}

	public void SetSetting(string name, Variant value)
	{
		_cache[name] = value;
		string[] array = name.Split('/', 2);
		if (array.Length == 2)
		{
			_config.SetValue(array[0], array[1], value);
		}
		else
		{
			_config.SetValue("editor", name, value);
		}
		EmitSignal(SignalName.SettingsChanged);
	}

	public bool HasSetting(string name)
	{
		return _cache.ContainsKey(name);
	}

	public void InitializeDefaultSettings()
	{
		RegisterSetting("interface/theme", "Default");
		RegisterSetting("interface/language", "zh_CN");
		RegisterSetting("interface/reduced_motion", false);
		RegisterSetting("interface/low_performance_mode", false);
		RegisterSetting("editor/auto_save_interval", 300);
		RegisterSetting("editor/show_toasts", true);
	}

	private void EnsureDirectory()
	{
		if (!DirAccess.DirExistsAbsolute("user://ModEditor"))
		{
			DirAccess.MakeDirAbsolute("user://ModEditor");
		}
	}
}
