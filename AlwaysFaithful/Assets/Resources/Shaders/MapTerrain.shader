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
            };

            fixed4 _Color;

            Interpolator Vert(Input input)
            {
                Interpolator output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                float3 normal = UnityObjectToWorldNormal(input.normal);
                output.shade = lerp(.76, 1.06, saturate(normal.y * .5 + .5));
                return output;
            }

            fixed4 Frag(Interpolator input) : SV_Target
            {
                return fixed4(_Color.rgb * input.shade, 1);
            }
            ENDCG
        }
    }
}
