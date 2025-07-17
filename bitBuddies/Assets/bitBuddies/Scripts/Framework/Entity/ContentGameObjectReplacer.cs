using UnityEngine;

namespace Gameframework
{
    public class ContentGameObjectReplacer : BaseBehaviour
    {
        #region Public Properties
        [SerializeField]
        private GameObject HandHeldParent = null;

        [SerializeField]
        private GameObject TabletParent = null;

        [SerializeField]
        private GameObject[] GameObjectsToReplace = null;

        [SerializeField]
        private bool AutoSwapOnStart = false;

        [SerializeField]
        private int CurrentResizeDepth = 0;

        [SerializeField]
        protected int MaxDepthToResize = 2;
#if UNITY_ANDROID
        [SerializeField]
        private bool ResizeFromParent = true;
#endif

        #endregion
        protected virtual void Start()
        {
            if (AutoSwapOnStart)
            {
                SwapViewAndSwapObjects();
            }
        }

        public GameObject GetActiveView()
        {
            GameObject toReturn = this.gameObject;
            if (HandHeldParent != null || TabletParent != null)
            {
                if (TabletParent != null && TabletParent.activeInHierarchy)
                {
                    toReturn = TabletParent;
                }
                else if (HandHeldParent != null && HandHeldParent.activeInHierarchy)
                {
                    toReturn = HandHeldParent;
                }
            }

            return toReturn;
        }

        protected void SwapViewAndSwapObjects()
        {
            // do we have a built in seperation of handheld vs tablet view to override ? 
            if (HandHeldParent != null && TabletParent != null)
            {
                //bool isTablet = AspectRatio.IsLetterBox();
                //HandHeldParent.SetActive(!isTablet);
                //TabletParent.SetActive(isTablet);

#if UNITY_ANDROID
                if (!isTablet)
                {
                    Vector2 aspectRation = AspectRatio.GetAspectRatio(Screen.width, Screen.height);
                    float designWidth = 768;

                    float aspectRatio = aspectRation.x / aspectRation.y;
                    Transform activeTrans = GetActiveView().transform;
                    Transform activeParentTrans = activeTrans.transform.parent;

                    // Limit the aspect ratio to a max of 16:9
                    float ceiling = 16.0f / 9.0f;
                    if (aspectRatio > ceiling)
                        aspectRatio = ceiling;

                    m_resizeVector.x = designWidth * aspectRatio;

                    if (!ResizeFromParent)
                    {
                        activeParentTrans = activeTrans;
                    }
                    RecursiveResizeRectTransforms(activeParentTrans);
                }
#endif
                if (GameObjectsToReplace != null)
                {
                    Transform activeView = GetActiveView().transform;
                    Transform tempUpdatedTransform = null;
                    RectTransform rectTrans = null;
                    for (int i = 0; i < GameObjectsToReplace.Length; ++i)
                    {
                        tempUpdatedTransform = GameObjectsToReplace[i].transform;
                        rectTrans = GameObjectsToReplace[i].GetComponent<RectTransform>();

                        tempUpdatedTransform.SetParent(activeView);
                        tempUpdatedTransform.localScale = Vector3.one;

                        // TODO:: special case! how ? 
                        if (rectTrans != null)
                        {
                            rectTrans.offsetMax = new Vector2(0, rectTrans.offsetMax.y);
                            rectTrans.offsetMin = new Vector2(0, rectTrans.offsetMin.y);
                        }
                        else
                        {
                            tempUpdatedTransform.localPosition = Vector3.zero;
                        }
                    }
                }

                // remove the useless one
                if (HandHeldParent.activeInHierarchy)
                {
                    Destroy(TabletParent.gameObject);
                }
                else
                {
                    Destroy(HandHeldParent.gameObject);
                }
            }
        }

        protected void RecursiveResizeRectTransforms(Transform in_trans)
        {
            if (in_trans != null)
            {
                // try to resize the one coming in
                RectTransform hl = in_trans.gameObject.GetComponent<RectTransform>();
                if (hl != null)
                {
                    m_resizeVector.y = hl.sizeDelta.y;
                    hl.sizeDelta = m_resizeVector;
                }
                if (CurrentResizeDepth < MaxDepthToResize)
                {
                    // try resizing the children
                    for (int i = 0; i < in_trans.childCount; ++i)
                    {
                        hl = in_trans.GetChild(i).gameObject.GetComponent<RectTransform>();
                        if (hl != null)
                        {
                            m_resizeVector.y = hl.sizeDelta.y;
                            hl.sizeDelta = m_resizeVector;

                            ++CurrentResizeDepth;
                            RecursiveResizeRectTransforms(hl);
                        }
                    }
                }

            }
        }

        protected Vector2 m_resizeVector = new Vector2();
    }
}

