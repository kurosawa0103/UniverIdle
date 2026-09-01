Shader "UI/GlitchCRT"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [PerRendererData] _SoftMask ("Mask", 2D) = "white" {}

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        
        // 损坏效果参数（_GlitchStrength=0 时为完全正常画面，供开局播一次后关掉）
        _GlitchStrength ("撕裂强度", Range(0, 0.2)) = 0.1
        _GlitchSpeed ("抽搐速度", Range(0, 50)) = 20
        _GlitchAmount ("断层密度", Range(1, 10)) = 4
        _Scanline ("扫描线(仅故障时)", Range(0, 1)) = 0.35
        _RgbSplit ("RGB 分离", Range(0, 0.02)) = 0.008
        _RgbNoise ("RGB噪点强度", Range(0, 0.35)) = 0.1
        _NoiseBlock ("噪点块大小", Range(4, 200)) = 90
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile __ SOFTMASK_SIMPLE SOFTMASK_SLICED SOFTMASK_TILED

            #include "UnityCG.cginc"
            #include "Assets/Plugins/SoftMask/Shaders/Resources/SoftMask.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                SOFTMASK_COORDS(1)
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _GlitchStrength;
            float _GlitchSpeed;
            float _GlitchAmount;
            float _Scanline;
            float _RgbSplit;
            float _RgbNoise;
            float _NoiseBlock;

            // 伪随机，用于条带瞬移“抽搐”
            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                SOFTMASK_CALCULATE_COORDS(o, v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float s = _GlitchStrength;

                // 强度为 0：原样输出（无扫描线闪烁、无位移）
                if (s <= 1e-5)
                {
                    fixed4 c = tex2D(_MainTex, uv) * i.color;
                    c.a *= SOFTMASK_GET_MASK(i);
                    return c;
                }

                float time = _Time.y * _GlitchSpeed;
                float bands = 12.0 + _GlitchAmount * 10.0;
                float bandId = floor(uv.y * bands);
                // 每帧跳变的随机横向偏移 + 连续正弦，合成“抽搐”
                float2 hSeed = float2(bandId, floor(time * 18.0));
                float jitter = (hash21(hSeed) - 0.5) * 2.0;
                float wave = sin(uv.y * _GlitchAmount * 6.2831853 + time);
                float offset = (wave * 0.55 + jitter * 0.45) * s;
                uv.x += offset;

                // 扫描线、亮度抖动只在故障强度下出现
                float scan = sin(uv.y * 600.0 + time) * _Scanline * s;
                float brightness = 1.0 + scan;

                float split = _RgbSplit * s;
                fixed4 col;
                col.r = tex2D(_MainTex, uv + float2(split, 0)).r;
                col.g = tex2D(_MainTex, uv).g;
                col.b = tex2D(_MainTex, uv - float2(split, 0)).b;
                col.a = tex2D(_MainTex, uv).a;
                col *= i.color;
                col.rgb *= brightness;

                // 块状 RGB 故障噪点：每块 R/G/B 独立偏置，随时间换格
                float2 cell = floor(uv * _NoiseBlock) + floor(float2(time * 11.0, time * 7.3));
                float nr = hash21(cell + float2(0.17, 0)) * 2.0 - 1.0;
                float ng = hash21(cell + float2(4.11, 0)) * 2.0 - 1.0;
                float nb = hash21(cell + float2(8.03, 0)) * 2.0 - 1.0;
                float3 rgbN = float3(nr, ng, nb) * _RgbNoise * s;
                // 细颗粒再叠一层，偏色更明显
                float2 f = uv * 620.0 + float2(time * 19.0, time * 13.0);
                float3 fine = float3(hash21(f), hash21(f + 3.1), hash21(f + 6.2)) - 0.5;
                rgbN += fine * (_RgbNoise * 0.45 * s);
                col.rgb = saturate(col.rgb + rgbN);

                col.a *= SOFTMASK_GET_MASK(i);
                return col;
            }
            ENDCG
        }
    }
}