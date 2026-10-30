using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;

public partial class StateMachineCallbackRuntimeProbe : Node
{
	private readonly record struct CollectibleBindingReferences(WeakReference Assembly, WeakReference Host, StateMachineCallbackBinding Binding, bool Prepared);

	private const string OwnerId = "state-machine-callback-probe";

	private const string LifecycleLocalKey = "lifecycle";

	private const string GuardLocalKey = "allow-transition";

	private const string ThrowingGuardLocalKey = "throw-transition";

	private const string PhaseEnterLocalKey = "phase-enter";

	private const string PhaseExitLocalKey = "phase-exit";

	private const string PhaseProcessLocalKey = "phase-process";

	private const string PhasePhysicsLocalKey = "phase-physics";

	private const string LifecycleFullKey = "mod/state-machine-callback-probe/lifecycle";

	private const string GuardFullKey = "mod/state-machine-callback-probe/allow-transition";

	private const string ThrowingGuardFullKey = "mod/state-machine-callback-probe/throw-transition";

	private const string MissingLegacyFullKey = "mod/state-machine-callback-probe/removed-legacy-lifecycle";

	private const string DefinitionId = "probe.state-machine-callback-runtime";

	private readonly List<string> _failures = new List<string>();

	private readonly List<string> _trace = new List<string>();

	private int _enterCount;

	private int _exitCount;

	private int _processCount;

	private int _physicsCount;

	private int _guardCount;

	private int _throwingGuardCount;

	private bool _hostContextValid = true;

	private bool _guardContextValid = true;

	private double _processDelta;

	private double _physicsDelta;

	public static void RecordLifecycleEnter(in StateMachineCallbackContext context)
	{
		StateMachineCallbackRuntimeProbe stateMachineCallbackRuntimeProbe = context.RequireHost<StateMachineCallbackRuntimeProbe>();
		stateMachineCallbackRuntimeProbe._hostContextValid &= context.Host == stateMachineCallbackRuntimeProbe && context.Runtime != null && (context.State?.IsValid ?? false);
		stateMachineCallbackRuntimeProbe._enterCount++;
		stateMachineCallbackRuntimeProbe._trace.Add("enter:" + context.State?.StableId);
		context.SetProperty("callback_host_seen", true);
	}

	public static void RecordLifecycleExit(in StateMachineCallbackContext context)
	{
		StateMachineCallbackRuntimeProbe stateMachineCallbackRuntimeProbe = context.RequireHost<StateMachineCallbackRuntimeProbe>();
		stateMachineCallbackRuntimeProbe._hostContextValid &= context.Host == stateMachineCallbackRuntimeProbe && context.Runtime != null && (context.State?.IsValid ?? false);
		stateMachineCallbackRuntimeProbe._exitCount++;
		stateMachineCallbackRuntimeProbe._trace.Add("exit:" + context.State?.StableId);
	}

	public static void RecordLifecycleProcess(in StateMachineCallbackContext context, double delta)
	{
		StateMachineCallbackRuntimeProbe stateMachineCallbackRuntimeProbe = context.RequireHost<StateMachineCallbackRuntimeProbe>();
		stateMachineCallbackRuntimeProbe._hostContextValid &= context.Host == stateMachineCallbackRuntimeProbe && context.Runtime != null && context.State?.StableId == "Idle";
		stateMachineCallbackRuntimeProbe._processCount++;
		stateMachineCallbackRuntimeProbe._processDelta += delta;
		stateMachineCallbackRuntimeProbe._trace.Add("process:" + context.State?.StableId);
	}

	public static void RecordLifecyclePhysics(in StateMachineCallbackContext context, double delta)
	{
		StateMachineCallbackRuntimeProbe stateMachineCallbackRuntimeProbe = context.RequireHost<StateMachineCallbackRuntimeProbe>();
		stateMachineCallbackRuntimeProbe._hostContextValid &= context.Host == stateMachineCallbackRuntimeProbe && context.Runtime != null && context.State?.StableId == "Idle";
		stateMachineCallbackRuntimeProbe._physicsCount++;
		stateMachineCallbackRuntimeProbe._physicsDelta += delta;
		stateMachineCallbackRuntimeProbe._trace.Add("physics:" + context.State?.StableId);
	}

	public static bool RecordAllowTransition(in StateMachineGuardContext context)
	{
		StateMachineCallbackRuntimeProbe stateMachineCallbackRuntimeProbe = context.RequireHost<StateMachineCallbackRuntimeProbe>();
		stateMachineCallbackRuntimeProbe._guardContextValid &= context.Host == stateMachineCallbackRuntimeProbe && context.Runtime != null && context.SourceStateId == "Idle" && context.TargetStateId == "Done" && context.EventName == new StringName("Advance") && context.GetProperty("callback_host_seen", false).AsBool();
		stateMachineCallbackRuntimeProbe._guardCount++;
		stateMachineCallbackRuntimeProbe._trace.Add("guard:" + context.SourceStateId + "->" + context.TargetStateId);
		return true;
	}

	public static bool RecordThrowingGuard(in StateMachineGuardContext context)
	{
		context.RequireHost<StateMachineCallbackRuntimeProbe>()._throwingGuardCount++;
		throw new InvalidOperationException("State-machine callback throwing Guard probe.");
	}

