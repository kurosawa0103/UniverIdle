Shader "Custom/StencilMask"
{
    SubShader
    {
        Tags { "Queue" = "Geometry-10" "RenderType" = "Opaque" }
        Pass
        {
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }
            ColorMask 0     // 不写颜色，只写Stencil
            ZWrite On       // 遮挡其他物体的深度
        }
    }
}
