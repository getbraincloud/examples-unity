using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Gameframework
{
    public class ScrollSnapPage : BaseBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        #region Public Variables
        public int StartingPage = 0;
        public int FastSwipeThresholdDistance = 50;
        public float FastSwipeThresholdTime = 0.3f;
        public float DecelerationRate = 10f;
        #endregion

        #region EventName
        public const string ON_SCROLL_SNAP_INCREMENT_PAGE = "OnScrollSnapIncrementPage";
        #endregion

        #region MonoBehaviour
        void Start()
        {
            Init(0);
        }

        void Update()
        {
            // if moving to target position
            if (m_lerp)
            {
                // prevent overshooting with values greater than 1
                float decelerate = Mathf.Min(DecelerationRate * Time.deltaTime, 1f);
                m_container.anchoredPosition = Vector2.Lerp(m_container.anchoredPosition, m_lerpTo, decelerate);
                // time to stop lerping?
                if (Vector2.SqrMagnitude(m_container.anchoredPosition - m_lerpTo) < 0.25f)
                {
                    // snap to target and stop lerping
                    m_container.anchoredPosition = m_lerpTo;
                    m_lerp = false;
                    // clear also any scrollrect move that may interfere with our lerping
                    m_scrollRectComponent.velocity = Vector2.zero;
                }
            }
        }
        #endregion

        #region Public
        public void Init(int in_nbrPages)
        {
            m_scrollRectComponent = GetComponent<ScrollRect>();
            m_scrollRectRect = GetComponent<RectTransform>();
            m_container = m_scrollRectComponent.content;
            m_pageCount = in_nbrPages;

            m_lerp = false;

            // init
            SetPagePositions();
            SetPage(StartingPage);
        }

        public void OnBeginDrag(PointerEventData aEventData)
        {
            // if currently lerping, then stop it as user is draging
            m_lerp = false;
            // not dragging yet
            m_dragging = false;
        }

        public void OnEndDrag(PointerEventData aEventData)
        {
            // how much was container's content dragged
            float difference = m_startPosition.x - m_container.anchoredPosition.x;

            // test for fast swipe - swipe that moves only +/-1 item
            if (Time.unscaledTime - m_timeStamp < FastSwipeThresholdTime &&
                Mathf.Abs(difference) > FastSwipeThresholdDistance &&
                Mathf.Abs(difference) < m_fastSwipeThresholdMaxLimit)
            {
                if (difference > 0)
                {
                    IncrementScreen(1);
                }
                else
                {
                    IncrementScreen(-1);
                }
            }
            else
            {
                // if not fast time, look to which page we got to
                LerpToPage(GetNearestPage());
            }

            m_dragging = false;
        }

        public void OnDrag(PointerEventData aEventData)
        {
            if (!m_dragging)
            {
                // dragging started
                m_dragging = true;
                // save time - unscaled so pausing with Time.scale should not affect it
                m_timeStamp = Time.unscaledTime;
                // save current position of cointainer
                m_startPosition = m_container.anchoredPosition;
            }
        }

        public int GetCurrentPage()
        {
            return m_currentPage;
        }
        #endregion

        #region Private
        private void SetPagePositions()
        {
            int width = 0;
            int offsetX = 0;
            int containerWidth = 0;
            int containerHeight = 0;

            // screen width in pixels of scrollrect window
            width = (int)m_scrollRectRect.rect.width;
            // center position of all pages
            offsetX = width / 2;
            // total width
            containerWidth = width * m_pageCount;
            // limit fast swipe length - beyond this length it is fast swipe no more
            m_fastSwipeThresholdMaxLimit = width;

            // set width of container
            Vector2 newSize = new Vector2(containerWidth, containerHeight);
            m_container.sizeDelta = newSize;
            Vector2 newPosition = new Vector2(containerWidth / 2, containerHeight / 2);
            m_container.anchoredPosition = newPosition;

            // delete any previous settings
            m_pagePositions.Clear();

            // iterate through all container children and set their positions
            for (int i = 0; i < m_pageCount; i++)
            {
                RectTransform child = m_container.GetChild(i).GetComponent<RectTransform>();
                Vector2 childPosition = new Vector2(i * width - containerWidth / 2 + offsetX, 0f);

                child.anchoredPosition = childPosition;
                m_pagePositions.Add(-childPosition);
            }
        }

        private void SetPage(int aPageIndex)
        {
            aPageIndex = Mathf.Clamp(aPageIndex, 0, m_pageCount - 1);
            if (m_pagePositions.Count != 0)
            {
                m_container.anchoredPosition = m_pagePositions[aPageIndex];
                m_currentPage = aPageIndex;
                GEventManager.TriggerEvent(ScrollSnapPage.ON_SCROLL_SNAP_INCREMENT_PAGE);
            }
        }

        private void LerpToPage(int aPageIndex)
        {
            aPageIndex = Mathf.Clamp(aPageIndex, 0, m_pageCount - 1);
            m_lerpTo = m_pagePositions[aPageIndex];
            m_lerp = true;
            m_currentPage = aPageIndex;
            GEventManager.TriggerEvent(ScrollSnapPage.ON_SCROLL_SNAP_INCREMENT_PAGE);
        }

        private void IncrementScreen(int in_value)
        {
            LerpToPage(m_currentPage + in_value);
        }

        private int GetNearestPage()
        {
            // based on distance from current position, find nearest page
            Vector2 currentPosition = m_container.anchoredPosition;

            float distance = float.MaxValue;
            int nearestPage = m_currentPage;

            for (int i = 0; i < m_pagePositions.Count; i++)
            {
                float testDist = Vector2.SqrMagnitude(currentPosition - m_pagePositions[i]);
                if (testDist < distance)
                {
                    distance = testDist;
                    nearestPage = i;
                }
            }

            return nearestPage;
        }

        private int m_fastSwipeThresholdMaxLimit;

        private ScrollRect m_scrollRectComponent;
        private RectTransform m_scrollRectRect;
        private RectTransform m_container;

        private int m_pageCount;
        private int m_currentPage;

        // whether lerping is in progress and target lerp position
        private bool m_lerp;
        private Vector2 m_lerpTo;

        // target position of every page
        private List<Vector2> m_pagePositions = new List<Vector2>();

        // in draggging, when dragging started and where it started
        private bool m_dragging;
        private float m_timeStamp;
        private Vector2 m_startPosition;
        #endregion
    }
}