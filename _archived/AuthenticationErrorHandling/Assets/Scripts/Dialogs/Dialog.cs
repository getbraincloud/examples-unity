using UnityEngine;

public class Dialog : MonoBehaviour
{
    protected string m_response = "";

    void Start()
    {
        ErrorHandlingApp.getInstance().attachDialog(gameObject);
    }


    protected void Detach()
    {
        ErrorHandlingApp.getInstance().detachDialog(gameObject);
    }

    protected void DisplayResponse()
    {
        if (m_response.Length > 0)
        {
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();

            GUILayout.Label("Response: ");
            GUILayout.TextArea(m_response);

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
        }
    }

    protected void CloseButton()
    {
        if (Util.Button("Close"))
        {
            Destroy(gameObject);
        }
    }
}