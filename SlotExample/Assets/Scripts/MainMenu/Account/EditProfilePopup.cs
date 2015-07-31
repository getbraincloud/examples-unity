using UnityEngine;

public class EditProfilePopup : MonoBehaviour
{

    void Start()
    {

    }

    public void OnClickEditPlayer()
    {
        gameObject.SetActive(true);
    }

    public void OnClickCancel()
    {
        gameObject.SetActive(false);
    }

    public void OnClickSave()
    {
        gameObject.SetActive(false);
    }
}
