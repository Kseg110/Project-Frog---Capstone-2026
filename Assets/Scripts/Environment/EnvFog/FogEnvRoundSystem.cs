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

    // 🔥 Current round (synced from wave system)
    public int CurrentRound;

    // 🔥 Track last applied round so we don’t spam updates
    private int lastRound = -1;

    // 🔥 Store the trigger player is currently inside
    private Collider currentTrigger;

    public List<RoundData> rounds = new List<RoundData>();

    void Update()
    {
        if (waveSystem == null) return;

        // Sync round
        CurrentRound = waveSystem.CurrentWave;

        // Only react if round actually changed
        if (CurrentRound == lastRound)
            return;

        lastRound = CurrentRound;

        // If player is still inside a trigger, re-apply rules
        if (currentTrigger != null)
        {
            ApplyRules(currentTrigger);
        }
    }

    // 🔹 Call when entering a trigger
    public void ActivateFromTrigger(Collider hitTrigger)
    {
        currentTrigger = hitTrigger;
        ApplyRules(hitTrigger);
    }

    // 🔥 Core logic (used on enter + round change)
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

            Debug.Log($"Applied Round Rule {round.fromRound}-{round.toRound} on {hitTrigger.name}");
        }
    }

    // 🔹 Enable objects
    public void EnableObjects(List<GameObject> list)
    {
        foreach (GameObject obj in list)
        {
            if (obj) obj.SetActive(true);
        }
    }

    // 🔻 Disable objects
    public void DisableObjects(List<GameObject> list)
    {
        foreach (GameObject obj in list)
        {
            if (obj) obj.SetActive(false);
        }
    }

    // Optional (call this on exit trigger)
    public void ClearTrigger()
    {
        currentTrigger = null;
    }
}