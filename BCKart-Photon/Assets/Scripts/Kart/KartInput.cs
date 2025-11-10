using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

public class KartInput : KartComponent, INetworkRunnerCallbacks
{
	public struct NetworkInputData : INetworkInput
	{
		public const uint ButtonAccelerate = 1 << 0;
		public const uint ButtonReverse = 1 << 1;
		public const uint ButtonDrift = 1 << 2;
		public const uint ButtonLookbehind = 1 << 3;
        public const uint UseItem = 1 << 4;

		public uint Buttons;
		public uint OneShots;

		private int _steer;
		public float Steer
		{
			get => _steer * .001f;
			set => _steer = (int)(value * 1000);
		}

		public bool IsUp(uint button) => IsDown(button) == false;
		public bool IsDown(uint button) => (Buttons & button) == button;

		public bool IsDownThisFrame(uint button) => (OneShots & button) == button;
        
		public bool IsAccelerate => IsDown(ButtonAccelerate);
		public bool IsReverse => IsDown(ButtonReverse);
		public bool IsDriftPressed => IsDown(ButtonDrift);
		public bool IsDriftPressedThisFrame => IsDownThisFrame(ButtonDrift);
	}

	public Gamepad gamepad;

	[SerializeField] private InputAction accelerate;
	[SerializeField] private InputAction reverse;
	[SerializeField] private InputAction drift;
	[SerializeField] private InputAction steer;
	[SerializeField] private InputAction lookBehind;
	[SerializeField] private InputAction useItem;
	[SerializeField] private InputAction pause;

    private bool _useItemPressed;
	private bool _driftPressed;

	public override void Spawned()
	{
		base.Spawned();

		Runner.AddCallbacks(this);

		accelerate = accelerate.Clone();
		reverse = reverse.Clone();
		drift = drift.Clone();
		steer = steer.Clone();
		lookBehind = lookBehind.Clone();
		useItem = useItem.Clone();
		pause = pause.Clone();

		accelerate.Enable();
		reverse.Enable();
		drift.Enable();
		steer.Enable();
		lookBehind.Enable();
		useItem.Enable();
		pause.Enable();
		
		useItem.started += UseItemPressed;
		drift.started += DriftPressed;
		pause.started += PausePressed;
	}

	public override void Despawned(NetworkRunner runner, bool hasState)
	{
        base.Despawned(runner, hasState);
        
		DisposeInputs();
		Runner.RemoveCallbacks(this);
	}

	private void OnDestroy()
	{
		DisposeInputs();
	}

    private void DisposeInputs()
	{
		accelerate.Dispose();
		reverse.Dispose();
		drift.Dispose();
		steer.Dispose();
		lookBehind.Dispose();
		useItem.Dispose();
		pause.Dispose();
		// disposal should handle these
		//useItem.started -= UseItemPressed;
		//drift.started -= DriftPressed;
		//pause.started -= PausePressed;
	}

    private void UseItemPressed(InputAction.CallbackContext ctx) => _useItemPressed = true;
    private void DriftPressed(InputAction.CallbackContext ctx) => _driftPressed = true;

    private void PausePressed(InputAction.CallbackContext ctx)
	{
		if (Kart.Controller.CanDrive) InterfaceManager.Instance.OpenPauseMenu();
	}

	/// This isn't networked, so is not inside the <see cref="NetworkInputData"/> struct
	public bool IsLookBehindPressed => ReadBool(lookBehind);

	private static bool ReadBool(InputAction action) => action.ReadValue<float>() != 0;
	private static float ReadFloat(InputAction action) => action.ReadValue<float>();

    public void OnInput(NetworkRunner runner, NetworkInput input) {
        gamepad = Gamepad.current;

        var userInput = new NetworkInputData();

        if ( ReadBool(accelerate) ) userInput.Buttons |= NetworkInputData.ButtonAccelerate;
        if ( ReadBool(reverse) ) userInput.Buttons |= NetworkInputData.ButtonReverse;
        if ( ReadBool(drift) ) userInput.Buttons |= NetworkInputData.ButtonDrift;
        if ( ReadBool(lookBehind) ) userInput.Buttons |= NetworkInputData.ButtonLookbehind;

        if ( _driftPressed ) userInput.OneShots |= NetworkInputData.ButtonDrift;
        if ( _useItemPressed ) userInput.OneShots |= NetworkInputData.UseItem;

        userInput.Steer = ReadFloat(steer);

        input.Set(userInput);

        _driftPressed = false;
        _useItemPressed = false;
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
	public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
	public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
	public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
	public void OnConnectedToServer(NetworkRunner runner) { }
	public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
	public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
	public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
	public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
	public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
	public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
	public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
	public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
	public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
	public void OnSceneLoadDone(NetworkRunner runner) { }
	public void OnSceneLoadStart(NetworkRunner runner) { }
}