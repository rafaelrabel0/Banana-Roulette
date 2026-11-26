// Visual retro do Banana Roulette.
//  - tremor: depois de projetado na tela, cada vertice pula para o ponto mais proximo de uma grade (_RetroGrade, global);
//  - face chapada: a normal vem da propria face, nao do vertice;
//  - luz e sombra da Unity (luz principal, sombra e luz ambiente), que o Rafael aprovou.
Shader "BananaRoulette/Retro"
{
    Properties
    {
        _BaseMap ("Textura", 2D) = "white" {}
        _BaseColor ("Cor", Color) = (1, 1, 1, 1)
        [Toggle] _SemLuz ("Sem luz (contorno e tinta)", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Lado desenhado", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            float _SemLuz;
            float _Cull;
        CBUFFER_END

        float4 _RetroGrade; // xy = metade da largura e da altura da imagem, em pixels. x = 0 desliga o tremor

        float4 Prende(float4 pc)
        {
            if (_RetroGrade.x > 0.5)
            {
                float3 n = pc.xyz / pc.w;
                n.xy = floor(n.xy * _RetroGrade.xy + 0.5) / _RetroGrade.xy;
                pc.xyz = n * pc.w;
            }
            return pc;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Entrada
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Saida
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float fog : TEXCOORD2;
            };

            Saida vert(Entrada i)
            {
                Saida o;
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = Prende(TransformWorldToHClip(o.positionWS));
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Saida i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                if (_SemLuz < 0.5)
                {
                    // normal da face; virada para a camera, para nao depender da plataforma
                    float3 n = normalize(cross(ddy(i.positionWS), ddx(i.positionWS)));
                    float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                    if (dot(n, v) < 0.0) n = -n;
                    Light luz = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                    half nl = saturate(dot(n, luz.direction));
                    half3 total = luz.color * (nl * luz.shadowAttenuation) + SampleSH(n);
                    c.rgb *= total;
                }
                c.rgb = MixFog(c.rgb, i.fog);
                return half4(c.rgb, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Entrada
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            float4 vert(Entrada i) : SV_POSITION
            {
                float3 pw = TransformObjectToWorld(i.positionOS.xyz);
                float3 nw = TransformObjectToWorldNormal(i.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 dir = normalize(_LightPosition - pw);
                #else
                    float3 dir = _LightDirection;
                #endif
                float4 pc = TransformWorldToHClip(ApplyShadowBias(pw, nw, dir));
                #if UNITY_REVERSED_Z
                    pc.z = min(pc.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    pc.z = max(pc.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return pc;
            }

            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 vert(float4 positionOS : POSITION) : SV_POSITION
            {
                return Prende(TransformObjectToHClip(positionOS.xyz));
            }

            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
