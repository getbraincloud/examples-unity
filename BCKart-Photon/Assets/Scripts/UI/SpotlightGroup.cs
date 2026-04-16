using System.Collections.Generic;
using UnityEngine;

public class SpotlightGroup : MonoBehaviour
{
    private static readonly Dictionary<string, SpotlightGroup> spotlights = new Dictionary<string, SpotlightGroup>();

	public string searchName = "";
	public int defaultIndex = -1;
	public List<GameObject> objects;

    private GameObject focused = null;

	public static bool Search(string spotlightName, out SpotlightGroup spotlight)
	{
		return spotlights.TryGetValue(spotlightName, out spotlight);
	}

	private void OnEnable()
	{
		if (string.IsNullOrEmpty(searchName) == false)
		{
			spotlights.Add(searchName, this);
		}
	}

	private void OnDisable()
	{
		if (string.IsNullOrEmpty(searchName) == false)
		{
			spotlights.Remove(searchName);
		}
	}

	private void Awake()
	{
		objects.ForEach((obj) => obj.SetActive(false));
		if (defaultIndex != -1)
		{
			FocusIndex(defaultIndex);
		}
	}

	public void FocusIndex(int index)
	{
		if (focused) focused.SetActive(false);
		focused = objects[index];
		focused.SetActive(true);
	}

	public void Defocus()
	{
		if (focused) focused.SetActive(false);
		focused = null;
	}
}
