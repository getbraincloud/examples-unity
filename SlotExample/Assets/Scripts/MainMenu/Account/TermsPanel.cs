using UnityEngine;
using UnityEngine.UI;
using BrainCloudSlots.Connection;

public class TermsPanel : MonoBehaviour
{
    public Text TermsText;
    private Scrollbar _scrollBar;

    void Awake()
    {
        _scrollBar = GetComponentsInChildren<Scrollbar>(true)[0];
    }

    void OnEnable()
    {
        _scrollBar.value = 1f;
        TermsText.text = BrainCloudStats.Instance.m_termsConditionsString;
    }
}
