using UnityEngine;
using System.Collections;
using BrainCloudPhotonExample.Connection;
using Photon.Pun;
using Photon.Realtime;

namespace BrainCloudPhotonExample.Game
{
    public class FlareController : MonoBehaviour
    {
        private bool m_isActive = false;
        private float m_lifeTime = 100;
        private GameObject m_offscreenIndicator;
        private Player m_player;
        private GameObject m_playerPlane;

        public void Activate(Player aPlayer)
        {
            m_lifeTime = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_flareLifetime;
            m_isActive = true;
            m_player = aPlayer;
            foreach (GameObject plane in GameObject.FindGameObjectsWithTag("Plane"))
            {
                if (plane.GetComponent<PhotonView>().Owner == aPlayer)
                {
                    m_playerPlane = plane;
                    break;
                }
            }

            if (m_player == PhotonNetwork.LocalPlayer)
            {
                transform.GetChild(1).GetChild(0).GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
                transform.GetChild(1).GetChild(0).GetChild(0).GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
                transform.GetChild(2).GetComponent<TextMesh>().color = new Color(1, 1, 1, 0);
            }

            if (m_playerPlane == null)
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            m_offscreenIndicator = transform.GetChild(1).gameObject;

        }

        void FixedUpdate()
        {
            m_lifeTime -= Time.fixedDeltaTime;
            if (m_lifeTime <= 0 && m_isActive)
            {
                Destroy(gameObject);
                m_isActive = false;
                return;
            }
        }

        void LateUpdate()
        {
            if (m_isActive && (int)m_player.CustomProperties["Team"] == (int)PhotonNetwork.LocalPlayer.CustomProperties["Team"])
            {
                m_offscreenIndicator.transform.position = m_playerPlane.transform.position;
                Vector3 position = m_offscreenIndicator.transform.position;
                Vector3 point = Camera.main.WorldToScreenPoint(position);

                bool isOffscreen = false;

                if (point.x > Screen.width - 35)
                {
                    isOffscreen = true;
                    point.x = Screen.width - 35;
                }
                if (point.x < 0 + 35)
                {
                    isOffscreen = true;
                    point.x = 0 + 35;
                }
                if (point.y > Screen.height - 35)
                {
                    isOffscreen = true;
                    point.y = Screen.height - 35;
                }
                if (point.y < 0 + 35)
                {
                    isOffscreen = true;
                    point.y = 0 + 35;
                }
                point.z = 10;
                point = Camera.main.ScreenToWorldPoint(point);
                m_offscreenIndicator.transform.position = point;
                point -= Camera.main.transform.position;
                m_offscreenIndicator.transform.eulerAngles = new Vector3(0, 0, Mathf.Atan2(point.y, point.x) * Mathf.Rad2Deg - 90);

                transform.GetChild(2).GetComponent<TextMesh>().text = m_player.CustomProperties["RoomDisplayName"].ToString();
                transform.GetChild(2).position = m_offscreenIndicator.transform.position + new Vector3(0, -0.8f, 0);
                transform.GetChild(2).eulerAngles = new Vector3(0, 0, 0);

                if (isOffscreen && m_player != PhotonNetwork.LocalPlayer)
                {
                    m_offscreenIndicator.transform.GetChild(0).GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);
                    m_offscreenIndicator.transform.GetChild(0).GetChild(0).GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);
                    transform.GetChild(2).GetComponent<TextMesh>().color = new Color(1, 1, 1, 1);
                }
                else
                {
                    m_offscreenIndicator.transform.GetChild(0).GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
                    m_offscreenIndicator.transform.GetChild(0).GetChild(0).GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
                    transform.GetChild(2).GetComponent<TextMesh>().color = new Color(1, 1, 1, 0);
                }
            }
            else
            {
                transform.GetChild(1).gameObject.SetActive(false);
                transform.GetChild(2).gameObject.SetActive(false);
            }
        }
    }
}
