// Standard lighting plus an emission pulse that runs along one world axis — the comet a
// tower-defence path tracer draws. Driven by world position, not UV, because the kit's
// per-face planar UVs flip direction from face to face. Positive _FlowSpeed carries the head
// along +_FlowDir with its tail trailing on the -_FlowDir side; negative speed runs the other
// way, tail flipped to match. The Meta pass emits the time-average so a lightmap bake does not
// depend on the moment it was pressed. Fallback Standard keeps the Kit_*.mat assets loadable.
Shader "GKB/EmissiveFlow"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0
        [HDR] _EmissionColor ("Emission", Color) = (0, 0, 0, 1)

        _FlowOn ("Flow On", Float) = 1
        _FlowDir ("Flow Direction (world)", Vector) = (0, 0, 1, 0)
        _FlowSpeed ("Flow Speed (m/s)", Float) = 3
        _FlowLength ("Pulse Spacing (m)", Float) = 4
        _FlowWidth ("Pulse Length (0-1 of spacing)", Range(0.02, 1)) = 0.35
        _FlowFloor ("Trough Brightness", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        half _Glossiness;
        half _Metallic;
        half4 _EmissionColor;
        float _FlowOn, _FlowSpeed, _FlowLength, _FlowWidth, _FlowFloor;
        float4 _FlowDir;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;

            float pulse;
        #ifdef UNITY_PASS_META
            // the smoothstep ramp averages to half its width over one spacing
            pulse = _FlowWidth * 0.5;
        #else
            float s = _FlowSpeed < 0 ? -1.0 : 1.0;
            float d = dot(IN.worldPos, normalize(_FlowDir.xyz)) * s;
            // phase 1 is the head; the tail ramps up into it from phase 1 - width
            float phase = frac((d - abs(_FlowSpeed) * _Time.y) / max(_FlowLength, 0.001));
            pulse = smoothstep(1.0 - _FlowWidth, 1.0, phase);
        #endif
            float bright = lerp(1.0, lerp(_FlowFloor, 1.0, pulse), saturate(_FlowOn));
            o.Emission = _EmissionColor.rgb * bright;
        }
        ENDCG
    }

    FallBack "Standard"
}
