using UnityEngine;
using System.Collections;
using BrainCloudPhotonExample.Game.PlayerInput;

namespace BrainCloudPhotonExample.Game
{
    public class TargetPointDisplay : MonoBehaviour
    {
        private Color m_color;
        private int m_team;

        void Start()
        {
            m_color = GetComponent<SpriteRenderer>().color;
            m_team = transform.parent.parent.parent.parent.parent.GetComponent<ShipController>().m_team;
        }

        void LateUpdate()
        {
            if (GameObject.Find("PlayerController").GetComponent<WeaponController>().HasBombs())
            {
                
                if (PhotonNetwork.player.CustomProperties["Team"] != null && m_team == (int)PhotonNetwork.player.CustomProperties["Team"])
                {
                    GetComponent<SpriteRenderer>().color = new Color(m_color.r, m_color.g, m_color.b, 0.0f);
                }
                else
                {
                    GetComponent<SpriteRenderer>().color = new Color(m_color.r, m_color.g, m_color.b, 0.4f);
                }
            }
            else
            {
                GetComponent<SpriteRenderer>().color = new Color(m_color.r, m_color.g, m_color.b, 0.0f);
            }
        }
    }
}