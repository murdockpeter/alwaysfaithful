Shader "AlwaysFaithful/MapSolid"
{
    // Same always-on-top overlay compositing as MapOverlay (so counters and
    // props never z-fight the terrain mesh they sit just above), but adds a
    // cheap fixed-direction lambert term so primitive-built solids (cubes,
    // spheres, cylinders) show real light/dark faces instead of a flat
    // silhouette. Not used for line/ring overlays, which want to stay pure
    // flat color from every angle.
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest Always

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            struct Input
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
            };

            struct Interpolator
            {
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                fixed4 color : COLOR;
            };

            fixed4 _Color;

            Interpolator Vert(Input input)
            {
                Interpolator output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                output.color = input.color * _Color;
                return output;
            }

            fixed4 Frag(Interpolator input) : SV_Target
            {
                float3 lightDir = normalize(float3(-.35, .82, .45));
                float lambert = saturate(dot(normalize(input.worldNormal), lightDir));
                float shade = lerp(.62, 1.12, lambert);
                return fixed4(input.color.rgb * shade, input.color.a);
            }
            ENDCG
        }
    }
}
