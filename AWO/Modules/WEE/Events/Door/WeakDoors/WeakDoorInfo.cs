using GTFO.API;
using LevelGeneration;

namespace AWO.Modules.WEE.Events.Door.WeakDoors;

public class WeakDoorInfo
{
    public static readonly Dictionary<int, List<WeakDoorInfo>> WeakDoors = new();

    static WeakDoorInfo()
    {
        LevelAPI.OnLevelCleanup += WeakDoors.Clear;
    }

    public readonly LG_WeakDoor Door;
    private readonly (LG_DoorButton button, Interact_Base interact)[] _buttons;
    private bool _canInteract = true;
    public WeakDoorInfo(LG_WeakDoor door)
    {
        Door = door;

        var buttons = door.m_buttons;
        _buttons = new (LG_DoorButton, Interact_Base)[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
            _buttons[i] = (buttons[i], buttons[i].GetComponent<Interact_Base>());
    }

    public void SetCanInteract(bool enable)
    {
        if (_canInteract == enable) return;
        _canInteract = enable;

        foreach ((var button, var interact) in _buttons)
        {
            button.m_anim.gameObject.active = enable;
            interact.SetActive(enable);
        }
    }

    public void PlayButtonAnimation(bool open)
    {
        if (!_canInteract) return;

        foreach ((var button, _) in _buttons)
            ActivateButtonAnim(button, open);
    }

    private static void ActivateButtonAnim(LG_DoorButton button, bool open)
    {
        var anim = button.m_anim;
        if (!anim.enabled)
        {
            anim.enabled = true;
            button.StartCoroutine(button.DelayDisableAnimator());
        }
        anim.Play(open ? "ButtonDoorOpening" : "ButtonDoorClosing");
    }

    public void DoInteract(bool open)
    {
        Door.m_sync.AttemptDoorInteraction(open ? eDoorInteractionType.Open : eDoorInteractionType.Close);
    }
}
