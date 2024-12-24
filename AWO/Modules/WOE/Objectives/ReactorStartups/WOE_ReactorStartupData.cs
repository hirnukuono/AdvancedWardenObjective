using UnityEngine;

namespace AWO.Modules.WOE.Objectives.ReactorStartups;

internal enum ReactorWavePuzzleType
{
    Default,
    CustomLock,
    UseCommand_OnMainTerminal,
    UseCommand_InZone,
    PowerGenerator_InZone
}

[Obsolete]
internal sealed class WOE_ReactorStartupData : WOE_DataBase
{
    public bool RemoveMainStartupCommand { get; set; } = false;
    public bool RemoveMainVerifyCommand { get; set; } = false;
    public ScriptedWaveData[] WaveDatas { get; set; } = Array.Empty<ScriptedWaveData>();
    public ReactorWavePuzzleData[] WavePuzzles { get; set; } = Array.Empty<ReactorWavePuzzleData>();
}

internal enum SettingWarpMode
{
    Clamped,
    Repeat,
    PingPong
}

internal sealed class ScriptedWaveData
{
    public float[] IntroDuration { get; set; } = Array.Empty<float>();
    public SettingWarpMode IntroDurationWarpMode { get; set; } = SettingWarpMode.Clamped;
    public float[] WaveDuration { get; set; } = Array.Empty<float>();
    public SettingWarpMode WaveDurationWarpMode { get; set; } = SettingWarpMode.Clamped;
    public string[][] WaveInstructions { get; set; } = Array.Empty<string[]>();
    public SettingWarpMode WaveInstructionsWarpMode { get; set; } = SettingWarpMode.Clamped;
}

internal sealed class ReactorWavePuzzleData
{
    public ReactorWavePuzzleType Type { get; set; } = ReactorWavePuzzleType.Default;
    public bool ShowBeacon { get; set; } = false;
    public string BeaconText { get; set; } = "Auxiliary Terminal";
    public Color BeaconColor { get; set; } = Color.magenta;
    public string Command { get; set; } = "REACTOR_CONTINUE";
    public string CommandDescription { get; set; } = "CONTINUE REACTOR STARTUP PROCESS";
    public bool ForceJumpWaveWhenSolved { get; set; } = true;
}