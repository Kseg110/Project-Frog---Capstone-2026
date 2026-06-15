using System.Collections.Generic;
using UnityEngine;



public class FOGactivationtimer : MonoBehaviour
{

    public WaveRoundSystem waveSystem;

    public int Currentround;

    [SerializeField]
    private List<Roundpushdata> rounds =
        new List<Roundpushdata>();

    void Update()
    {
        Currentround = waveSystem.CurrentWave;
        foreach (var r in rounds)
        {
            // START ENABLE PROCESS
            if (!r.started && Currentround == r.fromRound)
            {
                r.started = true;
                r.timer = r.timeeachenables;
                r.index = 0;
            }

            if (!r.started || r.disabled)
                continue;

            // DISABLE ALL AT END ROUND
            if (Currentround >= r.toRound)
            {
                for (int i = 0; i < r.enableList.Count; i++)
                {
                    if (r.enableList[i] != null)
                        r.enableList[i].SetActive(false);
                }

                r.disabled = true;
                continue;
            }

            // ENABLE ONE BY ONE
            r.timer -= Time.deltaTime;

            if (r.timer <= 0f && r.index < r.enableList.Count)
            {
                if (r.enableList[r.index] != null)
                    r.enableList[r.index].SetActive(true);

                r.index++;
                r.timer = r.timeeachenables;
            }
        }
    }
}
[System.Serializable]
public class Roundpushdata
{
    public int fromRound;
    public int toRound;

    public float timeeachenables = 1f;

    public List<GameObject> enableList = new List<GameObject>();
    public List<GameObject> disableList = new List<GameObject>();

    [HideInInspector] public bool started;
    [HideInInspector] public float timer;
    [HideInInspector] public int index;
    [HideInInspector] public bool disabled;
}