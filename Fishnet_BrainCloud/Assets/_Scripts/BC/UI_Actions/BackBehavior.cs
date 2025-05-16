using UnityEngine;
using UnityEngine.SceneManagement;

public class BackBehavior : MonoBehaviour
{
    public void OnMainMenu()
    {
        SceneManager.LoadScene("Main");
    }
}
