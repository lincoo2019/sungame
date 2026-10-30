using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Debugging;
using PVZHE.ModEditor.ModSystem;

namespace PVZHE.ModEditor.ScriptEditor;

public sealed class XWScriptDebugSession
{
	public sealed class Breakpoint
	{
		public string FilePath { get; init; } = "";

		public int Line { get; init; }
	}

	public sealed class StartResult
	{
		public bool Success;

		public bool AssemblyLoaded;

		public bool EntryPointInvoked;

		public string Message = "";

		public XWScriptCompiler.CompileResult CompileResult;
	}

	public sealed class DebugContext
	{
		private Func<Action, Task> _mainThreadDispatcher;

		private Func<CancellationToken, Task> _nextProcessFrame;

		private XWModDebugController _debugController;

		public string ProjectRoot { get; init; } = "";

		public IReadOnlyList<Breakpoint> Breakpoints { get; init; } = Array.Empty<Breakpoint>();

		public CancellationToken CancellationToken { get; init; }

		public Node PreviewRoot { get; internal init; }

		public int MainThreadId { get; internal init; }

		internal void BindMainThread(Func<Action, Task> dispatcher, Func<CancellationToken, Task> nextProcessFrame)
		{
			_mainThreadDispatcher = dispatcher;
			_nextProcessFrame = nextProcessFrame;
		}

		internal void BindDebugger(XWModDebugController controller)
		{
			_debugController = controller;
		}

		public Task RunOnMainThreadAsync(Action action)
		{
			if (action == null)
			{
				return Task.CompletedTask;
			}
			return _mainThreadDispatcher?.Invoke(action) ?? Task.FromException(new InvalidOperationException("The ModEditor debug main-thread dispatcher is unavailable."));
		}

		public async Task<T> RunOnMainThreadAsync<T>(Func<T> action)
		{
			if (action == null)
			{
				return default(T);
			}
			T result = default(T);
			await RunOnMainThreadAsync(() => result = action());
			return result;
		}

		public Task NextProcessFrameAsync()
		{
			return _nextProcessFrame?.Invoke(CancellationToken) ?? Task.FromException(new InvalidOperationException("The ModEditor debug frame dispatcher is unavailable."));
		}

		public Task<XWModDebugCheckpointResult> CheckpointAsync(IReadOnlyDictionary<string, object> variables = null, bool forcePause = false, int frameDepth = -1, [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0, [CallerMemberName] string memberName = "")
		{
			if (_debugController == null)
			{
				return Task.FromResult(XWModDebugCheckpointResult.Continue);
			}
			IReadOnlyList<XWModDebugStackFrame> callStack = CaptureCallStack(filePath, line, memberName);
			XWModDebugCheckpoint checkpoint = XWModDebugCheckpoint.ForCSharp(filePath, line, memberName, frameDepth, callStack, CaptureVariables(variables), forcePause);
			return _debugController.CheckpointAsync(checkpoint, CancellationToken);
		}

		public Task<XWModDebugCheckpointResult> BreakAsync(IReadOnlyDictionary<string, object> variables = null, [CallerFilePath] string filePath = "", [CallerLineNumber] int line = 0, [CallerMemberName] string memberName = "")
		{
			return CheckpointAsync(variables, forcePause: true, -1, filePath, line, memberName);
		}

		[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "调试调用栈只读取运行时可用的显示元数据，缺失时会回退到编译器提供的调用位置。")]
		private static IReadOnlyList<XWModDebugStackFrame> CaptureCallStack(string fallbackFile, int fallbackLine, string fallbackMember)
		{
			List<XWModDebugStackFrame> list = new List<XWModDebugStackFrame>();
			try
			{
				StackFrame[] array = new StackTrace(1, fNeedFileInfo: true).GetFrames() ?? Array.Empty<StackFrame>();
				foreach (StackFrame stackFrame in array)
				{
					MethodBase method = stackFrame.GetMethod();
					if (!(method?.DeclaringType == typeof(DebugContext)))
					{
						string text = method?.DeclaringType?.FullName ?? "";
						string text2 = ((!string.IsNullOrWhiteSpace(text)) ? (text + "." + method?.Name) : (method?.Name ?? fallbackMember));
						string sourcePath = stackFrame.GetFileName() ?? "";
						int fileLineNumber = stackFrame.GetFileLineNumber();
						list.Add(new XWModDebugStackFrame($"csharp:{list.Count}:{text2}", text2, sourcePath, fileLineNumber));
						if (list.Count >= 24)
						{
							break;
						}
					}
				}
			}
			catch
			{
			}
			if (list.Count == 0 || !string.Equals(Path.GetFullPath(list[0].SourcePath ?? "."), Path.GetFullPath(fallbackFile ?? "."), StringComparison.OrdinalIgnoreCase) || list[0].Line != fallbackLine)
			{
				list.Insert(0, new XWModDebugStackFrame("csharp:caller", string.IsNullOrWhiteSpace(fallbackMember) ? "C# 检查点" : fallbackMember, fallbackFile, fallbackLine));
			}
			return list;
		}

