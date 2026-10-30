using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PVZHE.ModEditor.ScriptEditor;

public sealed class XWModComponentAssemblyCatalog
{
	public sealed class RefreshResult
	{
		public bool Success;

		public string Message = "";

		public XWScriptCompiler.CompileResult CompileResult;

		public XWModAssemblyLoader.LoadedModAssembly LoadedAssembly;

		public Assembly ReplacedAssembly;

		public IReadOnlyList<Type> DefinitionTypes = Array.Empty<Type>();

		public IReadOnlyList<Type> RuntimeTypes = Array.Empty<Type>();
	}

	private readonly XWModAssemblyLoader _loader = new XWModAssemblyLoader();

	private readonly SemaphoreSlim _refreshGate = new SemaphoreSlim(1, 1);

	private string _loadId = "";

	private Assembly _activeAssembly;

	private IReadOnlyList<Type> _activeDefinitionTypes = Array.Empty<Type>();

	private IReadOnlyList<Type> _activeRuntimeTypes = Array.Empty<Type>();

	private long _nextLoadGeneration;

	public Assembly ActiveAssembly => _activeAssembly;

	public IReadOnlyList<Type> ActiveDefinitionTypes => _activeDefinitionTypes;

	public IReadOnlyList<Type> ActiveRuntimeTypes => _activeRuntimeTypes;

