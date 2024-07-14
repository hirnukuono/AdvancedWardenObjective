using AWO.WEE.Events;
using BepInEx;
using FluffyUnderware.DevTools.Extensions;
using LevelGeneration;
using Player;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AWO.Modules.WEE.Events.HUD;

internal sealed class MultiProgressionEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.MultiProgression;

    protected override void TriggerCommon(WEE_EventData e)
    {
        // Credits to McBreezy
        CoroutineManager.StartCoroutine(ApplyProgression(e).WrapToIl2Cpp());
    }

    static IEnumerator ApplyProgression(WEE_EventData e)
    {
        PUI_GameObjectives wo = GuiManager.PlayerLayer.m_wardenObjective;

        foreach (var sub in e.MultiProgression)
        {
            int num = (int)sub.Index + 100;

            string body = sub.CustomSubObjective.ToString().IsNullOrWhiteSpace() ? string.Empty : wo.StyleSubObjText(WOManager.ReplaceFragmentsInString(sub.Layer, WOManager.GetCurrentChainIndex(sub.Layer), sub.CustomSubObjective.ToString(), true), PUI_GameObjectives.SubObjectiveStyleType.Normal, 5);
            string header = FormatHeader(wo, sub, ref body);

            if (!wo.m_progressionObjectiveMap.ContainsKey(num))
            {
                PUI_ProgressionObjective prog = Object.Instantiate(wo.m_progressionObjectivePrefab, wo.m_progressionObjectivesParent);
                wo.m_progressionObjectives.Add(prog);
                wo.m_progressionObjectiveMap[num] = prog;
                wo.m_progressionObjectivePriorityMap[prog] = 1;
                prog.m_header.text = header;
                prog.m_text.text = body;
                //if (sub.LocalToLayer)
                    //prog.gameObject.AddComponent<LocalToLayer>().Attach(sub.Layer, prog
                prog.ResizeAccordingToText();
                CoroutineManager.BlinkIn(prog.gameObject);
                CoroutineManager.BlinkIn(prog.Header, 0.1f);
                CoroutineManager.BlinkIn(prog.SubObjective, 0.5f);
            }
            else if (!header.IsNullOrWhiteSpace())
            {
                PUI_ProgressionObjective prog = wo.m_progressionObjectiveMap[num];
                prog.m_header.text = header;
                prog.m_text.text = body;
                prog.ResizeAccordingToText();
                CoroutineManager.BlinkIn(prog.Header, 0.1f);
                CoroutineManager.BlinkIn(prog.SubObjective, 0.5f);
            }
            else
            {
                wo.RemoveProgressionObjective(num);
            }

            yield return null;
        }
    }

    private static string FormatHeader(PUI_GameObjectives wo, WEE_SubObjectiveData sub, ref string body)
    {
        string header = sub.CustomSubObjectiveHeader.ToString();

        if (header.IsNullOrWhiteSpace())
        {
            header = sub.CustomSubObjective.ToString();
            body = string.Empty;
        }

        return wo.StyleMainObjText
        (
            WOManager.ReplaceFragmentsInString(sub.Layer, WOManager.GetCurrentChainIndex(sub.Layer), header, true),
            false,
            sub.OverrideTag
        );
    }

    /*public class LocalToLayer : MonoBehaviour
    {
        public LG_LayerType Layer;
        private LG_LayerType previousLayer;
        public PUI_ProgressionObjective Progression;
        private bool Initialized;
        public bool HasVisitedLayer = false;

        public void Attach(LG_LayerType Layer, PUI_ProgressionObjective Progression)
        {
            this.Layer = Layer;
            this.Progression = Progression;
            Initialized = true;
            previousLayer = PlayerManager.GetLocalPlayerAgent().CourseNode.LayerType;
        }

        public void Update()
        {
            if (!Initialized) return;

            LG_LayerType currentLayer = PlayerManager.GetLocalPlayerAgent().CourseNode.LayerType;

            if (currentLayer != previousLayer)
            {
                if (currentLayer != Layer && HasVisitedLayer)
                {
                    CoroutineManager.BlinkOut(Progression.Header, 0.1f);
                    CoroutineManager.BlinkOut(Progression.SubObjective, 0.5f);
                }
                else if (currentLayer == Layer && HasVisitedLayer)
                {
                    CoroutineManager.BlinkIn(Progression.Header, 0.1f);
                    CoroutineManager.BlinkIn(Progression.SubObjective, 0.5f);
                }
                else
                {
                    HasVisitedLayer = true;
                }

                previousLayer = currentLayer;
            }
        }
    }*/
}
