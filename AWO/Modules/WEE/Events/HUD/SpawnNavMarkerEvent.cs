using AWO.WEE.Events;
using BepInEx;
using GTFO.API;
using LevelGeneration;
using UnityEngine;

namespace AWO.Modules.WEE.Events.HUD;

internal class SpawnNavMarkerEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SpawnNavMarker;

    protected override void TriggerCommon(WEE_EventData e)
    {
        var name = $"AMAWO_{e.Count}";
        var marker = EntryPoint.NavMarkers.FirstOrDefault(go => go.name == name);

        if (marker == null)
        {
            var nav = new GameObject(name).AddComponent<LG_WorldEventNavMarker>();
            nav.transform.position = GetNavMarkerPosition(e.Position, e.SpecialText);
            nav.m_placeNavMarkerOnGo.type = PlaceNavMarkerOnGO.eMarkerType.Guidance;
            nav.m_placeNavMarkerOnGo.m_placeOnStart = true;

            EntryPoint.NavMarkers.Add(nav);
            LevelAPI.OnLevelCleanup += OnLevelCleanup;
            marker = nav;
        }

        if (e.Enabled)
        {
            marker.OnTrigger(null, true, true);
        }
        else
        {
            marker.OnTrigger(null, false, true);
        }
    }

    private static Vector3 GetNavMarkerPosition(Vector3 pos, string weObjectFilter)
    {
        if (pos != Vector3.zero) return pos;

        if (weObjectFilter.IsNullOrWhiteSpace()) return Vector3.zero;

        foreach (var weObject in WorldEventManager.Current.m_worldEventObjects)
            if (weObject.gameObject.name == weObjectFilter)
                return weObject.gameObject.transform.position;

        Logger.Error($"[SpawnNavMarkerEvent] Could not find WorldEventObjectFilter {weObjectFilter}");
        return Vector3.zero;
    }

    private void OnLevelCleanup()
    {
        Logger.Debug("[SpawnNavMarkerEvent] Cleaning up Nav Markers...");
        EntryPoint.NavMarkers.Clear();
    }
}
