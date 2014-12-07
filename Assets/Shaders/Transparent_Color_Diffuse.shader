Shader "Custom/Transparent Color Diffuse" {
	Properties {
		_Color ("Color", Color) = (1, 1, 1, 1)
	}
	SubShader {
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" }
		Blend One OneMinusSrcAlpha
		CGPROGRAM
		#pragma surface surf Lambert

		struct Input {
			fixed _;
		};

		fixed4 _Color;

		void surf (Input IN, inout SurfaceOutput o) {
			o.Albedo = _Color.rgb * _Color.a;
			o.Alpha = _Color.a;
		}
		ENDCG
	}
}
