using System.Collections.Generic;

namespace PVZHE.ModEditor.Inspector;

internal sealed record XWResourcePickerLibraryProfile(string Category, string DisplayName, IReadOnlyList<string> ClassNames, IReadOnlyList<string> BuiltInPathMarkers, string IconPath);
