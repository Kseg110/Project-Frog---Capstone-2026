using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class FMODTest : MonoBehaviour
{
    private Bus master;

    void Start()
    {
        master = RuntimeManager.GetBus("bus:/");

        //RESULT result = master.setVolume(0.5f);

        //UnityEngine.Debug.Log(result);
    }

    public void SetMasterVolume(float value)
    {
        master.setVolume(value);
    }
}