using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.GUI;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;

public partial class ModEditorResourceLazyInstantiationRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	public override async void _Ready()
	{
		double startupMs = 0.0;
		int paletteEntries = -1;
		int initialCount = -1;
		int generalCount = -1;
		int generalRepeatCount = -1;
		int cardCount = -1;
		int cardRepeatCount = -1;
		try
		{
			IReadOnlyList<XWVisualEditorDescriptor> descriptors = XWResourceEditorRegistry.GetAllEditors();
			Require(descriptors.Count == 37, $"Expected 37 resource routes, actual {descriptors.Count}.");
			XWVisualEditorDescriptor generalDescriptor = descriptors.Single((XWVisualEditorDescriptor item) => item.Category == "General");
			XWVisualEditorDescriptor cardDescriptor = descriptors.Single((XWVisualEditorDescriptor item) => item.Category == "Card");
			ModEditorManager modEditorManager = ModEditorManager.Instance;
			if (!GodotObject.IsInstanceValid(modEditorManager))
			{
				modEditorManager = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Core/ModEditorManager.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<ModEditorManager>(PackedScene.GenEditState.Disabled);
				if (GodotObject.IsInstanceValid(modEditorManager))
				{
					AddChild(modEditorManager, forceReadableName: false, InternalMode.Disabled);
				}
			}
			Require(GodotObject.IsInstanceValid(modEditorManager), "ModEditorManager could not be instantiated.");
			if (!GodotObject.IsInstanceValid(modEditorManager))
			{
				Finish(startupMs, paletteEntries, initialCount, generalCount, generalRepeatCount, cardCount, cardRepeatCount);
				return;
			}
			await WaitFrames(2);
			Stopwatch startupTimer = Stopwatch.StartNew();
			Input.ParseInputEvent(new InputEventKey
			{
				Keycode = Key.F3,
				PhysicalKeycode = Key.F3,
				Pressed = true
			});
			Input.ParseInputEvent(new InputEventKey
			{
				Keycode = Key.F3,
				PhysicalKeycode = Key.F3,
				Pressed = false
			});
			ModEditorPanel panel = await WaitForPanel(1200);
			startupTimer.Stop();
			startupMs = startupTimer.Elapsed.TotalMilliseconds;
			Require(GodotObject.IsInstanceValid(panel), "F3 did not finish loading ModEditorPanel.");
			if (!GodotObject.IsInstanceValid(panel))
			{
				Finish(startupMs, paletteEntries, initialCount, generalCount, generalRepeatCount, cardCount, cardRepeatCount);
				return;
			}
			XWResourceWorkspacePalette palette = FindNodeOfType<XWResourceWorkspacePalette>(panel);
			paletteEntries = palette?.EntryCount ?? (-1);
			Require(GodotObject.IsInstanceValid(palette), "Resource workspace palette is missing after F3.");
			Require(paletteEntries == 37, $"Palette must retain 37 entries after F3, actual {paletteEntries}.");
			Require(palette != null && palette.VisibleEntryCount == 37, $"Palette must expose all 37 entries initially, actual {palette?.VisibleEntryCount ?? (-1)}.");
			initialCount = panel.LoadedResourceEditorCount;
			Require(initialCount == 0, $"F3 eagerly instantiated {initialCount} resource editors.");
			Require(descriptors.All((XWVisualEditorDescriptor item) => !GodotObject.IsInstanceValid(XWEditorInterface.Instance.TryGetLoadedResourceEditor(item.DockKey))), "At least one resource editor was loaded before its first route opened.");
			LabelSettings generalResource = new LabelSettings
			{
				FontSize = 24
			};
			Require(XWResourceEditorRegistry.TryGetEditor(generalResource, "user://ModEditorLazyInstantiation/General.tres", out var descriptor) && descriptor.DockKey == generalDescriptor.DockKey, "General fixture did not route to resource_editor.");
			bool generalOpened = XWResourceEditorRegistry.TryOpen(generalResource, "user://ModEditorLazyInstantiation/General.tres");
			await WaitFrames(3);
			generalCount = panel.LoadedResourceEditorCount;
			Control generalEditor = XWEditorInterface.Instance.TryGetLoadedResourceEditor(generalDescriptor.DockKey);
			Require(generalOpened && generalCount == 1, $"General first open must create exactly one editor: opened={generalOpened} count={generalCount}.");
			Require(generalEditor is XWGenericVisualResourceEditor, "General route did not load a visual resource editor.");
			bool generalRepeated = XWResourceEditorRegistry.TryOpen(generalResource, "user://ModEditorLazyInstantiation/General.tres");
			await WaitFrames(2);
			generalRepeatCount = panel.LoadedResourceEditorCount;
			Require(generalRepeated && generalRepeatCount == 1 && generalEditor == XWEditorInterface.Instance.TryGetLoadedResourceEditor(generalDescriptor.DockKey), $"Repeated General open duplicated or replaced its editor: opened={generalRepeated} count={generalRepeatCount}.");
			TowerDefensePacketConfig cardResource = new TowerDefensePacketConfig
			{
				ResourceName = "LazyInstantiationCard",
				name = "Lazy Instantiation Card"
			};
			Require(XWResourceEditorRegistry.TryGetEditor(cardResource, "user://ModEditorLazyInstantiation/Card.tres", out var descriptor2) && descriptor2.DockKey == cardDescriptor.DockKey, "Card fixture did not route to card_editor.");
			bool cardOpened = XWResourceEditorRegistry.TryOpen(cardResource, "user://ModEditorLazyInstantiation/Card.tres");
			await WaitFrames(3);
			cardCount = panel.LoadedResourceEditorCount;
			Control cardEditor = XWEditorInterface.Instance.TryGetLoadedResourceEditor(cardDescriptor.DockKey);
			Require(cardOpened && cardCount == 2, $"Card first open must create only the second editor: opened={cardOpened} count={cardCount}.");
			Require(cardEditor is XWCardVisualResourceEditor, "Card route loaded " + (cardEditor?.GetType().Name ?? "none") + " instead of XWCardVisualResourceEditor.");
			bool cardRepeated = XWResourceEditorRegistry.TryOpen(cardResource, "user://ModEditorLazyInstantiation/Card.tres");
			await WaitFrames(2);
			cardRepeatCount = panel.LoadedResourceEditorCount;
			Require(cardRepeated && cardRepeatCount == 2 && cardEditor == XWEditorInterface.Instance.TryGetLoadedResourceEditor(cardDescriptor.DockKey), $"Repeated Card open duplicated or replaced its editor: opened={cardRepeated} count={cardRepeatCount}.");
			Require(palette.EntryCount == 37 && palette.VisibleEntryCount == 37, "Opening resources changed the 37-entry palette manifest.");
			Require(descriptors.Where((XWVisualEditorDescriptor item) => item.DockKey != generalDescriptor.DockKey && item.DockKey != cardDescriptor.DockKey).All((XWVisualEditorDescriptor item) => !GodotObject.IsInstanceValid(XWEditorInterface.Instance.TryGetLoadedResourceEditor(item.DockKey))), "Opening General and Card instantiated an unrelated resource editor.");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish(startupMs, paletteEntries, initialCount, generalCount, generalRepeatCount, cardCount, cardRepeatCount);
	}

	private async Task<ModEditorPanel> WaitForPanel(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			ModEditorPanel modEditorPanel = XWEditorInterface.Instance?.GetEditorPanel() as ModEditorPanel;
			Node instance = modEditorPanel?.FindChild("LoadingOverlay", recursive: true, owned: false);
			XWResourceWorkspacePalette xWResourceWorkspacePalette = FindNodeOfType<XWResourceWorkspacePalette>(modEditorPanel);
			if (GodotObject.IsInstanceValid(modEditorPanel) && !GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(xWResourceWorkspacePalette) && xWResourceWorkspacePalette.EntryCount == 37)
			{
				return modEditorPanel;
			}
			await WaitFrames(1);
		}
		return null;
	}

	private static T FindNodeOfType<T>(Node root) where T : Node
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		if (root is T result)
		{
			return result;
		}
		foreach (Node child in root.GetChildren())
		{
			T val = FindNodeOfType<T>(child);
			if (GodotObject.IsInstanceValid(val))
			{
				return val;
			}
		}
		return null;
	}

	private async Task WaitFrames(int count)
	{
		for (int index = 0; index < count; index++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
			GD.Print("[MOD_EDITOR_RESOURCE_LAZY_FAILURE] " + message);
		}
	}

	private void Finish(double startupMs, int paletteEntries, int initialCount, int generalCount, int generalRepeatCount, int cardCount, int cardRepeatCount)
	{
		foreach (string failure in _failures)
		{
			GD.Print("[MOD_EDITOR_RESOURCE_LAZY_FAILURE] " + failure);
		}
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(169, 8, invariantCulture);
		handler.AppendLiteral("[MOD_EDITOR_RESOURCE_LAZY] palette=");
		handler.AppendFormatted(paletteEntries);
		handler.AppendLiteral(" initial=");
		handler.AppendFormatted(initialCount);
		handler.AppendLiteral(" ");
		handler.AppendLiteral("general=");
		handler.AppendFormatted(generalCount);
		handler.AppendLiteral(" generalRepeat=");
		handler.AppendFormatted(generalRepeatCount);
		handler.AppendLiteral(" card=");
		handler.AppendFormatted(cardCount);
		handler.AppendLiteral(" cardRepeat=");
		handler.AppendFormatted(cardRepeatCount);
		handler.AppendLiteral(" ");
		handler.AppendLiteral("generalRoute=resource_editor cardRoute=card_editor f3StartupMs=");
		handler.AppendFormatted(startupMs, "F3");
		handler.AppendLiteral(" errors=0 failures=");
		handler.AppendFormatted(_failures.Count);
		GD.Print(string.Create(invariantCulture, ref handler));
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
