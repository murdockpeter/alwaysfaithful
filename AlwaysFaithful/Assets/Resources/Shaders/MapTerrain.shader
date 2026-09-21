Shader "AlwaysFaithful/MapTerrain"
{
    Properties
    {
        _Color ("Terrain Color", Color) = (1, 1, 1, 1)
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

            Interpolator Vert(Input input)
            {
                Interpolator output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                float3 normal = UnityObjectToWorldNormal(input.normal);
                // Raised from the original .76 floor so each hex's vertical
                // side wall doesn't read as a near-black ring at close zoom.
                output.shade = lerp(.86, 1.06, saturate(normal.y * .5 + .5));
                output.worldXZ = mul(unity_ObjectToWorld, input.vertex).xz;
                return output;
            }

            // Cheap two-octave hash noise, no texture asset — keeps this
            // codebase's flat-primitive/no-imported-assets idiom while giving
            // each hex face a little grain instead of one perfectly flat
            // swatch. Small enough not to disturb the elevation/cover color
            // coding, and it averages back to the plain tint from a distance.
            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 Frag(Interpolator input) : SV_Target
            {
                float grain = Hash(floor(input.worldXZ * 6.0)) * .5 + Hash(floor(input.worldXZ * 19.0)) * .5;
                float grainShade = lerp(.94, 1.03, grain);
                return fixed4(_Color.rgb * input.shade * grainShade, 1);
            }
            ENDCG
        }
    }
}
