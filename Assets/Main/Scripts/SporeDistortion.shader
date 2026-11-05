Shader "Custom/SporeDistortion"
{
    Properties
    {
        _Intensity ("Intensity (set by script)", Range(0, 1)) = 0
        _Tint ("Tint Color", Color) = (0.55, 0.9, 0.25, 1)
        _TintStrength ("Tint Strength", Range(0, 1)) = 0.5
        _Obscure ("Obscure Amount", Range(0, 1)) = 0.55
        _WaveStrength ("Wave Strength", Range(0, 0.1)) = 0.02
        _WaveFrequency ("Wave Frequency", Float) = 18
        _WaveSpeed ("Wave Speed", Float) = 2.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off
        Cull Off
        ZTest Always

        Pass
        {
            Name "SporeDistortion"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Intensity;
                half4 _Tint;
                float _TintStrength;
                float _Obscure;
                float _WaveStrength;
                float _WaveFrequency;
                float _WaveSpeed;
            CBUFFER_END

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                // 0 in the middle of the screen, ~1 in the corners
                float edge = saturate(length(uv - 0.5) * 1.6);

                // wobbling offset: two sine waves moving over time
                float t = _Time.y * _WaveSpeed;
                float2 wave;
                wave.x = sin(uv.y * _WaveFrequency + t);
                wave.y = cos(uv.x * _WaveFrequency * 0.8 + t * 1.3);

                float strength = _WaveStrength * _Intensity * (0.6 + 0.4 * edge);
                uv += wave * strength;

                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                // color tint
                half3 tinted = lerp(col.rgb, col.rgb * _Tint.rgb, _TintStrength * _Intensity);

                // haze that covers the screen, thicker toward the edges
                float haze = _Obscure * _Intensity * (0.35 + 0.65 * edge);
                half3 result = lerp(tinted, _Tint.rgb * 0.5, haze);

                return half4(result, col.a);
            }
            ENDHLSL
        }
    }
}
