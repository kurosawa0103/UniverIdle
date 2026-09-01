Shader "Custom/TVNoise"
{
    Properties
    {
        _MainTex("Main Tex", 2D) = "white" {}
        _NoiseStrength("Noise Strength", Range(0,1)) = 1
        _DistortionStrength("Distortion Strength", Range(0,0.1)) = 0.05
        _Speed("Speed", Range(0,10)) = 1
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 100

            Pass
            {
                ZWrite Off
                Cull Off
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "UnityCG.cginc"

                sampler2D _MainTex;
                float _NoiseStrength;
                float _DistortionStrength;
                float _Speed;

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                };

                // Hash function for pseudo-randomness
                float hash(float2 p)
                {
                    return frac(sin(dot(p, float2(12.9898,78.233))) * 43758.5453);
                }

                // Value noise
                float noise(float2 uv)
                {
                    float2 i = floor(uv);
                    float2 f = frac(uv);

                    // Four corners
                    float a = hash(i);
                    float b = hash(i + float2(1.0, 0.0));
                    float c = hash(i + float2(0.0, 1.0));
                    float d = hash(i + float2(1.0, 1.0));

                    // Smooth interpolation
                    float2 u = f * f * (3.0 - 2.0 * f);
                    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
                }

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    return o;
                }

                float4 frag(v2f i) : SV_Target
                {
                    float time = _Time.y * _Speed;

                // Add random offset for distortion in horizontal direction
                float distortion = sin(i.uv.y * 100 + time * 5.0) * _DistortionStrength;

                // Apply distortion
                float2 uv = i.uv + float2(distortion, 0);

                // Scale noise for finer grain
                float2 noiseUV = uv * float2(320, 240); // adjust resolution of noise
                noiseUV += float2(0, time * 30.0); // scroll vertically for animation

                // Flashy snow noise
                float n = noise(noiseUV + time * 10.0);
                float white = step(0.5, n); // binary snow

                return float4(white * _NoiseStrength, white * _NoiseStrength, white * _NoiseStrength, 1.0);
            }
            ENDCG
        }
        }
}