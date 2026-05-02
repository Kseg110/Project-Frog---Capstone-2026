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

    [Header("Disable In This Range (NOT auto used)")]
    public List<GameObject> disableList = new List<GameObject>();

    [Header("Enable In This Range")]
    public List<GameObject> enableList = new List<GameObject>();
}

public class FogEnvRoundSystem : MonoBehaviour
{
    public int currentRound = 0;

    public List<RoundData> rounds = new List<RoundData>();

    // 🔹 Trigger ONLY enables
    public void ActivateFromTrigger(Collider hitTrigger)
    {
        foreach (var round in rounds)
        {
            if (round.trigger != hitTrigger)
                continue;

            if (currentRound < round.fromRound || currentRound > round.toRound)
                continue;

            EnableObjects(round.enableList);
            DisableObjects(round.disableList);
        }
    }

    // 🔹 Enable (USED)
    public void EnableObjects(List<GameObject> list)
    {
        foreach (GameObject obj in list)
        {
            if (obj != null)
                obj.SetActive(true);
        }
    }

    // 🔻 Disable (UNUSED for now — you said you’ll use later)
    public void DisableObjects(List<GameObject> list)
    {
        foreach (GameObject obj in list)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }
}