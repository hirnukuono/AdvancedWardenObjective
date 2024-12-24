using GameData;

namespace AWO.Modules.WOE;

[Obsolete]
internal abstract class WOE_DataBase
{
    public uint ObjectiveID { get; set; }
    public WardenObjectiveDataBlock? GameData { get; set; }
}
