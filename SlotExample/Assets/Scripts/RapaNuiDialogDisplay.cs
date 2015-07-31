using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class RapaNuiDialogDisplay : MonoBehaviour {

    public GameObject m_Canvas;

	public void CreateBigWinDialog(string aMessage, float aLifeTime = 3)
    {
        GameObject instance = (GameObject)Instantiate(transform.GetChild(0).gameObject, transform.GetChild(0).position, Quaternion.identity);
        instance.transform.SetParent(m_Canvas.transform);
        instance.GetComponent<DestroyAfterSeconds>().enabled = true;
        instance.GetComponent<DestroyAfterSeconds>().m_lifeTime = aLifeTime;
        instance.transform.GetChild(1).GetComponent<Text>().text = aMessage;
    }

    public void CreateBonusDialog(string aMessage, float aLifeTime = 3)
    {
        GameObject instance = (GameObject)Instantiate(transform.GetChild(1).gameObject, transform.GetChild(1).position, Quaternion.identity);
        instance.transform.SetParent(m_Canvas.transform);
        instance.GetComponent<DestroyAfterSeconds>().enabled = true;
        instance.GetComponent<DestroyAfterSeconds>().m_lifeTime = aLifeTime;
        instance.transform.GetChild(1).GetComponent<Text>().text = aMessage;
    }
}
