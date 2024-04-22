using UnityEngine;
using UnityEngine.SceneManagement;

public class LogoutButton : MonoBehaviour
{
    public void Logout()
    {
        App.Bc.Logout(true);
        SceneManager.LoadScene("BrainCloudConnect");
    }
}
