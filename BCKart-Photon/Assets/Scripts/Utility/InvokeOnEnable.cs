using UnityEngine;
using UnityEngine.Events;

public class InvokeOnEnable : MonoBehaviour
{
	public UnityEvent onEnable;

	private void OnEnable()
	{
		onEnable.Invoke();
	}
}
