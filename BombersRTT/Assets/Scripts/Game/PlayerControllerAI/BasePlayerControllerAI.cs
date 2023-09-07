using Gameframework;
using System.Collections.Generic;
using UnityEngine;

namespace BrainCloudUNETExample.Game
{
    public class BasePlayerControllerAI : BaseBehaviour
    {
        #region public 
        public void LeftBounds()
        {
            _fsm.ChangeState(ePlayerControllerAIStates.LEFTBOUNDS);
        }

        public void EnteredBounds()
        {
            _fsm.ChangeState(ePlayerControllerAIStates.ENTEREDBOUNDS);
        }

        public void LateInit(BombersPlayerController controller)
        {
            _playerController = controller;

            _fsm = StateMachine<ePlayerControllerAIStates>.Initialize(this);
            _fsm.ChangeState(ePlayerControllerAIStates.IDLE);
            _fsm.Changed += _fsm_Changed;
        }

        public AIControlOutput GetActionState()
        {
            return _actionControl;
        }

        public void ClearOutputs()
        {
            _actionControl.PlayerOutputs.Clear();
        }

        void Update()
        {
            // keep my sphere pointed with me
            Vector3 eulerAngles = _playerController.m_playerPlane.transform.eulerAngles;
            eulerAngles.z += 90.0f;

            // just acquired a bomb
            int getBombs = _playerController.WeaponController.GetBombs();
            if (_numBombs < getBombs)
            {
                _fsm.ChangeState(ePlayerControllerAIStates.ACQUIREDBOMB);
            }
            _numBombs = getBombs;
        }

        protected int _numBombs = 0;

        #endregion

        #region virtual state machine
        protected void IDLE_Enter()
        {
            _currentTarget = null;
            _shipTarget = null;
        }

        protected void LEFTBOUNDS_Enter()
        {
            _currentTarget = null;
            _shipTarget = null;
        }
        protected void LEFTBOUNDS_Update()
        {
            // make a decision
            if (_fsm.TimeInState >= PLAYER_REACTION_TIME)
            {
                updateToPosition(Vector3.zero);

                // accelerate after turning a bit
                if (_fsm.TimeInState >= PLAYER_REACTION_TIME * 3.0f)
                {
                    _actionControl.PlayerOutputs.Add(ePlayerControllerInputs.ACCELERATE);
                }
            }
        }
        
        protected void ENTEREDBOUNDS_Enter()
        {
            _currentTarget = null;
            _shipTarget = null;
        }
        protected void ENTEREDBOUNDS_Update()
        {
            // go back to idle so it makes a choice for us
            if (_fsm.TimeInState >= PLAYER_REACTION_TIME)
            {
                _fsm.ChangeState(ePlayerControllerAIStates.IDLE);
            }
        }

        protected void ACQUIREDBOMB_Enter()
        {
            _currentTarget = null;

            // find the closest ship target
            if (_playerController != null && _playerController.m_playerPlane != null)
            {
                if (GameObject.Find("GameManager") == null)
                {
                    _fsm.ChangeState(ePlayerControllerAIStates.IDLE);
                    return;
                }
                GameObject closestEnemyShip = GameObject.Find("GameManager").GetComponent<GameManager>().GetClosestEnemyShip(_playerController.m_playerPlane.transform.position, _playerController.m_team);
                if (closestEnemyShip != null)
                {
                    _shipTarget = closestEnemyShip.GetComponent<ShipController>().GetActiveShipTarget();
                }
            }
        }

        #endregion

        #region protected
        protected void changeToIdleState()
        {
            _fsm.ChangeState(ePlayerControllerAIStates.IDLE);
        }
        protected void changeToPreviousState()
        {
            _fsm.ChangeState(_fsm.LastState);
        }

        protected bool ignoreDetectedCollision(PlaneController in_controller)
        {
            return !in_controller.PlayerController.m_planeActive ||                                                    // non active plane
               in_controller.IsServerBot && in_controller.PlayerController.m_team == _playerController.m_team;
        }

        protected bool planeControllerCloserThenCurrentTarget(PlaneController in_controller)
        {
            float currentTargetMag = _currentTarget != null ? (_currentTarget.m_playerPlane.transform.position - _playerController.m_playerPlane.transform.position).sqrMagnitude : float.MaxValue;
            float incomingTargetMag = (in_controller.transform.position - _playerController.m_playerPlane.transform.position).sqrMagnitude;
            return incomingTargetMag <= currentTargetMag;
        }

