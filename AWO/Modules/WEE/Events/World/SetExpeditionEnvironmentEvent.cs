using GameData;
using GTFO.API;
using LevelGeneration;
using UnityEngine;

namespace AWO.Modules.WEE.Events;

internal sealed class SetExpeditionEnvironmentEvent : BaseEvent
{
    public override WEE_Type EventType => WEE_Type.SetExpeditionEnvironment;

    private static ExpeditionData? _activeExpedition;
    private static float _cachedRealityWetness;
    private static Color _cachedRealityDustColor;
    private static float _cachedRealityDustTurbulence;

    protected override void OnSetup()
    {
        LevelAPI.OnLevelDataUpdated += OnLevelDataUpdated;
        LevelAPI.OnBuildStart += OnBuildStart;
        LevelAPI.OnLevelCleanup += OnLevelCleanup;
    }

    private void OnLevelDataUpdated(ActiveExpedition activeExp, ExpeditionInTierData expData)
    {
        _activeExpedition = expData.Expedition;
    }

    private void OnBuildStart() // cache reality env data
    {
        if (_activeExpedition == null) return;
        _cachedRealityWetness = _activeExpedition.EnvironmentWetness;
        _cachedRealityDustColor = _activeExpedition.DustColor;
        _cachedRealityDustTurbulence = _activeExpedition.DustTurbulence;
    }

    private void OnLevelCleanup() // restore reality env data
    {
        if (_activeExpedition == null) return;
        _activeExpedition.EnvironmentWetness = _cachedRealityWetness;
        _activeExpedition.DustColor = _cachedRealityDustColor;
        _activeExpedition.DustTurbulence = _cachedRealityDustTurbulence;
    }

    protected override void TriggerCommon(WEE_EventData e)
    {
        if (!Dimension.GetDimension(e.DimensionIndex, out var dim) || dim.DimensionData == null)
        { 
            LogError("Dimension does not exist, or unable to find DimensionData?");
            return;
        }
        
        var envData = e.EnvironmentData ?? new();

        if (e.DimensionIndex == eDimensionIndex.Reality)
        {
            if (_activeExpedition == null) return;
            _activeExpedition.EnvironmentWetness = envData.EnvironmentWetness.GetAbsValue(_activeExpedition.EnvironmentWetness);
            _activeExpedition.DustColor = envData.UpdateColor ? envData.DustColor : _activeExpedition.DustColor;
            _activeExpedition.DustTurbulence = envData.DustTurbulence.GetAbsValue(_activeExpedition.DustTurbulence);
        }
        else
        {
            var dimData = dim.DimensionData;
            dimData.EnvironmentWetness = envData.EnvironmentWetness.GetAbsValue(dimData.EnvironmentWetness);
            dimData.DustColor = envData.UpdateColor ? envData.DustColor : dimData.DustColor;
            dimData.DustAlphaBoost = envData.DustAlphaBoost.GetAbsValue(dimData.DustAlphaBoost);
            dimData.DustTurbulence = envData.DustTurbulence.GetAbsValue(dimData.DustTurbulence);
        }
    }
}
