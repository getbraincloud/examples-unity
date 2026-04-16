using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class KartAudio : KartComponent
{
	public AudioSource StartSound;
	public AudioSource IdleSound;
	public AudioSource RunningSound;
	public AudioSource ReverseSound;
	public AudioSource Drift;
	public AudioSource Boost;
	public AudioSource Offroad;
	public AudioSource Crash;
	public AudioSource Horn;
    [Range(0.1f, 1.0f)] public float RunningSoundMaxVolume = 1.0f;
	[Range(0.1f, 2.0f)] public float RunningSoundMaxPitch = 1.0f;
	[Range(0.1f, 1.0f)] public float ReverseSoundMaxVolume = 0.5f;
	[Range(0.1f, 2.0f)] public float ReverseSoundMaxPitch = 0.6f;
	[Range(0.1f, 1.0f)] public float IdleSoundMaxVolume = 0.6f;

	[Range(0.1f, 1.0f)] public float DriftMaxVolume = 0.5f;

    private KartController Controller => Kart.Controller;

    public override void Spawned() {
        base.Spawned();

        Kart.Controller.OnSpinoutChanged += val => {
            if ( !val ) return;
            AudioManager.PlayAndFollow("slipSFX", transform, AudioManager.MixerTarget.SFX);
        };
        
        Kart.Controller.OnBoostTierIndexChanged += val => {
            if ( val == 0 ) return;

            Boost.Play();
        };
    }

    public override void Render() {
        base.Render();

        var rb = Controller.Rigidbody;
        var speed = rb.transform.InverseTransformVector(rb.linearVelocity / Controller.maxSpeedBoosting).z;

        HandleDriftAudio(speed);
        HandleOffroadAudio(speed);
        HandleDriveAudio(speed);
		
        IdleSound.volume = Mathf.Lerp(IdleSoundMaxVolume, 0.0f, speed * 4);
    }

    private void HandleDriveAudio(float speed) {
        if ( speed < 0.0f ) {
            // In reverse
            RunningSound.volume = 0.0f;
            ReverseSound.volume = Mathf.Lerp(0.1f, ReverseSoundMaxVolume, -speed * 1.2f);
            ReverseSound.pitch = Mathf.Lerp(0.1f, ReverseSoundMaxPitch, -speed + (Mathf.Sin(Time.time) * .1f));
        } else {
            // Moving forward
            ReverseSound.volume = 0.0f;
            RunningSound.volume = Mathf.Lerp(0.1f, RunningSoundMaxVolume, speed * 1.2f);
            RunningSound.pitch = Mathf.Lerp(0.3f, RunningSoundMaxPitch, speed + (Mathf.Sin(Time.time) * .1f));
        }
    }

    private void HandleDriftAudio(float speed) {
        var b = Controller.IsDrifting && Controller.IsGrounded
            ? speed * DriftMaxVolume
            : 0.0f;
        Drift.volume = Mathf.Lerp(Drift.volume, b, Time.deltaTime * 20f);
    }
    
    private void HandleOffroadAudio(float speed) {
        Offroad.volume = Controller.IsOffroad
            ? Mathf.Lerp(0, 0.25f, Mathf.Abs(speed) * 1.2f)
            : Mathf.Lerp(Offroad.volume, 0, Time.deltaTime * 10f);
    }

    public void PlayHorn()
	{
		Horn.Play();
	}
}
