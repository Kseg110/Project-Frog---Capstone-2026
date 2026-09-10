using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.UI;

public class FMODTest : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;

    private Bus master;
    private Bus sfx;
    private Bus music;

    void Start()
    {
        master = RuntimeManager.GetBus("bus:/");
        sfx = RuntimeManager.GetBus("bus:/");
        music = RuntimeManager.GetBus("bus:/");

        SyncSlider(masterSlider, master);
        SyncSlider(sfxSlider, sfx);
        SyncSlider(musicSlider, music);
    }

    private void SyncSlider(Slider slider, Bus bus)
    {
        if (slider == null || !bus.isValid()) return;

        if (bus.getVolume(out float volume) == RESULT.OK)
        {
            slider.SetValueWithoutNotify(volume);
        }
    }

    public void SetMasterVolume(float value)
    {
        master.setVolume(value);
    }

    public void SetSFXVolume(float value)
    {
        sfx.setVolume(value);
    }

    public void SetMusicVolume(float value)
    {
        music.setVolume(value);
    }
}