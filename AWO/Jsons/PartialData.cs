using MTFO.Ext.PartialData;

namespace AWO.Jsons;

internal static class PartialData
{
    public static bool TryGetGUID(string text, out uint guid)
    {
        if (PersistentIDManager.TryGetId(text, out uint id))
        {
            guid = id;
            return true;
        }

        guid = 0u;
        return false;
    }
}
