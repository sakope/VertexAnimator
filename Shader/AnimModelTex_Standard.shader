﻿Shader "Custom/[UsingMatrixTex] AnimTex_Standard" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
        
        _AnimTex ("PosTex", 2D) = "white" {}
        _AnimTex_NormalTex ("Normal Tex", 2D) = "white" {}
        _AnimTex_Scale ("Scale", Vector) = (1,1,1,1)
        _AnimTex_Offset ("Offset", Vector) = (0,0,0,0)
        _ModelTex ("ModelTex", 2D) = "white"{}
		_ModelTex_MposScale ("ModelScale", float) = 1
		_ModelTex_MposOffset ("ModelOffset", float) = 0
		_ModelTex_MnormScale ("ModelNormScale", float) = 1
		_ModelTex_MnormOffset ("ModelNormOffset", float) = 0
        _AnimTex_AnimEnd ("End (Time, Frame)", Vector) = (0, 0, 0, 0)
        _AnimTex_T ("Time", float) = 0
        _AnimTex_FPS ("Frame per Sec(FPS)", Float) = 30
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard addshadow vertex:vert
		#pragma target 5.0
        #include "ModelTexture.cginc"

		sampler2D _MainTex;

        struct appdata {
            float4 vertex : POSITION;
            float4 tangent : TANGENT;
            float3 normal : NORMAL;
            float4 texcoord : TEXCOORD0;
            float4 texcoord1 : TEXCOORD1;
            float4 texcoord2 : TEXCOORD2;
            float4 texcoord3 : TEXCOORD3;
        #if defined(SHADER_API_XBOX360)
            half4 texcoord4 : TEXCOORD4;
            half4 texcoord5 : TEXCOORD5;
        #endif
            fixed4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            uint vid : SV_VertexID;
        };
		struct Input {
			float2 uv_MainTex;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

        void vert(inout appdata v) {
                float t = _AnimTex_T;
                t = clamp(t, 0, _AnimTex_AnimEnd.x);
                v.vertex.xyz = AnimTexVertexPos(v.vid, t);
                v.normal = normalize(AnimTexNormal(v.vid, t));

                //Some local pivot animation here.

                float4x4 ModelM = ModelTexVertexPos(t);
                float4x4 ModelN = ModelTexNormal(t);
                v.vertex = mul(ModelM, float4(v.vertex.xyz, 1));
                v.normal = normalize(mul((float3x3)ModelN, v.normal));
                v.normal = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
        }
		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
