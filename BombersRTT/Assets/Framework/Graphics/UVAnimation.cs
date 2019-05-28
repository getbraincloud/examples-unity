using UnityEngine;
using System.Collections;

namespace Gameframework
{
    public class UVAnimation : BaseBehaviour
    {
        public int uvTileY = 4; // texture sheet columns 
        public int uvTileX = 4; // texture sheet rows

        public int fps = 30;

        public int endPause = 0; // amount of time to pause at the end, before looping again

        #region BaseBehaviour
        private void Start()
        {
            m_renderer = GetComponent<Renderer>();
            m_startTime = 0.0f;
        }

        private void Update()
        {
            //calculate the index
            int previousIndex = m_index;
            m_startTime += Time.deltaTime;
            m_index = (int)(m_startTime * fps);
            
            if (!m_paused && previousIndex != m_index)
            {
                //repeat when exhausting all frames
                m_index = m_index % (uvTileY * uvTileX);

                if (m_index == (uvTileY * uvTileX) - 1)
                {
                    m_paused = true;
                    Invoke("onResume", endPause);
                }

                //size of each tile  
                m_size.x = 1.0f / uvTileY;
                m_size.y = 1.0f / uvTileX;

                //build the offset   
                //v coordinate is at the bottom of the image in openGL, so we invert it
                m_offset.x = (m_index % uvTileX) * m_size.x;
                m_offset.y = 1.0f - m_size.y - (m_index / uvTileX) * m_size.y;

                m_renderer.material.SetTextureOffset("_MainTex", m_offset);
                m_renderer.material.SetTextureScale("_MainTex", m_size);

                if (this.name.Contains("fan")) Destroy(this);
            }
        }

        protected override void OnDestroy()
        {
            m_renderer = null;
            base.OnDestroy();
        }
        #endregion

        #region Private
        private void onResume()
        {
            m_paused = false;
        }

        private int m_index;
        private bool m_paused = false;

        private Vector2 m_size;
        private Vector2 m_offset;

        private float m_startTime = 0.0f;

        private Renderer m_renderer;
        #endregion
    }
}
