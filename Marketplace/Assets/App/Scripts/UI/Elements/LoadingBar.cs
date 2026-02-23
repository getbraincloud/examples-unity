using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LoadingBar : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent OnLoadingComplete;

    [Header("LoadingBar config")]
    [SerializeField]
    private bool _fakeLoading = true;
    [SerializeField]
    private Vector2 _increaseMinMax = Vector2.one * 0.25f;

    [SerializeField]
    private Vector2 _waitMinMax = Vector2.one * 0.15f;

    private Slider _loadingBar;
    public bool loadingComplete { get; private set; }

    private float _loadingValue;


    void Start()
    {
        _loadingBar = GetComponent<Slider>();
        if (_fakeLoading) StartCoroutine(StartFakeLoading());
    }

    IEnumerator StartFakeLoading()
    {
        while (_loadingValue < 1f)
        {
            float randIncrease = Random.Range(_increaseMinMax.x, _increaseMinMax.y);
            _loadingValue += randIncrease;
            _loadingValue = Mathf.Clamp(_loadingValue, 0f, 1f);

            float randWait = Random.Range(_waitMinMax.x, _waitMinMax.y);
            yield return new WaitForSeconds(randWait);
        }
    }

    IEnumerator DelayedLoadingEnd(float t)
    {
        yield return new WaitForSeconds(t);
        if (!loadingComplete)
        {
            //loading complete!
            loadingComplete = true;
            OnLoadingComplete?.Invoke();
        }
    }

    public void SetLoadingValue(float value)
    {
        _loadingValue = value;
        if(_loadingValue == 1f)
        {
            StartCoroutine(DelayedLoadingEnd(1f));
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_loadingBar.value != _loadingValue)
        {
            _loadingBar.value = Mathf.Lerp(_loadingBar.value, _loadingValue, Time.deltaTime * 3.5f);
        }
    }
}
