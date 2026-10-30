namespace PVZHE.ModEditor.Tools;

public sealed class XWAdobeAnimateBatchExportResult
{
	public bool Success { get; set; }

	public string ExporterPath { get; set; } = "";

	public string DatPath { get; set; } = "";

	public int ExitCode { get; set; }

	public string Output { get; set; } = "";

	public string Error { get; set; } = "";
}