	public override async void _Ready()
	{
		bool f3 = false;
		bool scan = false;
		bool scanRejected = false;
		bool idempotent = false;
		bool catalog = false;
		bool unicodeKey = false;
		bool uiOpened = false;
		bool uiEdited = false;
		bool undoRedo = false;
		bool phaseUi = false;
		bool guardUi = false;
		bool dedicatedGuardUi = false;
		bool initialized = false;
		bool leaseBlocked = false;
		bool entered = false;
		bool lifecycle = false;
		bool guard = false;
		bool guardExceptionStopsFallback = false;
		bool hostType = false;
		bool order = false;
		bool phaseSpecific = false;
		bool legacyFullyCovered = false;
		bool legacyPartiallyCovered = false;
		bool missingDiagnostic = false;
		bool disposedUnload = false;
		bool bindingCollectible = false;
		bool sharedRegistered = false;
		StateMachineController controller = null;
		StateMachineUnloadBlockers blockers;
		try
		{
			_ = 2;
			try
			{
				f3 = await OpenRealEditorAsync();
				Require(f3, "F3 did not open the ModEditor state-machine workbench.");
				unicodeKey = StateMachineCallbackKey.TryBuild("测试", "进入状态", out var fullKey, out var _) && fullKey == "mod/测试/进入状态" && StateMachineCallbackKey.TryParse(fullKey, out var ownerId, out var localKey) && ownerId == "测试" && localKey == "进入状态";
				Require(unicodeKey, "Unicode Mod owner/local callback key did not round-trip.");
				StateMachineCallbackRegistry stateMachineCallbackRegistry = new StateMachineCallbackRegistry();
				StateMachineCallbackRegistrationResult stateMachineCallbackRegistrationResult = stateMachineCallbackRegistry.RegisterAssembly("state-machine-callback-invalid", CreateRejectedCallbackAssembly(duplicateAttribute: false));
				StateMachineCallbackRegistrationResult stateMachineCallbackRegistrationResult2 = stateMachineCallbackRegistry.RegisterAssembly("state-machine-callback-duplicate", CreateRejectedCallbackAssembly(duplicateAttribute: true));
				scanRejected = !stateMachineCallbackRegistrationResult.Success && stateMachineCallbackRegistrationResult.Error.Contains("does not match", StringComparison.OrdinalIgnoreCase) && !stateMachineCallbackRegistrationResult2.Success && stateMachineCallbackRegistrationResult2.Error.Contains("Duplicate", StringComparison.OrdinalIgnoreCase) && stateMachineCallbackRegistry.GetCatalogSnapshot().Entries.Length == 0 && !stateMachineCallbackRegistry.TryUnloadOwner("state-machine-callback-invalid", out blockers) && !stateMachineCallbackRegistry.TryUnloadOwner("state-machine-callback-duplicate", out blockers);
				Require(scanRejected, "Invalid signature or duplicate callback registration left a partial owner/catalog entry.");
				StateMachineCallbackRegistry registry = new StateMachineCallbackRegistry();
				Assembly assembly = CreateCallbackAssembly();
				await ProbeRegistrationTransactionAsync(assembly);
				StateMachineCallbackRegistrationResult stateMachineCallbackRegistrationResult3 = registry.RegisterAssembly("state-machine-callback-probe", assembly);
				StateMachineCallbackRegistrationResult stateMachineCallbackRegistrationResult4 = registry.RegisterAssembly("state-machine-callback-probe", assembly);
				StateMachineCallbackRegistrationResult stateMachineCallbackRegistrationResult5 = StateMachineCallbackRegistry.Shared.RegisterAssembly("state-machine-callback-probe", assembly);
				sharedRegistered = stateMachineCallbackRegistrationResult5.Success;
				scan = stateMachineCallbackRegistrationResult3.Success && stateMachineCallbackRegistrationResult3.CallbackCount == 10 && stateMachineCallbackRegistrationResult5.Success && stateMachineCallbackRegistrationResult5.CallbackCount == 10;
				idempotent = stateMachineCallbackRegistrationResult4.Success && stateMachineCallbackRegistrationResult4.CallbackCount == stateMachineCallbackRegistrationResult3.CallbackCount;
				Require(scan, "Callback assembly scan did not register all lifecycle and Guard phases.");
				Require(idempotent, "Registering the same owner/assembly twice was not idempotent.");
				StateMachineCallbackCatalogSnapshot catalogSnapshot = registry.GetCatalogSnapshot();
				StateMachineCallbackCatalogEntry stateMachineCallbackCatalogEntry = catalogSnapshot.Entries.FirstOrDefault((StateMachineCallbackCatalogEntry entry) => entry.Key == "mod/state-machine-callback-probe/lifecycle");
				StateMachineCallbackCatalogEntry stateMachineCallbackCatalogEntry2 = catalogSnapshot.Entries.FirstOrDefault((StateMachineCallbackCatalogEntry entry) => entry.Key == "mod/state-machine-callback-probe/allow-transition");
				StateMachineCallbackPhaseFlags stateMachineCallbackPhaseFlags = StateMachineCallbackPhaseFlags.Enter | StateMachineCallbackPhaseFlags.Exit | StateMachineCallbackPhaseFlags.Process | StateMachineCallbackPhaseFlags.PhysicsProcess;
				catalog = stateMachineCallbackCatalogEntry.OwnerId == "state-machine-callback-probe" && (stateMachineCallbackCatalogEntry.Phases & stateMachineCallbackPhaseFlags) == stateMachineCallbackPhaseFlags && stateMachineCallbackCatalogEntry2.OwnerId == "state-machine-callback-probe" && (stateMachineCallbackCatalogEntry2.Phases & StateMachineCallbackPhaseFlags.Guard) != 0;
				Require(catalog, "Callback catalog did not merge the registered phases by key.");
				StateMachineDefinition definition = CreateDefinition();
				(uiOpened, uiEdited, undoRedo, phaseUi, guardUi, dedicatedGuardUi) = await ProbeCallbackUiAsync(definition);
				Require(uiOpened, "State-machine resource did not open its visible lifecycle callback workbench.");
				Require(uiEdited, "CallbackKeyEdit did not change the selected state through the visual editor.");
				Require(undoRedo, "Visual CallbackKey editing did not round-trip through Undo/Redo.");
				Require(phaseUi, "The four phase-specific action cards were not mounted in the real Mod editor.");
				Require(guardUi, "Callback Guard did not expose its visual catalog and full-key editor.");
				Require(dedicatedGuardUi, "Dedicated Guard editor did not expose its callback card and full-key editor.");
				controller = new StateMachineController(registry);
				initialized = controller.Initialize(definition, this, "state-machine-callback-probe");
				Require(initialized, "Controller failed to bind the registered callback program: " + controller.InitializationError);
				leaseBlocked = !registry.TryUnloadOwner("state-machine-callback-probe", out var blockers2) && blockers2.LeaseCount == 1 && blockers2.Entries.Length == 1 && blockers2.Entries[0].DefinitionId == "probe.state-machine-callback-runtime" && blockers2.Entries[0].HostType == typeof(StateMachineCallbackRuntimeProbe).FullName;
				Require(leaseBlocked, "An active callback binding did not report its owner lease blocker.");
				int num;
				if (initialized)
				{
					entered = controller.EnterInitialState();
					controller.TickProcess(0.25);
					controller.TickPhysics(0.5);
					bool flag = controller.SendEvent("Advance");
					if (entered && flag && _enterCount == 2 && _exitCount == 1 && _processCount == 1 && _physicsCount == 1 && Math.Abs(_processDelta - 0.25) < 1E-06 && Math.Abs(_physicsDelta - 0.5) < 1E-06)
					{
						StateHandle stateById = controller.GetStateById("Idle");
						if (stateById != null && !stateById.IsActive)
						{
							num = ((controller.GetStateById("Done")?.IsActive ?? false) ? 1 : 0);
							goto IL_07ab;
						}
					}
					num = 0;
					goto IL_07ab;
				}
				goto IL_07f4;
				IL_07ab:
				lifecycle = (byte)num != 0;
				guard = _guardCount == 1 && _guardContextValid;
				hostType = _hostContextValid;
				order = string.Join(">", _trace) == "enter:Idle>process:Idle>physics:Idle>guard:Idle->Done>exit:Idle>enter:Done";
				goto IL_07f4;
				IL_07f4:
				Require(entered, "Initial state entry did not run.");
				Require(lifecycle, "Enter/Exit/Process/Physics callbacks did not run exactly once in the expected active states.");
				Require(guard, "The callback Guard did not receive the compiled transition context.");
				Require(hostType, "Callback contexts did not preserve the strongly typed host.");
				Require(order, "Callback execution order changed: " + string.Join(">", _trace));
				using (StateMachineController stateMachineController = new StateMachineController(registry))
				{
					int transitionCount = 0;
					stateMachineController.TransitionTaken += delegate
					{
						transitionCount++;
					};
					bool flag2 = stateMachineController.Initialize(CreateThrowingGuardFallbackDefinition(), this, "state-machine-callback-probe") && stateMachineController.EnterInitialState();
					bool flag3 = false;
					try
					{
						stateMachineController.SendEvent("Choose");
					}
					catch (InvalidOperationException ex)
					{
						flag3 = ex.Message.Contains("throwing Guard probe", StringComparison.Ordinal);
					}
					int num2;
					if (flag2 && flag3 && _throwingGuardCount == 1 && transitionCount == 0 && (stateMachineController.GetStateById("Idle")?.IsActive ?? false))
					{
						StateHandle stateById2 = stateMachineController.GetStateById("Preferred");
						if (stateById2 != null && !stateById2.IsActive)
						{
							StateHandle stateById3 = stateMachineController.GetStateById("Fallback");
							num2 = ((stateById3 != null && !stateById3.IsActive) ? 1 : 0);
							goto IL_0949;
						}
					}
					num2 = 0;
					goto IL_0949;
					IL_0949:
					guardExceptionStopsFallback = (byte)num2 != 0;
				}
				Require(guardExceptionStopsFallback, "A throwing high-priority Guard selected a lower-priority fallback or changed state.");
				int enterCount = _enterCount;
				int exitCount = _exitCount;
				int processCount = _processCount;
				int physicsCount = _physicsCount;
				int count = _trace.Count;
				using (StateMachineController stateMachineController2 = new StateMachineController(registry))
				{
					bool flag4 = stateMachineController2.Initialize(CreatePhaseSpecificDefinition(), this, "state-machine-callback-probe") && stateMachineController2.EnterInitialState();
					stateMachineController2.TickProcess(0.125);
					stateMachineController2.TickPhysics(0.25);
					bool flag5 = stateMachineController2.SendEvent("PhaseAdvance");
					string text = string.Join(">", _trace.Skip(count));
					phaseSpecific = flag4 && flag5 && _enterCount - enterCount == 2 && _exitCount - exitCount == 1 && _processCount - processCount == 1 && _physicsCount - physicsCount == 1 && text == "enter:Idle>process:Idle>physics:Idle>exit:Idle>enter:Done";
				}
				Require(phaseSpecific, "Independent Enter/Exit/Process/Physics keys did not bind and execute in order.");
				int enterCount2 = _enterCount;
				int exitCount2 = _exitCount;
				int processCount2 = _processCount;
				int physicsCount2 = _physicsCount;
				using (StateMachineController stateMachineController3 = new StateMachineController(registry))
				{
					bool flag6 = stateMachineController3.Initialize(CreateLegacyCoverageDefinition(fullyCovered: true), this, "state-machine-callback-probe") && stateMachineController3.EnterInitialState();
					stateMachineController3.TickProcess(0.125);
					stateMachineController3.TickPhysics(0.25);
					bool flag7 = stateMachineController3.SendEvent("PhaseAdvance");
					legacyFullyCovered = flag6 && flag7 && _enterCount - enterCount2 == 2 && _exitCount - exitCount2 == 1 && _processCount - processCount2 == 1 && _physicsCount - physicsCount2 == 1;
				}
				Require(legacyFullyCovered, "A removed legacy CallbackKey blocked fully phase-specific lifecycle callbacks.");
				using (StateMachineController stateMachineController4 = new StateMachineController(registry))
				{
					legacyPartiallyCovered = !stateMachineController4.Initialize(CreateLegacyCoverageDefinition(fullyCovered: false), this, "state-machine-callback-probe") && stateMachineController4.InitializationError.Contains("[SMB007]", StringComparison.Ordinal) && stateMachineController4.InitializationError.Contains("mod/state-machine-callback-probe/removed-legacy-lifecycle", StringComparison.Ordinal);
				}
				Require(legacyPartiallyCovered, "A partially covered removed legacy CallbackKey was silently ignored.");
				controller.Dispose();
				controller = null;
				disposedUnload = registry.TryUnloadOwner("state-machine-callback-probe", out var blockers3) && blockers3.LeaseCount == 0;
				Require(disposedUnload, "Disposing the controller did not release the owner lease for unload.");
				using StateMachineController stateMachineController5 = new StateMachineController(new StateMachineCallbackRegistry());
				missingDiagnostic = !stateMachineController5.Initialize(CreateMissingDefinition(), this, "state-machine-callback-missing") && stateMachineController5.InitializationError.Contains("[SMB007]", StringComparison.Ordinal) && stateMachineController5.InitializationError.Contains("mod/state-machine-callback-missing/missing", StringComparison.Ordinal);
				Require(missingDiagnostic, "A missing callback key did not fail binding with SMB007 and the full key.");
				CollectibleBindingReferences collectibleBindingReferences = CreateDisposedBindingWeakReferences();
				bindingCollectible = collectibleBindingReferences.Prepared && WaitForCollection(collectibleBindingReferences.Assembly, collectibleBindingReferences.Host);
				GC.KeepAlive(collectibleBindingReferences.Binding);
				Require(bindingCollectible, "Disposed callback binding retained its collectible assembly or host.");
			}
			catch (Exception ex2)
			{
				Require(condition: false, ex2.ToString());
			}
		}
		finally
		{
			controller?.Dispose();
			if (sharedRegistered && !StateMachineCallbackRegistry.Shared.TryUnloadOwner("state-machine-callback-probe", out blockers))
			{
				Require(condition: false, "Shared UI callback catalog owner did not unload after the probe.");
			}
			foreach (string failure in _failures)
			{
				GD.PrintErr("[STATE_MACHINE_CALLBACK_RUNTIME_FAILURE] " + failure);
			}
			GD.Print($"[STATE_MACHINE_CALLBACK_RUNTIME] f3={f3} scan={scan} scanRejected={scanRejected} idempotent={idempotent} catalog={catalog} unicodeKey={unicodeKey} uiOpened={uiOpened} uiEdited={uiEdited} undoRedo={undoRedo} phaseUi={phaseUi} guardUi={guardUi} dedicatedGuardUi={dedicatedGuardUi} initialized={initialized} leaseBlocked={leaseBlocked} entered={entered} lifecycle={lifecycle} guard={guard} guardExceptionStopsFallback={guardExceptionStopsFallback} hostType={hostType} order={order} phaseSpecific={phaseSpecific} legacyFullyCovered={legacyFullyCovered} legacyPartiallyCovered={legacyPartiallyCovered} missingDiagnostic={missingDiagnostic} disposedUnload={disposedUnload} bindingCollectible={bindingCollectible} failures={_failures.Count}");
			await WaitFrames(2);
			GetTree().Quit((_failures.Count != 0) ? 1 : 0);
		}
	}

