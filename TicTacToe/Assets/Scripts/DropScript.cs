using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropScript : MonoBehaviour
{
    private bool doDrop = false;
    private float dropTime = 0.0f;

    public void Drop()
    {
        transform.position = new Vector3(transform.position.x, 4.7f, transform.position.z);
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
                CalculateHeight(dropTime),
                transform.position.z
                );

            if(dropTime >= 1.3f)
            {
                doDrop = false;
                transform.position = new Vector3(transform.position.x, 0.7f, transform.position.z);
            }
        }
    }

    private float CalculateHeight(float time)
    {
        float parabola1 = -8 * Mathf.Pow(time, 2) + 4;
        float parabola2 = -10 * (time - 0.7f) * (time - 1.3f);
        return Mathf.Max(0, parabola1, parabola2) + 0.7f;
        //Two parabolas to simulate gravity and a bounce
    }
}
