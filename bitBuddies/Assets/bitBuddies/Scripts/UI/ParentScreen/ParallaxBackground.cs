using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    public Transform[] layers; // Array of background layers
    public float[] parallaxScales; // Speed of each layer
    public Transform scrollViewContent; // Reference to the ScrollView's content

    private void Update()
    {
        for (int i = 0; i < layers.Length; i++)
        {
            float parallax = (scrollViewContent.position.x * parallaxScales[i]);
            layers[i].position = new Vector3(parallax, layers[i].position.y, layers[i].position.z);
        }
    }
}
