Shader "Custom/Shadercontinent"
{
    Properties
    {
        // ── Color principal ──
        _BaseColor ("Color base", Color) = (1, 1, 1, 1)
        _MainTex ("Textura principal", 2D) = "white" {}

        // ── Iluminación toon ──
        [NoScaleOffset] _RampTex ("Rampa de sombreado", 2D) = "white" {}
        _ShadowSmoothness ("Suavizado de sombra", Range(0, 0.5)) = 0.1
        _ShadowStrength ("Intensidad de sombra", Range(0, 1)) = 0.7

        // ── Specular toon ──
        _SpecularColor ("Color specular", Color) = (1, 1, 1, 1)
        _SpecularSize ("Tamaño specular", Range(0, 1)) = 0.1
        _SpecularSmoothness ("Suavizado specular", Range(0, 0.2)) = 0.05

        // ── Rim light ──
        [HDR] _RimColor ("Color rim", Color) = (1, 1, 1, 1)
        _RimPower ("Potencia rim", Range(0, 10)) = 3.0

        // ── Outline ──
        _OutlineColor ("Color del borde", Color) = (0, 0, 0, 1)
        _OutlineWidth ("Grosor del borde", Range(0, 0.1)) = 0.01

        // ── Brillo ambiental ──
        _AmbientBoost ("Refuerzo ambiental", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
        }
        LOD 200

        // ────────────────────────
        //  PASS 0: OUTLINE (hull invertido)
        // ────────────────────────
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front   // Renderiza las caras traseras expandidas

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
            CBUFFER_END

            Varyings OutlineVert(Attributes input)
            {
                Varyings output;

                // Expandir vértices a lo largo de la normal
                float3 normalCS = TransformObjectToWorldNormal(input.normalOS);
                float3 posWS    = TransformObjectToWorld(input.positionOS.xyz);

                // Escalar por distancia de cámara para grosor consistente
                float3 viewDir = GetWorldSpaceViewDir(posWS);
                float distance  = length(viewDir);
                float outlineScale = distance * _OutlineWidth;

                posWS += normalCS * outlineScale;

                output.positionCS = TransformWorldToHClip(posWS);
                return output;
            }

            half4 OutlineFrag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // ────────────────────────
        //  PASS 1: ILUMINACIÓN TOON
        // ────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex ToonVert
            #pragma fragment ToonFrag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float4 shadowCoord: TEXCOORD3;
                float3 viewDirWS  : TEXCOORD4;
            };

            TEXTURE2D(_MainTex);       SAMPLER(sampler_MainTex);
            TEXTURE2D(_RampTex);       SAMPLER(sampler_RampTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MainTex_ST;
                float  _ShadowSmoothness;
                float  _ShadowStrength;
                float4 _SpecularColor;
                float  _SpecularSize;
                float  _SpecularSmoothness;
                float4 _RimColor;
                float  _RimPower;
                float  _AmbientBoost;
            CBUFFER_END

            Varyings ToonVert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs posInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInput.positionCS;
                output.positionWS = posInput.positionWS;

                VertexNormalInputs normInput = GetVertexNormalInputs(input.normalOS);
                output.normalWS  = normInput.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(posInput.positionWS);

                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                // Coordenadas de sombra para la luz principal
                output.shadowCoord = TransformWorldToShadowCoord(posInput.positionWS);

                return output;
            }

            half4 ToonFrag(Varyings input) : SV_Target
            {
                // ── Textura y color base ──
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half3 albedo   = texColor.rgb * _BaseColor.rgb;

                // ── Datos de iluminación ──
                Light mainLight = GetMainLight(input.shadowCoord);
                float3 lightDir = normalize(mainLight.direction);
                float3 normalWS = normalize(input.normalWS);
                float3 viewDir  = normalize(input.viewDirWS);

                float NdotL = dot(normalWS, lightDir);

                // ── Atenuación de sombra ──
                float shadowAtten = mainLight.shadowAttenuation;

                // ── Sombreado toon con rampa ──
                // Mapear NdotL de [-1, 1] a [0, 1] y aplicar smoothstep
                float diffuse01 = NdotL * 0.5 + 0.5;
                float toonDiffuse = smoothstep(0.5 - _ShadowSmoothness,
                                               0.5 + _ShadowSmoothness,
                                               diffuse01);

                // Rampa 1D: oscuro (izq) → claro (der)
                float rampUV = lerp(0.2, 1.0, toonDiffuse);
                half3 rampColor = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(rampUV, 0.5)).rgb;

                // Mezclar entre sombra y luz
                half3 shadowColor = rampColor * _ShadowStrength;
                half3 litColor    = albedo;

                // Aplicar atenuación de sombras
                float finalDiffuse = toonDiffuse * shadowAtten;
                half3 diffuseLight = lerp(shadowColor, litColor, finalDiffuse);

                // ── Specular toon (blinn-phong con step) ──
                float3 halfVec = normalize(lightDir + viewDir);
                float NdotH = dot(normalWS, halfVec);
                float specular = smoothstep(1.0 - _SpecularSize - _SpecularSmoothness,
                                            1.0 - _SpecularSize + _SpecularSmoothness,
                                            NdotH);
                half3 specularLight = specular * _SpecularColor.rgb * mainLight.color;

                // ── Rim light ──
                float NdotV = saturate(dot(normalWS, viewDir));
                float rim = 1.0 - NdotV;
                rim = pow(rim, _RimPower);
                half3 rimLight = rim * _RimColor.rgb;

                // ── Luz ambiental mejorada ──
                half3 ambient = SampleSH(normalWS) * _AmbientBoost;

                // ── Composición final ──
                half3 finalColor = diffuseLight * mainLight.color
                                 + specularLight
                                 + rimLight
                                 + ambient;

                // Luces adicionales (toonizado)
                #ifdef _ADDITIONAL_LIGHTS
                    uint pixelLightCount = GetAdditionalLightsCount();
                    for (uint i = 0; i < pixelLightCount; i++)
                    {
                        Light addLight = GetAdditionalLight(i, input.positionWS);
                        float addNdotL = dot(normalWS, normalize(addLight.direction));
                        float addDiffuse = smoothstep(0.4, 0.6, addNdotL * 0.5 + 0.5);
                        finalColor += albedo * addLight.color * addDiffuse * addLight.distanceAttenuation;
                    }
                #endif

                // Fog URP
                #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                    float fogFactor = ComputeFogFactor(input.positionCS.z);
                    finalColor = MixFog(finalColor, fogFactor);
                #endif

                return half4(finalColor, _BaseColor.a * texColor.a);
            }
            ENDHLSL
        }

        // ────────────────────────
        //  PASS 2: SHADOW CASTER
        // ────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;
            float3 _LightPosition;

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(input.normalOS);

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
