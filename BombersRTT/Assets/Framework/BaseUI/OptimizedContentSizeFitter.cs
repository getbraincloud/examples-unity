using UnityEngine;
using UnityEngine.UI;

namespace Gameframework
{
    [RequireComponent(typeof(RectTransform))]
    public class OptimizedContentSizeFitter : BaseBehaviour
    {
        public bool VerticalPerferredSize = true;

        void Start()
        {
            m_transform = GetComponent<RectTransform>();
            m_vertLayoutGroup = GetComponent<VerticalLayoutGroup>();
            m_horzLayoutGroup = GetComponent<HorizontalLayoutGroup>();
        }

        // Update is called once per frame
        void Update()
        {
            // find all my children, calc their height/width
            float calcValue = 0.0f;
            RectTransform tempTrans;
            int numActiveChildren = 0;
            for (int i = 0; i < m_transform.childCount; ++i)
            {
                tempTrans = m_transform.GetChild(i).GetComponent<RectTransform>();
                if (tempTrans.gameObject.activeInHierarchy)
                {
                    if (calcValue == 0)
                    {
                        calcValue += VerticalPerferredSize ? tempTrans.sizeDelta.y : tempTrans.sizeDelta.x;
                    }

                    calcValue += VerticalPerferredSize ? tempTrans.sizeDelta.y : tempTrans.sizeDelta.x;
                    ++numActiveChildren;
                }
            }

            m_transform.SetSizeWithCurrentAnchors(VerticalPerferredSize ? RectTransform.Axis.Vertical : RectTransform.Axis.Horizontal, 
                calcValue + (VerticalPerferredSize ? (numActiveChildren * m_vertLayoutGroup.spacing) + m_vertLayoutGroup.padding.top : (numActiveChildren * m_horzLayoutGroup.spacing) + m_horzLayoutGroup.padding.left));
        }

        private VerticalLayoutGroup m_vertLayoutGroup = null;
        private HorizontalLayoutGroup m_horzLayoutGroup = null;

        private RectTransform m_transform = null;
    }
}
