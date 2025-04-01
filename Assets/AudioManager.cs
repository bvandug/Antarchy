using System;
using UnityEngine;



public class AudioManager : MonoBehaviour
{
    public Sound[] sounds;

    

    public static AudioManager instance;

    void Awake()
    {
        if(instance == null)
            instance = this;
        else {
            Destroy(gameObject);
            return;
        }
        // Keep theme sound constant between scenes
        DontDestroyOnLoad(gameObject);

        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;

            s.source.volume = s.volume; // Fixed incorrect assignment
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;

            s.source.playOnAwake = false; // Optional: prevents auto-playing on start
        }
    }
    void Start()
    {
        Play("Theme");
    }
    public void Play(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound " + name + " not found");
            return;
        }
        s.source.Play();
    }     

    public void Pause(string name) 
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s.source.Pause();
        
    }

    

}