		private static IReadOnlyDictionary<string, XWModDebugVariable> CaptureVariables(IReadOnlyDictionary<string, object> variables)
		{
			Dictionary<string, XWModDebugVariable> dictionary = new Dictionary<string, XWModDebugVariable>(StringComparer.Ordinal);
			if (variables == null)
			{
				return dictionary;
			}
			foreach (var (text2, obj2) in variables.Take(128))
			{
				if (!string.IsNullOrWhiteSpace(text2))
				{
					string typeName;
					string text3;
					try
					{
						typeName = obj2?.GetType().Name ?? "null";
						text3 = obj2?.ToString() ?? "null";
					}
					catch (Exception ex)
					{
						typeName = "无法读取";
						text3 = "<" + ex.GetType().Name + ">";
					}
					if (text3.Length > 512)
					{
						text3 = text3.Substring(0, 512) + "…";
					}
					dictionary[text2] = new XWModDebugVariable(text2, typeName, text3);
				}
			}
			return dictionary;
		}
	}

	private readonly XWModAssemblyLoader _loader = new XWModAssemblyLoader();

	private readonly SemaphoreSlim _lifecycleGate = new SemaphoreSlim(1, 1);

	private readonly XWScriptDebugHost _host;

	private readonly Node _previewRoot;

	private string _sessionId = "";

	private CancellationTokenSource _sessionCancellation;

	private Task _entryTask = Task.CompletedTask;

	public bool IsStarting { get; private set; }

	public bool IsRunning { get; private set; }

	public bool AssemblyLoaded { get; private set; }

	public bool EntryPointInvoked { get; private set; }

	public int LoadedAssemblyCount => _loader.LoadedAssemblies.Count;

	public bool EntryTaskCompleted
	{
		get
		{
			if (_entryTask != null)
			{
				return _entryTask.IsCompleted;
			}
			return true;
		}
	}

	public bool HasLiveSession
	{
		get
		{
			if (!IsStarting && !IsRunning && !AssemblyLoaded)
			{
				return !string.IsNullOrWhiteSpace(_sessionId);
			}
			return true;
		}
	}

	public WeakReference LastLoadContextWeakReference { get; private set; }

	public string ProjectRoot { get; private set; } = "";

	public string StatusText { get; private set; } = "未启动";

	public XWModDebugController DebugController { get; } = new XWModDebugController();

	public XWScriptDebugSession(XWScriptDebugHost host, Node previewRoot)
	{
		_host = host ?? throw new ArgumentNullException("host");
		_previewRoot = previewRoot ?? throw new ArgumentNullException("previewRoot");
		DebugController.Stop();
	}

	public Task<StartResult> StartAsync(string projectRoot, IReadOnlyCollection<Breakpoint> breakpoints)
	{
		return StartAsync(projectRoot, breakpoints, CancellationToken.None);
	}

