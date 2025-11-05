using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;

public class GraphicsSettingsUI : MonoBehaviour
{
    public Dropdown graphicsDropdown;
    public Toggle postprocessingToggle;

    private void Awake()
    {
        InitGraphicsDropdown();
    }

    public void InitGraphicsDropdown()
    {
        string[] names = QualitySettings.names;
        List<string> options = new List<string>();

        for (int i = 0; i < names.Length; i++)
        {
            options.Add(names[i]);
        }
        graphicsDropdown.AddOptions(options);
        QualitySettings.SetQualityLevel(graphicsDropdown.options.Count - 1);
        graphicsDropdown.value = graphicsDropdown.options.Count - 1;
    }

    public void SetGraphicsQuality()
    {
        QualitySettings.SetQualityLevel(graphicsDropdown.value);
    }
    public void TogglePostProcessing()
    {
        if (Camera.main.TryGetComponent(out PostProcessLayer ppl))
        {
            ppl.enabled = postprocessingToggle.isOn;
        }
    }
}
