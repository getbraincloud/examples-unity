using UnityEngine;

namespace BrainCloudUNETExample.Game
{
    public class HunterAI : BasePlayerControllerAI
    {
        protected override void onCollisionEntered(Collider aCollision, PlaneController in_controller)
        {
            if (ignoreDetectedCollision(in_controller)) return;
            base.onCollisionEntered(aCollision, in_controller);

            switch (_fsm.State)
            {
                // do nothing if we left bounds, we need to get back inbounds
                case ePlayerControllerAIStates.LEFTBOUNDS:
                    { }break;

                case ePlayerControllerAIStates.ACQUIREDBOMB:
                    {
                        onAcquiredBombCollision(in_controller);
                    }
                    break;


                case ePlayerControllerAIStates.FOLLOW:
                    {
                        onFollowStateCollision(in_controller);
                    }
                    break;

                case ePlayerControllerAIStates.IDLE:
                default:
                    {
                        // new non enemy
                        if (_currentTarget == null || allowedToTarget(in_controller))
                        {
                            _currentTarget = in_controller.PlayerController;
                            _fsm.ChangeState(ePlayerControllerAIStates.FOLLOW);
                        }
                    }
                    break;
            }
        }

        protected override void onCollisionStay(Collider aCollision, PlaneController in_controller)
        {
            if (ignoreDetectedCollision(in_controller)) return;
            base.onCollisionStay(aCollision, in_controller);
            switch (_fsm.State)

            {
                case ePlayerControllerAIStates.ACQUIREDBOMB:
                    {
                        onAcquiredBombCollision(in_controller);
                    }
                    break;
                case ePlayerControllerAIStates.FOLLOW:
                    {
                        onFollowStateCollision(in_controller);
                    }
                    break;

                default: break;
            }
        }

        #region State machine
        protected void FOLLOW_Update()
        {
            if (_currentTarget != null && _currentTarget.m_planeActive)
            {
                updateToPosition(_currentTarget.m_playerPlane.transform.position);
            }
            else
            {
                _fsm.ChangeState(ePlayerControllerAIStates.IDLE);
            }
        }

        protected void ATTACKING_Update()
        {
            if  (_currentTarget != null && _currentTarget.m_planeActive)
            {
                updateToPosition(_currentTarget.m_playerPlane.transform.position);
                if (!_bActionTaken)
                {
                    if (_fsm.TimeInState >= PLAYER_REACTION_TIME)
                    {
                        _actionControl.PlayerOutputs.Add(ePlayerControllerInputs.FIRE_GUN);
                        Invoke("changeToPreviousState", PLAYER_REACTION_TIME);
                    }
                }
                else
                {
                    _actionControl.PlayerOutputs.Add(ePlayerControllerInputs.FIRE_GUN);
                }
            }
            else
            {
                Invoke("changeToPreviousState", PLAYER_REACTION_TIME);
            }   
        }

        protected void ACQUIREDBOMB_Update()
        {
            if (_shipTarget != null && _shipTarget.m_isAlive && _shipTarget.m_targetGraphic != null )
            {
                // which is closer ? the current target? or the ship target i was going to ? 
                float currentTargetMag = _currentTarget != null ? (_currentTarget.m_playerPlane.transform.position - _playerController.m_playerPlane.transform.position).sqrMagnitude : float.MaxValue;
                float incomingTargetMag = (_shipTarget.m_targetGraphic.transform.position - _playerController.m_playerPlane.transform.position).sqrMagnitude;

                // lets go that way
                updateToPosition(incomingTargetMag < currentTargetMag ? _shipTarget.m_targetGraphic.transform.position : _currentTarget.m_playerPlane.transform.position);

                // are we going to bomb ? 
                if (!_bActionTaken)
                {
                    // TODO how well to we make them bomb ? 
                    float distanceToTarget = (_shipTarget.m_targetGraphic.transform.position - _playerController.m_playerPlane.transform.position).magnitude;
                    if (distanceToTarget < BOMB_TARGET_PROXIMITY)
                    {
                        _bActionTaken = true;
                        _actionControl.PlayerOutputs.Add(ePlayerControllerInputs.DROP_BOMB);
                        // next tick
                        _shipTarget = null;
                    }
                }
            }
            // someone else killed the target, lets find another!
            else if (_playerController != null && _playerController.m_playerPlane != null && _playerController.WeaponController.GetBombs() > 0)
            {
                if (GameObject.Find("GameManager") == null)
                {
                    _fsm.ChangeState(ePlayerControllerAIStates.IDLE);
                    return;
                }
                GameObject closestEnemyShip = GameObject.Find("GameManager").GetComponent<GameManager>().GetClosestEnemyShip(_playerController.m_playerPlane.transform.position,
                                                                                                             _playerController.m_team);
                if (closestEnemyShip != null)
                {
                    // ensure we are going to the closest ship target
                    _shipTarget = closestEnemyShip.GetComponent<ShipController>().GetActiveShipTarget();
                }
                else
                {
                    _fsm.ChangeState(ePlayerControllerAIStates.IDLE);
                }
            }
            // we had a target, lets start following them then!
            else if (_currentTarget != null)
            {
                _fsm.ChangeState(ePlayerControllerAIStates.FOLLOW);
            }
            // otherwise go back to idle
            else 
            {
                _fsm.ChangeState(ePlayerControllerAIStates.IDLE);
            }
        }

        #endregion

        #region private 
        private void onAcquiredBombCollision(PlaneController in_controller)
        {
            if (_currentTarget != null && 
                in_controller.PlayerController.m_team != _playerController.m_team &&           // not on the same team 
                _currentTarget.m_displayName == in_controller.PlayerController.m_displayName)  // same target
            {
                _fsm.ChangeState(ePlayerControllerAIStates.ATTACKING);
            }
        }

        private void onFollowStateCollision(PlaneController in_controller)
        {
            if (_currentTarget != null && 
                in_controller.PlayerController.m_team != _playerController.m_team &&           // not on the same team 
                _currentTarget.m_displayName == in_controller.PlayerController.m_displayName)  // same target
            {
                _fsm.ChangeState(ePlayerControllerAIStates.ATTACKING);
            }
            else if (allowedToTarget(in_controller))
            {
                // change the current target, we are still just going to follow
                if (planeControllerCloserThenCurrentTarget(in_controller))
                {
                    _currentTarget = in_controller.PlayerController;
                }
            }
        }

        private bool allowedToTarget(PlaneController in_controller)
        {
            return planeControllerCloserThenCurrentTarget(in_controller) &&
                   (in_controller.PlayerController.m_team != _playerController.m_team ||    // not on the same team 
                   in_controller.PlayerController.m_team == _playerController.m_team && (_currentTarget == null || _currentTarget.m_team == _playerController.m_team));
        }
        #endregion
    }
}
