using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class CardBehaviorHost
{
	private readonly List<CardBehaviorRuntime> _runtimes = new List<CardBehaviorRuntime>();

	public void Bind(TowerDefenseInGamePacketShow owner, TowerDefensePacketConfig config)
	{
		Release();
		if (!GodotObject.IsInstanceValid(owner) || config == null)
		{
			return;
		}
		foreach (CardBehaviorDefinition item in TowerDefenseBehaviorRegistry.Resolve(config.behaviorIds, config.behaviors))
		{
			BindDefinition(item, owner, config);
		}
		BindActions(GetEffectiveActions(config, pressed: true), CardActionBehaviorTrigger.Pressed, owner, config);
		BindActions(GetEffectiveActions(config, pressed: false), CardActionBehaviorTrigger.UseSucceeded, owner, config);
	}

	public void NotifyPressed()
	{
		Dispatch(delegate(CardBehaviorRuntime runtime)
		{
			runtime.OnPressed();
		}, "E_PRESS");
	}

	public void NotifyUseSucceeded(TowerDefenseCharacter createdCharacter, bool includeActions = true)
	{
		Dispatch(delegate(CardBehaviorRuntime runtime)
		{
			runtime.OnUseSucceeded(createdCharacter);
		}, "E_USE", includeActions);
	}

	public void Release()
	{
		for (int num = _runtimes.Count - 1; num >= 0; num--)
		{
			CardBehaviorRuntime cardBehaviorRuntime = _runtimes[num];
			try
			{
				cardBehaviorRuntime.Release();
			}
			catch (Exception ex)
			{
				GD.PushError($"[CardBehavior:E_RELEASE] behavior='{cardBehaviorRuntime.Definition?.GetDiagnosticName()}' reason='{ex.Message}'");
			}
		}
		_runtimes.Clear();
	}

	private void Dispatch(Action<CardBehaviorRuntime> callback, string errorCode, bool includeActions = true)
	{
		for (int i = 0; i < _runtimes.Count; i++)
		{
			CardBehaviorRuntime cardBehaviorRuntime = _runtimes[i];
			if (cardBehaviorRuntime.Enabled && (includeActions || !(cardBehaviorRuntime.Definition is CardActionBehaviorDefinition)))
			{
				try
				{
					callback(cardBehaviorRuntime);
				}
				catch (Exception ex)
				{
					cardBehaviorRuntime.Disable();
					GD.PushError($"[CardBehavior:{errorCode}] behavior='{cardBehaviorRuntime.Definition?.GetDiagnosticName()}' packet='{cardBehaviorRuntime.Config?.saveKey}' reason='{ex.Message}'");
				}
			}
		}
	}

	private void BindActions(Array<CardActionBehaviorDefinition> definitions, CardActionBehaviorTrigger trigger, TowerDefenseInGamePacketShow owner, TowerDefensePacketConfig config)
	{
		if (definitions == null)
		{
			return;
		}
		foreach (CardActionBehaviorDefinition definition in definitions)
		{
			if (GodotObject.IsInstanceValid(definition) && definition.InitiallyEnabled)
			{
				try
				{
					BindRuntime(definition, definition.CreateRuntimeForTrigger(trigger), owner, config);
				}
				catch (Exception exception)
				{
					ReportBindError(definition, config, exception);
				}
			}
		}
	}

	private void BindDefinition(CardBehaviorDefinition definition, TowerDefenseInGamePacketShow owner, TowerDefensePacketConfig config)
	{
		if (!GodotObject.IsInstanceValid(definition) || !definition.InitiallyEnabled)
		{
			return;
		}
		try
		{
			BindRuntime(definition, definition.CreateRuntime(), owner, config);
		}
		catch (Exception exception)
		{
			ReportBindError(definition, config, exception);
		}
	}

	private void BindRuntime(CardBehaviorDefinition definition, CardBehaviorRuntime runtime, TowerDefenseInGamePacketShow owner, TowerDefensePacketConfig config)
	{
		if (!GodotObject.IsInstanceValid(definition) || !definition.InitiallyEnabled || runtime == null)
		{
			return;
		}
		try
		{
			runtime.Bind(definition, owner, config);
			_runtimes.Add(runtime);
		}
		catch (Exception exception)
		{
			ReportBindError(definition, config, exception);
		}
	}

	private static void ReportBindError(CardBehaviorDefinition definition, TowerDefensePacketConfig config, Exception exception)
	{
		GD.PushError($"[CardBehavior:E_BIND] behavior='{definition?.GetDiagnosticName()}' packet='{config?.saveKey}' reason='{exception.Message}'");
	}

	private static Array<CardActionBehaviorDefinition> GetEffectiveActions(TowerDefensePacketConfig config, bool pressed)
	{
		Array<CardActionBehaviorDefinition> result = (pressed ? config.pressedActions : config.useSucceededActions);
		if (!GodotObject.IsInstanceValid(config._override))
		{
			return result;
		}
		Array<CardActionBehaviorDefinition> array = (pressed ? config._override.pressedActions : config._override.useSucceededActions);
		if (array == null || array.Count <= 0)
		{
			return result;
		}
		return array;
	}
}
