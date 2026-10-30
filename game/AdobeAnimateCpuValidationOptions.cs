internal sealed record AdobeAnimateCpuValidationOptions(string ReportPath, AdobeAnimateCpuValidationFault Fault)
{
	public static AdobeAnimateCpuValidationOptions Normal(string reportPath)
	{
		return new AdobeAnimateCpuValidationOptions(reportPath, AdobeAnimateCpuValidationFault.None);
	}

	public static AdobeAnimateCpuValidationOptions ForExport(string exportPath)
	{
		return new AdobeAnimateCpuValidationOptions(exportPath + ".adobe-animate-cpu-validation.json", AdobeAnimateCpuValidationFault.None);
	}
}
