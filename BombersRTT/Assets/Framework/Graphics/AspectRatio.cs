using UnityEngine;
using System.Collections;
namespace Gameframework
{
    public static class AspectRatio
    {
        public static Vector2 GetAspectRatio()
        {
            return GetAspectRatio(Screen.width, Screen.height);
        }

        public static Vector2 GetAspectRatio(int x, int y)
        {
            float f = (float)x / (float)y;
            int i = 0;
            while (true)
            {
                i++;
                if (System.Math.Round(f * i, 2) == Mathf.RoundToInt(f * i))
                    break;
            }
            return new Vector2((float)System.Math.Round(f * i, 2), i);
        }

        public static Vector2 GetAspectRatio(Vector2 xy)
        {
            float f = xy.x / xy.y;
            int i = 0;
            while (true)
            {
                i++;
                if (System.Math.Round(f * i, 2) == Mathf.RoundToInt(f * i))
                    break;
            }
            return new Vector2((float)System.Math.Round(f * i, 2), i);
        }

        public static bool IsLetterBox()
        {
            Vector2 aspectRation = GetAspectRatio();

            return (aspectRation.x == 4 && aspectRation.y == 3) ||
                    (aspectRation.x == 3 && aspectRation.y == 4);
        }
    }
}