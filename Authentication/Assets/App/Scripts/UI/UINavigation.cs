using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Simple way to navigate UI elements using Tab and Shift+Tab.
/// TODO: Will be expanded upon in the future for controller support.
/// </summary>
public class UINavigation : MonoBehaviour, ISelectHandler
{
    //[SerializeField] private Selectable NextNavigation = default;
    //[SerializeField] private Selectable PreviousNavigation = default;
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

    private void Start()
    {
        Debug.Assert(eventSystem != null, "There is no EventSystem in the scene!");

        if (eventSystem.currentSelectedGameObject == null)
        {
            SetSelectedGameObject(UIElementNavigationOrder[0].gameObject);
        }
    }

    private void OnDestroy()
    {
        eventSystem = null;
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
            SetSelectedGameObject(getPrev ? GetPreviousUIElement() : getNext ? GetNextUIElement() : null);
        }
        else if (!hasSelectedGameObject && selectedIndex >= 0 && (getNext || getPrev ||
                 Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
                 Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow)))
        {
            selectedIndex--;
            selectedIndex = selectedIndex < 0 ? maxIndex - 1 : selectedIndex;
            SetSelectedGameObject(GetNextUIElement());
        }
    }

    #endregion

    #region UINavigation

    public void OnSelect(BaseEventData eventData)
    {
        if (selectedIndex < 0)
        {
            selectedIndex = maxIndex;
        }
        else
        {
            selectedIndex--;
            selectedIndex = selectedIndex < 0 ? maxIndex - 1 : selectedIndex;
        }
        
        SetSelectedGameObject(GetNextUIElement());
    }

    private void SetSelectedGameObject(GameObject next, BaseEventData eventData = null)
    {
        if (next != null)
        {
            eventSystem.SetSelectedGameObject(next, eventData != null ? eventData : new BaseEventData(eventSystem));
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

    private GameObject GetNextUIElement()
    {
        if (selectedIndex < 0)
        {
            return null;
        }

        Selectable selected = null;
        for (int i = 0; i < maxIndex; i++)
        {
            //if(selectedIndex++ >= maxIndex && IsSelectable(NextSelectable))
            //{
            //    selectedIndex = -1;
            //    return NextSelectable.gameObject;
            //}

            selectedIndex++;
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
            //if (selectedIndex-- < 0 && IsSelectable(PreviousSelectable))
            //{
            //    selectedIndex = -1;
            //    return PreviousSelectable.gameObject;
            //}

            selectedIndex--;
            selectedIndex = selectedIndex < 0 ? maxIndex - 1 : selectedIndex;
            selected = UIElementNavigationOrder[selectedIndex];

            if (IsSelectable(selected))
            {
                break;
            }
        }

        return selected != null ? selected.gameObject : null;
    }

    #endregion
}
