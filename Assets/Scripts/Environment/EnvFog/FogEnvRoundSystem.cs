using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoundData
{
    [Header("Active Between These Rounds")]
    public int fromRound;
    public int toRound;

    [Header("Trigger To Activate This Rule")]
    public Collider trigger;

    [Header("Disable In This Range")]
    public List<GameObject> disableList = new List<GameObject>();

    [Header("Enable In This Range")]
    public List<GameObject> enableList = new List<GameObject>();
}

public class FogEnvRoundSystem : MonoBehaviour
{
    public WaveRoundSystem waveSystem;

    public Transform player; // 👈 ADD THIS

    public int CurrentRound;
    private int lastRound = -1;

    private Collider currentTrigger;

    public List<RoundData> rounds = new List<RoundData>();

    void Update()
    {
        if (waveSystem == null) return;

        CurrentRound = waveSystem.CurrentWave;

        // 🔥 ALWAYS validate trigger state (this is the fix)
        ValidateTriggerState();

        if (CurrentRound == lastRound)
            return;

        lastRound = CurrentRound;

        if (currentTrigger != null)
        {
            ApplyRules(currentTrigger);
        }
    }

    // 🔥 NEW: ensures trigger logic always correct
    private void ValidateTriggerState()
    {
        if (player == null)
            return;

        bool stillInside = false;

        foreach (var round in rounds)
        {
            if (round.trigger == null)
                continue;

            Collider col = round.trigger;

            if (col == null)
                continue;

            // closest possible “inside trigger” check
            if (col.bounds.Contains(player.position))
            {
                currentTrigger = col;
                stillInside = true;
                ApplyRules(col);
                break;
            }
        }

        if (!stillInside)
            currentTrigger = null;
    }

    public void ActivateFromTrigger(Collider hitTrigger)
    {
        currentTrigger = hitTrigger;
        ApplyRules(hitTrigger);
    }

    private void ApplyRules(Collider hitTrigger)
    {
        foreach (var round in rounds)
        {
            if (round.trigger == null)
                continue;

            if (round.trigger != hitTrigger)
                continue;

            if (CurrentRound < round.fromRound || CurrentRound > round.toRound)
                continue;

            EnableObjects(round.enableList);
            DisableObjects(round.disableList);
        }
    }

    public void EnableObjects(List<GameObject> list)
    {
        foreach (GameObject obj in list)
            if (obj) obj.SetActive(true);
    }

    public void DisableObjects(List<GameObject> list)
    {
        foreach (GameObject obj in list)
            if (obj) obj.SetActive(false);
    }

    public void ClearTrigger()
    {
        currentTrigger = null;
    }
}