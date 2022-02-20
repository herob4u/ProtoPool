using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PoolBallAudio : MonoBehaviour
{
    [SerializeField] private SoundBank ScrapeSounds;
    [SerializeField] private SoundBank LowImpactSounds;
    [SerializeField] private SoundBank HighImpactSounds;

    [SerializeField, Min(0.0f)] private float MinImpactForce;
    [SerializeField, Min(0.0f)] private float MaxImpactForce;
    [SerializeField, Range(0, 3)] private float PitchVariation;
    [SerializeField, Range(0, 22000)] private float LowPassCutoffHz = 16000.0f;

    private AudioSource AudioSource;
    private Rigidbody RigidBody;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void Awake()
    {
        AudioSource = GetComponent<AudioSource>();
        RigidBody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if(MaxImpactForce < MinImpactForce)
        {
            MaxImpactForce = MinImpactForce;
        }

        // Update scraping sound 
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(!RigidBody)
        {
            return;
        }

        // Am I scraping something?
        float dot = Vector3.Dot(collision.GetContact(0).normal, RigidBody.velocity.normalized);
        if(dot > 0.99f && dot <= 1.0f)
        {
            // Scraping the table likely, ignore.
            return;
        }
        
        ImpactEventInfo impactEvent;
        impactEvent.Instigator = collision.gameObject;
        impactEvent.OtherObject = collision.gameObject;
        impactEvent.Impulse = collision.impulse;
        impactEvent.ImpactPoint = collision.GetContact(0);

        PlayImpactSound(impactEvent);
    }

    bool PlayImpactSound(ImpactEventInfo impactEvent)
    {
        float impulseMag = impactEvent.Impulse.magnitude;

        if (AudioSource.isPlaying)
        {
            return false;
        }

        if(MaxImpactForce <= MinImpactForce)
        {
            Debug.LogWarning("Invalid Max and Min impact force values");
            return false;
        }

        // Mapped value from [0-1] that tells us how much closer we are to either min or max impact.
        //float mixFactor = (MinImpactForce + (impulseMag - MinImpactForce) / (MaxImpactForce - MinImpactForce)) / MinImpactForce;
        float mixFactor = ((impulseMag - MinImpactForce) / (MaxImpactForce - MinImpactForce));
        AudioClip sound;
        float volume;
        float lowPassCutoff = LowPassCutoffHz;

        if (impulseMag < MinImpactForce)
        {
            // Verryyy minor tiny impact, play a very weak, soft sound. We need this because minor collisions produce an impulse of 0.
            sound           = LowImpactSounds.GetSound();
            volume          = Mathf.Lerp(0.01f, 0.15f, Random.Range(0.0f,1.0f));
            lowPassCutoff   = Random.Range(800.0f, 1400.0f);
        }
        else if (mixFactor > 0.5f)
        {
            sound   = HighImpactSounds.GetSound();
            volume  = Mathf.Lerp(0.5f, 1.0f, (mixFactor - 0.5f));
        }
        else
        {
            sound   = LowImpactSounds.GetSound();
            volume  = Mathf.Lerp(0.15f, 1.0f, (mixFactor));
        }

        if (sound == null)
        {
            Debug.Log("No suitable impact sound in sound bank");
            return false;
        }

        if (IsBallImpact(impactEvent))
        {
            SetLowPass(lowPassCutoff);

            //Debug.Log("Playing ball impact");
            AudioSource.clip = sound;
            AudioSource.volume = volume;
            AudioSource.pitch = Mathf.Abs(Random.Range(1 - PitchVariation, 1 + PitchVariation));
            AudioSource.Play();

            return true;
        }
        else if(IsTableImpact(impactEvent))
        {

        }

        return false;
    }

    void SetLowPass(float freqHz)
    {
        if(AudioSource.outputAudioMixerGroup)
        {
            AudioSource.outputAudioMixerGroup.audioMixer.SetFloat("LP_Cutoff", freqHz);
        }
    }

    bool IsBallImpact(ImpactEventInfo impactEvent)
    {
        if(impactEvent.OtherObject)
        {
            return impactEvent.OtherObject.GetComponent<PoolBall>() != null;
        }

        return false;
    }

    bool IsTableImpact(ImpactEventInfo impactEvent)
    {
        if (impactEvent.OtherObject)
        {
            return impactEvent.OtherObject.GetComponent<PoolTable>() != null;
        }

        return false;
    }
}
