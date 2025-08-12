Shader "Custom/ATOceanVertexColor"
{
    Properties
    {
        // ���ǿ������һ��������ɫ��Ϊ������ɫ�ĳ���������ֱ��ʹ�ö�����ɫ
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

            // �� Properties �л�ȡ����
            uniform float4 _Color;
            uniform sampler2D _MainTex;
            uniform float4 _MainTex_ST;

            // ������ɫ������ṹ
            struct appdata
            {
                float4 vertex : POSITION;       // ����λ��
                float4 color : COLOR;          // ������ɫ (�ؼ�!)
                float2 uv : TEXCOORD0;         // UV ����
            };

            // ������ɫ����� / Ƭ����ɫ������ṹ
            struct v2f
            {
                float4 pos : SV_POSITION;      // �ü��ռ�λ��
                float4 color : COLOR;          // ���ݸ�Ƭ����ɫ���Ķ�����ɫ
                float2 uv : TEXCOORD0;         // ���ݸ�Ƭ����ɫ���� UV
            };

            // ������ɫ��
            v2f vert (appdata v)
            {
                v2f o;
                // ������λ�ô�ģ�Ϳռ�ת�����ü��ռ�
                o.pos = UnityObjectToClipPos(v.vertex);
                // ֱ�Ӵ��ݶ�����ɫ��Ҳ������ _Color ��ˣ� o.color = v.color * _Color;
                o.color = v.color;
                // ���ݲ����ܽ���������������ź�ƽ��
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // Ƭ����ɫ��
            fixed4 frag (v2f i) : SV_Target
            {
                // �������в�����ɫ
                fixed4 texColor = tex2D(_MainTex, i.uv);
                // ��������ɫ�붥����ɫ���
                // ����������ɫ��"Ⱦɫ"����
                fixed4 finalColor = texColor * i.color;
                // �����ֻ����ȫʹ�ö�����ɫ����������ֱ�ӷ��� i.color ����
                // fixed4 finalColor = i.color;
                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse" // ������� SubShader ��֧�֣�ʹ�ñ�׼�� Diffuse Shader ��Ϊ��ѡ
}