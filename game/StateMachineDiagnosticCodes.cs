public static class StateMachineDiagnosticCodes
{
	public const string MissingDefinition = "SM001";

	public const string DuplicateStableId = "SM002";

	public const string InvalidRoot = "SM003";

	public const string InvalidParent = "SM004";

	public const string InvalidInitialChild = "SM005";

	public const string InvalidTransitionSource = "SM006";

	public const string InvalidTransitionTarget = "SM007";

	public const string NegativeDelay = "SM008";

	public const string AutomaticCycle = "SM009";

	public const string UnsupportedGuard = "SM010";

	public const string BaseDefinitionCycle = "SM011";

	public const string InvalidHistory = "SM012";

	public const string InvalidGuard = "SM013";

	public const string CompositionResourceFailure = "SM014";

	public const string InvalidGuardTree = "SM015";

	public const string InvalidCallbackKey = "SM016";
}
