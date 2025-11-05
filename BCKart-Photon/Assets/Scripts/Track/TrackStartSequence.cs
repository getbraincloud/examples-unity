using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackStartSequence : MonoBehaviour {
    public Action OnComplete;
    public float duration;
    public AudioClip sequenceAudio;

    private AudioSource _audioSource;

    public void StartSequence() {
        StartCoroutine(PlaySequence());
    }

    public IEnumerator PlaySequence() {
        if ( sequenceAudio != null ) {
            if ( _audioSource == null )
                _audioSource = new GameObject("Start Track Sequence Audio").AddComponent<AudioSource>();

            _audioSource.clip = sequenceAudio;
            _audioSource.Play();

            Destroy(_audioSource.gameObject, _audioSource.clip.length);
        }

        yield return new WaitForSeconds(duration);

        OnComplete?.Invoke();
    }
}