        protected void updateToPosition(Vector3 in_position)
        {
            // keep the update positions on the same plane
            in_position.z = _playerController.m_playerPlane.transform.position.z;

            Transform planeTransform = _playerController.m_playerPlane.transform;
            Vector3 vectorToOpp = in_position - planeTransform.position;

            bool useRightVector = HudHelper.QuickAbs(vectorToOpp.x) > HudHelper.QuickAbs(vectorToOpp.y);

            Vector3 lineToMiddleUp = Vector3.Project(vectorToOpp, useRightVector ? Vector3.right : Vector3.up);
            float angleToVectorOpp = Vector3.Angle(vectorToOpp, useRightVector ? transform.right : transform.up);

            // works well going up and down 
            if ((planeTransform.up.x * lineToMiddleUp.y) - (planeTransform.up.y * lineToMiddleUp.x) < 0)
            {
                angleToVectorOpp *= -1;
            }

            if (angleToVectorOpp > 0.0f) _actionControl.PlayerOutputs.Add(ePlayerControllerInputs.LEFT);
            else if (angleToVectorOpp < -0.0f) _actionControl.PlayerOutputs.Add(ePlayerControllerInputs.RIGHT);
        }

        // pass in -1 for closest controller
        protected BombersPlayerController GetClosestControllerOnTeam(int in_team)
        {
            BombersPlayerController closestController = null;
            BombersPlayerController tempController = null;
            Vector3 closestVector = Vector3.one * 99999999999999999.0f;
            Vector3 tempVector = Vector3.one;
            Vector3 myPos = transform.position;
            for (int i = 0; i < BombersNetworkManager.LobbyInfo.Members.Count; ++i)
            {
                tempController = BombersNetworkManager.LobbyInfo.Members[i].PlayerController;
                if (in_team < 0 || in_team == tempController.m_team)
                {
                    tempVector = myPos - tempController.m_playerPlane.transform.position;
                    if (tempVector.magnitude < closestVector.magnitude)
                    {
                        closestVector = tempVector;
                        closestController = tempController;
                    }
                }
            }

            return closestController;
        }

        protected AIControlOutput _actionControl = new AIControlOutput();
        protected StateMachine<ePlayerControllerAIStates> _fsm;
        protected BombersPlayerController _playerController = null;
        protected BombersPlayerController _currentTarget = null;
        protected ShipTarget _shipTarget = null;
        protected Collider _lastCollider = null;

        protected bool _bActionTaken = false;
        protected static float PLAYER_REACTION_TIME = 0.25f;
        protected static float BOMB_TARGET_PROXIMITY = 95.0f;
        #endregion

        #region private
        private void _fsm_Changed(ePlayerControllerAIStates obj)
        {
            ClearOutputs();
            _bActionTaken = false;
        }

        virtual protected void onCollisionEntered(Collider aCollision, PlaneController in_controller)
        {
            if (in_controller == _currentTarget)
                _lastCollider = aCollision;
        }
        virtual protected void onCollisionStay(Collider aCollision, PlaneController in_controller)
        {
            if (in_controller == _currentTarget)
                _lastCollider = aCollision;
        }
        virtual protected void onCollisionExit(Collider aCollision, PlaneController in_controller)
        {
        }

        void OnTriggerEnter(Collider aCollision)
        {
            BombersPlayerController planeController = aCollision.transform.parent.GetComponent<BombersPlayerController>();
            if (planeController != null)
            {
                onCollisionEntered(aCollision, planeController.m_playerPlane);
            }
        }

        void OnTriggerStay(Collider aCollision)
        {
            BombersPlayerController planeController = aCollision.transform.parent.GetComponent<BombersPlayerController>();
            if (planeController != null)
            {
                onCollisionStay(aCollision, planeController.m_playerPlane);
            }
        }

        void OnTriggerExit(Collider aCollision)
        {
            BombersPlayerController planeController = aCollision.transform.parent.GetComponent<BombersPlayerController>();
            if (planeController != null)
            {
                onCollisionExit(aCollision, planeController.m_playerPlane);
            }
        }

        #endregion
    }

    #region AIControlOutput class
    public class AIControlOutput
    {
        public List<ePlayerControllerInputs> PlayerOutputs = new List<ePlayerControllerInputs>();
    }

    public enum ePlayerControllerAIStates
    {
        PLAYER_CONTROLLED,
        RESET,
        IDLE,
        LEFTBOUNDS,
        ENTEREDBOUNDS,
        FOLLOW,
        ATTACKING, // firing guns


        ACQUIREDBOMB,  // will switch to nice state, when bombs > 0, and current state is below this one
        BOMBING,

        MAX
    }

    public enum ePlayerControllerInputs
    {
        LEFT,
        RIGHT,
        ACCELERATE,

        FIRE_GUN,
        DROP_BOMB,

        MAX
    }
    #endregion
}
