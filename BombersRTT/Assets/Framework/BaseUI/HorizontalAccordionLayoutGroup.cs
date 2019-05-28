using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameframework
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class HorizontalAccordionLayoutGroup : BaseBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region Public Variables
        public float Sensitivity = 1.0f;
        public float ScrollSpeed = 5.0f;
        public int Padding = 15;
        public bool ChangeScale = true;
        public bool Interactable = true;

        public eFocusDirection FocusDirection = eFocusDirection.Left;
        public enum eFocusDirection { Left, Right }

        public delegate void OnIndexChangeDelegate(RectTransform in_currentObject, List<RectTransform> in_objectsToUpdate, int in_index);
        public OnIndexChangeDelegate OnIndexChange = null;
        #endregion

        #region MonoBehaviour
        private void Start()
        {
            List<object> transformList = new List<object>();

            for (int i = 0; i < transform.childCount; i++)
            {
                transformList.Add(transform.GetChild(i));
            }
            SetObjects(transformList);
            m_Index = m_ObjectsToUpdate.Count;
            clampTarget();
        }

        private void Update()
        {
            if (m_ObjectsToUpdate.Count > 0)
            {
                m_fOffset = Mathf.Lerp(m_fOffset, m_fTargetOffset, Time.deltaTime * ScrollSpeed);
                if (m_Index < 0 || m_Index >= m_ObjectsToUpdate.Count)
                    clampTarget();
                updatePositions();

                if (m_LastIndex != m_Index)
                {
                    if (OnIndexChange != null)
                        OnIndexChange.Invoke(m_ObjectsToUpdate[m_Index], m_ObjectsToUpdate, m_Index);
                    m_LastIndex = m_Index;
                }
            }
            else
            {
                m_Index = 0;
                m_fOffset = 0.0f;
                m_fTargetOffset = 0.0f;
            }
        }
        #endregion

        #region Public Accessors
        public List<RectTransform> ObjectsToControl { get { return m_ObjectsToUpdate; } }
        private RectTransform RectTransform
        {
            get
            {
                if (m_RectTransform == null)
                    m_RectTransform = transform as RectTransform;
                return m_RectTransform;
            }
        }
        #endregion 

        private float getDivisor()
        {
            float divisor = 1.0f / (m_ObjectsToUpdate.Count + (m_ObjectsToUpdate.Count % 2 == 0 ? 1 : 0));
            return divisor;
        }
        #region Interface Implementation
        public void OnBeginDrag(PointerEventData eventData) { }

        public void OnDrag(PointerEventData eventData)
        {
            if (Interactable)
            {
                float divisor = getDivisor();
                m_fTargetOffset += eventData.delta.x * 0.01f * Time.deltaTime * Sensitivity / m_ObjectsToUpdate.Count;
                m_Index = getIndex(m_fTargetOffset, divisor);
                clampTarget();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (Interactable)
            {
                if (m_ObjectsToUpdate.Count == 1)
                {
                    m_fTargetOffset = 0.0f;
                    m_Index = 0;
                }
                else
                {
                    float divisor = getDivisor();
                    m_fTargetOffset = (float)(HudHelper.QuickRound(m_fTargetOffset / divisor)) * divisor;
                    m_Index = getIndex(m_fTargetOffset, divisor);
                    clampTarget();
                }
            }
        }
        #endregion

        #region Public Methods
        public void RemoveAllItems()
        {
            for (int i = 0; i < m_ObjectsToUpdate.Count; ++i)
            {
                Destroy(m_ObjectsToUpdate[i].gameObject);
            }
            m_ObjectsToUpdate.Clear();
        }

        public void RemoveAtIndex(int in_iIndex)
        {
            List<object> list = new List<object>();
            for (int i = 0; i < m_ObjectsToUpdate.Count; i++)
            {
                if (i != in_iIndex)
                    list.Add(m_ObjectsToUpdate[i]);
            }
            SetObjects(list);
        }

        public void MoveRight()
        {
            if (m_Index > 0)
            {
                float divisor = getDivisor();
                m_fTargetOffset += divisor;
                OnEndDrag(null);
            }
        }

        public void MoveLeft()
        {
            if (m_Index < m_ObjectsToUpdate.Count - 1)
            {
                float divisor = getDivisor();
                m_fTargetOffset -= divisor;
                OnEndDrag(null);
            }
        }

        public void FocusOn(int in_iIndex, bool in_bIsInstant = false)
        {
            m_Index = in_iIndex;
            m_fTargetOffset = -m_ObjectsToUpdate.Count * m_Index + (m_ObjectsToUpdate.Count * (m_ObjectsToUpdate.Count / 2));

            OnEndDrag(null);

            if (in_bIsInstant)
                m_fOffset = m_fTargetOffset;
        }

        public void FocusOnIndexWithTarget(int in_index, float in_target)
        {
            m_Index = in_index;
            m_fTargetOffset = in_target;

            OnEndDrag(null);
        }

        public void FocusOnFirst(bool in_bIsInstant = false)
        {
            FocusOn(0, in_bIsInstant);
        }

        public void FocusOnLast(bool in_bIsInstant = false)
        {
            FocusOn(m_ObjectsToUpdate.Count - 1, in_bIsInstant);
        }

        public void FocusOnMiddle(bool in_bIsInstant = false)
        {
            FocusOn(m_ObjectsToUpdate.Count / 2, in_bIsInstant);
        }

        public void SetObjects(List<object> in_objects)
        {
            for (int i = 0; i < m_ObjectsToUpdate.Count; i++)
            {
                if (!in_objects.Contains(m_ObjectsToUpdate[i]))
                {
                    m_ObjectsToUpdate[i].SetParent(null);
                }
            }

            m_ObjectsToUpdate.Clear();
            List<Component> toIgnoreList = new List<Component>();

            for (int i = 0; i < in_objects.Count; i++)
            {
                RectTransform rect = null;

                if (in_objects[i] is GameObject)
                    rect = (in_objects[i] as GameObject).GetComponent<RectTransform>();
                else if (in_objects[i] is Component)
                    rect = (in_objects[i] as Component).GetComponent<RectTransform>();

                if (rect == null || !rect.gameObject.activeInHierarchy)
                    continue;

                rect.GetComponents(typeof(ILayoutIgnorer), toIgnoreList);
                if (toIgnoreList.Count == 0)
                {
                    m_ObjectsToUpdate.Add(rect);
                    rect.SetParent(this.transform);
                    continue;
                }
            }

            switch (FocusDirection)
            {
                case eFocusDirection.Right:
                    m_Index = m_ObjectsToUpdate.Count + 1;
                    break;
                default:
                case eFocusDirection.Left:
                    m_Index = -1;
                    break;
            }

            OnEndDrag(null);
        }

        public void AddObject(object in_object)
        {
            List<object> objects = new List<object>();
            objects.Add(in_object);
            AddObjects(objects);
        }

        public void AddObjects(List<object> in_objects)
        {
            List<object> objects = new List<object>();
            for (int i = 0; i < m_ObjectsToUpdate.Count; ++i)
                objects.Add(m_ObjectsToUpdate[i]);
            objects.AddRange(in_objects);
            SetObjects(objects);
        }

        public void AddObjectAtIndex(object in_object, int in_iIndex = 0)
        {
            List<object> objects = new List<object>();
            objects.Add(in_object);
            AddObjectsAtIndex(objects, in_iIndex);
        }

        public void AddObjectsAtIndex(List<object> in_objects, int in_iIndex = 0)
        {
            List<object> objects = new List<object>();
            for (int i = 0; i < m_ObjectsToUpdate.Count; ++i)
                objects.Add(m_ObjectsToUpdate[i]);
            objects.InsertRange(in_iIndex, in_objects);
            SetObjects(objects);

            if (OnIndexChange != null)
                OnIndexChange.Invoke(m_ObjectsToUpdate[in_iIndex], m_ObjectsToUpdate, in_iIndex);
        }
        #endregion

        #region Private
        private void clampTarget()
        {
            if (m_ObjectsToUpdate.Count == 0)
            {
                m_Index = 0;
                m_fTargetOffset = 0.0f;
                return;
            }
            if (m_Index < 0)
            {
                m_fTargetOffset = ((-m_ObjectsToUpdate.Count * 0.5f) / (-m_ObjectsToUpdate.Count)) - (getDivisor() * 0.1f);
                m_Index = 0;
            }
            else if (m_Index >= m_ObjectsToUpdate.Count)
            {
                int value = m_ObjectsToUpdate.Count + (m_ObjectsToUpdate.Count % 2 == 0 ? 1 : 0);
                m_fTargetOffset = (((m_ObjectsToUpdate.Count) - value * 0.5f) / (-value)) + (getDivisor() * 0.1f);
                m_Index = m_ObjectsToUpdate.Count - 1;
            }
        }

        private int getIndex(float in_fNumerator, float in_fDivisor)
        {
            return HudHelper.QuickRound(in_fNumerator / in_fDivisor) * -1 + m_ObjectsToUpdate.Count / 2;
        }

        private void updatePositions()
        {
            Vector3[] corners = new Vector3[4];
            RectTransform.GetWorldCorners(corners);
            m_Bounds.x = corners[0].x;
            m_Bounds.y = corners[2].x;
            m_Bounds.z = corners[1].y;
            m_Bounds.w = corners[0].y;

            m_FocusedObjectBounds.x = (m_Bounds.x + m_Bounds.y) / 2 - m_ObjectsToUpdate[m_Index].sizeDelta.x;
            m_FocusedObjectBounds.y = (m_Bounds.x + m_Bounds.y) / 2 + m_ObjectsToUpdate[m_Index].sizeDelta.x;
            setPosition(m_Index);
            for (int i = 0; i < m_ObjectsToUpdate.Count; i++)
            {
                if (m_Index != i)
                    setPosition(i);
            }
            m_ObjectsToUpdate[m_Index].SetAsLastSibling();
        }

        private void setPosition(int in_iIndex)
        {
            if (m_ObjectsToUpdate[in_iIndex] == null) return;

            if (in_iIndex < m_Index)
                m_ObjectsToUpdate[in_iIndex].SetAsLastSibling();
            else
                m_ObjectsToUpdate[in_iIndex].SetAsFirstSibling();

            float percentageAcross = ((float)in_iIndex + 0.5f) / (m_ObjectsToUpdate.Count + (m_ObjectsToUpdate.Count % 2 == 0 ? 1 : 0));
            float realX = getXPercentage(percentageAcross);


            m_TargetPosition = new Vector3(Mathf.Lerp(m_Bounds.x + m_ObjectsToUpdate[in_iIndex].sizeDelta.x / 2, m_Bounds.y - m_ObjectsToUpdate[in_iIndex].sizeDelta.x / 2, realX),
                                           transform.TransformPoint(Vector3.zero).y,
                                           transform.parent.position.z);

            if (m_Index != in_iIndex)
            {
                m_CurrentObjectBounds = getObjectBoundsGivenPosition(in_iIndex, m_TargetPosition);
                if (m_CurrentObjectBounds.y > m_FocusedObjectBounds.x && in_iIndex < m_Index)
                {
                    m_TargetPosition += Vector3.left * (m_CurrentObjectBounds.y - m_FocusedObjectBounds.x);
                    m_CurrentObjectBounds = getObjectBoundsGivenPosition(in_iIndex, m_TargetPosition);
                }
                if (m_CurrentObjectBounds.x < m_FocusedObjectBounds.y && in_iIndex > m_Index)
                {
                    m_TargetPosition += Vector3.right * (m_FocusedObjectBounds.y - m_CurrentObjectBounds.x);
                    m_CurrentObjectBounds = getObjectBoundsGivenPosition(in_iIndex, m_TargetPosition);
                }

                if (m_CurrentObjectBounds.x < m_Bounds.x)
                {
                    m_TargetPosition += Vector3.right * (m_Bounds.x - m_CurrentObjectBounds.x);
                }
                if (m_CurrentObjectBounds.y > m_Bounds.y)
                {
                    m_TargetPosition += Vector3.left * (m_CurrentObjectBounds.y - m_Bounds.y);
                }
            }

            m_ObjectsToUpdate[in_iIndex].position = Vector3.Lerp(m_ObjectsToUpdate[in_iIndex].position, m_TargetPosition, Time.deltaTime * ScrollSpeed * 2);

            if (ChangeScale)
            {
                float scale = Math.Max(Mathf.InverseLerp(m_ObjectsToUpdate[in_iIndex].position.x < (m_Bounds.x + m_Bounds.y) / 2 ? m_Bounds.x : m_Bounds.y,
                                                         (m_Bounds.x + m_Bounds.y) / 2,
                                                         m_ObjectsToUpdate[in_iIndex].position.x),
                                       0.9f);

                m_ObjectsToUpdate[in_iIndex].localScale = new Vector3(scale, scale, scale);
            }
        }

        private Vector2 getObjectBounds(int in_iIndex)
        {
            Vector2 toReturn = new Vector2();
            Vector3[] corners = new Vector3[4];
            m_ObjectsToUpdate[in_iIndex].GetWorldCorners(corners);
            toReturn.x = Mathf.Min(corners[0].x, corners[1].x);
            toReturn.y = Mathf.Max(corners[2].x, corners[3].x);
            if (in_iIndex == m_Index)
            {
                toReturn.x -= Padding;
                toReturn.y += Padding;
            }
            return toReturn;
        }

        private Vector2 getObjectBoundsGivenPosition(int in_iIndex, Vector3 in_givenPosition)
        {
            Vector2 toReturn = new Vector2();
            Vector3[] corners = new Vector3[4];
            m_ObjectsToUpdate[in_iIndex].GetWorldCorners(corners);
            toReturn.x = Mathf.Min(corners[0].x, corners[1].x);
            toReturn.y = Mathf.Max(corners[2].x, corners[3].x);
            if (in_iIndex == m_Index)
            {
                toReturn.x -= Padding;
                toReturn.y += Padding;
            }
            toReturn.x = toReturn.x - m_ObjectsToUpdate[in_iIndex].position.x + in_givenPosition.x;
            toReturn.y = toReturn.y - m_ObjectsToUpdate[in_iIndex].position.x + in_givenPosition.x;
            return toReturn;
        }

        private float getXPercentage(float in_fInput)
        {
            return (1.0f) / (1.0f + (10.0f * Mathf.Pow(0.000005f, (in_fInput + m_fOffset) - 0.31f)));
        }

        private List<RectTransform> m_ObjectsToUpdate = new List<RectTransform>();
        private RectTransform m_RectTransform = null;
        private float m_fOffset = 0.0f;
        private float m_fTargetOffset = 0.0f;
        private int m_Index = 0;
        private int m_LastIndex = 0;

        // x = left
        // y = right
        // z = up
        // w = down
        private Vector4 m_Bounds = new Vector4();

        // x = left
        // y = right
        private Vector2 m_FocusedObjectBounds = new Vector2();
        private Vector2 m_CurrentObjectBounds = new Vector2();
        private Vector3 m_TargetPosition = new Vector3();
        #endregion
    }
}