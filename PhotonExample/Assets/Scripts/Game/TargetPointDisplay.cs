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
                if (m_team == (int)PhotonNetwork.player.customProperties["Team"])
                {
                    GetComponent<SpriteRenderer>().color = new Color(m_color.r, m_color.g, m_color.b, 0);
                }
                else
                {
                    GetComponent<SpriteRenderer>().color = new Color(m_color.r, m_color.g, m_color.b, 1);
                }
            }
            else
            {
                GetComponent<SpriteRenderer>().color = new Color(m_color.r, m_color.g, m_color.b, 0);
            }
        }
    }
}