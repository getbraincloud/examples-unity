using Gameframework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BrainCloudUNETExample
{
    public class GameState : BaseState, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        public static string STATE_NAME = "gameState";

        #region BaseState
        // Use this for initialization
        protected override void Start()
        {
            _stateInfo = new StateInfo(STATE_NAME, this);
            base.Start();

            BombersNetworkManager.Instance.StartCoroutine(BombersNetworkManager.Instance.InitializeGameInfo());
        }

        public virtual void OnDrag(PointerEventData ped)
        {
            if (BombersNetworkManager.LocalPlayer != null) BombersNetworkManager.LocalPlayer.OnDrag(ped);
        }

        public virtual void OnPointerDown(PointerEventData ped)
        {
            if (BombersNetworkManager.LocalPlayer != null) BombersNetworkManager.LocalPlayer.OnPointerDown(ped);
        }

        public virtual void OnPointerUp(PointerEventData ped)
        {
            if (BombersNetworkManager.LocalPlayer != null) BombersNetworkManager.LocalPlayer.OnPointerUp(ped);
        }
        #endregion
    }
}