	public async Task<RefreshResult> RefreshAsync(string projectRoot, CancellationToken cancellationToken = default(CancellationToken))
	{
		try
		{
			await _refreshGate.WaitAsync(cancellationToken);
		}
		catch (OperationCanceledException)
		{
			return new RefreshResult
			{
				Message = "Component catalog refresh was cancelled."
			};
		}
		RefreshResult result = new RefreshResult();
		try
		{
			if (!TryCleanInactiveAssemblies(out var diagnostic))
			{
				result.Message = diagnostic;
				return result;
			}
			if (!string.IsNullOrEmpty(_loadId) && !_loader.CanUnloadMod(_loadId, out var _))
			{
				result.Message = "旧组件或回调仍有活动实例，请释放后重试刷新。";
				return result;
			}
			RefreshResult refreshResult = result;
			refreshResult.CompileResult = await XWScriptCompiler.CompileModProjectAsync(projectRoot, cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();
			XWScriptCompiler.CompileResult compileResult = result.CompileResult;
			if (compileResult == null || !compileResult.Success || !File.Exists(compileResult.OutputAssemblyPath))
			{
				result.Message = compileResult?.Output ?? "Component Mod compilation failed.";
				return result;
			}
			string candidateId = BuildLoadId(projectRoot, Interlocked.Increment(ref _nextLoadGeneration));
			XWModAssemblyLoader.LoadedModAssembly loaded = _loader.LoadStagedModAssembly(candidateId, compileResult.OutputAssemblyPath, new string[1] { Path.GetDirectoryName(compileResult.OutputAssemblyPath) });
			candidateId = loaded.ModId;
			Type[] definitions = _loader.FindTypesAssignableTo(candidateId, typeof(CharacterComponentDefinition)).ToArray();
			Type[] runtimes = _loader.FindTypesAssignableTo(candidateId, typeof(CharacterComponentRuntime)).ToArray();
			if (definitions.Length == 0 || runtimes.Length == 0)
			{
				result.Message = "The compiled assembly contains no modern component Definition/Runtime pair.";
				return result;
			}
			cancellationToken.ThrowIfCancellationRequested();
			CharacterComponentRuntimeTypeRegistry.WithCreationGate(() => StateMachineCallbackRegistry.Shared.WithRegistrationGate(delegate
			{
				StateMachineUnloadBlockers blockers2;
				XWModAssemblyLoader.LoadedModAssembly detached;
				try
				{
					if (!string.IsNullOrEmpty(_loadId) && !_loader.CanUnloadMod(_loadId, out blockers2))
					{
						result.Message = "提交被阻止：旧组件或回调仍有活动实例。";
						return false;
					}
					StateMachineCallbackRegistrationResult stateMachineCallbackRegistrationResult = _loader.RegisterStateMachineCallbacks(candidateId);
					if (!stateMachineCallbackRegistrationResult.Success)
					{
						result.Message = stateMachineCallbackRegistrationResult.Error;
						return false;
					}
					if (_loader.RegisterCharacterComponentRuntimeTypes(candidateId) == 0)
					{
						result.Message = "候选没有可构造的组件 Runtime。";
						return false;
					}
					Assembly activeAssembly = _activeAssembly;
					if (!string.IsNullOrEmpty(_loadId) && !_loader.TryDetachMod(_loadId, out detached, out blockers2))
					{
						result.Message = "旧组件注册无法注销，已撤销候选。";
						return false;
					}
					_loadId = candidateId;
					_activeAssembly = loaded.Assembly;
					_activeDefinitionTypes = definitions;
					_activeRuntimeTypes = runtimes;
					loaded.IsStaged = false;
					result.LoadedAssembly = loaded;
					result.ReplacedAssembly = activeAssembly;
					result.DefinitionTypes = definitions;
					result.RuntimeTypes = runtimes;
					result.Message = "Component catalog refreshed.";
					return result.Success = true;
				}
				finally
				{
					if (!result.Success && !_loader.TryDetachMod(candidateId, out detached, out blockers2))
					{
						result.Message += " 候选注销受阻，保留记录以便重试。";
					}
				}
			}));
			return result;
		}
		catch (OperationCanceledException)
		{
			result.Message = "Component catalog refresh was cancelled.";
			return result;
		}
		catch (Exception ex3)
		{
			result.Message = ex3.GetBaseException().Message;
			return result;
		}
		finally
		{
			try
			{
				if (!TryCleanInactiveAssemblies(out var diagnostic2))
				{
					RefreshResult refreshResult2 = result;
					refreshResult2.Message = refreshResult2.Message + " " + diagnostic2;
				}
			}
			finally
			{
				_refreshGate.Release();
			}
		}
	}

	private bool TryCleanInactiveAssemblies(out string diagnostic)
	{
		diagnostic = "";
		XWModAssemblyLoader.LoadedModAssembly[] array = _loader.LoadedAssemblies.Values.Where((XWModAssemblyLoader.LoadedModAssembly item) => item.ModId != _loadId).ToArray();
		foreach (XWModAssemblyLoader.LoadedModAssembly loadedModAssembly in array)
		{
			try
			{
				if (_loader.UnloadMod(loadedModAssembly.ModId))
				{
					continue;
				}
				diagnostic = "程序集 " + loadedModAssembly.ModId + " 清理未完成，保留记录以便重试：" + loadedModAssembly.LastUnloadDiagnostic;
				goto IL_008f;
			}
			catch (Exception ex)
			{
				diagnostic = "程序集 " + loadedModAssembly.ModId + " 清理失败，保留记录以便重试：" + ex.GetBaseException().Message;
				goto IL_008f;
			}
			IL_008f:
			return false;
		}
		return true;
	}

	public async Task<bool> UnloadAsync()
	{
		await _refreshGate.WaitAsync();
		try
		{
			string diagnostic;
			if (string.IsNullOrWhiteSpace(_loadId))
			{
				return _loader.LoadedAssemblies.Count > 0 && TryCleanInactiveAssemblies(out diagnostic);
			}
			if (!_loader.TryDetachMod(_loadId, out var _, out var _))
			{
				return false;
			}
			_loadId = "";
			_activeAssembly = null;
			_activeDefinitionTypes = Array.Empty<Type>();
			_activeRuntimeTypes = Array.Empty<Type>();
			return TryCleanInactiveAssemblies(out diagnostic);
		}
		finally
		{
			_refreshGate.Release();
		}
	}

	private static string BuildLoadId(string projectRoot, long generation)
	{
		string s = Path.GetFullPath(projectRoot ?? ".").ToLowerInvariant();
		string value = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s))).Substring(0, 16);
		return $"ModEditorComponent_{value}_{generation:X16}";
	}
}
