namespace AWO.Modules.WOE.Objectives.Uplinks;

[Obsolete]
internal sealed class WOE_UplinkContext : WOE_ContextBase
{
    public override eWardenObjectiveType TargetType => eWardenObjectiveType.TerminalUplink;
    public override Type DataType => typeof(WOE_UplinkData);
}
