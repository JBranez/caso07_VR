// Retícula de mirada: punto central + anillo + arco de progreso (dwell) para activar con la mirada.
Shader "LasPalmeras/Reticle"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Progress ("Progress", Range(0,1)) = 0
        _Hover ("Hover", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Overlay+1" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Progress;
            float _Hover;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;
                float r = length(p);
                float aa = 0.05;
                float dotR = lerp(0.14, 0.09, _Hover);
                float dotA = 1.0 - smoothstep(dotR - aa, dotR + aa, r);
                float ringA = smoothstep(0.62 - aa, 0.62, r) * (1.0 - smoothstep(0.82, 0.82 + aa, r));
                float ang = atan2(p.x, p.y) / 6.2831853 + 0.5;
                float prog = step(ang, _Progress) * ringA * step(0.001, _Progress);
                float a = max(dotA, max(ringA * (0.25 + 0.45 * _Hover), prog));
                fixed4 c;
                c.rgb = lerp(_Color.rgb, float3(1.0, 1.0, 1.0), prog);
                c.a = _Color.a * a;
                return c;
            }
            ENDCG
        }
    }
}
