using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LoadingCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text _counterText;
    // Start is called before the first frame update
    void OnEnable()
    {
        _counterText = this.GetComponent<TMP_Text>();
        _counterText.text = "0";
        StartCoroutine(Count());
    }
    void OnDisable()
    {
        StopCoroutine(Count());
        _counterText.text = "0";
    }

    private IEnumerator Count()
    {
        int count = 0;
        while (true)
        {
            count++;
            _counterText.text = count.ToString();
            yield return new WaitForSeconds(1f);
        }
    }
}
