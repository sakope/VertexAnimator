#ifndef MODEL_TEXTURE_INCLUDE
#define MODEL_TEXTURE_INCLUDE

#include "AnimTexture.cginc"

sampler2D _ModelTex;
float _ModelTex_MposScale;
float _ModelTex_MposOffset;
float _ModelTex_MnormScale;
float _ModelTex_MnormOffset;

half4 _ModelTex_TexelSize;

float4x4 ModelTexVertexPos_Bilinear(float t) {
    float frame = min(t * _AnimTex_FPS, _AnimTex_AnimEnd.y);
    float frame1 = frame;
    float4x4 model1, model2;
    float2 uvc1, uvc2, uvc3, uvc4;
    uvc1.xy = (0.5 + float2(0, frame1)) * _ModelTex_TexelSize;
    uvc2.xy = (0.5 + float2(1, frame1)) * _ModelTex_TexelSize;
    uvc3.xy = (0.5 + float2(2, frame1)) * _ModelTex_TexelSize;
    uvc4.xy = (0.5 + float2(3, frame1)) * _ModelTex_TexelSize;
    model1._11_21_31_41 = tex2Dlod(_ModelTex, float4(uvc1, 0, 0)).rgba;
    model1._12_22_32_42 = tex2Dlod(_ModelTex, float4(uvc2, 0, 0)).rgba;
    model1._13_23_33_43 = tex2Dlod(_ModelTex, float4(uvc3, 0, 0)).rgba;
    model1._14_24_34_44 = tex2Dlod(_ModelTex, float4(uvc4, 0, 0)).rgba;
    uvc1.y += 0.5;
    uvc2.y += 0.5;
    uvc3.y += 0.5;
    uvc4.y += 0.5;
    model2._11_21_31_41 = tex2Dlod(_ModelTex, float4(uvc1, 0, 0)).rgba;
    model2._12_22_32_42 = tex2Dlod(_ModelTex, float4(uvc2, 0, 0)).rgba;
    model2._13_23_33_43 = tex2Dlod(_ModelTex, float4(uvc3, 0, 0)).rgba;
    model2._14_24_34_44 = tex2Dlod(_ModelTex, float4(uvc4, 0, 0)).rgba;
    return (model1 + model2 * COLOR_DEPTH_INV) * _ModelTex_MposScale + _ModelTex_MposOffset;
}

