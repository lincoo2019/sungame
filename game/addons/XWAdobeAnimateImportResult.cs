namespace PVZHE.ModEditor.Tools;

public sealed class XWAdobeAnimateImportResult
{
	public bool Success { get; set; }

	public string ResourcePath { get; set; } = "";

	public string DatPath { get; set; } = "";

	public string Error { get; set; } = "";

	public XWAdobeAnimateBatchExportResult BatchResult { get; set; }
}
