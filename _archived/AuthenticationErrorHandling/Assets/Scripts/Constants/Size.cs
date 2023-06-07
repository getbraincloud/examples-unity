using UnityEngine;

/**
 * Default sizes for test app 
 */

public abstract class SIZE
{
    public static Rect FullScreen()
    {
        return new Rect(0, 0, Screen.width, Screen.height);
    }


    public static Rect Page()
    {
        float yPadding = Screen.height * 0.02f;
        float xPadding = Screen.width * 0.02f;

        return new Rect(0 + xPadding,
            0 + yPadding,
            Screen.width - xPadding * 2,
            Screen.height - yPadding * 2);
    }

    public static Rect Dialog()
    {
        float yPadding = Screen.height * 0.3f;
        float xPadding = Screen.width * 0.12f;

        return new Rect(0 + xPadding,
            0 + yPadding,
            Screen.width - xPadding * 2,
            Screen.height - yPadding * 2);
    }
}