float4x4 ModelTexVertexPos_Point(float t) {
    float frame = min(t * _AnimTex_FPS, _AnimTex_AnimEnd.y);
    float frame1 = floor(frame);
    float frame2 = min(frame1 + 1, _AnimTex_AnimEnd.y);
    float tFilter = frame - frame1;

    float4x4 model1, model2;
    float2 uvc1, uvc2, uvc3, uvc4;
    uvc1.xy = (0.5 + float2(0, frame1)) * _ModelTex_TexelSize;
    uvc2.xy = (0.5 + float2(1, frame1)) * _ModelTex_TexelSize;
    uvc3.xy = (0.5 + float2(2, frame1)) * _ModelTex_TexelSize;
    uvc4.xy = (0.5 + float2(3, frame1)) * _ModelTex_TexelSize;
    model1._11_21_31_41 = tex2Dlod(_ModelTex, float4(uvc1, 0, 0)).rgba;
    model1._12_22_32_42 = tex2Dlod(_ModelTex, float4(uvc2, 0, 0)).rgba;
    model1._13_23_33_43 = tex2Dlod(_ModelTex, float4(uvc3, 0, 0)).rgba;
    model1._14_24_34_44 = tex2Dlod(_ModelTex, float4(uvc4, 0, 0)).rgba;
    uvc1.y += 0.5;
    uvc2.y += 0.5;
    uvc3.y += 0.5;
    uvc4.y += 0.5;
    model2._11_21_31_41 = tex2Dlod(_ModelTex, float4(uvc1, 0, 0)).rgba;
    model2._12_22_32_42 = tex2Dlod(_ModelTex, float4(uvc2, 0, 0)).rgba;
    model2._13_23_33_43 = tex2Dlod(_ModelTex, float4(uvc3, 0, 0)).rgba;
    model2._14_24_34_44 = tex2Dlod(_ModelTex, float4(uvc4, 0, 0)).rgba;
    float4x4 model = (model1 + model2 * COLOR_DEPTH_INV) * _ModelTex_MposScale + _ModelTex_MposOffset;

    uvc1.xy = (0.5 + float2(0, frame1)) * _ModelTex_TexelSize;
    uvc2.xy = (0.5 + float2(1, frame1)) * _ModelTex_TexelSize;
    uvc3.xy = (0.5 + float2(2, frame1)) * _ModelTex_TexelSize;
    uvc4.xy = (0.5 + float2(3, frame1)) * _ModelTex_TexelSize;
    model1._11_21_31_41 = tex2Dlod(_ModelTex, float4(uvc1, 0, 0)).rgba;
    model1._12_22_32_42 = tex2Dlod(_ModelTex, float4(uvc2, 0, 0)).rgba;
    model1._13_23_33_43 = tex2Dlod(_ModelTex, float4(uvc3, 0, 0)).rgba;
    model1._14_24_34_44 = tex2Dlod(_ModelTex, float4(uvc4, 0, 0)).rgba;
    uvc1.y += 0.5;
    uvc2.y += 0.5;
    uvc3.y += 0.5;
    uvc4.y += 0.5;
    model2._11_21_31_41 = tex2Dlod(_ModelTex, float4(uvc1, 0, 0)).rgba;
    model2._12_22_32_42 = tex2Dlod(_ModelTex, float4(uvc2, 0, 0)).rgba;
    model2._13_23_33_43 = tex2Dlod(_ModelTex, float4(uvc3, 0, 0)).rgba;
    model2._14_24_34_44 = tex2Dlod(_ModelTex, float4(uvc4, 0, 0)).rgba;
    model2 = (model1 + model2 / COLOR_DEPTH) * _ModelTex_MposScale + _ModelTex_MposOffset;
    
    return model + (model2 - model) * tFilter; 
}			

float4x4 ModelTexVertexPos(float t) {
	#ifdef BILINEAR_ON
    return ModelTexVertexPos_Point(t);
	#else
    return ModelTexVertexPos_Bilinear(t);
	#endif
}

float4x4 ModelTexNormal(float t) {
    float frame = min(t * _AnimTex_FPS, _AnimTex_AnimEnd.y);
    float frame1 = frame;
    float4x4 model1, model2;
    float2 uvc1, uvc2, uvc3, uvc4;
    uvc1.xy = (0.5 + float2(4, frame1)) * _ModelTex_TexelSize;
    uvc2.xy = (0.5 + float2(5, frame1)) * _ModelTex_TexelSize;
    uvc3.xy = (0.5 + float2(6, frame1)) * _ModelTex_TexelSize;
    uvc4.xy = (0.5 + float2(7, frame1)) * _ModelTex_TexelSize;
    model1._11_21_31_41 = tex2Dlod(_ModelTex, float4(uvc1, 0, 0)).rgba;
    model1._12_22_32_42 = tex2Dlod(_ModelTex, float4(uvc2, 0, 0)).rgba;
    model1._13_23_33_43 = tex2Dlod(_ModelTex, float4(uvc3, 0, 0)).rgba;
    model1._14_24_34_44 = tex2Dlod(_ModelTex, float4(uvc4, 0, 0)).rgba;
    uvc1.y += 0.5;
    uvc2.y += 0.5;
    uvc3.y += 0.5;
    uvc4.y += 0.5;
    model2._11_21_31_41 = tex2Dlod(_ModelTex, float4(uvc1, 0, 0)).rgba;
    model2._12_22_32_42 = tex2Dlod(_ModelTex, float4(uvc2, 0, 0)).rgba;
    model2._13_23_33_43 = tex2Dlod(_ModelTex, float4(uvc3, 0, 0)).rgba;
    model2._14_24_34_44 = tex2Dlod(_ModelTex, float4(uvc4, 0, 0)).rgba;
    return (model1 + model2 * COLOR_DEPTH_INV) * _ModelTex_MnormScale + _ModelTex_MnormOffset;
}
#endif
