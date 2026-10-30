using System.Collections.Generic;
using System.Threading;

namespace PVZHE.ModEditor.ScriptEditor;

public static class XWCodeErrorCheckerRegistry
{
	private static readonly Dictionary<string, XWCodeErrorChecker> _checkers = new Dictionary<string, XWCodeErrorChecker>();

	private static readonly object _checkLock = new object();

	private static bool _initialized;

	public static void Init()
	{
		lock (_checkLock)
		{
			if (!_initialized)
			{
				_initialized = true;
				Register("cs", new XWCSharpErrorChecker());
			}
		}
	}

	public static void Register(string extension, XWCodeErrorChecker checker)
	{
		_checkers[extension.ToLower()] = checker;
	}

	public static XWCodeErrorChecker GetChecker(string extension)
	{
		if (extension == null)
		{
			return null;
		}
		_checkers.TryGetValue(extension.ToLower(), out var value);
		return value;
	}

	public static List<XWCodeErrorChecker.ErrorData> CheckCode(string code, string extension)
	{
		return CheckCode(code, extension, CancellationToken.None);
	}

	public static List<XWCodeErrorChecker.ErrorData> CheckCode(string code, string extension, CancellationToken cancellationToken)
	{
		XWCodeErrorChecker checker = GetChecker(extension);
		if (checker == null)
		{
			return new List<XWCodeErrorChecker.ErrorData>();
		}
		bool flag = false;
		try
		{
			while (!flag)
			{
				cancellationToken.ThrowIfCancellationRequested();
				flag = Monitor.TryEnter(_checkLock, 20);
			}
			return new List<XWCodeErrorChecker.ErrorData>(checker.CheckCode(code, cancellationToken));
		}
		finally
		{
			if (flag)
			{
				Monitor.Exit(_checkLock);
			}
		}
	}
}
