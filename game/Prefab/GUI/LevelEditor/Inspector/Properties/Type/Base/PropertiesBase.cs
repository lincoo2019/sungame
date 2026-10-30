using Godot;

[GlobalClass]
public partial class PropertiesBase : PanelContainer
{
	public delegate void DeleteEventHandler(string propertyName, string key);

	private Button _closeButton;

	public LevelEditorInspector inspector;

	public LevelEditorPropertiesContainer container;

	public GodotObject obj;

	public string propertyName = "";

	public PropertiesBox propertiesBox;

	public string key;

	public string path = "";

	public bool isTargetObj = true;

	public bool showCloseButton = true;

	public event DeleteEventHandler OnDelete;

	public override void _Ready()
	{
		_closeButton = GetNode<Button>("%CloseButton");
		_closeButton.Pressed += CloseButtonPressed;
	}

	public override void _Process(double delta)
	{
		if (base.Visible && propertiesBox != null && obj != null && propertyName != "" && CanSetValue())
		{
			Variant value = GetValue();
			propertiesBox.set_value(key, value);
			if (_closeButton != null)
			{
				_closeButton.Visible = showCloseButton && !isTargetObj;
			}
		}
	}

	public virtual bool CanSetValue()
	{
		return true;
	}

	public virtual string _GetType()
	{
		return "";
	}

	public virtual string GetObjName()
	{
		if (!isTargetObj)
		{
			return "";
		}
		return obj.Get("name").AsString();
	}

	public string GetPropertyNameString()
	{
		Variant indexed = obj.GetIndexed(propertyName);
		if (isTargetObj)
		{
			return key;
		}
		if (indexed.VariantType == Variant.Type.Dictionary)
		{
			return propertyName + "[" + key + "]";
		}
		if (indexed.VariantType == Variant.Type.Array)
		{
			return propertyName + "[" + key + "]";
		}
		return "";
	}

	public string GetPropertyName()
	{
		return propertyName;
	}

	public string GetKey()
	{
		return key;
	}

	public Variant GetValue()
	{
		Variant indexed = obj.GetIndexed(propertyName);
		if (isTargetObj)
		{
			return indexed;
		}
		if (indexed.VariantType == Variant.Type.Dictionary)
		{
			return indexed.AsGodotDictionary()[key];
		}
		if (indexed.VariantType == Variant.Type.Array)
		{
			return indexed.AsGodotArray()[key.ToInt()];
		}
		return default(Variant);
	}

	public void CloseButtonPressed()
	{
		if (!isTargetObj)
		{
			Variant indexed = obj.GetIndexed(propertyName);
			if (indexed.VariantType == Variant.Type.Dictionary)
			{
				indexed.AsGodotDictionary().Remove(key);
				this.OnDelete?.Invoke(propertyName, key);
			}
			if (indexed.VariantType == Variant.Type.Array)
			{
				indexed.AsGodotArray().RemoveAt(key.ToInt());
				this.OnDelete?.Invoke(propertyName, key.ToInt().ToString());
			}
			propertiesBox.delete_value(key);
		}
	}
}
