using UnityEngine;

[CreateAssetMenu(fileName = "New Track", menuName = "Scriptable Object/Track Definition")]
public class TrackDefinition : ScriptableObject
{
	public string trackName;
	public Sprite trackIcon;
	public int buildIndex;
}
