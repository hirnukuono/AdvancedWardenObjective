global using AWO.CustomFields;
global using AWO.Utils;
global using BepInEx.Unity.IL2CPP.Utils.Collections;
global using Il2CppInterop.Runtime.Attributes;
global using WOManager = WardenObjectiveManager;

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "UNT0026:GetComponent always allocates / Use TryGetComponent", Justification = "TryGetComponent is broken in GTFO Il2Cpp Environment")]
[assembly: SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "GTFO is a windows-only game.")]
[assembly: SuppressMessage("Style", "IDE0130:Namespace does not match folder structure", Justification = "Shut up: Extraneous namespaces.", Scope = "namespace", Target = "~N:AWO.Modules.WEE.Events")]
