using AmorLib.Utils.Extensions;
using BepInEx;
using GTFO.API;
using LevelGeneration;
using Player;
using System.Collections.Immutable;

namespace AWO.Modules.WEE.Events;

internal sealed class MultiProgressionEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.MultiProgression;

    
    public static ImmutableDictionary<LG_LayerType, List<LocalMPData>> TrackedMPs = ImmutableDictionary.CreateRange(new KeyValuePair<LG_LayerType, List<LocalMPData>>[]
    {
        new(LG_LayerType.MainLayer, new()),
        new(LG_LayerType.SecondaryLayer, new()),
        new(LG_LayerType.ThirdLayer, new())
    });
    
    private static PUI_GameObjectives ObjHud => GuiManager.PlayerLayer.WardenObjectives;

    public class LocalMPData
    {
        public PUI_ProgressionObjective ProgObj { get; private set; }
        public int Index { get; private set; }
        public string Header => ProgObj.m_header.text;
        public string Body => ProgObj.m_text.text;
        public int Priority { get; private set; }
        public bool IsHidden { get; set; } = false;

        public LocalMPData(PUI_ProgressionObjective a, int b, int c)
        {
            ProgObj = a;
            Index = b;
            Priority = c;
        }
    }

    protected override void OnSetup()
    {
        LevelAPI.OnEnterLevel += (() => WOManager.OnLocalPlayerEnterNewLayerCallback += (Action<PlayerAgent, LG_LayerType>)LocalToLayer);
        LevelAPI.OnLevelCleanup += OnLevelCleanup;
    }

    private void LocalToLayer(PlayerAgent player, LG_LayerType layer)
    {
        foreach (var localMP in TrackedMPs[layer])
        {
            if (localMP.IsHidden)
            {
                SetMultiProgression(localMP.Index, localMP.Header, localMP.Body, localMP.Priority);
                localMP.IsHidden = false;
            }
        }

        foreach (var otherLayers in TrackedMPs.Keys.Where(key => key != layer))
        {
            foreach (var nonLocalMP in TrackedMPs[otherLayers])
            {
                if (!nonLocalMP.IsHidden)
                {
                    ObjHud.RemoveProgressionObjective(nonLocalMP.Index);
                    nonLocalMP.IsHidden = true;
                }
            }
        }
    }

    private static void OnLevelCleanup()
    {
        TrackedMPs.ForEachValue(list => list.Clear());
    }

    protected override void TriggerCommon(WEE_EventData e)
    {
        foreach (var sub in e.MultiProgression)
        {
            int key = (int)sub.Index + 100;
            LG_LayerType layer = ResolveFieldsFallback(e.Layer, sub.Layer, false);
            var (header, body) = SetAndStyleSubObjective(sub, layer);
            bool isInVanillaMap = ObjHud.m_progressionObjectiveMap.ContainsKey(key);
            bool isInTrackedMap = TrackedMPs.Values.Any(list => list.Any(localMP => localMP.Index == key));

            if (!isInVanillaMap && !isInTrackedMap) // add new MP
            {
                LogDebug($"Adding new SubObjective with Index: {sub.Index}, IsLayerIndependent: {sub.IsLayerIndependent}");
                var prog = SetMultiProgression(key, header, body, sub.Priority);

                if (!sub.IsLayerIndependent)
                {
                    TrackedMPs[layer].Add(new(prog, key, sub.Priority));
                }
            }
            else if (!header.IsNullOrWhiteSpace()) // update MP text
            {
                LogDebug($"Updating text for SubObjective with Index: {sub.Index}");
                
                if (isInVanillaMap)
                {
                    var prog1 = ObjHud.m_progressionObjectiveMap[key];
                    prog1.m_header.text = header;
                    prog1.m_text.text = body;
                    prog1.ResizeAccordingToText();
                    CoroutineManager.BlinkIn(prog1.Header, 0.1f);
                    CoroutineManager.BlinkIn(prog1.SubObjective, 0.3f);
                    CoroutineManager.StartCoroutine(ObjHud.DoUpdateObjectiveLayoutAfterTime(CoroutineManager.BlinkDuration));
                }                
                if (isInTrackedMap)
                {
                    var prog2 = TrackedMPs.Values.SelectMany(list => list).FirstOrDefault(localMP => localMP.Index == key)!.ProgObj;
                    prog2.m_header.text = header;
                    prog2.m_text.text = body;
                }
            }
            else // remove MP
            {
                LogDebug($"Removing SubObjective with Index: {sub.Index}");
                ObjHud.RemoveProgressionObjective(key);
                TrackedMPs.ForEachValue(list => list.RemoveAll(LocalMPData => LocalMPData.Index == key));
            }
        }
    }

    public static PUI_ProgressionObjective SetMultiProgression(int key, string header, string body, int priority)
    {
        PUI_ProgressionObjective prog = UnityEngine.Object.Instantiate(ObjHud.m_progressionObjectivePrefab, ObjHud.m_progressionObjectivesParent);

        ObjHud.m_progressionObjectives.Add(prog);
        ObjHud.m_progressionObjectiveMap[key] = prog;
        ObjHud.m_progressionObjectivePriorityMap[prog] = priority;

        prog.m_header.text = header;
        prog.m_text.text = body;
        prog.ResizeAccordingToText();

        CoroutineManager.BlinkIn(prog.gameObject);
        CoroutineManager.BlinkIn(prog.Header, 0.1f);
        CoroutineManager.BlinkIn(prog.SubObjective, 0.5f);

        ObjHud.UpdateObjectivesLayout();
        CoroutineManager.StartCoroutine(ObjHud.DoUpdateObjectiveLayoutAfterTime(CoroutineManager.BlinkDuration));

        return prog;
    }

    public static (string, string) SetAndStyleSubObjective(WEE_SubObjectiveData sub, LG_LayerType layer)
    {
        string header = string.IsNullOrEmpty(sub.CustomSubObjectiveHeader) ? sub.CustomSubObjective : sub.CustomSubObjectiveHeader;
        string body = string.IsNullOrEmpty(sub.CustomSubObjectiveHeader) ? string.Empty : sub.CustomSubObjective;

        header = StyleText(header, layer, sub.OverrideTag, true);
        body = StyleText(body, layer, string.Empty, false);

        return (header, body);
    }

    public static string StyleText(string text, LG_LayerType layer, string tag, bool isHeader)
    {
        string styledText = WOManager.ReplaceFragmentsInString
        (
            layer,
            WOManager.GetCurrentChainIndex(layer),
            text
        );

        return isHeader
            ? ObjHud.StyleMainObjText(styledText, false, tag)
            : ObjHud.StyleSubObjText(styledText, PUI_GameObjectives.SubObjectiveStyleType.Normal);
    }
}
