using GTFO.API;
using LevelGeneration;
using System.Collections;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal class SpawnNavMarkerEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SpawnNavMarker;
    public static readonly Dictionary<int, NavMarker> NavMarkers = new();

    protected override void OnSetup()
    {
        LevelAPI.OnLevelCleanup += OnLevelCleanup;
    }

    private void OnLevelCleanup()
    {
        NavMarkers.Clear();
    }

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!NavMarkers.TryGetValue(e.Count, out var marker))
        {
            var trackingObj = new GameObject($"AMAWO_{e.Count}") 
            { 
                transform = { position = GetPositionFallback(e.Position, e.SpecialText) } 
            };
            marker = GuiManager.NavMarkerLayer.PlaceCustomMarker(e.NavMarker.Style, trackingObj, e.NavMarker.Title, e.Duration);
            marker.SetColor(e.NavMarker.Color);
            marker.SetPinEnabled(e.NavMarker.UsePin);
            if (Dimension.TryGetCourseNodeFromPos(e.Position, out var courseNode))
            {
                string str = "Z" + courseNode.m_area.m_navInfo.GetFormattedText(LG_NavInfoFormat.NumberOnly);
                marker.SetSignInfo(str);
            }
            
            NavMarkers.Add(e.Count, marker);

            if (e.Duration > 0.0f)
            {
                CoroutineManager.StartCoroutine(DestroyAfterDelay(e.Count, e.Duration).WrapToIl2Cpp());
            }
        }

        marker.SetVisible(e.Enabled);
    }

    static IEnumerator DestroyAfterDelay(int index, float duration)
    {
        yield return new WaitForSeconds(duration);
        NavMarkers.Remove(index);
    }
}
