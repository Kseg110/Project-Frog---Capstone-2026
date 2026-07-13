using System.Collections.Generic;
using UnityEngine;



public class FOGactivationtimer : MonoBehaviour
{

    public WaveRoundSystem waveSystem;

    public int Currentround;

    [SerializeField]
    private List<Roundpushdata> rounds = new List<Roundpushdata>();

    void Update()
    {
        Currentround = waveSystem.CurrentWave;
        foreach (var r in rounds)
        {

            // Disable outside the active round range
            if (Currentround < r.fromRound || Currentround >= r.toRound)
            {
                for (int i = 0; i < r.disableList.Count; i++)
                {
                    if (r.disableList[i] != null)
                        r.disableList[i].SetActive(false);
                }

                continue;
            }
            // START ENABLE PROCESS
            if (!r.started && Currentround >= r.fromRound && Currentround <= r.toRound)
            {
                r.timer = r.timeeachenables;
                r.index = 0;
                r.started = true;
            }
            else if (r.started && Currentround <= r.fromRound && Currentround >= r.toRound)
            {
                r.index = 0;
                r.started = false;
            }

            if (!r.started)
                continue;



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

    public bool started;
    public float timer;
    public int index;
}