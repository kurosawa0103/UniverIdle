Shader "Custom/SpriteLit_YClip_URP2D"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _CutoffY("Y Cutoff", Float) = 0
    }

        SubShader
        {
            Tags
            {
                "Queue" = "Transparent"
                "RenderType" = "Transparent"
                "IgnoreProjector" = "True"
                "CanUseSpriteAtlas" = "True"
                "LightMode" = "Universal2D"  // 关键，启用URP 2D光照
            }

            Pass
            {
                Name "UniversalForward"
                Blend SrcAlpha OneMinusSrcAlpha
                Cull Off
                ZWrite Off

                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag

            // URP 2D光照核心库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_ShapeLightTexture0);
            SAMPLER(sampler_ShapeLightTexture0);

            float4 _MainTex_ST;
            float4 _Color;
            float _CutoffY;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                if (IN.worldPos.y < _CutoffY)
                    discard;

                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
                clip(baseColor.a - 0.01);

                // 采样屏幕光照贴图，获取2D光照
                float2 screenUV = IN.positionHCS.xy / _ScreenParams.xy;
                float4 lighting = SAMPLE_TEXTURE2D(_ShapeLightTexture0, sampler_ShapeLightTexture0, screenUV);

                // 乘以光照
                baseColor.rgb *= lighting.rgb;

                return baseColor;
            }

            ENDHLSL
        }
        }
}
