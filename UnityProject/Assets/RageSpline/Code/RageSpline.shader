Shader "RageSpline/Basic" {
	Properties {

	}

	Category {
		Tags {"RenderType"="Transparent" "Queue"="Transparent"}
		Lighting Off
		BindChannels {
			Bind "Color", color
			Bind "Vertex", vertex
		}
		
		SubShader {
			Pass {
				ZWrite Off
				Cull Off
				Blend SrcAlpha OneMinusSrcAlpha
			}
		}
	}
}