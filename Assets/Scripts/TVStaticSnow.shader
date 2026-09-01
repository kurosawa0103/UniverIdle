Shader "UI/TVStaticSnow"
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

        _StaticStrength ("雪花强度", Range(0, 1)) = 0
        _FlickerSpeed ("闪烁速度", Range(0, 80)) = 28
        _GrainScale ("颗粒密度", Range(40, 1200)) = 320
        _FineGrainScale ("细粒密度", Range(100, 2400)) = 960
        _Contrast ("颗粒对比", Range(0, 1)) = 0.58
        _SnowMix ("雪花覆盖", Range(0, 1)) = 0.94
        _RgbSplit ("RGB 分离", Range(0, 0.05)) = 0.014
        _RgbSplitSnow ("雪花 RGB 分离", Range(0, 0.05)) = 0.018
        _ChromaBleed ("通道色偏", Range(0, 0.35)) = 0.12
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
            float _StaticStrength;
            float _FlickerSpeed;
            float _GrainScale;
            float _FineGrainScale;
            float _Contrast;
            float _SnowMix;
            float _RgbSplit;
            float _RgbSplitSnow;
            float _ChromaBleed;

            float hash21(float2 p)
            {
                p = frac(p * float2(443.897, 441.423));
                p += dot(p, p.yx + 19.19);
                return frac(p.x * p.y);
            }

            float grainThreshold()
            {
                return lerp(0.38, 0.74, _Contrast);
            }

            // 旋转 UV 后再分格，打破横/竖条纹感
            float2 rotateUV(float2 uv, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                return float2(c * uv.x - s * uv.y, s * uv.x + c * uv.y);
            }

            float grainRotated(float2 uv, float scale, float timeStep, float angle, float2 offset)
            {
                float2 ruv = rotateUV(uv, angle);
                float2 cell = floor(ruv * scale + offset);
                float h = hash21(cell + timeStep * 1.73 + offset * 3.1);
                return step(grainThreshold(), h);
            }

            float pixelGrain(float2 uv, float scale, float timeStep, float2 seed)
            {
                float h = hash21(uv * scale + seed + timeStep * 7.19);
                return step(grainThreshold() + 0.04, h);
            }

            float buildSnowLum(float2 uv, float timeStep)
            {
                float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
                float2 nuv = float2(uv.x * aspect, uv.y);

                float t0 = timeStep;
                float t1 = timeStep * 1.37 + 3.0;
                float t2 = timeStep * 0.83 + 7.0;

                float g0 = grainRotated(nuv, _GrainScale, t0, 0.00, float2(0.0, 0.0));
                float g1 = grainRotated(nuv, _GrainScale * 0.91, t1, 1.27, float2(17.3, 9.1));
                float g2 = grainRotated(nuv, _GrainScale * 1.07, t2, 2.55, float2(41.7, 23.6));

                float2 cell = floor(nuv * _GrainScale);
                float2 jitter = float2(hash21(cell + t0), hash21(cell + t0 + 4.7)) - 0.5;
                float gJitter = step(grainThreshold(), hash21(cell + jitter * 1.8 + t1 * 0.61));

                float fine0 = pixelGrain(nuv, _FineGrainScale, t0, float2(3.7, 11.9));
                float fine1 = pixelGrain(nuv, _FineGrainScale * 1.63, t2, float2(91.3, 47.2));
                float sparkle = step(0.84, hash21(nuv * (_FineGrainScale * 2.6) + float2(t0 * 13.1, t1 * 19.7)));
                float white = hash21(nuv * 1800.0 + float2(t0 * 43.17, t2 * 29.53));

                float lum = g0 * 0.26 + g1 * 0.24 + g2 * 0.22 + gJitter * 0.18
                          + fine0 * 0.20 + fine1 * 0.16 + sparkle * 0.18 + white * 0.22;
                return saturate(lum - 0.08);
            }

            // 雪花仍偏灰白底，但 R/G/B 各自偏移采样 + 通道独立噪，分离才看得见
            float3 buildSnowRgb(float2 uv, float timeStep, float splitAmt)
            {
                float2 off = float2(splitAmt, 0);
                float lumR = buildSnowLum(uv + off, timeStep);
                float lumG = buildSnowLum(uv, timeStep);
                float lumB = buildSnowLum(uv - off, timeStep);

                float3 snow = float3(lumR, lumG, lumB);

                if (_ChromaBleed > 1e-4)
                {
                    float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
                    float2 nuv = float2(uv.x * aspect, uv.y);
                    float2 rp = rotateUV(nuv, 0.78) * _GrainScale;
                    float2 c = floor(rp);
                    float t0 = timeStep;
                    float t1 = timeStep * 1.37 + 3.0;
                    float t2 = timeStep * 0.83 + 7.0;
                    snow.r += (hash21(c + t0 + 1.7) - 0.5) * _ChromaBleed;
                    snow.g += (hash21(c + t1 + 5.3) - 0.5) * _ChromaBleed * 0.55;
                    snow.b += (hash21(c + t2 + 9.1) - 0.5) * _ChromaBleed;
                    // 细颗粒 RGB 故障色（参考 GlitchCRT）
                    float2 f = nuv * 620.0 + float2(t0 * 19.0, t1 * 13.0);
                    float3 fine = float3(hash21(f), hash21(f + 3.1), hash21(f + 6.2)) - 0.5;
                    snow += fine * (_ChromaBleed * 0.65);
                }
                return saturate(snow);
            }

            float3 sampleBaseRgb(float2 uv, float splitAmt, fixed4 tint)
            {
                fixed4 cr = tex2D(_MainTex, uv + float2(splitAmt, 0)) * tint;
                fixed4 cg = tex2D(_MainTex, uv) * tint;
                fixed4 cb = tex2D(_MainTex, uv - float2(splitAmt, 0)) * tint;
                return float3(cr.r, cg.g, cb.b);
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
                float s = _StaticStrength;

                if (s <= 1e-5)
                {
                    fixed4 base = tex2D(_MainTex, uv) * i.color;
                    base.a *= SOFTMASK_GET_MASK(i);
                    return base;
                }

                float splitBase = _RgbSplit * s;
                float splitSnow = _RgbSplitSnow * s;
                float3 baseRgb = sampleBaseRgb(uv, splitBase, i.color);

                float timeStep = floor(_Time.y * _FlickerSpeed);
                float2 timeJitter = float2(
                    hash21(uv * 97.0 + timeStep),
                    hash21(uv * 173.0 + timeStep + 3.1)
                ) * 0.97;
                float t = timeStep + timeJitter.x + timeJitter.y;
                float3 snow = buildSnowRgb(uv, t, splitSnow);

                fixed4 col;
                col.rgb = lerp(baseRgb, snow, s * _SnowMix);
                col.a = (tex2D(_MainTex, uv) * i.color).a;
                col.a *= SOFTMASK_GET_MASK(i);
                return col;
            }
            ENDCG
        }
    }
}
