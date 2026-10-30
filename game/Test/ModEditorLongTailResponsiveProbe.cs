using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;
using PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

public partial class ModEditorLongTailResponsiveProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	public override async void _Ready()
	{
		bool f3 = false;
		bool responsive820 = false;
		bool picker = false;
		bool pickerVisual = false;
		bool pickerInteraction = false;
		bool pickerStopped = false;
		bool visualChoicePaging = false;
		bool visualChoiceInteraction = false;
		bool visualChoiceStopped = false;
		bool buff = false;
		bool buffInteraction = false;
		bool collision = false;
		bool collisionInteraction = false;
		bool containerGroup = false;
		bool containerInteraction = false;
		bool inspectorUntouched = false;
		bool hiddenStopped = false;
		try
		{
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
				return;
			}
			await WaitFrames(2);
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
			XWBuffVisualResourceEditor buffEditor = await WaitForEditor<XWBuffVisualResourceEditor>("buff_visual_editor", 900);
			Window modWindow = FindAncestorWindow(buffEditor);
			f3 = buffEditor != null && modWindow != null;
			Require(f3, "F3 did not mount the live ModEditor resource workbench.");
			if (!f3)
			{
				return;
			}
			modWindow.MinSize = new Vector2I(820, 600);
			modWindow.Size = new Vector2I(820, 700);
			await WaitFrames(3);
			responsive820 = modWindow.Size.X <= 820 && modWindow.Size.X >= 800;
			Require(responsive820, $"F3 ModEditor did not accept the compact 820px host width: {modWindow.Size}.");
			XWInspector inspector = XWEditorInterface.Instance?.GetInspector() as XWInspector;
			Node sentinel = new Node
			{
				Name = "LongTailInspectorSentinel"
			};
			AddChild(sentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance?.InspectObject(sentinel);
			await WaitFrames(2);
			XWGameplayResourcePickerWindow pickerWindow = XWGameplayResourcePickerWindow.Create();
			Require(pickerWindow != null, "Gameplay resource picker scene could not be instantiated.");
			if (pickerWindow != null)
			{
				modWindow.AddChild(pickerWindow, forceReadableName: false, InternalMode.Disabled);
				bool chosen = false;
				List<XWGameplayResourceChoice> choices = new List<XWGameplayResourceChoice>
				{
					new XWGameplayResourceChoice("probe_resource", "Probe Resource", "res://icon.svg", null, XWGameplayResourceKind.Resource, IsModResource: true)
				};
				pickerWindow.OpenChoices("Probe Resource Library", "Compact visual selection", "", choices, delegate
				{
					chosen = true;
				});
				await WaitFrames(3);
				picker = pickerWindow.Visible && pickerWindow.Size.X >= 680 && pickerWindow.Size.X <= modWindow.Size.X && pickerWindow.Size.Y >= 480 && pickerWindow.Size.Y <= modWindow.Size.Y;
				Button nodeOrNull = pickerWindow.GetNodeOrNull<Button>("%ResourceTab");
				Tree nodeOrNull2 = pickerWindow.GetNodeOrNull<Tree>("%ChoiceTree");
				pickerVisual = nodeOrNull?.Icon != null && nodeOrNull2 != null && pickerWindow.GetNodeOrNull<TextureRect>("%PreviewTexture") != null;
				TreeItem treeItem = nodeOrNull2?.GetRoot()?.GetFirstChild();
				if (treeItem != null)
				{
					treeItem.Select(0);
					nodeOrNull2.EmitSignal(Tree.SignalName.ItemSelected);
					await WaitFrames(2);
					pickerWindow.GetNode<Button>("%ConfirmButton").EmitSignal(BaseButton.SignalName.Pressed);
					await WaitFrames(2);
				}
				pickerInteraction = chosen && !pickerWindow.Visible;
				pickerWindow.OpenResourceLibrary("probe", "Probe", "", "res://Tests", Array.Empty<string>(), Array.Empty<string>(), "", delegate
				{
				});
				await WaitFrames(1);
				pickerWindow.Dismiss();
				await WaitFrames(3);
				AudioStreamPlayer nodeOrNull3 = pickerWindow.GetNodeOrNull<AudioStreamPlayer>("%AudioPreviewPlayer");
				pickerStopped = !pickerWindow.Visible && !pickerWindow.IsProcessing() && (nodeOrNull3 == null || !nodeOrNull3.Playing);
			}
			Require(picker, "Resource picker did not fit inside the compact F3 host.");
			Require(pickerVisual, "Resource picker lost icon categories, choice tree, or visual preview.");
			Require(pickerInteraction, "Resource picker selection did not invoke its live callback and close.");
			Require(pickerStopped, "Hidden resource picker kept indexing, audio, or frame processing alive.");
			HFlowContainer visualChoiceHost = new HFlowContainer
			{
				Name = "ProbeVisualChoiceHost"
			};
			OptionButton visualChoiceSource = new OptionButton
			{
				Name = "ProbeVisualChoiceSource"
			};
			visualChoiceHost.AddChild(visualChoiceSource, forceReadableName: false, InternalMode.Disabled);
			modWindow.AddChild(visualChoiceHost, forceReadableName: false, InternalMode.Disabled);
			for (int num = 0; num < 80; num++)
			{
				visualChoiceSource.AddItem($"Choice {num:D3}", 1000 + num);
			}
			using (XWVisualOptionGallery visualGallery = new XWVisualOptionGallery(visualChoiceSource, visualChoiceHost, "ProbePagedGallery"))
			{
				visualGallery.Rebuild();
				await WaitFrames(2);
				Button galleryButton = visualChoiceHost.FindChild("ProbePagedGallery", recursive: true, owned: false) as Button;
				galleryButton?.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(2);
				Button nextPage = visualChoiceHost.FindChild("ProbePagedGalleryNextPage", recursive: true, owned: false) as Button;
				nextPage?.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(2);
				Button secondPageChoice = visualChoiceHost.FindChild("GalleryOption36", recursive: true, owned: false) as Button;
				visualChoicePaging = visualGallery.CardCount == 36 && nextPage != null && secondPageChoice != null;
				secondPageChoice?.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(2);
				bool secondPageSemanticSelection = visualChoiceSource.Selected == 36 && visualChoiceSource.GetSelectedId() == 1036 && (secondPageChoice?.ButtonPressed ?? false);
				galleryButton?.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(2);
				if (visualChoiceHost.FindChild("ProbePagedGallerySearch", recursive: true, owned: false) is LineEdit lineEdit)
				{
					lineEdit.Text = "Choice 072";
					lineEdit.EmitSignal(LineEdit.SignalName.TextChanged, lineEdit.Text);
				}
				await WaitFrames(2);
				Button filteredChoice = visualChoiceHost.FindChild("GalleryOption72", recursive: true, owned: false) as Button;
				filteredChoice?.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(2);
				visualChoiceInteraction = secondPageSemanticSelection && visualChoiceSource.Selected == 72 && visualChoiceSource.GetSelectedId() == 1072 && (filteredChoice?.ButtonPressed ?? false) && !visualChoiceSource.Visible;
				visualChoiceStopped = visualChoiceHost.FindChild("ProbePagedGalleryGallery", recursive: true, owned: false) is PopupPanel { Visible: false } popupPanel && popupPanel.ProcessMode == ProcessModeEnum.Disabled;
			}
			visualChoiceHost.QueueFree();
			Require(visualChoicePaging, "Visual option gallery did not cap cards or navigate to its second page.");
			Require(visualChoiceInteraction, "Paged/search visual choice did not preserve the hidden OptionButton id and highlight.");
			Require(visualChoiceStopped, "Hidden visual option gallery kept its process mode enabled.");
			BuffVisualDefinition buffDefinition = new BuffVisualDefinition
			{
				ResourceName = "ProbeBuff",
				buffKey = "ProbeBuff",
				drawBand = AdobeAnimateExternalVisualDrawBand.BehindAnimation
			};
			buffEditor = await OpenEditor<XWBuffVisualResourceEditor>(buffDefinition, "user://probe_buff_visual.tres", 300);
			Button button = buffEditor?.FindChild("BuffFrontAnimationBand", recursive: true, owned: false) as Button;
			Control control = buffEditor?.FindChild("BuffVisualWorkbench", recursive: true, owned: false) as Control;
			buff = (buffEditor?.IsVisibleInTree() ?? false) && control != null && control.GetCombinedMinimumSize().X <= 820f && button != null && buffEditor.FindChild("BuffTexturePicker", recursive: true, owned: false) != null;
			button?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(2);
			buffInteraction = buffDefinition.drawBand == AdobeAnimateExternalVisualDrawBand.InFrontOfAnimation;
			Require(buff, "Buff visual resource did not open as a compact direct-edit workbench.");
			Require(buffInteraction, "Buff draw-band visual segment did not edit the live resource.");
			CharacterHitBoxDefinition hitBox = new CharacterHitBoxDefinition
			{
				ResourceName = "ProbeHitBox",
				Size = new Vector2(96f, 128f),
				LocalTransform = Transform2D.Identity
			};
			XWCollisionGeometryVisualResourceEditor collisionEditor = await OpenEditor<XWCollisionGeometryVisualResourceEditor>(hitBox, "user://probe_collision_geometry.tres", 300);
			SpinBox originX = collisionEditor?.FindChild("CollisionOriginX", recursive: true, owned: false) as SpinBox;
			Control control2 = collisionEditor?.FindChild("CollisionGeometryWorkbench", recursive: true, owned: false) as Control;
			collision = (collisionEditor?.IsVisibleInTree() ?? false) && control2 != null && control2.GetCombinedMinimumSize().X <= 820f && collisionEditor.FindChild("CollisionGeometryCanvas", recursive: true, owned: false) is XWCollisionGeometryCanvas;
			if (originX != null)
			{
				originX.EmitSignal(Control.SignalName.FocusEntered);
				originX.Value = 37.0;
				await WaitFrames(2);
				originX.EmitSignal(Control.SignalName.FocusExited);
				await WaitFrames(2);
			}
			collisionInteraction = Math.Abs(hitBox.LocalTransform.Origin.X - 37f) < 0.01f;
			Require(collision, "Collision geometry resource did not open on its direct game canvas.");
			Require(collisionInteraction, "Collision origin control did not edit the live resource.");
			TowerDefensePacketBankData packetBank = new TowerDefensePacketBankData
			{
				ResourceName = "ProbeBank"
			};
			XWPacketBankVisualResourceEditor bankEditor = await OpenEditor<XWPacketBankVisualResourceEditor>(packetBank, "user://probe_packet_bank.tres", 300);
			bool bankReady = ProbeFlowLayout(bankEditor, "PacketBankVisualEditorLayout", "Split");
			bool bankEdited = EditResourceName(bankEditor, packetBank, "ProbeBankEdited");
			await WaitFrames(2);
			ShopConfig shop = new ShopConfig
			{
				ResourceName = "ProbeShop"
			};
			XWShopVisualResourceEditor shopEditor = await OpenEditor<XWShopVisualResourceEditor>(shop, "user://probe_shop.tres", 300);
			bool shopReady = ProbeFlowLayout(shopEditor, "ShopVisualEditorLayout", "Split");
			bool shopEdited = EditResourceName(shopEditor, shop, "ProbeShopEdited");
			await WaitFrames(2);
			TowerDefensePacketOverride packetOverride = new TowerDefensePacketOverride
			{
				ResourceName = "ProbeOverride"
			};
			XWPacketOverrideVisualResourceEditor overrideEditor = await OpenEditor<XWPacketOverrideVisualResourceEditor>(packetOverride, "user://probe_packet_override.tres", 300);
			bool overrideReady = ProbeFlowLayout(overrideEditor, "PacketOverrideVisualEditorLayout", "Lists");
			bool overrideEdited = EditResourceName(overrideEditor, packetOverride, "ProbeOverrideEdited");
			await WaitFrames(2);
			containerGroup = bankReady && shopReady && overrideReady;
			containerInteraction = bankEdited && shopEdited && overrideEdited && packetBank.ResourceName.ToString() == "ProbeBankEdited" && shop.ResourceName.ToString() == "ProbeShopEdited" && packetOverride.ResourceName.ToString() == "ProbeOverrideEdited";
			Require(containerGroup, "Responsive PacketBank, Shop, or PacketOverride layout did not mount with wrapping columns.");
			Require(containerInteraction, "Responsive container group did not directly edit its live resources.");
			inspectorUntouched = inspector == null || inspector.CurrentObject == sentinel;
			Require(inspectorUntouched, "Direct visual resource panels replaced the global raw Inspector selection.");
			buffEditor?.Hide();
			collisionEditor?.Hide();
			bankEditor?.Hide();
			shopEditor?.Hide();
			overrideEditor?.Hide();
			pickerWindow?.Dismiss();
			await WaitFrames(3);
			hiddenStopped = IsEditorStopped(buffEditor) && IsEditorStopped(collisionEditor) && IsEditorStopped(bankEditor) && IsEditorStopped(shopEditor) && IsEditorStopped(overrideEditor) && (pickerWindow == null || !pickerWindow.IsProcessing());
			Require(hiddenStopped, "Hidden visual panels kept resource or preview processing active.");
		}
		catch (Exception value)
		{
			_failures.Add($"Unhandled probe exception: {value}");
		}
		finally
		{
			GD.Print($"[MOD_EDITOR_LONG_TAIL_RESPONSIVE_PROBE] f3={f3} responsive820={responsive820} picker={picker} pickerVisual={pickerVisual} pickerInteraction={pickerInteraction} pickerStopped={pickerStopped} visualChoicePaging={visualChoicePaging} visualChoiceInteraction={visualChoiceInteraction} visualChoiceStopped={visualChoiceStopped} buff={buff} buffInteraction={buffInteraction} collision={collision} collisionInteraction={collisionInteraction} containerGroup={containerGroup} containerInteraction={containerInteraction} inspectorUntouched={inspectorUntouched} hiddenStopped={hiddenStopped} failures={_failures.Count}");
			foreach (string failure in _failures)
			{
				GD.PrintErr("[MOD_EDITOR_LONG_TAIL_RESPONSIVE_PROBE_FAILURE] " + failure);
			}
			GetTree().Quit((_failures.Count != 0) ? 1 : 0);
		}
	}

	private async Task<T> OpenEditor<T>(Resource resource, string path, int maxFrames) where T : XWGenericVisualResourceEditor
	{
		if (!XWResourceEditorRegistry.TryGetEditor(resource, path, out var descriptor) || descriptor == null || !XWResourceEditorRegistry.TryOpen(resource, path))
		{
			return null;
		}
		return await WaitForEditor<T>(descriptor.DockKey, maxFrames);
	}

	private async Task<T> WaitForEditor<T>(string dockKey, int maxFrames) where T : XWGenericVisualResourceEditor
	{
		for (int i = 0; i < maxFrames; i++)
		{
			Control control = XWEditorInterface.Instance?.GetResourceEditor(dockKey);
			if (control is T editor)
			{
				await WaitFrames(3);
				return editor;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return null;
	}

	private static bool ProbeFlowLayout(XWGenericVisualResourceEditor editor, string rootName, string flowName)
	{
		Control control = editor?.FindChild(rootName, recursive: true, owned: false) as Control;
		if (editor != null && editor.IsVisibleInTree() && control != null && control.GetCombinedMinimumSize().X <= 820f)
		{
			return control.FindChild(flowName, recursive: true, owned: false) is HFlowContainer;
		}
		return false;
	}

	private static bool EditResourceName(XWGenericVisualResourceEditor editor, Resource resource, string value)
	{
		if (!(editor?.FindChild("ResourceNameEdit", recursive: true, owned: false) is LineEdit lineEdit))
		{
			return false;
		}
		lineEdit.EmitSignal(Control.SignalName.FocusEntered);
		lineEdit.Text = value;
		lineEdit.EmitSignal(LineEdit.SignalName.TextSubmitted, value);
		return true;
	}

	private static bool IsEditorStopped(XWGenericVisualResourceEditor editor)
	{
		if (editor != null)
		{
			if (!editor.IsVisibleInTree() && !editor.IsProcessing())
			{
				return editor.ProcessMode == ProcessModeEnum.Disabled;
			}
			return false;
		}
		return true;
	}

	private async Task WaitFrames(int count)
	{
		for (int i = 0; i < count; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private static Window FindAncestorWindow(Node node)
	{
		Node node2 = node;
		while (GodotObject.IsInstanceValid(node2))
		{
			if (node2 is Window result)
			{
				return result;
			}
			node2 = node2.GetParent();
		}
		return null;
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
		}
	}
}
