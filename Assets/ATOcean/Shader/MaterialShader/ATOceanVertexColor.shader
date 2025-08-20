Shader "Custom/ATOceanVertexColor"
{
    Properties
    {
        // 我们可以添加一个基础颜色作为顶点颜色的乘数，或者直接使用顶点颜色
        _Color ("Base Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // 从 Properties 中获取参数
            uniform float4 _Color;
            uniform sampler2D _MainTex;
            uniform float4 _MainTex_ST;

            // 顶点着色器输入结构
            struct appdata
            {
                float4 vertex : POSITION;       // 顶点位置
                float4 color : COLOR;          // 顶点颜色 (关键!)
                float2 uv : TEXCOORD0;         // UV 坐标
            };

            // 顶点着色器输出 / 片段着色器输入结构
            struct v2f
            {
                float4 pos : SV_POSITION;      // 裁剪空间位置
                float4 color : COLOR;          // 传递给片段着色器的顶点颜色
                float2 uv : TEXCOORD0;         // 传递给片段着色器的 UV
            };

            // 顶点着色器
            v2f vert (appdata v)
            {
                v2f o;
                // 将顶点位置从模型空间转换到裁剪空间
                o.pos = UnityObjectToClipPos(v.vertex);
                // 直接传递顶点颜色，也可以与 _Color 相乘： o.color = v.color * _Color;
                o.color = v.color;
                // 传递并可能进行纹理坐标的缩放和平移
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // 片段着色器
            fixed4 frag (v2f i) : SV_Target
            {
                // 从纹理中采样颜色
                fixed4 texColor = tex2D(_MainTex, i.uv);
                // 将纹理颜色与顶点颜色相乘
                // 这样顶点颜色会"染色"纹理
                fixed4 finalColor = texColor * i.color;
                // 如果你只想完全使用顶点颜色而忽略纹理，直接返回 i.color 即可
                // fixed4 finalColor = i.color;
                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse" // 如果上述 SubShader 不支持，使用标准的 Diffuse Shader 作为备选
}