Shader "Ocean/OceanMine"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        
        _LOD_scale("LOD_scale", Range(1,10)) = 0
        
        _FoamScale("Foam Scale", Range(0,20)) = 1
        
        _MaxGloss("Max Gloss", Range(0,1)) = 0
        _Roughness("Distant Roughness", Range(0,1)) = 0
        _RoughnessScale("Roughness Scale", Range(0, 0.01)) = 0.1
        
        _SSSColor("SSS Color", Color) = (1,1,1,1)
        _SSSStrength("SSSStrength", Range(0,1)) = 0.2
        _SSSScale("SSS Scale", Range(0.1,50)) = 4.0
        _SSSBase("SSS Base", Range(-5,1)) = 0
        
        _FoamColor("Foam Color", Color) = (1,1,1,1)
        
        _FoamTexture("Foam Texture", 2D) = "grey" {}
        
        _FoamBiasLOD0("Foam Bias LOD0", Range(0,7)) = 1
        _FoamBiasLOD1("Foam Bias LOD1", Range(0,7)) = 1
        _FoamBiasLOD2("Foam Bias LOD2", Range(0,7)) = 1
        
        _ContactFoam("Contact Foam", Range(0,1)) = 1
        
        _Displacement_c0("Displacement C0", 2D) = "black" {}
        _Derivatives_c0("Derivatives C0", 2D) = "black" {}
        _Turbulence_c0("Turbulence C0", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
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

        sampler2D _Displacement_c0;
        sampler2D _Derivatives_c0;
        sampler2D _Turbulence_c0;

        float LengthScale0;
        float LengthScale1;
        float LengthScale2;

        sampler2D _CameraDepthTexture;
        sampler2D _FoamTexture;

        fixed4 _FoamColor;
        fixed4 _SSSColor;

        float _LOD_scale;
        float _FoamBiasLOD0, _FoamBiasLOD1, _FoamBiasLOD2, _FoamScale, _ContactFoam;
        float _SSSStrength;
        float _Roughness, _RoughnessScale, _MaxGloss;
        
        

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        float pow5(float f)
        {
            return f * f * f * f * f;
        }


        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
            float4 worldUV = float4(worldPos.xz, 0,0);
            o.worldUV = worldUV.xy;

            o.viewVector = _WorldSpaceCameraPos.xyz - mul(unity_ObjectToWorld, v.vertex).xyz;
            float viewDist = length(o.viewVector);

            float lod_c0 = min(_LOD_scale * LengthScale0 / viewDist, 1);
            float lod_c1 = min(_LOD_scale * LengthScale1 / viewDist, 1);
            float lod_c2 = min(_LOD_scale * LengthScale2 / viewDist, 1);

            float3 displacement = 0;
            float largeWavesBias = 0;

            displacement += tex2Dlod(_Displacement_c0, worldUV / LengthScale0) * lod_c0;
            largeWavesBias = displacement.y;

            v.vertex.xyz += mul(unity_WorldToObject, displacement);
        }

         float3 WorldToTangentNormalVector(Input IN, float3 normal) {
            float3 t2w0 = WorldNormalVector(IN, float3(1, 0, 0));
            float3 t2w1 = WorldNormalVector(IN, float3(0, 1, 0));
            float3 t2w2 = WorldNormalVector(IN, float3(0, 0, 1));
            float3x3 t2w = float3x3(t2w0, t2w1, t2w2);
            return normalize(mul(t2w, normal));
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float4 derivatives = tex2D(_Derivatives_c0, IN.worldUV / LengthScale0);

            float2 slope = float2(derivatives.x / (1 + derivatives.z),
                derivatives.y / (1 + derivatives.w));
            float3 worldNormal = normalize(float3(-slope.x, 1, -slope.y));

            o.Normal = WorldToTangentNormalVector(IN, worldNormal);

            float jacobian = tex2D(_Turbulence_c0, IN.worldUV / LengthScale0).x;
            jacobian = min(1, max(0, (-jacobian + _FoamBiasLOD0) * _FoamScale));

            float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
            float backgroundDepth =
                LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV));
            float surfaceDepth = UNITY_Z_0_FAR_FROM_CLIPSPACE(IN.screenPos.z);
            float depthDifference = max(0, backgroundDepth - surfaceDepth - 0.1);
            float foam = tex2D(_FoamTexture, IN.worldUV * 0.5 + _Time.r).r;
            jacobian += _ContactFoam * saturate(max(0, foam - depthDifference) * 5) * 0.9;

            o.Albedo = lerp(0, _FoamColor, jacobian);
            float distanceGloss = lerp(1 - _Roughness, _MaxGloss, 1 / (1 + length(IN.viewVector) * _RoughnessScale));
            o.Smoothness = lerp(distanceGloss, 0, jacobian);
            o.Metallic = 0;

            float3 viewDir = normalize(IN.viewVector);
            float3 H = normalize(-worldNormal + _WorldSpaceLightPos0);
            float ViewDotH = pow5(saturate(dot(viewDir, -H))) * 30 * _SSSStrength;
            fixed3 color = lerp(_Color, saturate(_Color + _SSSColor.rgb * ViewDotH * IN.lodScales.w), IN.lodScales.z);

            float fresnel = dot(worldNormal, viewDir);
            fresnel = saturate(1 - fresnel);
            fresnel = pow5(fresnel);

            o.Emission = lerp(color * (1 - fresnel), 0, jacobian);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