	public async Task<StartResult> StartAsync(string projectRoot, IReadOnlyCollection<Breakpoint> breakpoints, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return new StartResult
			{
				Message = "Mod debug build was cancelled."
			};
		}
		bool flag;
		try
		{
			flag = await _lifecycleGate.WaitAsync(0, cancellationToken);
		}
		catch (OperationCanceledException)
		{
			return new StartResult
			{
				Message = "Mod debug build was cancelled."
			};
		}
		if (!flag)
		{
			return new StartResult
			{
				Message = "调试生命周期正忙；已拒绝并发启动。"
			};
		}
		try
		{
			if (IsStarting)
			{
				return new StartResult
				{
					Message = "调试会话正在启动。"
				};
			}
			if (IsRunning || AssemblyLoaded || !string.IsNullOrWhiteSpace(_sessionId))
			{
				return new StartResult
				{
					Message = "已有调试程序集仍在运行；请先成功停止后再启动。"
				};
			}
			string root = NormalizeRoot(projectRoot);
			if (string.IsNullOrWhiteSpace(root))
			{
				return new StartResult
				{
					Message = "没有打开可调试的 Mod 工程。"
				};
			}
			IsStarting = true;
			StatusText = "后台编译调试程序集…";
			try
			{
				XWScriptCompiler.CompileResult compile = await XWScriptCompiler.CompileModProjectAsync(root, cancellationToken);
				if (compile == null || !compile.Success || string.IsNullOrWhiteSpace(compile.OutputAssemblyPath) || !File.Exists(compile.OutputAssemblyPath))
				{
					StatusText = "编译失败";
					return new StartResult
					{
						CompileResult = compile,
						Message = "Mod 调试编译失败。"
					};
				}
				cancellationToken.ThrowIfCancellationRequested();
				if (!TryResolveDebugOwnerId(root, out var ownerId, out var error))
				{
					StatusText = error;
					return new StartResult
					{
						CompileResult = compile,
						Message = error
					};
				}
				XWModAssemblyLoader.LoadedModAssembly loaded = _loader.LoadModAssembly(ownerId, compile.OutputAssemblyPath, new string[1] { Path.GetDirectoryName(compile.OutputAssemblyPath) });
				_sessionId = ownerId;
				AssemblyLoaded = loaded?.Assembly != null;
				if (!AssemblyLoaded)
				{
					await StopCoreAsync();
					StatusText = "程序集加载失败";
					return new StartResult
					{
						CompileResult = compile,
						Message = "调试程序集未能加载。"
					};
				}
				Breakpoint[] array = (from item in breakpoints ?? Array.Empty<Breakpoint>()
					where item != null && IsInsideRoot(item.FilePath, root) && item.Line >= 0
					select new Breakpoint
					{
						FilePath = Path.GetFullPath(item.FilePath),
						Line = item.Line
					}).ToArray();
				DebugController.ReplaceBreakpoints(array.Select((Breakpoint item) => XWModDebugBreakpoint.ForCSharp(item.FilePath, item.Line + 1)));
				DebugController.Start();
				_sessionCancellation = new CancellationTokenSource();
				DebugContext context = new DebugContext
				{
					ProjectRoot = root,
					Breakpoints = array,
					CancellationToken = _sessionCancellation.Token,
					PreviewRoot = _previewRoot,
					MainThreadId = _host.MainThreadId
				};
				context.BindMainThread(_host.DispatchAsync, _host.NextProcessFrameAsync);
				context.BindDebugger(DebugController);
				(EntryPointInvoked, _entryTask) = await _host.DispatchAsync(() => InvokeDebugEntry(loaded.Assembly, context));
				if (_entryTask.IsFaulted)
				{
					throw _entryTask.Exception?.GetBaseException() ?? new InvalidOperationException("Mod debug entry failed.");
				}
				LastLoadContextWeakReference = new WeakReference(loaded.LoadContext);
				ProjectRoot = root;
				IsRunning = true;
				StatusText = (EntryPointInvoked ? "调试运行中 · 已进入入口" : "调试运行中 · 程序集已加载");
				return new StartResult
				{
					Success = true,
					AssemblyLoaded = true,
					EntryPointInvoked = EntryPointInvoked,
					CompileResult = compile,
					Message = StatusText
				};
			}
			catch (OperationCanceledException)
			{
				await StopCoreAsync();
				StatusText = "Mod debug build was cancelled.";
				return new StartResult
				{
					Message = StatusText
				};
			}
			catch (Exception ex3)
			{
				await StopCoreAsync();
				StatusText = "启动失败: " + ex3.GetBaseException().Message;
				return new StartResult
				{
					Message = StatusText
				};
			}
			finally
			{
				IsStarting = false;
			}
			IL_07db:
			StartResult result;
			return result;
		}
		finally
		{
			_lifecycleGate.Release();
		}
	}

	public async Task<bool> StopAsync(int timeoutMilliseconds = 2500)
	{
		await _lifecycleGate.WaitAsync();
		try
		{
			return await StopCoreAsync(timeoutMilliseconds);
		}
		finally
		{
			_lifecycleGate.Release();
		}
	}

	private async Task<bool> StopCoreAsync(int timeoutMilliseconds = 2500)
	{
		bool wasActive = IsRunning || AssemblyLoaded || !string.IsNullOrWhiteSpace(_sessionId);
		DebugController.Stop();
		CancellationTokenSource cancellation = _sessionCancellation;
		Task entryTask = _entryTask ?? Task.CompletedTask;
		cancellation?.Cancel();
		if (!entryTask.IsCompleted && await Task.WhenAny(entryTask, Task.Delay(Math.Max(100, timeoutMilliseconds))) != entryTask)
		{
			StatusText = "停止失败：调试入口未响应取消请求";
			return false;
		}
		try
		{
			await entryTask;
		}
		catch (OperationCanceledException)
		{
		}
		catch
		{
		}
		_entryTask = Task.CompletedTask;
		_sessionCancellation = null;
		cancellation?.Dispose();
		await _host.ClearPreviewRootAsync(_previewRoot);
		string sessionId = _sessionId;
		if (!string.IsNullOrWhiteSpace(sessionId) && !(await Task.Run(() => _loader.UnloadMod(sessionId))))
		{
			IsRunning = false;
			AssemblyLoaded = true;
			EntryPointInvoked = false;
			IsStarting = false;
			StatusText = "停止未完成：状态机动作仍有活动运行实例。请停止相关预览后再次点击停止。";
			return false;
		}
		_sessionId = "";
		ProjectRoot = "";
		IsRunning = false;
		AssemblyLoaded = false;
		EntryPointInvoked = false;
		IsStarting = false;
		StatusText = "已停止";
		return wasActive;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Player-authored debug entry points are discovered from a runtime-loaded Mod assembly by design.")]
	private static (bool Invoked, Task EntryTask) InvokeDebugEntry(Assembly assembly, DebugContext context)
	{
		foreach (Type item in GetTypesSafe(assembly).OrderBy<Type, string>((Type type) => type.FullName, StringComparer.Ordinal))
		{
			MethodInfo[] methods = item.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo in methods)
			{
				if (!string.Equals(methodInfo.Name, "ModEditorDebugEntry", StringComparison.Ordinal))
				{
					continue;
				}
				ParameterInfo[] parameters = methodInfo.GetParameters();
				object[] parameters2;
				if (parameters.Length == 0)
				{
					parameters2 = Array.Empty<object>();
				}
				else
				{
					if (parameters.Length != 1 || !(parameters[0].ParameterType == typeof(DebugContext)))
					{
						continue;
					}
					parameters2 = new object[1] { context };
				}
				object obj = methodInfo.Invoke(null, parameters2);
				return (Invoked: true, EntryTask: (obj as Task) ?? Task.CompletedTask);
			}
		}
		return (Invoked: false, EntryTask: Task.CompletedTask);
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Runtime-loaded Mod debug assemblies require type discovery by design.")]
	private static IEnumerable<Type> GetTypesSafe(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			return ex.Types.Where((Type type) => type != null);
		}
	}

	private static string NormalizeRoot(string root)
	{
		if (string.IsNullOrWhiteSpace(root))
		{
			return "";
		}
		try
		{
			string fullPath = Path.GetFullPath(root);
			return Directory.Exists(fullPath) ? fullPath : "";
		}
		catch
		{
			return "";
		}
	}

	private static bool TryResolveDebugOwnerId(string projectRoot, out string ownerId, out string error)
	{
		ownerId = "";
		error = "";
		try
		{
			XWModManifest xWModManifest = XWModManifest.Load(Path.Combine(projectRoot ?? string.Empty, "mod.json"));
			if (xWModManifest == null)
			{
				error = "调试启动失败：找不到 mod.json。";
				return false;
			}
			if (!StateMachineCallbackKey.TryNormalizeOwnerId(xWModManifest.Id, out ownerId) || string.Equals(ownerId, "builtin", StringComparison.Ordinal))
			{
				ownerId = "";
				error = "调试启动失败：mod.json 的 id 无效。";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			error = "调试启动失败：读取 Mod ID 时出错：" + ex.Message;
			return false;
		}
	}

	private static bool IsInsideRoot(string path, string root)
	{
		if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(root))
		{
			return false;
		}
		try
		{
			string fullPath = Path.GetFullPath(path);
			string value = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
			return fullPath.StartsWith(value, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}
}
