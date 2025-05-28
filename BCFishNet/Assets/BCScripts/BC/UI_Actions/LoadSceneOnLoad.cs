using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneOnLoad : MonoBehaviour
{
    public string SceneToLoad = "Main";
    // Start is called before the first frame update
    void Start()
    {
        Invoke("DelayedLoad", 0.05f);
    }

    // Update is called once per frame
    void DelayedLoad()
    {
        SceneManager.LoadScene(SceneToLoad);
    }
}
