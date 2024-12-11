using AWO.Networking;
using GTFO.API;
using LevelGeneration;
using static AWO.Sessions.LG_Objects;

namespace AWO.Sessions;

internal struct BlackoutStatus
{
    public bool blackoutEnabled;
}

internal static class BlackoutState
{
    private static StateReplicator<BlackoutStatus>? _Replicator;
    public static bool BlackoutEnabled { get; private set; } = false;

    internal static void AssetLoaded()
    {
        if (_Replicator != null) return;

        /*if (!StateReplicator<BlackoutStatus>.TryCreate(1u, new() { blackoutEnabled = false }, LifeTimeType.Permanent, out var replicator))
        {
            Logger.Error("Failed to create BlackoutState Replicator!");
            return;
        }
        _Replicator = replicator;*/
        _Replicator = StateReplicator<BlackoutStatus>.Create(1u, new() { blackoutEnabled = false }, LifeTimeType.Permanent);

        _Replicator.OnStateChanged += OnStateChanged;
        LevelAPI.OnLevelCleanup += LevelCleanup;
    }

    private static void LevelCleanup()
    {
        SetEnabled(false);
    }

    public static void SetEnabled(bool enabled)
    {
        _Replicator?.SetState(new() { blackoutEnabled = enabled });
    }

    private static void OnStateChanged(BlackoutStatus _, BlackoutStatus state, bool isRecall)
    {
        var isNormal = !state.blackoutEnabled;

        foreach (var display in TrackedList<LG_LabDisplay>())
        {
            if (display?.m_Text != null)
            {
                display.m_Text.enabled = isNormal;
            }
        }
        
        foreach (var terminal in TrackedList<LG_ComputerTerminal>())
        {
            if (terminal == null) continue;

            terminal.OnProximityExit();

            var interact = terminal.GetComponentInChildren<Interact_ComputerTerminal>(true);
            if (interact != null)
            {
                interact.enabled = isNormal;
                interact.SetActive(isNormal);
            }

            if (terminal.gameObject.TryAndGetComponent(out GUIX_VirtualSceneLink guixSceneLink) && guixSceneLink.m_virtualScene != null)
            {
                var virtCam = guixSceneLink.m_virtualScene.virtualCamera;
                float nearClip = isNormal ? 0.3f : 0.0f;
                float farClip = isNormal ? 1000.0f : 0.0f;
                virtCam.SetFovAndClip(virtCam.paramCamera.fieldOfView, nearClip, farClip);
            }

            if (terminal.m_text != null)
            {
                terminal.m_text.enabled = isNormal;
            }

            if (!isNormal)
            {
                var interactionSource = terminal.m_localInteractionSource;
                if (interactionSource != null && interactionSource.FPItemHolder.InTerminalTrigger)
                {
                    terminal.ExitFPSView();
                }
            }
        }

        foreach (var doorButton in TrackedList<LG_DoorButton>())
        {
            if (doorButton == null) continue;

            doorButton.m_anim.gameObject.SetActive(isNormal);
            doorButton.m_enabled = isNormal;

            if (isNormal)
            {
                var weakLock = doorButton.GetComponentInChildren<LG_WeakLock>();
                if (weakLock == null || weakLock.Status == eWeakLockStatus.Unlocked)
                {
                    doorButton.m_enabled = true;
                }
            }
        }

        foreach (var locks in TrackedList<LG_WeakLock>())
        {
            if (locks == null) continue;

            locks.m_intHack.m_isActive = isNormal;

            var display = locks.transform.FindChild("HackableLock/SecurityLock/g_WeakLock/Security_Display_Locked")
                ?? locks.transform.FindChild("HackableLock/Security_Display_Locked");
            if (display != null)
            {
                display.gameObject.active = isNormal;
            }
        }

        foreach (var activator in TrackedList<LG_HSUActivator_Core>())
        {
            if (activator == null || !activator.m_isWardenObjective) continue;

            if (activator.m_stateReplicator.State.status == eHSUActivatorStatus.WaitingForInsert)
            {
                activator.m_insertHSUInteraction.SetActive(isNormal);
                foreach (var obj in activator.m_activateWhenActive)
                {
                    obj.SetActive(isNormal);
                }
            }
        }

        BlackoutEnabled = state.blackoutEnabled;
    }
}
