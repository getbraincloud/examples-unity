Shader "Custom/NoRender"
{
    Properties
    {
		_Color("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags {"Queue" = "Geometry-1" }

		Lighting Off

		Pass

		{
			ZWrite Off
			ColorMask 0
		}
    }
}