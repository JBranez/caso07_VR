// Monitor CCTV: ruido de «SIN SEÑAL» (modo 0) o imagen sintética de cámara (modo 1; modo 2 = con caja).
Shader "LasPalmeras/CctvNoise"
{
    Properties
    {
        _Mode ("Mode (0 noise, 1 feed, 2 feed + box)", Float) = 0
        _Tint ("Tint", Color) = (0.7, 1.0, 0.8, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Pass
        {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Mode;
            fixed4 _Tint;

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

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = floor(_Time.y * 24.0);
                float row = floor(uv.y * 40.0);
                float band = step(0.965, hash(float2(row, t)));
                float shift = (hash(float2(row, t + 3.0)) - 0.5) * 0.2 * band;
                float n = hash((uv + float2(shift, 0.0)) * float2(320.0, 180.0) + t * 1.37);
                float scan = 0.82 + 0.18 * sin(uv.y * 600.0);
                float vign = smoothstep(0.85, 0.25, length(uv - 0.5));
                float3 col;

                if (_Mode < 0.5)
                {
                    float roll = frac(uv.y - _Time.y * 0.15);
                    float bar = smoothstep(0.0, 0.02, roll) * (1.0 - smoothstep(0.02, 0.12, roll));
                    col = _Tint.rgb * (n * 0.55 + 0.05 + bar * 0.15);
                }
                else
                {
                    float3 bg = lerp(float3(0.03, 0.05, 0.04), float3(0.12, 0.16, 0.13), uv.y);
                    bg = lerp(bg, float3(0.10, 0.12, 0.10), step(uv.y, 0.32));
                    float door = step(0.10, uv.x) * step(uv.x, 0.22) * step(0.32, uv.y) * step(uv.y, 0.78);
                    bg = lerp(bg, float3(0.02, 0.03, 0.03), door);
                    float desk = step(0.62, uv.x) * step(uv.x, 0.90) * step(0.32, uv.y) * step(uv.y, 0.46);
                    bg = lerp(bg, float3(0.07, 0.09, 0.08), desk);
                    float px = frac(_Time.y * 0.05) * 1.2 - 0.1;
                    float2 pc = float2(px, 0.46);
                    float body = smoothstep(1.0, 0.85, length((uv - pc) / float2(0.045, 0.20)));
                    float headM = smoothstep(1.0, 0.8, length((uv - (pc + float2(0.0, 0.24))) / float2(0.03, 0.04)));
                    float person = saturate(body + headM);
                    float box = 0.0;
                    if (_Mode > 1.5)
                    {
                        float2 bc = pc + float2(0.05, 0.10);
                        box = step(abs(uv.x - bc.x), 0.035) * step(abs(uv.y - bc.y), 0.045);
                    }
                    col = bg + person * 0.22 + box * float3(0.35, 0.30, 0.15);
                    col = col * float3(0.7, 1.0, 0.8) + n * 0.06;
                }
                col *= scan * vign;
                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}
