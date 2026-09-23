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
            };

            fixed4 _Color;
            float _Shimmer;

            Interpolator Vert(Input input)
            {
                Interpolator output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                float3 normal = UnityObjectToWorldNormal(input.normal);
                output.shade = lerp(.86, 1.06, saturate(normal.y * .5 + .5));
                output.worldXZ = mul(unity_ObjectToWorld, input.vertex).xz;
                return output;
            }

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 Frag(Interpolator input) : SV_Target
            {
                // Two noise layers drifting at different speeds/directions
                // stand in for a cheap animated ripple -- same hash idiom as
                // MapTerrain's static grain, just time-scrolled.
                float2 driftA = input.worldXZ * 7.0 + float2(_Time.y * .18, _Time.y * .11);
                float2 driftB = input.worldXZ * 17.0 - float2(_Time.y * .09, _Time.y * .21);
                float ripple = Hash(floor(driftA)) * .5 + Hash(floor(driftB)) * .5;
                float rippleShade = lerp(1.0, lerp(.90, 1.12, ripple), saturate(_Shimmer));

                // Sparse, short-lived bright glints -- a sunlit-water
                // sparkle rather than a uniformly moving pattern.
                float sparkle = pow(Hash(floor(input.worldXZ * 33.0) + floor(_Time.y * 3.0)), 26.0) * saturate(_Shimmer);

                fixed3 color = _Color.rgb * input.shade * rippleShade + sparkle * .5;
                return fixed4(color, 1);
            }
            ENDCG
        }
    }
}
