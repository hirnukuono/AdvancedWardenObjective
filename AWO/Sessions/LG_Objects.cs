using LevelGeneration;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AWO.Sessions;

public static class LG_Objects
{
    public static IEnumerable<LG_ComputerTerminal> Terminals => _TerminalList;
    public static IEnumerable<LG_LabDisplay> LabDisplays => _LabDisplayList;
    public static IEnumerable<LG_DoorButton> DoorButtons => _DoorButtonList;
    public static IEnumerable<LG_WeakLock> WeakLocks => _WeakLockList;
    public static IEnumerable<LG_HSUActivator_Core> HSUActivators => _HSUActivatorList;

    private readonly static List<LG_ComputerTerminal> _TerminalList = new();
    private readonly static List<LG_LabDisplay> _LabDisplayList = new();
    private readonly static List<LG_DoorButton> _DoorButtonList = new();
    private readonly static List<LG_WeakLock> _WeakLockList = new();
    private readonly static List<LG_HSUActivator_Core> _HSUActivatorList = new();

    static LG_Objects()
    {
        LevelEvents.OnLevelCleanup += OnLevelCleanup;
    }

    private static void OnLevelCleanup()
    {
        Clear();
    }

    internal static void Clear()
    {
        _TerminalList.Clear();
        _LabDisplayList.Clear();
        _DoorButtonList.Clear();
        _WeakLockList.Clear();
        _HSUActivatorList.Clear();
        EntryPoint.AWOCommandList.Clear();
        EntryPoint.Portals.Clear();
    }

    public static void AddTerminal(LG_ComputerTerminal terminal) => AddToList(in _TerminalList, terminal);
    public static void RemoveTerminal(LG_ComputerTerminal terminal) => RemoveFromList(in _TerminalList, terminal);
    public static void AddLabDisplay(LG_LabDisplay display) => AddToList(in _LabDisplayList, display);
    public static void RemoveLabDisplay(LG_LabDisplay display) => RemoveFromList(in _LabDisplayList, display);
    public static void AddDoorButton(LG_DoorButton button) => AddToList(in _DoorButtonList, button);
    public static void RemoveDoorButton(LG_DoorButton button) => RemoveFromList(in _DoorButtonList, button);
    public static void AddWeakLock(LG_WeakLock weaklock) => AddToList(in _WeakLockList, weaklock);
    public static void RemoveWeakLock(LG_WeakLock weaklock) => RemoveFromList(in _WeakLockList, weaklock);
    public static void AddHSUActivator(LG_HSUActivator_Core activator) => AddToList(in _HSUActivatorList, activator);
    public static void RemoveHSUActivator(LG_HSUActivator_Core activator) => RemoveFromList(in _HSUActivatorList, activator);

    private static void AddToList<O>(in List<O> list, O itemToAdd) where O : Component
    {
        var id = itemToAdd.GetInstanceID();
        if (!list.Any(t => t.GetInstanceID() == id))
        {
            list.Add(itemToAdd);
        }
    }

    private static void RemoveFromList<O>(in List<O> list, O itemToRemove) where O : Component
    {
        var id = itemToRemove.GetInstanceID();
        var index = list.FindIndex(i => i.GetInstanceID() == id);
        if (index > -1)
        {
            list.RemoveAt(index);
        }
    }
}
