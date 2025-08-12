Shader "Example/URPUnlitShaderBasic"
{
    // Unity 着色器的 Properties 代码块。在此示例中，这个代码块为空，
    // 因为在片元着色器代码中预定义了输出颜色。
    Properties
    { }

    // 包含 Shader 代码的 SubShader 代码块。
    SubShader
    {
        // SubShader Tags 定义何时以及在何种条件下执行某个 SubShader 代码块
        // 或某个通道。
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            // HLSL 代码块。Unity SRP 使用 HLSL 语言。
            HLSLPROGRAM
            // 此行定义顶点着色器的名称。
            #pragma vertex vert
            // 此行定义片元着色器的名称。
            #pragma fragment frag

            // Core.hlsl 文件包含常用的 HLSL 宏和
            // 函数的定义，还包含对其他 HLSL 文件（例如
            // Common.hlsl、SpaceTransforms.hlsl 等）的 #include 引用。
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // 结构定义将定义它包含哪些变量。
            // 此示例使用 Attributes 结构作为顶点着色器中的
            // 输入结构。
            struct Attributes
            {
                // positionOS 变量包含对象空间中的顶点
                // 位置。
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                // 此结构中的位置必须具有 SV_POSITION 语义。
                float4 positionHCS  : SV_POSITION;
            };

            // 顶点着色器定义具有在 Varyings 结构中定义的
            // 属性。vert 函数的类型必须与它返回的类型（结构）
            // 匹配。
            Varyings vert(Attributes IN)
            {
                // 使用 Varyings 结构声明输出对象 (OUT)。
                Varyings OUT;
                // TransformObjectToHClip 函数将顶点位置
                // 从对象空间变换到齐次裁剪空间。
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                // 返回输出。
                return OUT;
            }

            // 片元着色器定义。
            half4 frag() : SV_Target
            {
                // 定义颜色变量并返回它。
                half4 customColor = half4(0.5, 0, 0, 1);
                return customColor;
            }
            ENDHLSL
        }
    }
}