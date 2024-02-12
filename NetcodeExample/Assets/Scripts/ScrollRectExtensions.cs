using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
public class ScrollRectExtensions : MonoBehaviour
{
	private ScrollRect _scrollRect;

	private void Awake()
	{
		_scrollRect = GetComponent<ScrollRect>();
	}

	public void ScrollToBottom()
	{
		_scrollRect.normalizedPosition = new Vector2(0, 0);
	}
}