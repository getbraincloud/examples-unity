using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollUV : MonoBehaviour
{
	public string textureTarget;
	new public Renderer renderer;
	public Vector2 scroll = Vector2.up;
	public float quantization = 0;

	private void Reset()
	{
		renderer = GetComponent<Renderer>();
		textureTarget = "_MainTex";
	}

    private void Update()
	{
		renderer.material.SetTextureOffset(textureTarget, scroll * (quantization == 0 ? Time.time : quantization * (int)(Time.time/quantization)));
	}
}
