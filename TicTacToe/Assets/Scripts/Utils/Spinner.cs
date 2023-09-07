using UnityEngine;

public class Spinner : MonoBehaviour
{
    private float _spinTime;
    public float SpinSpeed = .10f;

    private void Update()
    {
        _spinTime -= Time.deltaTime;
        while (_spinTime <= 0)
        {
            if (SpinSpeed <= 0) SpinSpeed = .10f;
            _spinTime += SpinSpeed;
            transform.Rotate(new Vector3(0, 45.0f, 0));
        }
    }
}