	private static StateMachineDefinition CreateDefinition()
	{
		return new StateMachineDefinition
		{
			DefinitionId = "probe.state-machine-callback-runtime",
			RootStateId = "Root",
			States = 
			{
				new StateMachineStateDefinition
				{
					StableId = "Root",
					DisplayName = "Root",
					Kind = StateMachineStateKind.Compound,
					InitialChildId = "Idle"
				},
				new StateMachineStateDefinition
				{
					StableId = "Idle",
					DisplayName = "Idle",
					Kind = StateMachineStateKind.Atomic,
					ParentId = "Root",
					ProcessFlags = (StateMachineProcessFlags.Process | StateMachineProcessFlags.PhysicsProcess),
					CallbackKey = "mod/state-machine-callback-probe/lifecycle"
				},
				new StateMachineStateDefinition
				{
					StableId = "Done",
					DisplayName = "Done",
					Kind = StateMachineStateKind.Atomic,
					ParentId = "Root",
					CallbackKey = "mod/state-machine-callback-probe/lifecycle"
				}
			},
			Transitions = 
			{
				new StateMachineTransitionDefinition
				{
					StableId = "AdvanceToDone",
					SourceStateId = "Idle",
					TargetStateId = "Done",
					TriggerKind = StateMachineTriggerKind.Event,
					EventName = "Advance",
					GuardDefinition = new StateMachineGuardDefinition
					{
						Kind = StateMachineGuardKind.Callback,
						CallbackKey = "mod/state-machine-callback-probe/allow-transition"
					}
				}
			}
		};
	}

