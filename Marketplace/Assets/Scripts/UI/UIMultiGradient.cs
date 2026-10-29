using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/UI MultiGradient")]
public class UIMultiGradient : BaseMeshEffect
{
    public enum GradientDirection
    {
        Horizontal,
        Vertical
    }

    [SerializeField]
    private GradientDirection direction = GradientDirection.Vertical;

    [SerializeField]
    private List<Color> colors = new List<Color>
    {
        Color.white,
        Color.black
    };

    private readonly List<UIVertex> _vertexList = new List<UIVertex>();

    public override void ModifyMesh(VertexHelper helper)
    {
        if (!IsActive() || helper.currentVertCount == 0 || colors == null || colors.Count < 2)
            return;

        _vertexList.Clear();
        helper.GetUIVertexStream(_vertexList);

        // Find min/max positions
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int i = 0; i < _vertexList.Count; i++)
        {
            float value = direction == GradientDirection.Horizontal
                ? _vertexList[i].position.x
                : _vertexList[i].position.y;

            min = Mathf.Min(min, value);
            max = Mathf.Max(max, value);
        }

        float range = max - min;
        if (Mathf.Approximately(range, 0f))
            return;

        // Apply gradient
        for (int i = 0; i < _vertexList.Count; i++)
        {
            UIVertex v = _vertexList[i];

            float value = direction == GradientDirection.Horizontal
                ? v.position.x
                : v.position.y;

            float t = Mathf.InverseLerp(min, max, value);
            v.color = EvaluateGradient(t);

            _vertexList[i] = v;
        }

        helper.Clear();
        helper.AddUIVertexTriangleStream(_vertexList);
    }

    private Color EvaluateGradient(float t)
    {
        t = Mathf.Clamp01(t);

        int segmentCount = colors.Count - 1;
        float scaledT = t * segmentCount;

        int index = Mathf.FloorToInt(scaledT);
        index = Mathf.Clamp(index, 0, segmentCount - 1);

        float localT = scaledT - index;

        return Color.Lerp(colors[index], colors[index + 1], localT);
    }
}
