Shader "AlwaysFaithful/MapTerrain"
{
    Properties
    {
        _Color ("Terrain Color", Color) = (1, 1, 1, 1)
        _EdgeStrength ("Hex Edge Strength", Range(0, 1)) = 0
        _Atmosphere ("Distance Atmosphere", Range(0, 1)) = 0
        _AtmosphereColor ("Atmosphere Color", Color) = (.035, .065, .068, 1)
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
            float _EdgeStrength;
            float _Atmosphere;

            Interpolator Vert(Input input)
            {
                Interpolator output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                float3 normal = UnityObjectToWorldNormal(input.normal);
                // Raised from the original .76 floor so each hex's vertical
                // side wall doesn't read as a near-black ring at close zoom.
                output.shade = lerp(.86, 1.06, saturate(normal.y * .5 + .5));
                output.worldXZ = mul(unity_ObjectToWorld, input.vertex).xz;
                output.localXZ = input.vertex.xz;
                output.worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
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
                // A broad, low-contrast color drift breaks up the single
                // swatch look without competing with authored elevation and
                // cover colors.
                float macro = Hash(floor(input.worldXZ * .42));
                float macroShade = lerp(.98, 1.02, macro);

                // Inset bevel on the upward face. The local-space expression
                // is the signed distance approximation for a flat-top hex;
                // walls already receive normal-based shading, so this stays
                // off them. It visually seats adjacent tiles without turning
                // the board into a heavy black grid.
                float edgeDistance = max(abs(input.localXZ.x), max(abs(.5 * input.localXZ.x + .8660254 * input.localXZ.y), abs(.5 * input.localXZ.x - .8660254 * input.localXZ.y)));
                float topFace = step(.72, input.shade);
                float edge = smoothstep(.92, .985, edgeDistance) * topFace * saturate(_EdgeStrength);
                float3 color = _Color.rgb * input.shade * grainShade * macroShade * lerp(1.0, .76, edge);

                float cameraDistance = distance(_WorldSpaceCameraPos, input.worldPosition);
                float haze = smoothstep(18.0, 43.0, cameraDistance) * saturate(_Atmosphere);
                color = lerp(color, _AtmosphereColor.rgb, haze);
                return fixed4(color, 1);
            }
            ENDCG
        }
    }
}
