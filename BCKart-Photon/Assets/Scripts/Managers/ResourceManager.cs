using FusionExamples.Utility;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
	public GameUI hudPrefab;
	public NicknameUI nicknameCanvasPrefab;
	public KartDefinition[] kartDefinitions;
	public GameType[] gameTypes;
	public TrackDefinition[] tracks;
	public Powerup[] powerups;
	public Powerup noPowerup;

	public static ResourceManager Instance => Singleton<ResourceManager>.Instance;

	private void Awake()
	{
		DontDestroyOnLoad(gameObject);
	}
}
