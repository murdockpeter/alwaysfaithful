Shader "AlwaysFaithful/MapProp"
{
    // Depth-tested scenery shader. Tactical vegetation, rocks, and buildings
    // must disappear behind nearer relief and inherit their cell's fog; units
    // and order overlays intentionally remain on the separate MapSolid and
    // MapOverlay shaders so gameplay information stays readable.
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _FogAmount ("Fog Amount", Range(0, 1)) = 0
        _FogColor ("Fog Color", Color) = (.025, .055, .064, 1)
    }

    SubShader
    {
        Tags { "Queue" = "Geometry+10" "RenderType" = "Opaque" }

        Pass
        {
            Cull Back
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
                float3 worldNormal : TEXCOORD0;
                float3 worldPosition : TEXCOORD1;
            };

            fixed4 _Color;
            fixed4 _FogColor;
            float _FogAmount;

            Interpolator Vert(Input input)
            {
                Interpolator output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                output.worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
                return output;
            }

            fixed4 Frag(Interpolator input) : SV_Target
            {
                float3 normal = normalize(input.worldNormal);
                float3 key = normalize(float3(-.35, .82, .45));
                float lambert = saturate(dot(normal, key));
                float hemisphere = normal.y * .5 + .5;
                float shade = lerp(.50, 1.12, lambert) + hemisphere * .08;
                float rim = pow(1.0 - saturate(dot(normal, normalize(_WorldSpaceCameraPos - input.worldPosition))), 3.0);
                float3 color = _Color.rgb * shade + rim * _Color.rgb * .10;
                color = lerp(color, _FogColor.rgb, saturate(_FogAmount));
                return fixed4(color, 1);
            }
            ENDCG
        }
    }
}
