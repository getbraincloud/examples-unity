/*
 * Most game logic and spawning of items is controlled through the player controller, 
 * which also controls the movement and display of the plane, and collision with billet and bomb pickups
 * 
 * UNET doesn't allow non-player objects to communicate with the server, so the play object has to implement all of the same game logic code
 * that is normally done in the GameManager.
 */

using BrainCloudUNETExample.Connection;
using Gameframework;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrainCloudUNETExample.Game
{
    public class BombersPlayerController : BaseNetworkBehavior
    {
        public static BombersPlayerController GetPlayer(string aID)
        {
            return BombersNetworkManager.LobbyInfo.GetMemberWithProfileId(aID).PlayerController;
        }

        public static BombersPlayerController GetPlayer(short aID)
        {
            return BombersNetworkManager.LobbyInfo.GetMemberWithNetId(aID).PlayerController;
        }

        //[syncVar]
        public int m_score;

        //[syncVar]
        public string/*int*/ ProfileId
        {
            get { return m_playerId; }
            set
            {
                m_playerId = value;
                m_playerPlane.PlayerId = m_playerId;
            }
        }
        private string m_playerId = "";

        // wrapper to the base behavior one
        public short NetId
        {
            get { return _netId; }
            set
            {
                _netId = value;
                _hasAuthority = BombersNetworkManager.LocalPlayer != null && _netId == BombersNetworkManager.LocalPlayer._netId;
                m_playerPlane.NetId = _netId;
            }
        }

        public WeaponController WeaponController { get; private set; }

        //[syncVar]
        public int m_team;

        //[syncVar]
        public int m_kills;

        //[syncVar]
        public int m_deaths;

        //[syncVar]
        public int m_ping;

        //[syncVar]
        public string m_displayName;

        //[syncVar]
        public bool m_planeActive = false;

        public PlaneController m_playerPlane;
        public GameObject m_missionText;

        public LobbyMemberInfo MemberInfo;

        public AudioClip m_engineSnd;

        protected override void Start()
        {
            _classType = BombersNetworkManager.BOMBERS_PLANE_CONTROLLER;
            lazyLoadReferences();
            if (IsServer) SendStart(_classType, m_playerId, _fileName, transform);
            base.Start();

            if (!IsLocalPlayer || !_hasAuthority)
            {
                return;
            }

            m_mobileHud = GameObject.Find("MobileHUD");
            if (m_mobileHud != null)
                m_mobileHud.SetActive(false);

#if (UNITY_IOS || UNITY_ANDROID)
            if (m_mobileHud != null) m_mobileHud.SetActive(true);
            Transform directionHud = m_mobileHud.transform.Find("DirectionHUD");
            m_DirectionHudRect = directionHud.GetComponent<RectTransform>();
            m_mobileHudBGImg = directionHud.transform.Find("Base").GetComponent<Image>();
            m_joystickBase = directionHud.transform.Find("JoyStickBase").gameObject;
            m_joystickImg = m_joystickBase.transform.Find("JoyStickImage").GetComponent<Image>();
            m_joystickBase.SetActive(false);
            m_InputDirection = Vector3.zero;

            m_menuButton = m_mobileHud.transform.Find("Menu").GetComponent<Button>();
            m_bombButton = m_mobileHud.transform.Find("Bomb").GetComponent<Button>();
            m_fireButton = m_mobileHud.transform.Find("Fire").GetComponent<Button>();

            m_menuButton.onClick.AddListener(OnMenuButton);

            var bombPointerDown = new EventTrigger.Entry();
            bombPointerDown.eventID = EventTriggerType.PointerDown;
            EventTrigger bombTrigger = m_bombButton.gameObject.AddComponent<EventTrigger>();
            bombPointerDown.callback.AddListener((x) => OnBombButton());
            bombTrigger.triggers.Add(bombPointerDown);

            var firePointerDown = new EventTrigger.Entry();
            firePointerDown.eventID = EventTriggerType.PointerDown;
            EventTrigger fireTrigger = m_fireButton.gameObject.AddComponent<EventTrigger>();
            firePointerDown.callback.AddListener((x) => OnFireButton());
            fireTrigger.triggers.Add(firePointerDown);
#endif
        }

        private void lazyLoadReferences()
        {
            if (m_gMan == null) m_gMan = GameObject.Find("GameManager").GetComponent<GameManager>();
            if (WeaponController == null) WeaponController = GetComponent<WeaponController>();

            BrainCloudStats bcStats = BrainCloudStats.Instance;
            m_turnSpeed = GConfigManager.GetFloatValue("TurnSpeed");
            m_turnSpeedModifier = GConfigManager.GetFloatValue("TurnSpeedModifier");
            m_acceleration = GConfigManager.GetFloatValue("PlaneAcceleration");
            m_linearAcceleration = GConfigManager.GetFloatValue("LinearAcceleration");
            m_linearDeceleration = GConfigManager.GetFloatValue("LinearDeceleration");
            m_baseHealth = GConfigManager.GetIntValue("BasePlaneHealth");
            m_maxSpeedMultiplier = GConfigManager.GetFloatValue("MaxSpeedMultiplier");

            m_missionText = m_gMan.MissionText;
        }

        public void SetPlayerPlane(PlaneController playerPlane)
        {
            m_playerPlane.m_health = m_baseHealth;
            m_planeActive = true;
            ActivatePlane();
            m_leftBoundsTimer = 4;
            StopCoroutine("PulseMissionText");
            StartCoroutine("PulseMissionText");
            m_leftBounds = false;
            m_currentRotation = m_playerPlane.gameObject.transform.rotation.eulerAngles.z;
            m_isActive = true;
            WeaponController.SetPlayerPlane(m_playerPlane);
        }

        IEnumerator PulseMissionText()
        {
            if (!IsLocalPlayer || !_hasAuthority)
            {
                yield break;
            }
            bool goingToColor1 = true;
            float time = 0;
            while (true)
            {
                while (goingToColor1)
                {
                    m_missionText.GetComponent<TextMeshProUGUI>().color = Color.Lerp(m_missionText.GetComponent<TextMeshProUGUI>().color, new Color(1, 0, 0, 1), 4 * Time.fixedDeltaTime);
                    time += Time.fixedDeltaTime;
                    if (time > 0.3f)
                    {
                        goingToColor1 = !goingToColor1;
                    }
                    yield return YieldFactory.GetWaitForFixedUpdate();
                }
                time = 0;
                while (!goingToColor1)
                {
                    m_missionText.GetComponent<TextMeshProUGUI>().color = Color.Lerp(m_missionText.GetComponent<TextMeshProUGUI>().color, new Color(0.3f, 0, 0, 1), 4 * Time.fixedDeltaTime);
                    time += Time.fixedDeltaTime;
                    if (time > 0.3f)
                    {
                        goingToColor1 = !goingToColor1;
                    }
                    yield return YieldFactory.GetWaitForFixedUpdate();
                }
                time = 0;
            }
        }

        public void ActivatePlane()
        {
            m_playerPlane.GetComponent<PlaneController>().enabled = true;
            m_playerPlane.transform.GetChild(0).gameObject.SetActive(true);
            m_playerPlane.transform.GetChild(1).gameObject.SetActive(true);
            AudioSource audioSrc = m_playerPlane.GetComponent<AudioSource>();
            if (!audioSrc.isPlaying)
            {
                audioSrc.clip = m_engineSnd;
                audioSrc.Play();
            }
            m_playerPlane.GetComponent<Collider>().enabled = true;
        }

        public void DeactivatePlane()
        {
            //m_playerPlane.GetComponent<PlaneController>().enabled = false;
            m_playerPlane.transform.GetChild(0).gameObject.SetActive(false);
            m_playerPlane.transform.GetChild(1).gameObject.SetActive(false);
            m_playerPlane.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            m_playerPlane.GetComponent<AudioSource>().Stop();
            m_playerPlane.GetComponent<Collider>().enabled = false;
        }

        void Update()
        {
            if (!m_planeActive && !m_respawning)
            {
                m_respawning = false;
                SpawnPlayer(GCore.Wrapper.Client.ProfileId);
            }

            if (m_planeActive)
            {
                ActivatePlane();
            }
            else
            {
                DeactivatePlane();
            }

            if (IsLocalPlayer)
            {
                if (m_leftBounds)
                {
                    m_missionText.SetActive(true);
                    m_missionText.GetComponent<TextMeshProUGUI>().text = "0:0" + Mathf.CeilToInt(m_leftBoundsTimer);
                }
                else
                {
                    m_missionText.GetComponent<TextMeshProUGUI>().text = "";
                    m_missionText.SetActive(false);
                }
            }

            if ((m_playerPlane.IsServerBot && !IsServer) && (!IsLocalPlayer || !_hasAuthority))
            {
                return;
            }

            if (m_leftBounds)
            {
                if (m_leftBoundsTimer > 0)
                {
                    m_leftBoundsTimer -= Time.deltaTime;
                    if (m_leftBoundsTimer <= 0 && m_leftBounds)
                    {
                        SuicidePlayer();
                    }
                }
            }

            if (!m_planeActive)
            {
                m_isAccelerating = false;
                m_isTurningLeft = false;
                m_isTurningRight = false;
                return;
            }

            if (!m_isActive) return;
            m_playerPlane.GetComponent<AudioSource>().pitch = 1 + ((m_speedMultiplier - 1) / m_maxSpeedMultiplier) * 0.5f;

            // player input actions
            if (!m_playerPlane.IsServerBot && IsLocalPlayer && !m_gMan.IsQuitMenuVisible && !m_gMan.IsResultsMenuVisible)
            {
#if !(UNITY_IOS || UNITY_ANDROID)
                if (Input.GetAxis("Horizontal") > 0 && !m_isTurningLeft)
                {
                    m_isTurningRight = true;
                }
                else
                {
                    m_isTurningRight = false;
                }

                if (Input.GetAxis("Horizontal") < 0 && !m_isTurningRight)
                {
                    m_isTurningLeft = true;
                }
                else
                {
                    m_isTurningLeft = false;
                }

                if (Input.GetAxis("Vertical") > 0)
                {
                    m_isAccelerating = true;
                }
                else
                {
                    m_isAccelerating = false;
                }

                if (Input.GetButton("Fire1") || Input.GetAxisRaw("Fire1-XB1") > 0)
                {
                    WeaponController.FireWeapon(m_isAccelerating);
                }

                if (Input.GetButtonDown("Fire2"))
                {
                    WeaponController.DropBomb();
                }

                /*
                if (Input.GetButtonDown("Fire3"))
                {
                    WeaponController.FireFlare(m_playerPlane.transform.position, m_playerPlane.GetComponent<Rigidbody>().velocity);
                }
                */
#endif
            }
            // playerbot input actions!
            else if (m_playerPlane.IsServerBot && IsServer)
            {
                // server client decides what happens
                // find the attached ai component, to decide which input the user wants
                // outputs 
                //        - horizontal > 0 = Turn Left
                //        - horizontal < 0 = Turn Right
                //        - vertical > 0 = Accelerate

                //        - Fire1 = Fire Weapon
                //        - Fire2 = Drop Bomb
                AIControlOutput controlOutput = null;
                if (m_ai != null)
                {
                    controlOutput = m_ai.GetActionState();
                }

                if (controlOutput != null)
                {
                    if (controlOutput.PlayerOutputs.Contains(ePlayerControllerInputs.RIGHT) && !m_isTurningLeft)
                    {
                        m_isTurningRight = true;
                    }
                    else
                    {
                        m_isTurningRight = false;
                    }

                    if (controlOutput.PlayerOutputs.Contains(ePlayerControllerInputs.LEFT) && !m_isTurningRight)
                    {
                        m_isTurningLeft = true;
                    }
                    else
                    {
                        m_isTurningLeft = false;
                    }

                    if (controlOutput.PlayerOutputs.Contains(ePlayerControllerInputs.ACCELERATE))
                    {
                        m_isAccelerating = true;
                    }
                    else
                    {
                        m_isAccelerating = false;
                    }

                    if (controlOutput.PlayerOutputs.Contains(ePlayerControllerInputs.FIRE_GUN))
                    {
                        WeaponController.FireWeapon(m_isAccelerating);
                    }

                    if (controlOutput.PlayerOutputs.Contains(ePlayerControllerInputs.DROP_BOMB))
                    {
                        WeaponController.DropBomb();
                    }

                    // remove after read in
                    m_ai.ClearOutputs();
                }
            }

            if (m_isAccelerating)
            {
                m_speedMultiplier += m_linearAcceleration * Time.deltaTime;
            }
            else
            {
                m_speedMultiplier -= m_linearDeceleration * Time.deltaTime;
            }

            if (m_speedMultiplier > m_maxSpeedMultiplier)
            {
                m_speedMultiplier = m_maxSpeedMultiplier;
            }
            else if (m_speedMultiplier < 1)
            {
                m_speedMultiplier = 1;
            }

            m_playerPlane.GetComponent<AudioSource>().pitch = 1 + ((m_speedMultiplier - 1) / m_maxSpeedMultiplier) * 0.8f;
        }

        void FixedUpdate()
        {
            if ((m_playerPlane.IsServerBot && !IsServer) && (!IsLocalPlayer || !_hasAuthority || !m_planeActive || !m_isActive))
            {
                return;
            }

            if (m_isTurningLeft && m_isAccelerating)
            {
                m_currentRotation += m_turnSpeed * m_turnSpeedModifier * Time.deltaTime;
            }
            else if (m_isTurningLeft)
            {
                m_currentRotation += m_turnSpeed * Time.deltaTime;
            }
            else if (m_isTurningRight && m_isAccelerating)
            {
                m_currentRotation -= m_turnSpeed * m_turnSpeedModifier * Time.deltaTime;
            }
            else if (m_isTurningRight)
            {
                m_currentRotation -= m_turnSpeed * Time.deltaTime;
            }

            m_playerPlane.Accelerate(m_acceleration, m_speedMultiplier);
            m_playerPlane.SetRotation(m_currentRotation);
        }

        void LateUpdate()
        {
            if (!IsLocalPlayer || !_hasAuthority)
            {
                return;
            }
            if (m_playerPlane != null)
            {
                Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, new Vector3(m_playerPlane.transform.Find("CameraPosition").position.x, m_playerPlane.transform.Find("CameraPosition").position.y, -110.0f), 0.5f);
                Camera.main.transform.GetChild(0).position = m_playerPlane.transform.position;
                m_playerPlane.GetComponent<AudioSource>().spatialBlend = 0;
            }

            Vector3 cameraPosition = Camera.main.transform.position;
            float height = 2 * 132 * Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float width = height * Camera.main.aspect;
            Bounds bounds = new Bounds(new Vector3(cameraPosition.x, cameraPosition.y, 22.0f), new Vector3(width, height, 0));
            Bounds mapBounds = GameObject.Find("MapBounds").GetComponent<Collider>().bounds;

            if (bounds.min.x < mapBounds.min.x)
            {
                cameraPosition.x = mapBounds.min.x - (bounds.min.x - bounds.center.x);
            }
            else if (bounds.max.x > mapBounds.max.x)
            {
                cameraPosition.x = mapBounds.max.x - (bounds.max.x - bounds.center.x);
            }

            if (bounds.min.y < mapBounds.min.y)
            {
                cameraPosition.y = mapBounds.min.y - (bounds.min.y - bounds.center.y);
            }
            else if (bounds.max.y > mapBounds.max.y)
            {
                cameraPosition.y = mapBounds.max.y - (bounds.max.y - bounds.center.y);
            }

            m_originalCamPosition = cameraPosition;
            if (m_shakeIntensity > 0)
            {
                cameraPosition = m_originalCamPosition + Random.insideUnitSphere * m_shakeIntensity;
                m_shakeIntensity -= (m_shakeIntensity / m_shakeDecay) * Time.deltaTime;
            }
            else
            {
                cameraPosition = m_originalCamPosition;
            }

            Camera.main.transform.position = cameraPosition;
        }

        public void ShakeCamera(float aIntensity, float aDecay)
        {
            if (!IsLocalPlayer || !_hasAuthority)
            {
                return;
            }
            m_shakeIntensity = aIntensity;
            m_shakeDecay = aDecay;
        }

        public void SuicidePlayer()
        {
            if ((m_playerPlane.IsServerBot && !IsServer) && (!IsLocalPlayer || !_hasAuthority))
            {
                return;
            }
            m_leftBounds = false;
            m_playerPlane.m_health = 0;
            m_gMan.RpcDestroyPlayerPlane(NetId, -1);
        }

        public void DestroyPlayerPlane()
        {
            if (!m_planeActive)
            {
                return;
            }
            m_leftBounds = false;
            m_isActive = false;
            m_planeActive = false;
            DeactivatePlane();
            WeaponController.DestroyPlayerPlane();
            m_playerPlane.m_health = 0;
        }

        public GameObject GetPlayerPlane()
        {
            return m_playerPlane.gameObject;
        }

        public void EndGame()
        {
            m_isActive = false;
        }

        public void BombPickedUpCommand(short aPlayerID, int aPickupID)
        {
            if (!IsServer)
            {
                return;
            }
            m_gMan.CmdBombPickedUp(aPlayerID, aPickupID);
        }

        public void DestroyShipCommand(int shipID, string aJson)
        {
            m_gMan.CmdDestroyedShip(shipID, aJson);
        }

        public void HitShipTargetPointCommand(int aID, int aIndex, string aJson)
        {
            m_gMan.CmdHitShipTargetPoint(aID, aIndex, aJson);
        }

        public void DeleteBombCommand(BombController aBombController, int aID)
        {
            BombInfo aBombInfo = aBombController.BombInfo;
            m_gMan.CmdDeleteBomb(aBombInfo.GetJson(), aID);
        }

        public void DeleteBulletCommand(BulletInfo in_info)
        {
            if (!IsLocalPlayer || !_hasAuthority)
            {
                return;
            }
            DeleteBullet(in_info);
        }

        void DeleteBullet(BulletInfo in_info)
        {
            m_gMan.DeleteBullet(in_info);
            //SendDestroy(BombersNetworkManager.BULLET, in_info.m_bulletID.ToString(), in_info.GetJson());
            SendProjectileDestroy(BombersNetworkManager.BULLET, in_info.GetDict());
        }

        public void BulletHitPlayerCommand(BulletInfo bulletInfo, Vector3 aHitPoint)
        {
            BombersPlayerController tempController;
            foreach (LobbyMemberInfo member in BombersNetworkManager.LobbyInfo.Members)
            {
                tempController = member.PlayerController;
                if (tempController.NetId == bulletInfo.m_hitId)
                {
                    bulletInfo.m_startPosition = tempController.m_playerPlane.transform.position + aHitPoint;
                    SendProjectileStart(BombersNetworkManager.BULLET_HIT, bulletInfo.GetDict());
                    m_gMan.RpcBulletHitPlayer(bulletInfo);
                    break;
                }
            }
        }

        public void FireBulletCommand(string aJson)
        {
            if ((m_playerPlane.IsServerBot && !IsServer) && (!IsLocalPlayer || !_hasAuthority))
            {
                return;
            }

            if (!m_playerPlane.IsServerBot && (IsLocalPlayer || _hasAuthority))
                m_gMan.m_shotsFired++;

            CmdSpawnBullet();
        }

        //[command]
        void CmdSpawnBullet()
        {
            int id = m_gMan.GetNextBulletID();
            Transform m_bulletSpawnPoint = m_playerPlane.m_bulletSpawnPoint;
            Vector3 m_bulletVelocity = m_bulletSpawnPoint.forward.normalized;
            m_bulletVelocity *= WeaponController.m_bulletSpeed;
            m_bulletVelocity += m_playerPlane.GetComponent<Rigidbody>().linearVelocity;
            BulletInfo aBulletInfo = new BulletInfo(m_bulletSpawnPoint.position, m_bulletSpawnPoint.forward.normalized, NetId, m_bulletVelocity, id);
            GameObject bullet = WeaponController.SpawnBullet(aBulletInfo);
            m_gMan.AddBulletInfo(aBulletInfo);

            SendProjectileStart(BombersNetworkManager.BULLET, aBulletInfo.GetDict());

        }

        public void SpawnBombCommand(string aJson)
        {
            if ((m_playerPlane.IsServerBot && !IsServer) && (!IsLocalPlayer || !_hasAuthority))
            {
                return;
            }
            BombInfo bombInfo = BombInfo.GetBombInfo(aJson);
            m_gMan.CmdSpawnBomb(bombInfo.m_startPosition, bombInfo.m_startDirection, bombInfo.m_startVelocity, bombInfo.m_shooter);
        }

        public void TakeBulletDamage(short aShooter)
        {
            if (m_playerPlane.m_health == 0) return;
            m_playerPlane.m_health--;
            if (m_playerPlane.m_health <= 0)
            {
                m_playerPlane.m_health = 0;
                m_gMan.RpcDestroyPlayerPlane(NetId, aShooter);
            }
        }

        public void EnteredBounds()
        {
            if ((m_playerPlane.IsServerBot && !IsServer) && (!IsLocalPlayer || !_hasAuthority))
            {
                return;
            }

            if (m_leftBounds)
            {
                if (m_playerPlane.IsServerBot && IsServer)
                {
                    m_ai.EnteredBounds();
                }

                m_leftBounds = false;
                m_leftBoundsTimer = 4;
            }
        }

        public void LeftBounds()
        {
            if ((m_playerPlane.IsServerBot && !IsServer) && (!IsLocalPlayer || !_hasAuthority))
            {
                return;
            }

            if (m_playerPlane.IsServerBot && IsServer)
            {
                m_ai.LeftBounds();
            }

            m_leftBounds = true;
        }

        public void SpawnPlayer(string aPlayerID)
        {
            lazyLoadReferences();
            Vector3 spawnPoint = Vector3.zero;
            spawnPoint.z = 22.0f;

            if (m_team == 1)
            {
                spawnPoint.x = Random.Range(m_gMan.m_team1SpawnBounds.bounds.center.x - m_gMan.m_team1SpawnBounds.bounds.size.x / 2, m_gMan.m_team1SpawnBounds.bounds.center.x + m_gMan.m_team1SpawnBounds.bounds.size.x / 2) - 10;
                spawnPoint.y = Random.Range(m_gMan.m_team1SpawnBounds.bounds.center.y - m_gMan.m_team1SpawnBounds.bounds.size.y / 2, m_gMan.m_team1SpawnBounds.bounds.center.y + m_gMan.m_team1SpawnBounds.bounds.size.y / 2);
            }
            else if (m_team == 2)
            {
                spawnPoint.x = Random.Range(m_gMan.m_team2SpawnBounds.bounds.center.x - m_gMan.m_team2SpawnBounds.bounds.size.x / 2, m_gMan.m_team2SpawnBounds.bounds.center.x + m_gMan.m_team2SpawnBounds.bounds.size.x / 2) + 10;
                spawnPoint.y = Random.Range(m_gMan.m_team2SpawnBounds.bounds.center.y - m_gMan.m_team2SpawnBounds.bounds.size.y / 2, m_gMan.m_team2SpawnBounds.bounds.center.y + m_gMan.m_team2SpawnBounds.bounds.size.y / 2);
            }

            spawnPlayerHelper(aPlayerID, spawnPoint);
        }

        public void SpawnPlayer(string aPlayerID, Vector3 in_spawnPoint)
        {
            Vector3 spawnPoint = new Vector3(in_spawnPoint.x, in_spawnPoint.y, in_spawnPoint.z);
            spawnPlayerHelper(aPlayerID, spawnPoint);
        }

        #region private
        private void spawnPlayerHelper(string in_playerID, Vector3 in_spawnPoint)
        {
            lazyLoadReferences();
            m_playerPlane.transform.position = in_spawnPoint;

            m_playerPlane.transform.rotation = Quaternion.LookRotation(Vector3.forward, (new Vector3(0, 0, 22.0f) - in_spawnPoint));
            if (m_team == 1)
            {
                m_playerPlane.gameObject.layer = 8;
            }
            else if (m_team == 2)
            {
                m_playerPlane.gameObject.layer = 9;
            }
            SetPlayerPlane(m_playerPlane);
            m_playerPlane.GetComponent<Rigidbody>().isKinematic = false;
            m_gMan.m_gameState = GameManager.eGameState.GAME_STATE_PLAYING_GAME;
            ProfileId = in_playerID;
            NetId = BombersNetworkManager.LobbyInfo.GetMemberWithProfileId(in_playerID).NetId;

            if (IsServerBot)
            {
                m_playerPlane.m_health /= 2;
            }

            if (IsServer)
            {
                SendStart(_classType, ProfileId, _fileName, transform);
            }

            if (m_playerPlane.IsServerBot && m_ai == null)
            {
                m_ai = transform.FindDeepChild("smartsComponent").gameObject.AddComponent<HunterAI>();
                m_ai.LateInit(this);
            }
        }

        public void OnDrag(PointerEventData ped)
        {
#if (UNITY_IOS || UNITY_ANDROID)
            Vector2 pos = Vector2.zero;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle
                (m_DirectionHudRect,
                ped.position,
                ped.pressEventCamera,
                out pos))
            {
                pos.x = (pos.x / m_DirectionHudRect.sizeDelta.x);
                pos.y = (pos.y / m_DirectionHudRect.sizeDelta.y);

                float x = (m_DirectionHudRect.pivot.x == 1) ? pos.x * 2 + 1 : pos.x * 2 - 1;
                float y = (m_DirectionHudRect.pivot.y == 1) ? pos.y * 2 + 1 : pos.y * 2 - 1;

                m_InputDirection = new Vector3(x, 0, y);
                m_InputDirection = (m_InputDirection.magnitude > 1) ? m_InputDirection.normalized : m_InputDirection;

                m_joystickImg.rectTransform.anchoredPosition =
                    new Vector3(m_InputDirection.x * (m_DirectionHudRect.sizeDelta.x / 2),
                                m_InputDirection.z * (m_DirectionHudRect.sizeDelta.y / 2));

                m_isTurningRight = false;
                m_isTurningLeft = false;

                if (m_InputDirection.x > DEADZONE)
                {
                    m_isTurningRight = true;
                }
                else if (m_InputDirection.x < -DEADZONE)
                {
                    m_isTurningLeft = true;
                }

                m_isAccelerating = m_InputDirection.z > DEADZONE ? true : false;
            }
#endif
        }

        public void OnPointerDown(PointerEventData ped)
        {
#if (UNITY_IOS || UNITY_ANDROID)
            m_joystickBase.SetActive(true);
            OnDrag(ped);
#endif
        }

        public void OnPointerUp(PointerEventData ped)
        {
#if (UNITY_IOS || UNITY_ANDROID)
            m_InputDirection = Vector3.zero;
            m_joystickBase.SetActive(false);
            m_joystickImg.rectTransform.anchoredPosition = Vector3.zero;

            m_isTurningRight = false;
            m_isTurningLeft = false;
            m_isAccelerating = false;
#endif
        }

        public void OnMenuButton()
        {
#if (UNITY_IOS || UNITY_ANDROID)
            m_gMan.ToggleQuitMenu();
#endif
        }

        public void OnBombButton()
        {
#if (UNITY_IOS || UNITY_ANDROID)
            if(!m_gMan.IsQuitMenuVisible && !m_gMan.IsResultsMenuVisible)
                BombersNetworkManager.LocalPlayer.WeaponController.DropBomb();
#endif
        }

        public void OnFireButton()
        {
#if (UNITY_IOS || UNITY_ANDROID)
            if(!m_gMan.IsQuitMenuVisible && !m_gMan.IsResultsMenuVisible)
            {
                bool isAccelerating = m_InputDirection.z > 0;
                BombersNetworkManager.LocalPlayer.WeaponController.FireWeapon(isAccelerating);
            }
#endif
        }

        private BasePlayerControllerAI m_ai = null;
        private GameManager m_gMan = null;
        private int m_baseHealth = 5;

        private float m_speedMultiplier = 1.0f;
        private float m_linearAcceleration = 3.0f;
        private float m_linearDeceleration = 3.0f;
        private float m_currentRotation = 0.0f;

        private float m_maxSpeedMultiplier = 2.5f;
        private float m_leftBoundsTimer = 0.0f;
        private float m_turnSpeed = 1.0f;
        private float m_acceleration = 1.0f;

        private float m_shakeDecay = 0.0f;
        private float m_shakeIntensity = 0.0f;
        private float m_turnSpeedModifier = 0.5f;

        private bool m_isActive = false;
        private bool m_isAccelerating = false;
        private bool m_isTurningRight = false;
        private bool m_isTurningLeft = false;

        private bool m_leftBounds = false;
        private bool m_respawning = true;

        private Vector3 m_originalCamPosition = Vector3.zero;

        private GameObject m_mobileHud;
        private GameObject m_joystickBase;
        private Button m_menuButton;
        private Button m_bombButton;
        private Button m_fireButton;
        private Image m_mobileHudBGImg;
        private Image m_joystickImg;
        private RectTransform m_DirectionHudRect;
        private Vector3 m_InputDirection { set; get; }
        private const float DEADZONE = 0.1f;
        #endregion
    }
}
