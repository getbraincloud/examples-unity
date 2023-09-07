using UnityEngine;
using System.Collections;

namespace BrainCloudUNETExample
{
    public class ObjectRotation : MonoBehaviour
    {

        public float rotationSpeed = 1.0f; //controls the speed of the rotation
        public Vector3 rotationVector = Vector3.zero; //which axes to rotate around

        // Update is called once per frame
        void Update()
        {
            if (GetComponent<Renderer>() != null)
            {
                if (GetComponent<Renderer>().isVisible)
                {
                    UpdatePosition();
                }
            }
            else UpdatePosition();
        }

        void UpdatePosition()
        {
            gameObject.transform.Rotate(rotationVector * rotationSpeed * Time.deltaTime);
        }
    }
}
