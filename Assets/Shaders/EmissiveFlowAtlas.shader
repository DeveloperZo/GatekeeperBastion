// The Synty Generic_Basic look — atlas albedo, normal map, base tint, a masked emission — with
// GKB/EmissiveFlow's travelling pulse on the emission. For the gallery's deck tiles. The
// deck's vents all sample one atlas texel, so the mask cannot tell a lengthwise vent from a
// crosswise one; _Vent_Map does, as a 5 m tile map sampled by world XZ (red = vents that run
// along Z, green = along X). Each set has its own colour and pulse; whatever the mask lights
// outside the vent map keeps _Emission_Color and the main pulse. _FlowOn is the master switch.
// Same pulse maths and sign as EmissiveFlow: positive speed carries the head along +dir.
// The Meta pass emits time-averages so a lightmap bake does not depend on the moment.
Shader "GKB/EmissiveFlowAtlas"
{
    Properties
    {
        _Albedo_Map ("Albedo Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [Normal] _Normal_Map ("Normal Map", 2D) = "bump" {}
        _Normal_Amount ("Normal Amount", Range(0, 2)) = 1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Emission_Map ("Emission Map", 2D) = "black" {}
        [HDR] _Emission_Color ("Emission Color", Color) = (0, 0, 0, 1)
        _Enable_Emission ("Enable Emission", Float) = 1
        _Alpha_Clip_Threshold ("Alpha Clip Threshold", Range(0, 1)) = 0.5

        _FlowOn ("Flow On (master)", Float) = 1
        _FlowDir ("Flow Direction (world)", Vector) = (0, 0, 1, 0)
        _FlowSpeed ("Flow Speed (m/s)", Float) = 3
        _FlowLength ("Pulse Spacing (m)", Float) = 4
        _FlowWidth ("Pulse Length (0-1 of spacing)", Range(0.02, 1)) = 0.35
        _FlowFloor ("Trough Brightness", Range(0, 1)) = 0.25

        _Vent_Map ("Vent Map (R along Z, G along X; 5 m tile)", 2D) = "black" {}
        _Vent_Tile ("Vent Map Tile (m)", Float) = 5

        [HDR] _VentZ_Color ("Vents Along Z: Colour", Color) = (0, 0, 0, 1)
        _VentZ_FlowOn ("Vents Along Z: Flow On", Float) = 1
        _VentZ_FlowDir ("Vents Along Z: Direction", Vector) = (0, 0, 1, 0)
        _VentZ_FlowSpeed ("Vents Along Z: Speed (m/s)", Float) = 3
        _VentZ_FlowLength ("Vents Along Z: Spacing (m)", Float) = 4
        _VentZ_FlowWidth ("Vents Along Z: Pulse Length", Range(0.02, 1)) = 0.35
        _VentZ_FlowFloor ("Vents Along Z: Trough", Range(0, 1)) = 0.25

        [HDR] _VentX_Color ("Vents Along X: Colour", Color) = (0, 0, 0, 1)
        _VentX_FlowOn ("Vents Along X: Flow On", Float) = 1
        _VentX_FlowDir ("Vents Along X: Direction", Vector) = (1, 0, 0, 0)
        _VentX_FlowSpeed ("Vents Along X: Speed (m/s)", Float) = 3
        _VentX_FlowLength ("Vents Along X: Spacing (m)", Float) = 4
        _VentX_FlowWidth ("Vents Along X: Pulse Length", Range(0.02, 1)) = 0.35
        _VentX_FlowFloor ("Vents Along X: Trough", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _Albedo_Map, _Normal_Map, _Emission_Map, _Vent_Map;
        fixed4 _BaseColor;
        half _Normal_Amount, _Smoothness, _Metallic, _Enable_Emission, _Alpha_Clip_Threshold;
        half4 _Emission_Color, _VentZ_Color, _VentX_Color;
        float _FlowOn, _FlowSpeed, _FlowLength, _FlowWidth, _FlowFloor;
        float4 _FlowDir;
        float _Vent_Tile;
        float _VentZ_FlowOn, _VentZ_FlowSpeed, _VentZ_FlowLength, _VentZ_FlowWidth, _VentZ_FlowFloor;
        float4 _VentZ_FlowDir;
        float _VentX_FlowOn, _VentX_FlowSpeed, _VentX_FlowLength, _VentX_FlowWidth, _VentX_FlowFloor;
        float4 _VentX_FlowDir;

        struct Input
        {
            float2 uv_Albedo_Map;
            float3 worldPos;
        };

        // brightness multiplier of one pulse set at this point: 1 with the set off
        float Bright(float3 wp, float on, float3 dir, float speed, float len, float width, float floorLevel)
        {
            float pulse;
        #ifdef UNITY_PASS_META
            pulse = width * 0.5;
        #else
            float s = speed < 0 ? -1.0 : 1.0;
            float d = dot(wp, normalize(dir)) * s;
            float phase = frac((d - abs(speed) * _Time.y) / max(len, 0.001));
            pulse = smoothstep(1.0 - width, 1.0, phase);
        #endif
            return lerp(1.0, lerp(floorLevel, 1.0, pulse), saturate(on * _FlowOn));
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // the graph reads alpha off the base colour and clips it; alpha is whether the
            // surface exists, not a tint channel
            clip(_BaseColor.a - _Alpha_Clip_Threshold);

            fixed4 c = tex2D(_Albedo_Map, IN.uv_Albedo_Map);
            o.Albedo = c.rgb * _BaseColor.rgb;
            o.Normal = UnpackScaleNormal(tex2D(_Normal_Map, IN.uv_Albedo_Map), _Normal_Amount);
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Alpha = 1;

            fixed mask = tex2D(_Emission_Map, IN.uv_Albedo_Map).r;
            float2 tile = frac(IN.worldPos.xz / max(_Vent_Tile, 0.001));
            fixed2 vent = tex2D(_Vent_Map, tile).rg;
            float rest = saturate(1.0 - max(vent.r, vent.g));

            float3 e = vent.r * _VentZ_Color.rgb
                     * Bright(IN.worldPos, _VentZ_FlowOn, _VentZ_FlowDir.xyz, _VentZ_FlowSpeed,
                              _VentZ_FlowLength, _VentZ_FlowWidth, _VentZ_FlowFloor)
                     + vent.g * _VentX_Color.rgb
                     * Bright(IN.worldPos, _VentX_FlowOn, _VentX_FlowDir.xyz, _VentX_FlowSpeed,
                              _VentX_FlowLength, _VentX_FlowWidth, _VentX_FlowFloor)
                     + rest * _Emission_Color.rgb
                     * Bright(IN.worldPos, 1.0, _FlowDir.xyz, _FlowSpeed,
                              _FlowLength, _FlowWidth, _FlowFloor);
            o.Emission = mask * e * _Enable_Emission;
        }
        ENDCG
    }

    FallBack "Standard"
}
