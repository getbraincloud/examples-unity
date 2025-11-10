using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class HoverAction : MonoBehaviour, ISelectHandler, IPointerEnterHandler
{
	public UnityEvent onSelect;

	public void OnPointerEnter(PointerEventData eventData)
	{
		onSelect.Invoke();
	}

	public void OnSelect(BaseEventData eventData)
	{
		onSelect.Invoke();
	}
}
