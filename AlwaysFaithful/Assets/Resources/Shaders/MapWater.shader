Shader "AlwaysFaithful/MapWater"
{
    // MapTerrain's water tiles used the exact same static grain as land,
    // which read as a flat painted color with no sense of a water surface.
    // This variant keeps the same elevation-band contour tinting (still
    // driven by the incoming _Color) but replaces the static grain with a
    // time-scrolled ripple plus sparse bright glints, all still procedural
    // hash noise -- no texture asset, no real reflection/refraction cost.
    Properties
    {
        _Color ("Terrain Color", Color) = (1, 1, 1, 1)
        _Shimmer ("Shimmer Strength", Float) = 1
        _EdgeStrength ("Hex Edge Strength", Range(0, 1)) = 0
        _Atmosphere ("Distance Atmosphere", Range(0, 1)) = 0
        _AtmosphereColor ("Atmosphere Color", Color) = (.025, .06, .075, 1)
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }

        Pass
        {
            Cull Off
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            struct Input
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct Interpolator
            {
                float4 vertex : SV_POSITION;
                float shade : TEXCOORD0;
                float2 worldXZ : TEXCOORD1;
                float2 localXZ : TEXCOORD2;
                float3 worldPosition : TEXCOORD3;
            };

            fixed4 _Color;
            fixed4 _AtmosphereColor;
            float _Shimmer;
            float _EdgeStrength;
            float _Atmosphere;

            Interpolator Vert(Input input)
            {
                Interpolator output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                float3 normal = UnityObjectToWorldNormal(input.normal);
                output.shade = lerp(.86, 1.06, saturate(normal.y * .5 + .5));
                output.worldXZ = mul(unity_ObjectToWorld, input.vertex).xz;
                output.localXZ = input.vertex.xz;
                output.worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
                return output;
            }

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 Frag(Interpolator input) : SV_Target
            {
                // Directional wavelets read as water instead of animated TV
                // static. A tiny hash term keeps repetition from becoming
                // obvious across the whole operational board.
                float waveA = sin(dot(input.worldXZ, float2(5.7, 2.1)) + _Time.y * 1.15);
                float waveB = sin(dot(input.worldXZ, float2(-2.4, 8.3)) - _Time.y * .82);
                float irregular = Hash(floor(input.worldXZ * 11.0));
                float ripple = saturate(.50 + waveA * .20 + waveB * .14 + (irregular - .5) * .12);
                float rippleShade = lerp(1.0, lerp(.93, 1.09, ripple), saturate(_Shimmer));

                // Sparse, short-lived bright glints -- a sunlit-water
                // sparkle rather than a uniformly moving pattern.
                float sparkle = pow(Hash(floor(input.worldXZ * 33.0) + floor(_Time.y * 3.0)), 26.0) * saturate(_Shimmer);

                float edgeDistance = max(abs(input.localXZ.x), max(abs(.5 * input.localXZ.x + .8660254 * input.localXZ.y), abs(.5 * input.localXZ.x - .8660254 * input.localXZ.y)));
                float edge = smoothstep(.93, .985, edgeDistance) * step(.72, input.shade) * saturate(_EdgeStrength);
                fixed3 color = _Color.rgb * input.shade * rippleShade * lerp(1.0, .78, edge) + sparkle * .36;
                float cameraDistance = distance(_WorldSpaceCameraPos, input.worldPosition);
                float haze = smoothstep(18.0, 43.0, cameraDistance) * saturate(_Atmosphere);
                color = lerp(color, _AtmosphereColor.rgb, haze);
                return fixed4(color, 1);
            }
            ENDCG
        }
    }
}
