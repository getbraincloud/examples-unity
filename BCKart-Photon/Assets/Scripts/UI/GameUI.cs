using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameUI : MonoBehaviour
{
	public interface IGameUIComponent
	{
		void Init(KartEntity entity);
	}

	public CanvasGroup fader;
	public Animator introAnimator;
	public Animator countdownAnimator;
	public Animator itemAnimator;
	public GameObject timesContainer;
	public GameObject coinCountContainer;
	public GameObject lapCountContainer;
	public GameObject pickupContainer;
	public EndRaceUI endRaceScreen;
	public Image pickupDisplay;
	public Image boostBar;
	public Text coinCount;
	public Text lapCount;
	public Text raceTimeText;
	public Text[] lapTimeTexts;
	public Text introGameModeText;
	public Text introTrackNameText;
	public Button continueEndButton;
	private bool _startedCountdown;

	public KartEntity Kart { get; private set; }
	private KartController KartController => Kart.Controller;

	public void Init(KartEntity kart)
	{
		Kart = kart;

		var uis = GetComponentsInChildren<IGameUIComponent>(true);
		foreach (var ui in uis) ui.Init(kart);

		kart.LapController.OnLapChanged += SetLapCount;
		SetLapCount(1, GameManager.Instance.GameType.lapCount); // first lap of the lap count

		var track = Track.Current;

		if (track == null)
			Debug.LogWarning($"You need to initialize the GameUI on a track for track-specific values to be updated!");
		else
		{
			introGameModeText.text = GameManager.Instance.GameType.modeName;
			introTrackNameText.text = track.definition.trackName;
		}

		GameType gameType = GameManager.Instance.GameType;

		if (gameType.IsPracticeMode())
		{
			timesContainer.SetActive(false);
			lapCountContainer.SetActive(false);
		}

		if (gameType.hasPickups == false)
		{
			pickupContainer.SetActive(false);
		}
		else
		{
			ClearPickupDisplay();
		}

		if (gameType.hasCoins == false)
		{
			coinCountContainer.SetActive(false);
		}

		continueEndButton.gameObject.SetActive(kart.Object.HasStateAuthority);

		kart.OnHeldItemChanged += index =>
		{
			if (index == -1)
			{
				ClearPickupDisplay();
			}
			else
			{
				StartSpinItem();
			}
		};

		kart.OnCoinCountChanged += count =>
		{
			AudioManager.Play("coinSFX", AudioManager.MixerTarget.SFX);
			coinCount.text = $"{count:00}";
		};

        _keepAliveRoutine = StartCoroutine(SendKeepAliveLoop());
	}

	private void OnDestroy()
	{
		Kart.LapController.OnLapChanged -= SetLapCount;
		
		 // Stop the loop cleanly when the object is disabled or destroyed
        if (_keepAliveRoutine != null)
        {
            StopCoroutine(_keepAliveRoutine);
            _keepAliveRoutine = null;
        }
	}
	
	public void FinishCountdown()
	{
		// Kart.OnRaceStart();
	}

	public void HideIntro()
	{
		introAnimator.SetTrigger("Exit");
	}

	private void FadeIn()
	{
		StartCoroutine(FadeInRoutine());
	}

	private IEnumerator FadeInRoutine()
	{
		float t = 1;
		while (t > 0)
		{
			fader.alpha = 1 - t;
			t -= Time.deltaTime;
			yield return null;
		}
	}

	private void Update()
	{
		if (!Kart || !Kart.LapController.Object || !Kart.LapController.Object.IsValid)
			return;

		if (!_startedCountdown && Track.Current != null && Track.Current.StartRaceTimer.IsRunning)
		{
			var remainingTime = Track.Current.StartRaceTimer.RemainingTime(Kart.Runner);
			if (remainingTime != null && remainingTime <= 3.0f)
			{
				_startedCountdown = true;
				HideIntro();
				FadeIn();
				countdownAnimator.SetTrigger("StartCountdown");
			}
		}

		UpdateBoostBar();

		if (Kart.LapController.enabled) UpdateLapTimes();

		var controller = Kart.Controller;
		if (controller.BoostTime > 0f)
		{
			if (controller.BoostTierIndex == -1) return;

			Color color = controller.driftTiers[controller.BoostTierIndex].color;
			SetBoostBarColor(color);
		}
		else
		{
			if (!controller.IsDrifting) return;

			SetBoostBarColor(controller.DriftTierIndex < controller.driftTiers.Length - 1
				? controller.driftTiers[controller.DriftTierIndex + 1].color
				: controller.driftTiers[controller.DriftTierIndex].color);
		}
	}

	private void UpdateBoostBar()
	{
		if (!KartController.Object || !KartController.Object.IsValid)
			return;
		
		var driftIndex = KartController.DriftTierIndex;
		var boostIndex = KartController.BoostTierIndex;

		if (KartController.IsDrifting)
		{
			if (driftIndex < KartController.driftTiers.Length - 1)
				SetBoostBar((KartController.DriftTime - KartController.driftTiers[driftIndex].startTime) /
				            (KartController.driftTiers[driftIndex + 1].startTime - KartController.driftTiers[driftIndex].startTime));
			else
				SetBoostBar(1);
		}
		else
		{
			SetBoostBar(boostIndex == -1
				? 0f
				: KartController.BoostTime / KartController.driftTiers[boostIndex].boostDuration);
		}
	}

	private void UpdateLapTimes()
	{
		if (!Kart.LapController.Object || !Kart.LapController.Object.IsValid)
			return;
		var lapTimes = Kart.LapController.LapTicks;
		for (var i = 0; i < Mathf.Min(lapTimes.Length, lapTimeTexts.Length); i++)
		{
			var lapTicks = lapTimes.Get(i);

			if (lapTicks == 0)
			{
				lapTimeTexts[i].text = "";
			}
			else
			{
				var previousTicks = i == 0
					? Kart.LapController.StartRaceTick
					: lapTimes.Get(i - 1);

				var deltaTicks = lapTicks - previousTicks;
				var time = TickHelper.TickToSeconds(Kart.Runner, deltaTicks);

				SetLapTimeText(time, i);
			}
		}

		SetRaceTimeText(Kart.LapController.GetTotalRaceTime());
	}

	public void SetBoostBar(float amount)
	{
		boostBar.fillAmount = amount;
	}

	public void SetBoostBarColor(Color color)
	{
		boostBar.color = color;
	}

	public void SetCoinCount(int count)
	{
		coinCount.text = $"{count:00}";
	}

	private void SetLapCount(int lap, int maxLaps)
	{
		var text = $"{(lap > maxLaps ? maxLaps : lap)}/{maxLaps}";
		lapCount.text = text;
	}

	public void SetRaceTimeText(float time)
	{
		raceTimeText.text = $"{(int) (time / 60):00}:{time % 60:00.000}";
	}

	public void SetLapTimeText(float time, int index)
	{
		lapTimeTexts[index].text = $"<color=#FFC600>L{index + 1}</color> {(int) (time / 60):00}:{time % 60:00.000}";
	}

	public void StartSpinItem()
	{
		StartCoroutine(SpinItemRoutine());
	}

	private IEnumerator SpinItemRoutine()
	{
		itemAnimator.SetBool("Ticking", true);
		float dur = 3;
		float spd = Random.Range(9f, 11f); // variation, for flavor.
		float x = 0;
		while (x < dur)
		{
			x += Time.deltaTime;

			itemAnimator.speed = (spd - 1) / (dur * dur) * (x - dur) * (x - dur) + 1;
			yield return null;
		}

		itemAnimator.SetBool("Ticking", false);
		SetPickupDisplay(Kart.HeldItem);
		// Kart.canUseItem = true;
	}

	public void SetPickupDisplay(Powerup item)
	{
		if (item)
			pickupDisplay.sprite = item.itemIcon;
		else
			pickupDisplay.sprite = null;
	}

	public void ClearPickupDisplay()
	{
		SetPickupDisplay(ResourceManager.Instance.noPowerup);
	}

	public void ShowEndRaceScreen()
	{
		endRaceScreen.gameObject.SetActive(true);

		BCManager.LobbyManager.SendCompleteGame();
	}

	// UI Hook

	public void OpenPauseMenu()
	{
		InterfaceManager.Instance.OpenPauseMenu();
	}

    private Coroutine _keepAliveRoutine;
	private IEnumerator SendKeepAliveLoop()
    {
        while (true)
        {
			// Wait 1440 seconds before sending again
			yield return new WaitForSeconds((float)BCManager.LobbyManager.KeepAliveRateSeconds);

			BCManager.LobbyManager.SendKeepAlive();
        }
    }
}