using UnityEngine;

public class AudioDestroyer : MonoBehaviour
{
    private AudioSource src;
	private void Awake()
	{
		src = GetComponent<AudioSource>();
	}

    private void Update()
    {
		if (src == null) src = GetComponent<AudioSource>();
		// it's possible that a playOnAwake sound will not play if too many other sounds are playing
		if (src.timeSamples == src.clip.samples || src.isPlaying == false)
		{
			Destroy(gameObject);
		}
    }
}
