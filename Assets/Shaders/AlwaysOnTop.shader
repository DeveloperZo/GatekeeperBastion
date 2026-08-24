// Project-owned, Built-In RP. Draws a mesh in flat colour with depth testing switched off, so
// it reads through walls. VEIL's PING is the only caller: the crew is told where every
// Gatekeeper is, and geometry must not hide the answer.
//
// Not a vendor shader and not a Shader Graph — lint-vendor gates those, and this needs to work
// on the Built-In pipeline the project actually ships.
Shader "GKB/AlwaysOnTop"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "Queue" = "Overlay" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }

    Fallback "Unlit/Color"
}
