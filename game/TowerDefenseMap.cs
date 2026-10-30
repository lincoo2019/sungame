using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseMap : Node2D
{
	private const string RevealStateKey = "revealNodeVisibility";

	private const string ShaderTimeStateKey = "shaderTime";

	private const int MaxEventFunctionArguments = 10;

	private const int MaxRejectedEventFunctionWarnings = 64;

	private static readonly ConcurrentDictionary<(Type MapType, string FunctionName, int ArgumentCount), byte> RejectedEventFunctionWarnings = new ConcurrentDictionary<(Type, string, int), byte>();

	private IReadOnlyDictionary<string, int> _eventFunctions;

	private readonly List<CanvasItem> _enterRoomRevealNodes = new List<CanvasItem>();

	private readonly List<ShaderMaterial> _shaderTimeMaterials = new List<ShaderMaterial>();

	private double _shaderTime;

	[Export(PropertyHint.None, "")]
	public Node2D stripe;

	[Export(PropertyHint.None, "")]
	public GradientTexture1D canvasModulateGradient;

	[ExportGroup("Enter Room", "")]
	[Export(PropertyHint.None, "")]
	public double enterRoomTargetY = 375.0;

	[Export(PropertyHint.None, "")]
	public double enterRoomMoveSpeed = 200.0;

	[Export(PropertyHint.None, "")]
	public double enterRoomShadowOffsetY = 36.0;

	[Export(PropertyHint.None, "")]
	public double enterRoomCharacterSpeedMultiplier = 2.0;

	[Export(PropertyHint.None, "")]
	public double enterRoomSettleDuration = 3.0;

	[Export(PropertyHint.MultilineText, "")]
	public string enterRoomRevealNodeNames = "";

	[ExportGroup("Shader Time", "")]
	[Export(PropertyHint.MultilineText, "")]
	public string shaderTimeNodeNames = "";

	[Export(PropertyHint.None, "")]
	public StringName shaderTimeParameter = "timer";

	[Export(PropertyHint.None, "")]
	public double waveSpeed = 0.1;

	public override void _Ready()
	{
		ResolveConfiguredNodes();
		ApplyShaderTime();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_shaderTimeMaterials.Count != 0 && double.IsFinite(delta) && double.IsFinite(waveSpeed))
		{
			_shaderTime += delta * waveSpeed;
			if (!double.IsFinite(_shaderTime))
			{
				_shaderTime = 0.0;
			}
			ApplyShaderTime();
		}
	}

	public virtual async Task EnterRoom(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character))
		{
			return;
		}
		double num = (double.IsFinite(enterRoomMoveSpeed) ? Math.Max(0.0, enterRoomMoveSpeed) : 0.0);
		Vector2 logicalGlobalPosition = character.GetLogicalGlobalPosition();
		double num2 = (double.IsFinite(enterRoomTargetY) ? enterRoomTargetY : ((double)logicalGlobalPosition.Y));
		double num3 = Math.Abs((double)logicalGlobalPosition.Y - num2);
		double duration = ((num > 0.0) ? (num3 / num) : 0.0);
		Vector2 vector = new Vector2(logicalGlobalPosition.X, (float)num2);
		Tween tween = CreateTween().SetParallel();
		tween.TweenMethod(Callable.From<Vector2>(character.SetLogicalGlobalPosition), logicalGlobalPosition, vector, duration);
		ShadowComponent shadowComponent = character.shadowComponent;
		if (shadowComponent != null && !shadowComponent.IsReleased)
		{
			double num4 = (double.IsFinite(enterRoomShadowOffsetY) ? enterRoomShadowOffsetY : 0.0);
			character.shadowComponent.TweenSaveShadowPositionY(tween, (float)(num2 + num4), duration);
		}
		await ToSignal(tween, Tween.SignalName.Finished);
		if (GodotObject.IsInstanceValid(character) && double.IsFinite(enterRoomCharacterSpeedMultiplier))
		{
			character.timeScaleInit *= enterRoomCharacterSpeedMultiplier;
		}
		foreach (CanvasItem enterRoomRevealNode in _enterRoomRevealNodes)
		{
			if (GodotObject.IsInstanceValid(enterRoomRevealNode))
			{
				enterRoomRevealNode.Visible = true;
			}
		}
		double num5 = (double.IsFinite(enterRoomSettleDuration) ? Math.Max(0.0, enterRoomSettleDuration) : 0.0);
		if (num5 > 0.0 && IsInsideTree())
		{
			await ToSignal(GetTree().CreateTimer(num5, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		}
	}

	public virtual Dictionary SaveMapBase()
	{
		Dictionary dictionary = new Dictionary();
		if (_enterRoomRevealNodes.Count > 0)
		{
			Dictionary dictionary2 = new Dictionary();
			foreach (CanvasItem enterRoomRevealNode in _enterRoomRevealNodes)
			{
				if (GodotObject.IsInstanceValid(enterRoomRevealNode))
				{
					dictionary2[GetPathTo(enterRoomRevealNode).ToString()] = enterRoomRevealNode.Visible;
				}
			}
			dictionary["revealNodeVisibility"] = dictionary2;
		}
		if (_shaderTimeMaterials.Count > 0)
		{
			dictionary["shaderTime"] = _shaderTime;
		}
		return dictionary;
	}

	public virtual void LoadMapBase(Dictionary data)
	{
		if (data == null)
		{
			return;
		}
		foreach (KeyValuePair<Variant, Variant> item in data.GetValueOrDefault("revealNodeVisibility", new Dictionary()).AsGodotDictionary())
		{
			CanvasItem nodeOrNull = GetNodeOrNull<CanvasItem>(item.Key.AsString());
			if (GodotObject.IsInstanceValid(nodeOrNull))
			{
				nodeOrNull.Visible = item.Value.AsBool();
			}
		}
		double num = data.GetValueOrDefault("shaderTime", _shaderTime).AsDouble();
		_shaderTime = (double.IsFinite(num) ? num : 0.0);
		ApplyShaderTime();
	}

	public virtual bool CanSaveProgress(out string reason)
	{
		reason = "";
		return true;
	}

	protected void ConfigureEnterRoomRevealNodes(params string[] uniqueNodeNames)
	{
		enterRoomRevealNodeNames = JoinNodeNames(uniqueNodeNames);
	}

	protected void ConfigureShaderTimeNodes(params string[] uniqueNodeNames)
	{
		shaderTimeNodeNames = JoinNodeNames(uniqueNodeNames);
	}

	private void ResolveConfiguredNodes()
	{
		_enterRoomRevealNodes.Clear();
		_shaderTimeMaterials.Clear();
		foreach (string item2 in ParseNodeNames(enterRoomRevealNodeNames))
		{
			CanvasItem nodeOrNull = GetNodeOrNull<CanvasItem>(new NodePath("%" + item2));
			if (GodotObject.IsInstanceValid(nodeOrNull) && !_enterRoomRevealNodes.Contains(nodeOrNull))
			{
				_enterRoomRevealNodes.Add(nodeOrNull);
			}
		}
		foreach (string item3 in ParseNodeNames(shaderTimeNodeNames))
		{
			CanvasItem nodeOrNull2 = GetNodeOrNull<CanvasItem>(new NodePath("%" + item3));
			if (GodotObject.IsInstanceValid(nodeOrNull2) && nodeOrNull2.Material is ShaderMaterial item && !_shaderTimeMaterials.Contains(item))
			{
				_shaderTimeMaterials.Add(item);
			}
		}
	}

	private void ApplyShaderTime()
	{
		foreach (ShaderMaterial shaderTimeMaterial in _shaderTimeMaterials)
		{
			if (GodotObject.IsInstanceValid(shaderTimeMaterial))
			{
				shaderTimeMaterial.SetShaderParameter(shaderTimeParameter, _shaderTime);
			}
		}
	}

	private static IEnumerable<string> ParseNodeNames(string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			string[] array = value.Split(new char[4] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			for (int i = 0; i < array.Length; i++)
			{
				yield return array[i];
			}
		}
	}

	private static string JoinNodeNames(IEnumerable<string> names)
	{
		if (names != null)
		{
			return string.Join(",", names);
		}
		return "";
	}

	public bool CanExecuteEventFunction(string functionName, int argumentCount)
	{
		if (string.IsNullOrEmpty(functionName) || argumentCount < 0 || argumentCount > 10)
		{
			return false;
		}
		if (GetEventFunctions().TryGetValue(functionName, out var value))
		{
			return (value & (1 << argumentCount)) != 0;
		}
		return false;
	}

	public Variant FunctionExecute(string functionName, Godot.Collections.Array variable)
	{
		if (variable == null)
		{
			variable = new Godot.Collections.Array();
		}
		if (!CanExecuteEventFunction(functionName, variable.Count))
		{
			WarnRejectedEventFunction(functionName, variable.Count);
			return default(Variant);
		}
		return variable.Count switch
		{
			0 => Call(functionName), 
			1 => Call(functionName, variable[0]), 
			2 => Call(functionName, variable[0], variable[1]), 
			3 => Call(functionName, variable[0], variable[1], variable[2]), 
			4 => Call(functionName, variable[0], variable[1], variable[2], variable[3]), 
			5 => Call(functionName, variable[0], variable[1], variable[2], variable[3], variable[4]), 
			6 => Call(functionName, variable[0], variable[1], variable[2], variable[3], variable[4], variable[5]), 
			7 => Call(functionName, variable[0], variable[1], variable[2], variable[3], variable[4], variable[5], variable[6]), 
			8 => Call(functionName, variable[0], variable[1], variable[2], variable[3], variable[4], variable[5], variable[6], variable[7]), 
			9 => Call(functionName, variable[0], variable[1], variable[2], variable[3], variable[4], variable[5], variable[6], variable[7], variable[8]), 
			10 => Call(functionName, variable[0], variable[1], variable[2], variable[3], variable[4], variable[5], variable[6], variable[7], variable[8], variable[9]), 
			_ => default(Variant), 
		};
	}

	private IReadOnlyDictionary<string, int> GetEventFunctions()
	{
		if (_eventFunctions != null)
		{
			return _eventFunctions;
		}
		System.Collections.Generic.Dictionary<string, int> dictionary = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
		RegisterEventFunctions(dictionary);
		_eventFunctions = dictionary;
		return _eventFunctions;
	}

	protected virtual void RegisterEventFunctions(IDictionary<string, int> functions)
	{
		RegisterEventFunction(functions, "UseStripe", 1);
		RegisterEventFunction(functions, "ShowShovel", 0);
		RegisterEventFunction(functions, "BackShovel", 0);
		RegisterEventFunction(functions, "ShowGlove", 0);
		RegisterEventFunction(functions, "BackGlove", 0);
		RegisterEventFunction(functions, "CharacterClear", 0);
		RegisterEventFunction(functions, "LineUseSet", 2);
	}

	protected static void RegisterEventFunction(IDictionary<string, int> functions, string functionName, int minimumArgumentCount, int maximumArgumentCount = -1)
	{
		if (functions == null || string.IsNullOrEmpty(functionName))
		{
			throw new ArgumentException("Map event function registration requires a target and a name.");
		}
		if (maximumArgumentCount < 0)
		{
			maximumArgumentCount = minimumArgumentCount;
		}
		if (minimumArgumentCount < 0 || maximumArgumentCount < minimumArgumentCount || maximumArgumentCount > 10)
		{
			throw new ArgumentOutOfRangeException("minimumArgumentCount", $"Map event functions support 0..{10} arguments.");
		}
		functions.TryGetValue(functionName, out var value);
		for (int i = minimumArgumentCount; i <= maximumArgumentCount; i++)
		{
			value |= 1 << i;
		}
		functions[functionName] = value;
	}

	private void WarnRejectedEventFunction(string functionName, int argumentCount)
	{
		if (RejectedEventFunctionWarnings.Count < 64)
		{
			(Type, string, int) key = (GetType(), functionName ?? string.Empty, argumentCount);
			if (RejectedEventFunctionWarnings.TryAdd(key, 0))
			{
				GD.PushWarning($"[Map] Rejected unregistered event function '{functionName}' with {argumentCount} argument(s) on {GetType().Name}.");
			}
		}
	}

	public void UseStripe(int row)
	{
		stripe.Visible = true;
		TowerDefenseManager.GetMapFeature().stripeRow = row;
		stripe.GlobalPosition = new Vector2(TowerDefenseManager.Instance.GetMapCellPos(new Vector2I(row + 1, 1)).X - 10f, stripe.GlobalPosition.Y);
	}

	public void ShowShovel()
	{
		GlobalFeatureManager.Instance?.Unlock("Shovel");
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature) && GodotObject.IsInstanceValid(mapFeature.shovelManager))
		{
			mapFeature.shovelManager.GlobalPosition = new Vector2(mapFeature.shovelManager.GlobalPosition.X, 0f);
			mapFeature.shovelManager.shovelShow = true;
		}
	}

	public void BackShovel()
	{
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature) && GodotObject.IsInstanceValid(mapFeature.shovelManager))
		{
			mapFeature.shovelManager.shovelShow = false;
			mapFeature.shovelManager.Position = new Vector2(mapFeature.shovelManager.Position.X, 0f);
		}
	}

	public void ShowGlove()
	{
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature) && GodotObject.IsInstanceValid(mapFeature.gloveManager))
		{
			mapFeature.gloveManager.GlobalPosition = new Vector2(mapFeature.gloveManager.GlobalPosition.X, 0f);
			mapFeature.gloveManager.gloveShow = true;
		}
	}

	public void BackGlove()
	{
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature) && GodotObject.IsInstanceValid(mapFeature.gloveManager))
		{
			mapFeature.gloveManager.Position = new Vector2(mapFeature.gloveManager.Position.X, 0f);
		}
	}

	public void CharacterClear()
	{
		HashSet<ulong> destroyedInstanceIds = new HashSet<ulong>();
		foreach (Variant item in TowerDefenseManager.Instance.GetCharacter())
		{
			DestroyCharacterOnce(item.AsGodotObject() as TowerDefenseCharacter, destroyedInstanceIds);
		}
		SceneTree tree = TowerDefenseManager.Instance.GetTree();
		if (!GodotObject.IsInstanceValid(tree))
		{
			return;
		}
		foreach (Node item2 in tree.GetNodesInGroup("Gravestone"))
		{
			DestroyCharacterOnce(item2 as TowerDefenseGravestone, destroyedInstanceIds);
		}
	}

	private static void DestroyCharacterOnce(TowerDefenseCharacter character, HashSet<ulong> destroyedInstanceIds)
	{
		if (GodotObject.IsInstanceValid(character) && !character.isDestroy && destroyedInstanceIds.Add(character.GetInstanceId()))
		{
			character.ClearFromMap();
		}
	}

	public void LineUseSet(int line, bool open)
	{
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature))
		{
			mapFeature.SetLineUse(line, open);
		}
	}
}
