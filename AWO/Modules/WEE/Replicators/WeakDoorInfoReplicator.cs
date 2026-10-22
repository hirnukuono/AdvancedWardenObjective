using AmorLib.Networking.StateReplicators;
using LevelGeneration;
using SNetwork;
using UnityEngine;

namespace AWO.Modules.WEE.Replicators;

public struct WeakDoorState
{
    public bool canInteract;

    public WeakDoorState()
    {
        canInteract = true;
    }

    public WeakDoorState(bool enable) 
    {
        canInteract = enable;
    }
}

public readonly struct WeakDoorInfo
{
    public readonly LG_WeakDoor weakDoor;
    public readonly (LG_DoorButton button, Interact_Base interact)[] buttons;

    public WeakDoorInfo(LG_WeakDoor door)
    {
        weakDoor = door;
        var buttons = weakDoor.m_buttons;

        this.buttons = new (LG_DoorButton, Interact_Base)[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            this.buttons[i] = (buttons[i], buttons[i].GetComponent<Interact_Base>());
        }
    }
}

public sealed class WeakDoorInfoReplicator : MonoBehaviour, IStateReplicatorHolder<WeakDoorState>
{
    [HideFromIl2Cpp]
    public StateReplicator<WeakDoorState>? Replicator { get; private set; }
    public LG_Zone Zone = null!;
    public List<WeakDoorInfo> WeakDoorsInZone = new();
    private bool _canInteract = true;

    public void Setup(LG_WeakDoor weakDoor)
    {
        Zone ??= GetComponent<LG_Zone>();
        WeakDoorsInZone.Add(new(weakDoor));
        Replicator ??= StateReplicator<WeakDoorState>.Create((uint)Zone.ID + 1, new(), LifeTimeType.Session, this);
    }

    public void OnDestroy()
    {
        Replicator?.Unload();
    }

    public void SetCanInteract(bool enable)
    {
        Replicator?.SetState(new(enable));
    }

    public void OnStateChange(WeakDoorState oldState, WeakDoorState state, bool isRecall)
    {
        if (oldState.canInteract == state.canInteract)
            return;

        _canInteract = state.canInteract;
        foreach (var (button, interact) in WeakDoorsInZone.SelectMany(info => info.buttons))
        {
            button.m_anim.gameObject.active = _canInteract;
            interact.SetActive(_canInteract);
        }
    }

    public void PlayButtonAnimation(bool open)
    {
        if (!_canInteract) 
            return;

        foreach ((var button, _) in WeakDoorsInZone.SelectMany(info => info.buttons))
        {
            var anim = button.m_anim;
            if (!anim.enabled)
            {
                anim.enabled = true;
                button.StartCoroutine(button.DelayDisableAnimator());
            }
            anim.Play(open ? "ButtonDoorOpening" : "ButtonDoorClosing");
        }
    }

    public void DoInteract(bool open)
    {
        if (SNet.IsMaster)
            WeakDoorsInZone.ForEach(info => info.weakDoor.m_sync.AttemptDoorInteraction(open ? eDoorInteractionType.Open : eDoorInteractionType.Close));
    }
}
