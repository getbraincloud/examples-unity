using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlowLayoutGroup : LayoutGroup
{
    private readonly List<int> RowStarts = new List<int>();
    private readonly List<float> RowHeights = new List<float>();

    [SerializeField] private Vector2 Spacing;

    public bool ExpandToFillRow { get; set; } = false;

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();

        float widest = 0f; // Minimum width is the widest single item, since everything else can wrap
        foreach (var child in rectChildren)
        {
            widest = Mathf.Max(widest, LayoutUtility.GetPreferredWidth(child));
        }

        float width = padding.horizontal + widest;
        SetLayoutInputForAxis(width, width, -1, 0);
    }

    public override void CalculateLayoutInputVertical()
    {
        BuildRows();

        float total = padding.vertical + Spacing.y * Mathf.Max(0, RowHeights.Count - 1);
        foreach (float h in RowHeights)
        {
            total += h;
        }

        SetLayoutInputForAxis(total, total, -1, 1);
    }

    public override void SetLayoutHorizontal() { } // Positions depend on wrapping, which depends on the final width, so everything is placed in SetLayoutVertical

    public override void SetLayoutVertical()
    {
        float available = AvailableWidth;
        float y = padding.top;

        for (int r = 0; r < RowStarts.Count; r++)
        {
            int start = RowStarts[r];
            int end = r + 1 < RowStarts.Count ? RowStarts[r + 1] : rectChildren.Count;

            float extraPerItem = 0f;
            if (ExpandToFillRow)
            {
                float used = Spacing.x * (end - start - 1);
                for (int i = start; i < end; i++)
                    used += ChildWidth(rectChildren[i], available);
                extraPerItem = (available - used) / (end - start);
            }

            float x = padding.left;
            for (int i = start; i < end; i++)
            {
                RectTransform child = rectChildren[i];
                float w = ChildWidth(child, available) + extraPerItem;

                SetChildAlongAxis(child, 0, x, w);
                SetChildAlongAxis(child, 1, y, RowHeights[r]);
                x += w + Spacing.x;
            }

            y += RowHeights[r] + Spacing.y;
        }
    }

    private float AvailableWidth => rectTransform.rect.width - padding.horizontal;

    // Clamp so a single item wider than the container doesn't overflow
    private static float ChildWidth(RectTransform child, float available) =>
        Mathf.Min(LayoutUtility.GetPreferredWidth(child), available);

    private void BuildRows()
    {
        RowStarts.Clear();
        RowHeights.Clear();

        float available = AvailableWidth;
        float x = 0f;
        float rowHeight = 0f;

        for (int i = 0; i < rectChildren.Count; i++)
        {
            RectTransform child = rectChildren[i];
            float w = ChildWidth(child, available);
            float h = LayoutUtility.GetPreferredHeight(child);

            if (i == 0 || x + Spacing.x + w > available)
            {
                if (i > 0) // Start a new row
                {
                    RowHeights.Add(rowHeight);
                }

                RowStarts.Add(i);
                x = w;
                rowHeight = h;
            }
            else
            {
                x += Spacing.x + w;
                rowHeight = Mathf.Max(rowHeight, h);
            }
        }

        if (rectChildren.Count > 0)
        {
            RowHeights.Add(rowHeight);
        }
    }
}
