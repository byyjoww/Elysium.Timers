using Elysium.Timers;
using Elysium.Utils.Timers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersistentTimerTest : MonoBehaviour
{
    [SerializeField] private PersistentTimerSO persistentTimer = default;

    [ContextMenu("Start timer with 10 seconds")]
    private void StartTimerWith10()
    {
        persistentTimer.StartNewTimer(10f);
    }
}
