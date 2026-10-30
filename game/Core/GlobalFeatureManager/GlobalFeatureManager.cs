using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class GlobalFeatureManager : Node
{
	private const string LegacyShovelUnlockLevel = "Level1_5";

	private GameSaveManager _saveManager;

	public static GlobalFeatureManager Instance { get; private set; }

	public event Action<string, int, int> FeatureChanged;

	public event Action FeaturesReloaded;

	public override void _Ready()
	{
		Instance = this;
		_saveManager = GameSaveManager.Instance;
		if (_saveManager == null)
		{
			GD.PushError("GlobalFeatureManager requires GameSaveManager to be loaded first.");
			return;
		}
		_saveManager.FeatureValueChanged += OnFeatureValueChanged;
		_saveManager.UserChanged += OnUserChanged;
		MigrateLegacyFeatures();
	}

	public override void _ExitTree()
	{
		if (_saveManager != null)
		{
			_saveManager.FeatureValueChanged -= OnFeatureValueChanged;
			_saveManager.UserChanged -= OnUserChanged;
		}
		if (Instance == this)
		{
			Instance = null;
		}
		base._ExitTree();
	}

	public bool IsRegistered(string featureId)
	{
		if (!string.IsNullOrWhiteSpace(featureId) && _saveManager != null)
		{
			return _saveManager.HasFeatureDefinition(featureId);
		}
		return false;
	}

	public int GetValue(string featureId)
	{
		if (!ValidateFeature(featureId))
		{
			return 0;
		}
		return _saveManager.GetFeatureValue(featureId);
	}

	public bool IsUnlocked(string featureId)
	{
		return GetValue(featureId) > 0;
	}

	public bool Unlock(string featureId, bool saveImmediately = true)
	{
		if (!ValidateFeature(featureId))
		{
			return false;
		}
		if (_saveManager.GetFeatureValue(featureId) > 0)
		{
			return false;
		}
		return SetValue(featureId, 1, saveImmediately);
	}

	public bool Lock(string featureId, bool saveImmediately = true)
	{
		return SetValue(featureId, 0, saveImmediately);
	}

	public bool SetValue(string featureId, int value, bool saveImmediately = true)
	{
		if (!ValidateFeature(featureId))
		{
			return false;
		}
		if (_saveManager.GetFeatureValue(featureId) == value)
		{
			return false;
		}
		_saveManager.SetFeatureValue(featureId, value);
		if (saveImmediately)
		{
			_saveManager.Save();
		}
		return true;
	}

	public bool AddValue(string featureId, int amount, bool saveImmediately = true)
	{
		if (!ValidateFeature(featureId))
		{
			return false;
		}
		return SetValue(featureId, _saveManager.GetFeatureValue(featureId) + amount, saveImmediately);
	}

	private bool ValidateFeature(string featureId)
	{
		if (_saveManager == null || string.IsNullOrWhiteSpace(featureId))
		{
			return false;
		}
		if (_saveManager.GetUserCurrent() == "")
		{
			return false;
		}
		if (_saveManager.HasFeatureDefinition(featureId))
		{
			return true;
		}
		GD.PushError("Global feature '" + featureId + "' is not registered in FeatureInit.json.");
		return false;
	}

	private void OnFeatureValueChanged(string featureId, int oldValue, int newValue)
	{
		this.FeatureChanged?.Invoke(featureId, oldValue, newValue);
	}

	private void OnUserChanged(string _user)
	{
		MigrateLegacyFeatures();
		this.FeaturesReloaded?.Invoke();
	}

	private void MigrateLegacyFeatures()
	{
		if (_saveManager != null && !(_saveManager.GetUserCurrent() == "") && !IsUnlocked("Shovel") && _saveManager.GetLevelValue("Level1_5").GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary()
			.GetValueOrDefault("Finish", 0)
			.AsInt32() > 0)
		{
			Unlock("Shovel");
		}
	}

	public GlobalFeatureManager()
	{
		StartupLoadDiagnostics.Mark("autoload.constructed/GlobalFeatureManager");
	}
}
