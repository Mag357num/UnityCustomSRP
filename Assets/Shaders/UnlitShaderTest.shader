Shader "UnlitShaderTest" {
	
	Properties {
		_Color ("Color", Color) = (1, 1, 1, 1)
	}
	
	SubShader {
		
		Pass
		{
			HLSLPROGRAM
			
			#pragma target 3.5
			
			#pragma multi_compile_instancing // 在自定义材质界面添加Enable GPU Instancing选项
			#pragma instancing_options assumeuniformscaling
			
			#pragma vertex UnlitPassVertex
			#pragma fragment UnlitPassFragment
			
			#include "Assets/ShaderLibrary/Unlit.hlsl"
			
			ENDHLSL
		}
	}
}