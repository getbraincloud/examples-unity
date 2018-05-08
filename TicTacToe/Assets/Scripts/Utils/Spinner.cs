using UnityEngine;

public class Spinner : MonoBehaviour
{
    public float spinSpeed = .10f;
    private float spinTime;

    private void Update()
    {
        spinTime -= Time.deltaTime;
        while (spinTime <= 0)
        {
            if (spinSpeed <= 0) spinSpeed = .10f;
            spinTime += spinSpeed;
            transform.Rotate(new Vector3(0, 45.0f, 0));
        }
    }
}