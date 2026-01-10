using AmorLib.Utils;
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
        foreach (var eNav in e.NavMarker.Values)
        {
            int index = ResolveFieldsFallback(eNav.Index, e.Count, false);
            if (!NavMarkers.TryGetValue(index, out var marker))
            {
                var trackingObj = new GameObject($"AMAWO_{index}")
                {
                    transform = { position = GetPositionFallback(e.Position, e.SpecialText) }
                };
                marker = GuiManager.NavMarkerLayer.PlaceCustomMarker(eNav.Style, trackingObj, eNav.Title, e.Duration);
                marker.SetColor(eNav.Color);
                marker.SetPinEnabled(eNav.UsePin);
                var courseNode = CourseNodeUtil.GetCourseNode(e.Position, e.Position.GetDimension().DimensionIndex);
                if (courseNode != null)
                {
                    string str = "Z" + courseNode.m_area.m_navInfo.GetFormattedText(LG_NavInfoFormat.NumberOnly);
                    marker.SetSignInfo(str);
                }

                NavMarkers.Add(index, marker);

                if (e.Duration > 0f)
                {
                    CoroutineManager.StartCoroutine(DestroyAfterDelay(index, e.Duration).WrapToIl2Cpp());
                }
            }
            marker.SetVisible(e.Enabled);
        }
    }

    static IEnumerator DestroyAfterDelay(int index, float duration)
    {
        yield return new WaitForSeconds(duration);
        NavMarkers[index].SetVisible(false);
        NavMarkers.Remove(index);
    }
}