	private async Task<(bool UiOpened, bool UiEdited, bool UndoRedo, bool PhaseUi, bool GuardUi, bool DedicatedGuardUi)> ProbeCallbackUiAsync(StateMachineDefinition definition)
	{
		StateMachineStateDefinition idle = definition.States.FirstOrDefault((StateMachineStateDefinition state) => state?.StableId == "Idle");
		StateMachineTransitionDefinition transition = definition.Transitions.FirstOrDefault((StateMachineTransitionDefinition item) => item?.StableId == "AdvanceToDone");
		(XWEditorInterface.Instance?.GetEditorPanel()?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control)?.Hide();
		bool routed = XWResourceEditorRegistry.TryOpen(definition, "user://state_machine_callback_ui_probe.tres");
		XWEditorInterface.Instance?.FocusPanel("state_machine_editor");
		await WaitFrames(5);
		XWStateMachineVisualResourceEditor editor = XWEditorInterface.Instance?.GetResourceEditor("state_machine_editor") as XWStateMachineVisualResourceEditor;
		if (editor?.WorkbenchTabs != null)
		{
			editor.WorkbenchTabs.CurrentTab = 0;
		}
		StateMachineGraphEditorSurface surface = editor?.GraphSurface;
		surface?.NavigateToStableId("Idle");
		await EnsureDetailsVisibleAsync(surface);
		await WaitFrames(4);
		surface?.DetailsPanel?.Show();
		Control control = surface?.DetailsPanel?.FindChild("ActionWorkbenchRow", recursive: true, owned: false) as Control;
		LineEdit lineEdit = surface?.DetailsPanel?.FindChild("CallbackKeyEdit", recursive: true, owned: false) as LineEdit;
		Button button = FindCallbackCard(surface?.DetailsPanel, "LifecycleCallbackCard*", "mod/state-machine-callback-probe/lifecycle");
		bool uiOpened = routed && GodotObject.IsInstanceValid(editor) && surface?.Definition == definition && surface?.ActiveDetailsKind == "State" && surface.ActiveDetailsStableId == "Idle" && GodotObject.IsInstanceValid(control) && control.IsVisibleInTree() && GodotObject.IsInstanceValid(lineEdit) && lineEdit.IsVisibleInTree() && lineEdit.Text == "mod/state-machine-callback-probe/lifecycle" && GodotObject.IsInstanceValid(button) && button.IsVisibleInTree() && button.ButtonPressed;
		bool phaseUi = surface?.DetailsPanel?.FindChild("EnterCallbackKeyEdit", recursive: true, owned: false) is LineEdit && surface.DetailsPanel.FindChild("ExitCallbackKeyEdit", recursive: true, owned: false) is LineEdit && surface.DetailsPanel.FindChild("ProcessCallbackKeyEdit", recursive: true, owned: false) is LineEdit && surface.DetailsPanel.FindChild("PhysicsProcessCallbackKeyEdit", recursive: true, owned: false) is LineEdit;
		if (!uiOpened)
		{
			GD.Print("[STATE_MACHINE_CALLBACK_UI_OPEN_DIAGNOSTIC] " + $"routed={routed} editor={GodotObject.IsInstanceValid(editor)} " + $"surfaceDefinition={surface?.Definition == definition} " + $"surfaceVisible={surface?.IsVisibleInTree()} " + $"detailsVisible={surface?.DetailsPanel?.IsVisibleInTree()} " + $"kind={surface?.ActiveDetailsKind} id={surface?.ActiveDetailsStableId} " + $"action={GodotObject.IsInstanceValid(control)} " + $"actionVisible={control?.IsVisibleInTree()} " + $"edit={GodotObject.IsInstanceValid(lineEdit)} " + $"editVisible={lineEdit?.IsVisibleInTree()} " + "text=" + lineEdit?.Text + " " + $"card={GodotObject.IsInstanceValid(button)} " + $"cardVisible={button?.IsVisibleInTree()} " + $"cardPressed={button?.ButtonPressed}");
		}
		if (GodotObject.IsInstanceValid(lineEdit))
		{
			lineEdit.Text = "mod/测试/临时回调";
			lineEdit.EmitSignal(LineEdit.SignalName.TextSubmitted, "mod/测试/临时回调");
		}
		await WaitFrames(4);
		bool uiEdited = idle?.CallbackKey.ToString() == "mod/测试/临时回调" && surface?.DetailsPanel?.FindChild("CallbackKeyEdit", recursive: true, owned: false) is LineEdit lineEdit2 && lineEdit2.Text == "mod/测试/临时回调";
		surface?.GraphController?.Undo();
		await WaitFrames(3);
		bool undone = idle?.CallbackKey.ToString() == "mod/state-machine-callback-probe/lifecycle";
		surface?.GraphController?.Redo();
		await WaitFrames(3);
		bool redone = idle?.CallbackKey.ToString() == "mod/测试/临时回调";
		surface?.GraphController?.Undo();
		await WaitFrames(3);
		bool flag = idle?.CallbackKey.ToString() == "mod/state-machine-callback-probe/lifecycle";
		bool undoRedo = undone && redone && flag;
		bool guardRouted = XWResourceEditorRegistry.TryOpen(definition, "user://state_machine_callback_ui_probe.tres");
		await WaitFrames(5);
		editor = XWEditorInterface.Instance?.GetResourceEditor("state_machine_editor") as XWStateMachineVisualResourceEditor;
		if (editor?.WorkbenchTabs != null)
		{
			editor.WorkbenchTabs.CurrentTab = 0;
		}
		XWEditorInterface.Instance?.FocusPanel("state_machine_editor");
		await WaitFrames(3);
		surface = editor?.GraphSurface;
		surface?.NavigateToStableId("AdvanceToDone");
		await EnsureDetailsVisibleAsync(surface);
		await WaitFrames(4);
		surface?.DetailsPanel?.Show();
		Button button2 = surface?.DetailsPanel?.FindChild("GuardKindCallback", recursive: true, owned: false) as Button;
		Control control2 = surface?.DetailsPanel?.FindChild("GuardCallbackCatalog", recursive: true, owned: false) as Control;
		LineEdit lineEdit3 = surface?.DetailsPanel?.FindChild("GuardCallbackKeyEdit", recursive: true, owned: false) as LineEdit;
		Button button3 = FindCallbackCard(surface?.DetailsPanel, "GuardCallbackCard*", "mod/state-machine-callback-probe/allow-transition");
		bool guardUi = guardRouted && transition?.GuardDefinition == definition.Transitions[0].GuardDefinition && surface?.ActiveDetailsKind == "Transition" && surface.ActiveDetailsStableId == "AdvanceToDone" && GodotObject.IsInstanceValid(button2) && button2.IsVisibleInTree() && button2.ButtonPressed && GodotObject.IsInstanceValid(control2) && control2.IsVisibleInTree() && GodotObject.IsInstanceValid(lineEdit3) && lineEdit3.IsVisibleInTree() && lineEdit3.Text == "mod/state-machine-callback-probe/allow-transition" && GodotObject.IsInstanceValid(button3) && button3.IsVisibleInTree() && button3.ButtonPressed;
		if (!guardUi)
		{
			GD.Print("[STATE_MACHINE_CALLBACK_GUARD_UI_DIAGNOSTIC] " + $"surfaceDefinition={surface?.Definition == definition} " + $"surfaceVisible={surface?.IsVisibleInTree()} " + $"detailsVisible={surface?.DetailsPanel?.IsVisibleInTree()} " + $"kind={surface?.ActiveDetailsKind} id={surface?.ActiveDetailsStableId} " + $"guardKind={(transition?.GuardDefinition as StateMachineGuardDefinition)?.Kind} " + $"button={GodotObject.IsInstanceValid(button2)} " + $"buttonVisible={button2?.IsVisibleInTree()} " + $"buttonPressed={button2?.ButtonPressed} " + $"catalog={GodotObject.IsInstanceValid(control2)} " + $"catalogVisible={control2?.IsVisibleInTree()} " + $"edit={GodotObject.IsInstanceValid(lineEdit3)} " + $"editVisible={lineEdit3?.IsVisibleInTree()} " + "text=" + lineEdit3?.Text + " " + $"card={GodotObject.IsInstanceValid(button3)} " + $"cardVisible={button3?.IsVisibleInTree()} " + $"cardPressed={button3?.ButtonPressed}");
		}
		StateMachineGuardDefinition resource = transition?.GuardDefinition as StateMachineGuardDefinition;
		bool dedicatedRouted = XWResourceEditorRegistry.TryOpen(resource, "user://state_machine_callback_guard_ui_probe.tres");
		await WaitFrames(5);
		XWStateGuardVisualResourceEditor xWStateGuardVisualResourceEditor = XWEditorInterface.Instance?.GetResourceEditor("state_guard_editor") as XWStateGuardVisualResourceEditor;
		LineEdit lineEdit4 = xWStateGuardVisualResourceEditor?.FindChild("RuntimeGuardCallbackKeyEdit", recursive: true, owned: false) as LineEdit;
		Button button4 = FindCallbackCard(xWStateGuardVisualResourceEditor, "RuntimeGuardCallback*", "mod/state-machine-callback-probe/allow-transition");
		bool flag2 = dedicatedRouted && GodotObject.IsInstanceValid(xWStateGuardVisualResourceEditor) && xWStateGuardVisualResourceEditor.IsVisibleInTree() && GodotObject.IsInstanceValid(lineEdit4) && lineEdit4.IsVisibleInTree() && lineEdit4.Text == "mod/state-machine-callback-probe/allow-transition" && GodotObject.IsInstanceValid(button4) && button4.IsVisibleInTree() && button4.ButtonPressed;
		if (!flag2)
		{
			GD.Print("[STATE_MACHINE_CALLBACK_DEDICATED_GUARD_UI_DIAGNOSTIC] " + $"routed={dedicatedRouted} " + $"editor={GodotObject.IsInstanceValid(xWStateGuardVisualResourceEditor)} " + $"editorVisible={xWStateGuardVisualResourceEditor?.IsVisibleInTree()} " + $"edit={GodotObject.IsInstanceValid(lineEdit4)} " + $"editVisible={lineEdit4?.IsVisibleInTree()} " + "text=" + lineEdit4?.Text + " " + $"card={GodotObject.IsInstanceValid(button4)} " + $"cardVisible={button4?.IsVisibleInTree()} " + $"cardPressed={button4?.ButtonPressed}");
		}
		return (UiOpened: uiOpened, UiEdited: uiEdited, UndoRedo: undoRedo, PhaseUi: phaseUi, GuardUi: guardUi, DedicatedGuardUi: flag2);
	}

