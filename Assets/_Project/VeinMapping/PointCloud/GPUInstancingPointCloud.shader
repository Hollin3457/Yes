Shader "Custom/GPUInstancingPointCloud" {

	Properties {
		_Smoothness ("Smoothness", Range(0,1)) = 0.5
		_Color ("Main Color", Color) = (1, 1, 1, 1)
	}
	
	SubShader {
		Tags {"Queue" = "Transparent" "RenderType"="Transparent" }
		CGPROGRAM
		#pragma surface ConfigureSurface Standard alpha:fade
		#pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
		#pragma editor_sync_compilation
		#pragma target 4.5

#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		StructuredBuffer<float4> _Positions;
		StructuredBuffer<float4x4> _Matrices;
#endif

		float _Alpha;

		void ConfigureProcedural() {
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
			_Alpha = _Positions[unity_InstanceID].w;
			unity_ObjectToWorld = _Matrices[unity_InstanceID];
#endif
		}
		
		struct Input {
			float3 worldPos;
		};

		float4 _Color;
		float _Smoothness;
		
		void ConfigureSurface(Input input, inout SurfaceOutputStandard surface) {
			surface.Albedo = _Color.rgb;
			surface.Smoothness = _Smoothness;
			surface.Alpha = _Alpha;
		}
		ENDCG
	}
						
	FallBack "Diffuse"
}
