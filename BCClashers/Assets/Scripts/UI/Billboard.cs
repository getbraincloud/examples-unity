using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform _cam;
    // Start is called before the first frame update
    private void Start()
    {
        _cam = FindObjectOfType<Camera>().transform;
        if (!_cam)
        {
            enabled = false;
        }
    }

    // Update is called once per frame
    private void LateUpdate()
    {
        transform.LookAt(transform.position + _cam.forward);
    }
}
