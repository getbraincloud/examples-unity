using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

public class KartEntity : KartComponent
{
	public static event Action<KartEntity> OnKartSpawned;
	public static event Action<KartEntity> OnKartDespawned;

    public event Action<int> OnHeldItemChanged;
    public event Action<int> OnCoinCountChanged;
    
	public KartAnimator Animator { get; private set; }
	public KartCamera Camera { get; private set; }
	public KartController Controller { get; private set; }
	public KartInput Input { get; private set; }
	public KartLapController LapController { get; private set; }
	public KartAudio Audio { get; private set; }
	public GameUI Hud { get; private set; }
	public KartItemController Items { get; private set; }
	public NetworkRigidbody3D Rigidbody { get; private set; }

	public Powerup HeldItem =>
		HeldItemIndex == -1
			? null
			: ResourceManager.Instance.powerups[HeldItemIndex];

    [Networked]
	public int HeldItemIndex { get; set; } = -1;

	[Networked]
	public int CoinCount { get; set; }

	public Transform itemDropNode;

    private bool _despawned;
    
    private ChangeDetector _changeDetector;

	private static void OnHeldItemIndexChangedCallback(KartEntity changed)
	{
		changed.OnHeldItemChanged?.Invoke(changed.HeldItemIndex);

		if (changed.HeldItemIndex != -1)
		{
			foreach (var behaviour in changed.GetComponentsInChildren<KartComponent>())
				behaviour.OnEquipItem(changed.HeldItem, 3f);
		}
	}

	private static void OnCoinCountChangedCallback(KartEntity changed)
	{
		changed.OnCoinCountChanged?.Invoke(changed.CoinCount);
	}

	private void Awake()
	{
		// Set references before initializing all components
		Animator = GetComponentInChildren<KartAnimator>();
		Camera = GetComponent<KartCamera>();
		Controller = GetComponent<KartController>();
		Input = GetComponent<KartInput>();
		LapController = GetComponent<KartLapController>();
		Audio = GetComponentInChildren<KartAudio>();
		Items = GetComponent<KartItemController>();
		Rigidbody = GetComponent<NetworkRigidbody3D>();

		// Initializes all KartComponents on or under the Kart prefab
		var components = GetComponentsInChildren<KartComponent>();
		foreach (var component in components) component.Init(this);
	}

	public static readonly List<KartEntity> Karts = new List<KartEntity>();

	public override void Spawned()
	{
		base.Spawned();
		
		_changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
		
		if (Object.HasInputAuthority)
		{
			// Create HUD
			Hud = Instantiate(ResourceManager.Instance.hudPrefab);
			Hud.Init(this);

			Instantiate(ResourceManager.Instance.nicknameCanvasPrefab);
		}

		Karts.Add(this);
		OnKartSpawned?.Invoke(this);
	}
	
	public override void Render()
	{
		foreach (var change in _changeDetector.DetectChanges(this))
		{
			switch (change)
			{
				case nameof(HeldItemIndex):
					OnHeldItemIndexChangedCallback(this);
					break;
				case nameof(CoinCount):
					OnCoinCountChangedCallback(this);
					break;
			}
		}
	}

	public override void Despawned(NetworkRunner runner, bool hasState)
	{
		base.Despawned(runner, hasState);
		Karts.Remove(this);
		_despawned = true;
		OnKartDespawned?.Invoke(this);
	}

	private void OnDestroy()
	{
		Karts.Remove(this);
		if (!_despawned)
		{
			OnKartDespawned?.Invoke(this);
		}
	}

    private void OnTriggerStay(Collider other) {

		if (other.TryGetComponent(out ICollidable collidable))
        {
            collidable.Collide(this);
        }
    }

    public bool SetHeldItem(int index)
	{
		if (HeldItem != null) return false;
        
		HeldItemIndex = index;
		return true;
	}

	public void SpinOut()
	{
		Controller.IsSpinout = true;
	}

	private IEnumerable OnSpinOut()
	{
		yield return new WaitForSeconds(2f);

		Controller.IsSpinout = false;
	}
}