using Fusion;
using UnityEngine;

public class KartAnimator : KartComponent
{
	public ParticleSystem[] backfireEmitters;
	public ParticleSystem[] boostEmitters;
	public ParticleSystem[] driftEmitters;
	public ParticleSystem[] driftTierEmitters;
	public ParticleSystem[] tireSmokeEmitters;
	public ParticleSystem[] offroadDustEmitters;
	public TrailRenderer[] skidEmitters;

	[SerializeField] private NetworkMecanimAnimator _nma;
	[SerializeField] private Animator _animator;

    private KartController Controller => Kart.Controller;
    
    /// <summary>
	/// Anim hook
	/// </summary>
	public void AllowDrive()
	{
		Controller.RefreshAppliedSpeed();
		Controller.IsSpinout = false;
	}

	public override void Spawned()
	{
		base.Spawned();

		Kart.Controller.OnDriftTierIndexChanged += UpdateDriftState;
		Kart.Controller.OnBoostTierIndexChanged += UpdateBoostState;

		Kart.Controller.OnSpinoutChanged += val =>
		{
			if (!val) return;
			SetTrigger("Spinout");
		};

		Kart.Controller.OnBumpedChanged += val =>
		{
			if (val)
			{
				SetTrigger("Bump");
				AudioManager.Play("bumpSFX", AudioManager.MixerTarget.SFX, transform.position);
			}
			else
			{
				Kart.Controller.RefreshAppliedSpeed();
			}
		};

		Kart.Controller.OnBackfiredChanged += val =>
		{
			if (!val) return;
			PlayBackfire();
			AudioManager.Play("backfireSFX", AudioManager.MixerTarget.SFX, transform.position);
		};

        Kart.Controller.OnHopChanged += val => {
            if (!val) return;
            Kart.Animator.SetTrigger("Hop");
        };
    }

	private void OnDestroy()
	{
		Kart.Controller.OnDriftTierIndexChanged -= UpdateDriftState;
		Kart.Controller.OnBoostTierIndexChanged -= UpdateBoostState;
	}

	private void UpdateDriftState(int index)
	{
		if (index == -1)
		{
			StopDrift();
			return;
		}

		var color = Controller.driftTiers[index].color;
		foreach (var emitter in driftEmitters)
		{
			var main = emitter.main;
			main.startColor = color;
			foreach (var subEmitter in emitter.GetComponentsInChildren<ParticleSystem>())
			{
				var sub = subEmitter.main;
				sub.startColor = color;
			}

			emitter.Play(true);
		}

		foreach (var emitter in tireSmokeEmitters)
		{
			emitter.Play(true);
		}
	}

	private void StopDrift()
	{
		foreach (var emitter in driftEmitters)
		{
			emitter.Stop(true);
		}

		foreach (var emitter in tireSmokeEmitters)
		{
			emitter.Stop(true);
		}

		StopSkidFX();
	}

	private void UpdateBoostState(int index)
	{
		if (index == 0)
		{
			StopBoost();
			return;
		}

		SetTrigger("Boost");

		Color color = Controller.driftTiers[index].color;
		foreach (var emitter in boostEmitters)
		{
			var main = emitter.main;
			main.startColor = color;
			foreach (var subEmitter in emitter.GetComponentsInChildren<ParticleSystem>())
			{
				var sub = subEmitter.main;
				sub.startColor = color;
			}

			emitter.Play(true);
		}
		
		if (Object.HasInputAuthority)
		{
			Kart.Camera.speedLines.Play();
		}
	}

	public void StopBoost()
	{
		foreach (var emitter in boostEmitters)
		{
			emitter.Stop(true);
		}

		if (Object.HasInputAuthority)
		{
			Kart.Camera.speedLines.Stop();
		}
	}

	public void PlayOffroad()
	{
		foreach (var emitter in offroadDustEmitters)
		{
			emitter.Play(true);
		}
	}

	public void StopOffroad()
	{
		foreach (var emitter in offroadDustEmitters)
		{
			emitter.Stop(true);
		}
	}

	public void PlaySkidFX()
	{
		if (Kart.Controller.IsDrifting)
		{
			foreach (var trailRend in skidEmitters)
			{
				trailRend.emitting = true;
			}
		}
	}

	public void StopSkidFX()
	{
		foreach (var trailRend in skidEmitters)
		{
			trailRend.emitting = false;
		}
	}

	private void PlayBackfire()
	{
		SetTrigger("Stall");
		foreach (var emitter in backfireEmitters)
		{
			emitter.Play(true);
		}
	}

	// TODO: this should be replaced with NetworkMecanimAnimator's SetTrigger when Fusion implement animator prediction
	public void SetTrigger(string trigger)
	{
		if (Object.HasStateAuthority)
			_nma.SetTrigger(trigger);
		else if (Object.HasInputAuthority && Runner.IsForward)
			_animator.SetTrigger(trigger);
	}
}