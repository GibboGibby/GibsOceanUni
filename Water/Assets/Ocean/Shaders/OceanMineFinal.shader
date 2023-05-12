Shader "Ocean/OceanMineFinal"
{
    Properties
    {
        
        
        
        [Header(Main)]
        _Color("Color", Color) = (1,1,1,1)
        _MaxGloss("Max Gloss", Range(0,1)) = 0
        _Roughness("Distant Roughness", Range(0,1)) = 0
        _RoughnessScale("Roughness Scale", Range(0, 0.01)) = 0.1
        _LOD_scale("LOD_scale", Range(1,10)) = 0
        [Header(Lighting)]
        _SSSColor("SSS Color", Color) = (1,1,1,1)
        _SSSStrength("SSSStrength", Range(0,1)) = 0.2
        _SSSScale("SSS Scale", Range(0.1,50)) = 4.0
        _SSSBase("SSS Base", Range(-5,1)) = 0
        [Header(Foam)]
        _FoamColor("Foam Color", Color) = (1,1,1,1)
        _FoamTexture("Foam Texture", 2D) = "grey" {}
        _FoamBiasLOD0("Foam Bias LOD0", Range(0,7)) = 1
        _FoamBiasLOD1("Foam Bias LOD1", Range(0,7)) = 1
        _FoamBiasLOD2("Foam Bias LOD2", Range(0,7)) = 1
        _FoamScale("Foam Scale", Range(0,20)) = 1
        _ContactFoam("Contact Foam", Range(0,1)) = 1
        
        
        [HideInInspector]_Displacement_c0("Displacement C0", 2D) = "black" {}
        [HideInInspector]_Derivatives_c0("Derivatives C0", 2D) = "black" {}
        [HideInInspector]_Turbulence_c0("Turbulence C0", 2D) = "white" {}
        
        [HideInInspector]_Displacement_c1("Displacement C1", 2D) = "black" {}
        [HideInInspector]_Derivatives_c1("Derivatives C1", 2D) = "black" {}
        [HideInInspector]_Turbulence_c1("Turbulence C1", 2D) = "white" {}
        
        [HideInInspector]_Displacement_c2("Displacement C2", 2D) = "black" {}
        [HideInInspector]_Derivatives_c2("Derivatives C2", 2D) = "black" {}
        [HideInInspector]_Turbulence_c2("Turbulence C2", 2D) = "white" {}
        
    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma multi_compile _ MID CLOSE
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 4.0

        

        struct Input
        {
            float2 worldUV;
            float4 lodScales;
            float3 viewVector;
            float3 worldNormal;
            float4 screenPos;
            INTERNAL_DATA
        };

        // Cascade Textures
        sampler2D _Displacement_c0;
        sampler2D _Derivatives_c0;
        sampler2D _Turbulence_c0;

        sampler2D _Displacement_c1;
        sampler2D _Derivatives_c1;
        sampler2D _Turbulence_c1;

        sampler2D _Displacement_c2;
        sampler2D _Derivatives_c2;
        sampler2D _Turbulence_c2;

        float LengthScale0;
        float LengthScale1;
        float LengthScale2;
        float _LOD_scale;
        float _SSSBase;
        float _SSSScale;

        fixed4 _Color, _FoamColor, _SSSColor;
        float _SSSStrength;
        float _Roughness, _RoughnessScale, _MaxGloss;
        float _FoamBiasLOD0, _FoamBiasLOD1, _FoamBiasLOD2, _FoamScale, _ContactFoam;
        sampler2D _CameraDepthTexture;
        sampler2D _FoamTexture;

        float3 GetVertexPos(inout Input o, float viewDist)
        {
            float4 worldUV = float4(o.worldUV.xy,0,0);
            float lod0 = min(_LOD_scale * LengthScale0 / viewDist, 1);
            float lod1 = min(_LOD_scale * LengthScale1 / viewDist, 1);
            float lod2 = min(_LOD_scale * LengthScale2 / viewDist, 1);

            float3 displacement = 0;
            float largeWavesBias = 0;

            displacement += tex2Dlod(_Displacement_c0, worldUV / LengthScale0) * lod0;
            largeWavesBias = displacement.y;
            #if defined(MID) || defined(CLOSE)
            displacement += tex2Dlod(_Displacement_c1, worldUV / LengthScale1) * lod1;
            #endif
            #if defined(CLOSE)
            displacement += tex2Dlod(_Displacement_c2, worldUV / LengthScale2) * lod2;
            #endif

            o.lodScales = float4(lod0, lod1, lod2, max(displacement.y - largeWavesBias * 0.8 - _SSSBase, 0) / _SSSScale);

            return mul(unity_WorldToObject,displacement);
        }

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
            o.worldUV = worldPos.xz;

            o.viewVector = _WorldSpaceCameraPos.xyz - mul(unity_ObjectToWorld, v.vertex).xyz;
            float viewDist = length(o.viewVector);
            
            
            v.vertex.xyz += GetVertexPos(o, viewDist);

            

            
            
        }
        

        float3 WorldToTangentNormalVector(Input IN, float3 normal) {
            float3 t2w0 = WorldNormalVector(IN, float3(1, 0, 0));
            float3 t2w1 = WorldNormalVector(IN, float3(0, 1, 0));
            float3 t2w2 = WorldNormalVector(IN, float3(0, 0, 1));
            float3x3 t2w = float3x3(t2w0, t2w1, t2w2);
            return normalize(mul(t2w, normal));
        }

        float pow5(float f)
        {
            return f * f * f * f * f;
        }

        float power(float num, float power)
        {
           float val = num;
            for (int i = 0; i < power; i++)
            {
                val *= num;
            }
            return val;            
        }

        float4 CalculateDerivatives(Input IN)
        {
            float4 derivatives = tex2D(_Derivatives_c0, IN.worldUV / LengthScale0);
            #if defined(MID) || defined(CLOSE)
            derivatives += tex2D(_Derivatives_c1, IN.worldUV / LengthScale1) * IN.lodScales.y;
            #endif
            #if defined(CLOSE)
            derivatives += tex2D(_Derivatives_c2, IN.worldUV / LengthScale2) * IN.lodScales.z;
            #endif
            return derivatives;
        }

        float CalculateFoam(Input IN)
        {
            #if defined(CLOSE)
            float jacobian = tex2D(_Turbulence_c0, IN.worldUV / LengthScale0).x
                + tex2D(_Turbulence_c1, IN.worldUV / LengthScale1).x
                + tex2D(_Turbulence_c2, IN.worldUV / LengthScale2).x;
            jacobian = min(1, max(0, (-jacobian + _FoamBiasLOD2) * _FoamScale));
            #elif defined(MID)
            float jacobian = tex2D(_Turbulence_c0, IN.worldUV / LengthScale0).x
                + tex2D(_Turbulence_c1, IN.worldUV / LengthScale1).x;
            jacobian = min(1, max(0, (-jacobian + _FoamBiasLOD1) * _FoamScale));
            #else
            float jacobian = tex2D(_Turbulence_c0, IN.worldUV / LengthScale0).x;
            jacobian = min(1, max(0, (-jacobian + _FoamBiasLOD0) * _FoamScale));
            #endif

            float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
            float backgroundDepth =
                LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV));
            float surfaceDepth = UNITY_Z_0_FAR_FROM_CLIPSPACE(IN.screenPos.z);
            float depthDifference = max(0, backgroundDepth - surfaceDepth - 0.1);
            float foam = tex2D(_FoamTexture, IN.worldUV * 0.5 + _Time.r).r;
            jacobian += _ContactFoam * saturate(max(0, foam - depthDifference) * 5) * 0.9;
            
            return jacobian;
        }

        void CalculateNormalsFromDerivatives(Input IN, inout SurfaceOutputStandard o, float4 derivatives, out float3 worldNormal)
        {
            float2 slope = float2(derivatives.x / (1 + derivatives.z),
                derivatives.y / (1 + derivatives.w));
            worldNormal = normalize(float3(-slope.x, 1, -slope.y));
            o.Normal = WorldToTangentNormalVector(IN, worldNormal);
        }

        void CalculateEmission(inout SurfaceOutputStandard o, Input IN, float3 worldNormal, float jacobian)
        {
            float3 viewDir = normalize(IN.viewVector);
            float3 H = normalize(-worldNormal + _WorldSpaceLightPos0);
            float ViewDotH = power(saturate(dot(viewDir, -H)), 5) * 30 * _SSSStrength;
            fixed3 color = lerp(_Color, saturate(_Color + _SSSColor.rgb * ViewDotH * IN.lodScales.w), IN.lodScales.z);

            float fresnel = dot(worldNormal, viewDir);
            fresnel = saturate(1 - fresnel);
            fresnel = power(fresnel, 5);

            o.Emission = lerp(color * (1 - fresnel), 0, jacobian);
        }

        void AddMaterialProperties(inout SurfaceOutputStandard o, Input IN, float jacobian)
        {
            o.Albedo = lerp(0, _FoamColor, jacobian);
            float distanceGloss = lerp(1 - _Roughness, _MaxGloss, 1 / (1 + length(IN.viewVector) * _RoughnessScale));
            o.Smoothness = lerp(distanceGloss, 0, jacobian);
            o.Metallic = 0;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float4 derivatives = CalculateDerivatives(IN);
            float3 worldNormal;
            CalculateNormalsFromDerivatives(IN, o, derivatives, worldNormal);
            
            float jacobian = CalculateFoam(IN);
            
            
            AddMaterialProperties(o, IN, jacobian);
            

            CalculateEmission(o, IN, worldNormal, jacobian);
        }
        
        ENDCG
    }
    FallBack "Diffuse"
}