	private static Button FindCallbackCard(Node root, string pattern, string callbackKey)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		foreach (Node item in root.FindChildren(pattern, "Button", recursive: true, owned: false))
		{
			if (item is Button button && button.Text.StartsWith(callbackKey, StringComparison.Ordinal))
			{
				return button;
			}
		}
		return null;
	}

	private async Task EnsureDetailsVisibleAsync(StateMachineGraphEditorSurface surface)
	{
		if (GodotObject.IsInstanceValid(surface) && !(surface.DetailsPanel?.IsVisibleInTree() ?? false))
		{
			(surface.FindChild("ToggleStateDetailsButton", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(3);
			StateMachineDetailsPanel detailsPanel = surface.DetailsPanel;
			if (detailsPanel == null || !detailsPanel.IsVisibleInTree())
			{
				surface.DetailsPanel?.Show();
				await WaitFrames(2);
			}
		}
	}

	private static Assembly CreateCallbackAssembly()
	{
		AssemblyName assemblyName = new AssemblyName("StateMachineCallbackRuntimeProbe_" + Guid.NewGuid().ToString("N"));
		TypeBuilder typeBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect).DefineDynamicModule(assemblyName.Name).DefineType("Generated.StateMachineCallbackRuntimeCallbacks", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
		Type type = typeof(StateMachineCallbackContext).MakeByRefType();
		Type type2 = typeof(StateMachineGuardContext).MakeByRefType();
		DefineCallback(typeBuilder, "Enter", "lifecycle", StateMachineCallbackPhase.Enter, typeof(void), new Type[1] { type }, "RecordLifecycleEnter");
		DefineCallback(typeBuilder, "Exit", "lifecycle", StateMachineCallbackPhase.Exit, typeof(void), new Type[1] { type }, "RecordLifecycleExit");
		DefineCallback(typeBuilder, "Process", "lifecycle", StateMachineCallbackPhase.Process, typeof(void), new Type[2]
		{
			type,
			typeof(double)
		}, "RecordLifecycleProcess");
		DefineCallback(typeBuilder, "PhysicsProcess", "lifecycle", StateMachineCallbackPhase.PhysicsProcess, typeof(void), new Type[2]
		{
			type,
			typeof(double)
		}, "RecordLifecyclePhysics");
		DefineCallback(typeBuilder, "Guard", "allow-transition", StateMachineCallbackPhase.Guard, typeof(bool), new Type[1] { type2 }, "RecordAllowTransition");
		DefineCallback(typeBuilder, "ThrowingGuard", "throw-transition", StateMachineCallbackPhase.Guard, typeof(bool), new Type[1] { type2 }, "RecordThrowingGuard");
		DefineCallback(typeBuilder, "IndependentEnter", "phase-enter", StateMachineCallbackPhase.Enter, typeof(void), new Type[1] { type }, "RecordLifecycleEnter");
		DefineCallback(typeBuilder, "IndependentExit", "phase-exit", StateMachineCallbackPhase.Exit, typeof(void), new Type[1] { type }, "RecordLifecycleExit");
		DefineCallback(typeBuilder, "IndependentProcess", "phase-process", StateMachineCallbackPhase.Process, typeof(void), new Type[2]
		{
			type,
			typeof(double)
		}, "RecordLifecycleProcess");
		DefineCallback(typeBuilder, "IndependentPhysics", "phase-physics", StateMachineCallbackPhase.PhysicsProcess, typeof(void), new Type[2]
		{
			type,
			typeof(double)
		}, "RecordLifecyclePhysics");
		return typeBuilder.CreateType().Assembly;
	}

	private static Assembly CreateRejectedCallbackAssembly(bool duplicateAttribute)
	{
		AssemblyName assemblyName = new AssemblyName("StateMachineCallbackRejectedProbe_" + Guid.NewGuid().ToString("N"));
		TypeBuilder typeBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect).DefineDynamicModule(assemblyName.Name).DefineType("Generated.RejectedStateMachineCallback", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
		Type[] array = ((!duplicateAttribute) ? Type.EmptyTypes : new Type[1] { typeof(StateMachineCallbackContext).MakeByRefType() });
		MethodBuilder methodBuilder = typeBuilder.DefineMethod(duplicateAttribute ? "Duplicate" : "InvalidSignature", MethodAttributes.Public | MethodAttributes.Static, typeof(void), array);
		if (array.Length != 0)
		{
			methodBuilder.DefineParameter(1, ParameterAttributes.In, "context");
		}
		CustomAttributeBuilder customAttribute = CreateCallbackAttribute("rejected", StateMachineCallbackPhase.Enter);
		methodBuilder.SetCustomAttribute(customAttribute);
		if (duplicateAttribute)
		{
			methodBuilder.SetCustomAttribute(CreateCallbackAttribute("rejected", StateMachineCallbackPhase.Enter));
		}
		ILGenerator iLGenerator = methodBuilder.GetILGenerator();
		if (duplicateAttribute)
		{
			MethodInfo meth = typeof(StateMachineCallbackRuntimeProbe).GetMethod("RecordLifecycleEnter", BindingFlags.Static | BindingFlags.Public) ?? throw new MissingMethodException(typeof(StateMachineCallbackRuntimeProbe).FullName, "RecordLifecycleEnter");
			iLGenerator.Emit(OpCodes.Ldarg_0);
			iLGenerator.Emit(OpCodes.Call, meth);
		}
		iLGenerator.Emit(OpCodes.Ret);
		return typeBuilder.CreateType().Assembly;
	}

	private static CustomAttributeBuilder CreateCallbackAttribute(string localKey, StateMachineCallbackPhase phase)
	{
		return new CustomAttributeBuilder(typeof(StateMachineCallbackAttribute).GetConstructor(new Type[2]
		{
			typeof(string),
			typeof(StateMachineCallbackPhase)
		}) ?? throw new MissingMethodException(typeof(StateMachineCallbackAttribute).FullName, ".ctor"), new object[2] { localKey, phase });
	}

	private static void DefineCallback(TypeBuilder type, string methodName, string localKey, StateMachineCallbackPhase phase, Type returnType, Type[] parameterTypes, string bridgeMethodName)
	{
		MethodBuilder methodBuilder = type.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Static, returnType, parameterTypes);
		for (int i = 0; i < parameterTypes.Length; i++)
		{
			methodBuilder.DefineParameter(i + 1, (i == 0) ? ParameterAttributes.In : ParameterAttributes.None, "argument" + i);
		}
		methodBuilder.SetCustomAttribute(CreateCallbackAttribute(localKey, phase));
		MethodInfo meth = typeof(StateMachineCallbackRuntimeProbe).GetMethod(bridgeMethodName, BindingFlags.Static | BindingFlags.Public) ?? throw new MissingMethodException(typeof(StateMachineCallbackRuntimeProbe).FullName, bridgeMethodName);
		ILGenerator iLGenerator = methodBuilder.GetILGenerator();
		for (int j = 0; j < parameterTypes.Length; j++)
		{
			iLGenerator.Emit((j == 0) ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);
		}
		iLGenerator.Emit(OpCodes.Call, meth);
		iLGenerator.Emit(OpCodes.Ret);
	}

	private static StateMachineDefinition CreatePhaseSpecificDefinition()
	{
		return new StateMachineDefinition
		{
			DefinitionId = "probe.state-machine-phase-specific-callbacks",
			RootStateId = "Root",
			States = 
			{
				new StateMachineStateDefinition
				{
					StableId = "Root",
					DisplayName = "根状态",
					Kind = StateMachineStateKind.Compound,
					InitialChildId = "Idle"
				},
				new StateMachineStateDefinition
				{
					StableId = "Idle",
					DisplayName = "待机",
					Kind = StateMachineStateKind.Atomic,
					ParentId = "Root",
					ProcessFlags = (StateMachineProcessFlags.Process | StateMachineProcessFlags.PhysicsProcess),
					EnterCallbackKey = "mod/state-machine-callback-probe/phase-enter",
					ExitCallbackKey = "mod/state-machine-callback-probe/phase-exit",
					ProcessCallbackKey = "mod/state-machine-callback-probe/phase-process",
					PhysicsProcessCallbackKey = "mod/state-machine-callback-probe/phase-physics"
				},
				new StateMachineStateDefinition
				{
					StableId = "Done",
					DisplayName = "完成",
					Kind = StateMachineStateKind.Atomic,
					ParentId = "Root",
					EnterCallbackKey = "mod/state-machine-callback-probe/phase-enter"
				}
			},
			Transitions = 
			{
				new StateMachineTransitionDefinition
				{
					StableId = "PhaseAdvance",
					SourceStateId = "Idle",
					TargetStateId = "Done",
					TriggerKind = StateMachineTriggerKind.Event,
					EventName = "PhaseAdvance"
				}
			}
		};
	}

	private static StateMachineDefinition CreateLegacyCoverageDefinition(bool fullyCovered)
	{
		StateMachineDefinition stateMachineDefinition = CreatePhaseSpecificDefinition();
		stateMachineDefinition.DefinitionId = (fullyCovered ? "probe.state-machine-legacy-fully-covered" : "probe.state-machine-legacy-partially-covered");
		StateMachineStateDefinition stateMachineStateDefinition = stateMachineDefinition.States.First((StateMachineStateDefinition state) => state.StableId == "Idle");
		StateMachineStateDefinition stateMachineStateDefinition2 = stateMachineDefinition.States.First((StateMachineStateDefinition state) => state.StableId == "Done");
		stateMachineStateDefinition.CallbackKey = "mod/state-machine-callback-probe/removed-legacy-lifecycle";
		stateMachineStateDefinition2.CallbackKey = "mod/state-machine-callback-probe/removed-legacy-lifecycle";
		stateMachineStateDefinition2.ExitCallbackKey = "mod/state-machine-callback-probe/phase-exit";
		if (!fullyCovered)
		{
			stateMachineStateDefinition.ExitCallbackKey = new StringName();
		}
		return stateMachineDefinition;
	}

	private static StateMachineDefinition CreateMissingDefinition()
	{
		return new StateMachineDefinition
		{
			DefinitionId = "probe.state-machine-callback-missing",
			RootStateId = "Root",
			States = 
			{
				new StateMachineStateDefinition
				{
					StableId = "Root",
					DisplayName = "Root",
					Kind = StateMachineStateKind.Compound,
					InitialChildId = "Idle"
				},
				new StateMachineStateDefinition
				{
					StableId = "Idle",
					DisplayName = "Idle",
					Kind = StateMachineStateKind.Atomic,
					ParentId = "Root",
					CallbackKey = "mod/state-machine-callback-missing/missing"
				}
			}
		};
	}

	private static StateMachineDefinition CreateThrowingGuardFallbackDefinition()
	{
		StateMachineDefinition stateMachineDefinition = new StateMachineDefinition
		{
			DefinitionId = "probe.state-machine-throwing-guard-fallback",
			RootStateId = "Root"
		};
		stateMachineDefinition.States.Add(new StateMachineStateDefinition
		{
			StableId = "Root",
			DisplayName = "Root",
			Kind = StateMachineStateKind.Compound,
			InitialChildId = "Idle"
		});
		string[] array = new string[3] { "Idle", "Preferred", "Fallback" };
		foreach (string text in array)
		{
			stateMachineDefinition.States.Add(new StateMachineStateDefinition
			{
				StableId = text,
				DisplayName = text,
				Kind = StateMachineStateKind.Atomic,
				ParentId = "Root"
			});
		}
		stateMachineDefinition.Transitions.Add(new StateMachineTransitionDefinition
		{
			StableId = "PreferredThrowingGuard",
			SourceStateId = "Idle",
			TargetStateId = "Preferred",
			TriggerKind = StateMachineTriggerKind.Event,
			EventName = "Choose",
			Priority = 100,
			DeclarationOrder = 0,
			GuardDefinition = new StateMachineGuardDefinition
			{
				Kind = StateMachineGuardKind.Callback,
				CallbackKey = "mod/state-machine-callback-probe/throw-transition"
			}
		});
		stateMachineDefinition.Transitions.Add(new StateMachineTransitionDefinition
		{
			StableId = "LowerPriorityFallback",
			SourceStateId = "Idle",
			TargetStateId = "Fallback",
			TriggerKind = StateMachineTriggerKind.Event,
			EventName = "Choose",
			Priority = 0,
			DeclarationOrder = 1
		});
		return stateMachineDefinition;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static CollectibleBindingReferences CreateDisposedBindingWeakReferences()
	{
		string text = "state-machine-callback-collectible-" + Guid.NewGuid().ToString("N");
		Assembly assembly = CreateCallbackAssembly();
		WeakReference assembly2 = new WeakReference(assembly);
		object obj = new object();
		WeakReference host = new WeakReference(obj);
		StateMachineCallbackRegistry stateMachineCallbackRegistry = new StateMachineCallbackRegistry();
		StateMachineCallbackRegistrationResult stateMachineCallbackRegistrationResult = stateMachineCallbackRegistry.RegisterAssembly(text, assembly);
		string text2 = "mod/" + text + "/lifecycle";
		StateMachineDefinition stateMachineDefinition = new StateMachineDefinition
		{
			DefinitionId = "probe.state-machine-callback-collectible",
			RootStateId = "Root"
		};
		stateMachineDefinition.States.Add(new StateMachineStateDefinition
		{
			StableId = "Root",
			DisplayName = "Root",
			Kind = StateMachineStateKind.Compound,
			InitialChildId = "Idle"
		});
		stateMachineDefinition.States.Add(new StateMachineStateDefinition
		{
			StableId = "Idle",
			DisplayName = "Idle",
			Kind = StateMachineStateKind.Atomic,
			ParentId = "Root",
			CallbackKey = text2
		});
		StateMachineProgram program = null;
		StateMachineValidationResult validation = null;
		StateMachineCallbackBinding binding = null;
		StateMachineBindingDiagnostic diagnostic;
		bool flag = stateMachineCallbackRegistrationResult.Success && StateMachineCompiler.TryCompile(stateMachineDefinition, out program, out validation) && validation.IsValid && stateMachineCallbackRegistry.TryAcquireBinding(text, program, obj, out binding, out diagnostic);
		binding?.Dispose();
		StateMachineUnloadBlockers blockers;
		bool flag2 = stateMachineCallbackRegistry.TryUnloadOwner(text, out blockers) && blockers.LeaseCount == 0;
		stateMachineDefinition = null;
		stateMachineCallbackRegistry = null;
		obj = null;
		assembly = null;
		return new CollectibleBindingReferences(assembly2, host, binding, flag && binding.IsDisposed && flag2);
	}

	private async Task ProbeRegistrationTransactionAsync(Assembly assembly)
	{
		bool[] array = new bool[2] { false, true };
		foreach (bool commit in array)
		{
			StateMachineCallbackRegistry registry = new StateMachineCallbackRegistry();
			Require(registry.RegisterAssembly("state-machine-callback-probe", assembly).Success, "Atomic fixture could not register old callbacks.");
			StateMachineDefinition stateMachineDefinition = new StateMachineDefinition
			{
				DefinitionId = "atomic-callback",
				RootStateId = "Root"
			};
			stateMachineDefinition.States.Add(new StateMachineStateDefinition
			{
				StableId = "Root",
				Kind = StateMachineStateKind.Atomic,
				EnterCallbackKey = "mod/callback-atomic-candidate/lifecycle"
			});
			Require(StateMachineCompiler.TryCompile(stateMachineDefinition, out var program, out var _), "Atomic callback program did not compile.");
			ManualResetEventSlim attempting = new ManualResetEventSlim();
			try
			{
				Task<(bool Bound, string[] Owners)> consumer = null;
				bool flag = registry.WithRegistrationGate(delegate
				{
					if (!registry.RegisterAssembly("callback-atomic-candidate", assembly).Success)
					{
						return false;
					}
					consumer = Task.Run(delegate
					{
						attempting.Set();
						StateMachineCallbackBinding binding;
						StateMachineBindingDiagnostic diagnostic;
						bool item = registry.TryAcquireBinding("callback-atomic-candidate", program, this, out binding, out diagnostic);
						binding?.Dispose();
						return (bound: item, registry.GetCatalogSnapshot().Entries.Select((StateMachineCallbackCatalogEntry entry) => entry.OwnerId).Distinct().ToArray());
					});
					if (!attempting.Wait(TimeSpan.FromSeconds(5L)))
					{
						throw new TimeoutException("Binding worker did not reach the transaction barrier.");
					}
					if (consumer.IsCompleted)
					{
						throw new InvalidOperationException("Binding escaped the registration transaction.");
					}
					StateMachineUnloadBlockers blockers2;
					return registry.TryUnloadOwner(commit ? "state-machine-callback-probe" : "callback-atomic-candidate", out blockers2);
				});
				Require(flag && consumer != null, "Callback transaction did not complete.");
				if (consumer != null)
				{
					(bool, string[]) tuple = await consumer.WaitAsync(TimeSpan.FromSeconds(5L));
					Require(tuple.Item1 == commit && tuple.Item2.SequenceEqual(new string[1] { commit ? "callback-atomic-candidate" : "state-machine-callback-probe" }), "Concurrent binding observed an uncommitted candidate or mixed callback owners.");
				}
				Require(registry.TryUnloadOwner(commit ? "callback-atomic-candidate" : "state-machine-callback-probe", out var _), "Atomic callback fixture retained a binding lease.");
			}
			finally
			{
				if (attempting != null)
				{
					((IDisposable)attempting).Dispose();
				}
			}
		}
		GD.Print("[STATE_MACHINE_CALLBACK_ATOMIC] commit=True rollback=True concurrentBinding=True");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool WaitForCollection(params WeakReference[] references)
	{
		for (int i = 0; i < 16; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			bool flag = false;
			for (int j = 0; j < references.Length; j++)
			{
				flag |= references[j]?.IsAlive ?? false;
			}
			if (!flag)
			{
				return true;
			}
		}
		return false;
	}

	private async Task<bool> OpenRealEditorAsync()
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
		if (!GodotObject.IsInstanceValid(modEditorManager))
		{
			return false;
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
		for (int frame = 0; frame < 900; frame++)
		{
			Control control = XWEditorInterface.Instance?.GetEditorPanel();
			if (GodotObject.IsInstanceValid(control) && control.IsVisibleInTree() && XWEditorInterface.Instance?.GetResourceEditor("state_machine_editor") is XWStateMachineVisualResourceEditor)
			{
				return true;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return false;
	}

	private async Task WaitFrames(int count)
	{
		for (int i = 0; i < count; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
		}
	}
}
