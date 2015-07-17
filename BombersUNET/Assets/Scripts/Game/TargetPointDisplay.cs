using UnityEngine;
using System.Collections;
using BrainCloudUNETExample.Game.PlayerInput;

namespace BrainCloudUNETExample.Game
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
                
                if (m_team == BombersNetworkManager.m_localPlayer.m_team)
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