using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// This allows ScrollRects nested within ScrollRects to pass events to parent ScrollRects for proper usage.
/// </summary>
/// https://forum.unity.com/threads/nested-scrollrect.268551/#post-1906953
public class NestedScrollRect : ScrollRect
{
    private bool routeToParent = false;

    private void HandleEventForParents<T>(Action<T> action) where T : IEventSystemHandler
    {
        Transform parent = transform.parent;
        while (parent != null)
        {
            foreach (var component in parent.GetComponents<Component>())
            {
                if (component is T eventSystemHandler)
                {
                    action(eventSystemHandler);
                }
            }

            parent = parent.parent;
        }
    }

    public override void OnInitializePotentialDrag(PointerEventData data)
    {
        HandleEventForParents<IInitializePotentialDragHandler>((parent) =>
        {
            parent.OnInitializePotentialDrag(data);
        });

        base.OnInitializePotentialDrag(data);
    }

    public override void OnDrag(PointerEventData data)
    {
        if (routeToParent)
        {
            HandleEventForParents<IDragHandler>((parent) =>
            {
                parent.OnDrag(data);
            });
        }
        else
        {
            base.OnDrag(data);
        }
    }

    public override void OnBeginDrag(PointerEventData data)
    {
        if (!horizontal && Math.Abs(data.delta.x) > Math.Abs(data.delta.y))
        {
            routeToParent = true;
        }
        else if (!vertical && Math.Abs(data.delta.x) < Math.Abs(data.delta.y))
        {
            routeToParent = true;
        }
        else
        {
            routeToParent = false;
        }

        if (routeToParent)
        {
            HandleEventForParents<IBeginDragHandler>((parent) =>
            {
                parent.OnBeginDrag(data);
            });
        }
        else
        {
            base.OnBeginDrag(data);
        }
    }

    public override void OnEndDrag(PointerEventData data)
    {
        if (routeToParent)
        {
            HandleEventForParents<IEndDragHandler>((parent) =>
            {
                parent.OnEndDrag(data);
            });
        }
        else
        {
            base.OnEndDrag(data);
        }

        routeToParent = false;
    }

    public override void OnScroll(PointerEventData data)
    {
        Vector2 delta = data.scrollDelta;
        delta.y *= -1;
        if (!horizontal && Math.Abs(delta.x) > Math.Abs(delta.y))
        {
            routeToParent = true;
        }
        else if (!vertical && Math.Abs(delta.x) < Math.Abs(delta.y))
        {
            routeToParent = true;
        }
        else
        {
            routeToParent = false;
        }

        if (routeToParent)
        {
            HandleEventForParents<IScrollHandler>((parent) =>
            {
                parent.OnScroll(data);
            });
        }
        else
        {
            base.OnScroll(data);
        }
    }
}
