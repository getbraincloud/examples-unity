using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 
/// </summary>
public class UINavigation : MonoBehaviour
{
    [SerializeField] private Selectable NextUINavigation = default;
    [SerializeField] private Selectable PreviousUINavigation = default;
    [SerializeField] private Selectable[] UIElementNavigationOrder = default;

    private int selectedIndex = 0;
    private int maxIndex = 0;
    private EventSystem eventSystem = default;

    #region Unity Messages

    private void Awake()
    {
        maxIndex = UIElementNavigationOrder.Length;
        eventSystem = EventSystem.current;
    }

    private void OnEnable()
    {
        foreach (Selectable selectable in UIElementNavigationOrder)
        {

            //selectable.
        }
    }

    private void Start()
    {
        Debug.Assert(eventSystem != null, "There is no EventSystem in the scene!");

        if (eventSystem.currentSelectedGameObject == null)
        {
            SetSelectedGameObject(UIElementNavigationOrder[0].gameObject);
        }
    }

    private void Update()
    {
        bool hasSelectedGameObject = eventSystem.currentSelectedGameObject != null;
        bool getNext = Input.GetKeyDown(KeyCode.Tab);
        bool getPrev = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && getNext;

        if (hasSelectedGameObject &&
            eventSystem.currentSelectedGameObject.GetComponent<Selectable>() is Selectable current &&
            GetSelectableIndex(current) >= 0 && selectedIndex < maxIndex)
        {
            //bool isInputField = current.GetComponent<TMP_InputField>() != null;
            //
            //getNext = getNext || !isInputField && (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.RightArrow));
            //getPrev = getPrev || !isInputField && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.LeftArrow));

            SetSelectedGameObject(getPrev ? GetPreviousUIElement() : getNext ? GetNextUIElement() : null);
        }
        //else if (!hasSelectedGameObject && (getNext || getPrev ||
        //         Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
        //         Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow)))
        //{
        //    SetSelectedGameObject(GetCurrentElement());
        //}
    }

    private void OnDisable()
    {

    }

    private void OnDestroy()
    {
        eventSystem = null;
    }

    #endregion

    private void OnUIElementSelected(BaseEventData eventData)
    {

    }

    private void SetSelectedGameObject(GameObject next)
    {
        if (next != null)
        {
            Debug.Log(next.name);
            eventSystem.SetSelectedGameObject(next, new BaseEventData(eventSystem));
        }
    }

    private bool IsSelectable(Selectable selectable) =>
        selectable != null && selectable.interactable && selectable.enabled && selectable.gameObject.activeInHierarchy;

    private int GetSelectableIndex(Selectable selectable)
    {
        selectedIndex = -1;
        for (int i = 0; i < UIElementNavigationOrder.Length; i++)
        {
            if (selectable == UIElementNavigationOrder[i])
            {
                selectedIndex = i;
                break;
            }
        }

        return selectedIndex;
    }

    private GameObject GetCurrentElement()
    {
        if (selectedIndex < 0)
        {
            return null;
        }

        selectedIndex--;
        selectedIndex = selectedIndex < 0 ? maxIndex - 1 : selectedIndex;

        return GetNextUIElement();
    }

    private GameObject GetNextUIElement()
    {
        if (selectedIndex < 0)
        {
            return null;
        }

        Selectable selected = null;
        for (int i = 0; i < maxIndex; i++)
        {
            if(selectedIndex++ >= maxIndex && IsSelectable(NextUINavigation))
            {
                selectedIndex = -1;
                return NextUINavigation.gameObject;
            }

            selectedIndex = selectedIndex >= maxIndex ? 0 : selectedIndex;
            selected = UIElementNavigationOrder[selectedIndex];

            if (IsSelectable(selected))
            {
                break;
            }
        }

        return selected != null ? selected.gameObject : null;
    }

    private GameObject GetPreviousUIElement()
    {
        if (selectedIndex < 0)
        {
            return null;
        }

        Selectable selected = null;
        for (int i = 0; i < maxIndex; i++)
        {
            if (selectedIndex-- < 0 && IsSelectable(PreviousUINavigation))
            {
                selectedIndex = -1;
                return PreviousUINavigation.gameObject;
            }

            selectedIndex = selectedIndex < 0 ? maxIndex - 1 : selectedIndex;
            selected = UIElementNavigationOrder[selectedIndex];

            if (IsSelectable(selected))
            {
                break;
            }
        }

        return selected != null ? selected.gameObject : null;
    }
}
