using System.Collections;
using UnityEngine;

public class PromoPopup : MonoBehaviour
{
    public GameObject PopupObject;

    public void OnClickPromoButton()
    {
        PopupObject.SetActive(true);
    }

    public void OnClickPurchase()
    {
        PopupObject.SetActive(false);
    }

    public void OnClickNoThanks()
    {
        PopupObject.SetActive(false);
    }
}
