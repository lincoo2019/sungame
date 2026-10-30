using System.Collections.Generic;
using System.Threading;
using Godot;

namespace PVZHE.ModEditor.ScriptEditor;

public abstract partial class XWCodeErrorChecker : RefCounted
{
	public enum Severity
	{
		Error,
		Warning,
		Info
	}

	public class ErrorData
	{
		public int Line;

		public int Column;

		public int EndLine = -1;

		public int EndColumn = -1;

		public string Message = "";

		public Severity SeverityLevel;

		public string Code = "";

		public string FilePath = "";
	}

	public const int MaxDiagnostics = 256;

	protected readonly List<ErrorData> _errors = new List<ErrorData>();

	private CancellationToken _cancellationToken;

	public IReadOnlyList<ErrorData> Errors => _errors;

	protected CancellationToken CancellationToken => _cancellationToken;

	public List<ErrorData> CheckCode(string code)
	{
		return CheckCode(code, CancellationToken.None);
	}

	public List<ErrorData> CheckCode(string code, CancellationToken cancellationToken)
	{
		_errors.Clear();
		_cancellationToken = cancellationToken;
		try
		{
			ThrowIfCancellationRequested();
			DoCheck(code);
			ThrowIfCancellationRequested();
			return _errors;
		}
		finally
		{
			_cancellationToken = CancellationToken.None;
		}
	}

	protected abstract void DoCheck(string code);

	protected void ThrowIfCancellationRequested()
	{
		_cancellationToken.ThrowIfCancellationRequested();
	}

	protected void AddError(int line, int column, string message, Severity severity = Severity.Error, string code = "")
	{
		if (_errors.Count < 256)
		{
			_errors.Add(new ErrorData
			{
				Line = line,
				Column = column,
				Message = message,
				SeverityLevel = severity,
				Code = code
			});
		}
	}

	protected void AddErrorRange(int startLine, int startColumn, int endLine, int endColumn, string message, Severity severity = Severity.Error, string code = "")
	{
		if (_errors.Count < 256)
		{
			_errors.Add(new ErrorData
			{
				Line = startLine,
				Column = startColumn,
				EndLine = endLine,
				EndColumn = endColumn,
				Message = message,
				SeverityLevel = severity,
				Code = code
			});
		}
	}

	public int GetErrorCount()
	{
		int num = 0;
		foreach (ErrorData error in _errors)
		{
			if (error.SeverityLevel == Severity.Error)
			{
				num++;
			}
		}
		return num;
	}

	public int GetWarningCount()
	{
		int num = 0;
		foreach (ErrorData error in _errors)
		{
			if (error.SeverityLevel == Severity.Warning)
			{
				num++;
			}
		}
		return num;
	}

	public bool HasErrors()
	{
		foreach (ErrorData error in _errors)
		{
			if (error.SeverityLevel == Severity.Error)
			{
				return true;
			}
		}
		return false;
	}

	public bool HasWarnings()
	{
		foreach (ErrorData error in _errors)
		{
			if (error.SeverityLevel == Severity.Warning)
			{
				return true;
			}
		}
		return false;
	}

	public List<ErrorData> GetOnlyErrors()
	{
		List<ErrorData> list = new List<ErrorData>();
		foreach (ErrorData error in _errors)
		{
			if (error.SeverityLevel == Severity.Error)
			{
				list.Add(error);
			}
		}
		return list;
	}

	public List<ErrorData> GetOnlyWarnings()
	{
		List<ErrorData> list = new List<ErrorData>();
		foreach (ErrorData error in _errors)
		{
			if (error.SeverityLevel == Severity.Warning)
			{
				list.Add(error);
			}
		}
		return list;
	}
}
