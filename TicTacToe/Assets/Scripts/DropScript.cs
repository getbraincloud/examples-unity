using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropScript : MonoBehaviour
{
    private bool doDrop = false;
    private float dropTime = 0.0f;

    public void Drop()
    {
        doDrop = true;
        dropTime = 0.0f;
    }

    private void Update()
    {
        if (doDrop)
        {
            dropTime += Time.deltaTime;

            transform.position = new Vector3(
                transform.position.x,
                Mathf.Max(0, -10*(dropTime - 0.7f)*(dropTime - 1.3f), -8*Mathf.Pow(dropTime, 2) + 4) + 0.7f,
                transform.position.z
                );

            if(dropTime >= 1.3f)
            {
                doDrop = false;
                transform.position = new Vector3(transform.position.x, 0.7f, transform.position.z);
            }
        }
    }
}
