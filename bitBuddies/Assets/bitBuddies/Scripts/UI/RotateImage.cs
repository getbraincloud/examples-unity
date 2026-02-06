using UnityEngine;

public class RotateImage : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 180f; 
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void FixedUpdate()
    {
        if (gameObject.activeInHierarchy)
        {
            rectTransform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }
    }
}
