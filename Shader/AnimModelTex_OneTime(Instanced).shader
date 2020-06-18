Shader "VertexAnim/[UsingMatrixTex] OneTime(Instanced)" { 
	Properties {
		_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
		
		_AnimTex ("PosTex", 2D) = "white" {}
		_AnimTex_Scale ("Scale", Vector) = (1,1,1,1)
		_AnimTex_Offset ("Offset", Vector) = (0,0,0,0)
        _ModelTex ("ModelTex", 2D) = "white"{}
		_ModelTex_MposScale ("ModelScale", float) = 1
		_ModelTex_MposOffset ("ModelOffset", float) = 0
		_AnimTex_AnimEnd ("End (Time, Frame)", Vector) = (0, 0, 0, 0)
		//_AnimTex_T ("Time", float) = 0
		_AnimTex_FPS ("Frame per Sec(FPS)", Float) = 30
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		Cull Off
		
		Pass {
			CGPROGRAM
            #pragma multi_compile_instancing
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "ModelTexture.cginc"

            struct vsin {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                uint vid: SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct vs2ps {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            sampler2D _MainTex;
            float4 _Color;
            
            vs2ps vert(vsin v) {
                vs2ps OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, OUT);

                float t = UNITY_ACCESS_INSTANCED_PROP(_AnimTex_T_arr, _AnimTex_T);
                t = clamp(t, 0, _AnimTex_AnimEnd.x);
                v.vertex.xyz = AnimTexVertexPos(v.vid, t);

                //Some local pivot animation here.

                float4x4 ModelM = ModelTexVertexPos(t);
                v.vertex = mul(ModelM, float4(v.vertex.xyz, 1));

                OUT.vertex = UnityObjectToClipPos(float4(v.vertex.xyz, 1));
                //OUT.vertex = mul(UNITY_MATRIX_VP, float4(v.vertex.xyz, 1));
                OUT.uv = v.texcoord;
                return OUT;
            }

            float4 frag(vs2ps IN) : COLOR {
                return tex2D(_MainTex, IN.uv) * _Color;
            }
			ENDCG
		}
        Pass {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            CGPROGRAM
            #pragma multi_compile_shadowcaster
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "ModelTexture.cginc"

            struct vsin {
                uint vid: SV_VertexID;
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord : TEXCOORD0;
            };

            struct vs2ps {
                V2F_SHADOW_CASTER;
            };
            
            sampler2D _MainTex;
            float4 _Color;
            
            vs2ps vert(vsin v) {
                float t = _AnimTex_T;
                t = clamp(t, 0, _AnimTex_AnimEnd.x);
                v.vertex.xyz = AnimTexVertexPos(v.vid, t);
                
                //Some local pivot animation here.

                float4x4 ModelM = ModelTexVertexPos(t);
                v.vertex = mul(ModelM, float4(v.vertex.xyz, 1));

                vs2ps OUT;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(OUT);
                return OUT;
            }

            float4 frag(vs2ps IN) : COLOR {
                SHADOW_CASTER_FRAGMENT(IN);
            }
            ENDCG
        }
	}
	FallBack Off

}
