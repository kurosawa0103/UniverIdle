Shader "UI/DissolveEffect"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _DissolveTex("Dissolve Map", 2D) = "white" {}
        _Cutoff("Dissolve Amount", Range(0,1)) = 0.0
        _EdgeColor("Edge Color", Color) = (1,0.5,0,1)
    }

        SubShader
        {
            Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
            LOD 100
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                };

                sampler2D _MainTex;
                sampler2D _DissolveTex;
                float _Cutoff;
                fixed4 _EdgeColor;

                v2f vert(appdata_t v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    float dissolveValue = tex2D(_DissolveTex, i.uv).r;
                    fixed4 col = tex2D(_MainTex, i.uv);

                    if (dissolveValue < _Cutoff)
                        discard;

                    // Edge color blending
                    float edge = smoothstep(_Cutoff, _Cutoff + 0.05, dissolveValue);
                    col.rgb = lerp(_EdgeColor.rgb, col.rgb, edge);

                    return col;
                }
                ENDCG
            }
        }
}