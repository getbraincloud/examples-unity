using System.Collections.Generic;
using UnityEngine;

/**
 * Error Handling App. 
 * 
 * Description: Contains debug cases around error handling to serve as examples
 */

public class ErrorHandlingApp : MonoBehaviour
{
    private static ErrorHandlingApp m_instance;

    public static ErrorHandlingApp getInstance()
    {
        if (m_instance == null)
        {
            GameObject app = GameObject.Find("Application");
            if (app == null)
            {
                GameObject appObject = new GameObject("Application");
                return appObject.AddComponent<ErrorHandlingApp>();
            }
            else
            {
                return app.GetComponent<ErrorHandlingApp>();
            }
        }

        return m_instance;
    }

    public User m_user = new User();

    public MainPage m_mainPage;

    void Start()
    {
        if (m_instance)
        {
            throw new System.Exception("Application already declared");
        }

        m_instance = this;
        m_user.InitData();

        m_mainPage = gameObject.AddComponent<MainPage>();
    }

    List<GameObject> m_dialogs = new List<GameObject>();

    public void attachDialog(GameObject dialog)
    {
        m_dialogs.Add(dialog);
    }

    public void detachDialog(GameObject dialog)
    {
        m_dialogs.Remove(dialog);
    }

    public bool hasDialog()
    {
        return m_dialogs.Count > 0;
    }
}