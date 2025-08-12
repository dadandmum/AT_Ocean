Shader "Example/URPUnlitShaderBasic"
{
    // Unity ��ɫ���� Properties ����顣�ڴ�ʾ���У���������Ϊ�գ�
    // ��Ϊ��ƬԪ��ɫ��������Ԥ�����������ɫ��
    Properties
    { }

    // ���� Shader ����� SubShader ����顣
    SubShader
    {
        // SubShader Tags �����ʱ�Լ��ں���������ִ��ĳ�� SubShader �����
        // ��ĳ��ͨ����
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            // HLSL ����顣Unity SRP ʹ�� HLSL ���ԡ�
            HLSLPROGRAM
            // ���ж��嶥����ɫ�������ơ�
            #pragma vertex vert
            // ���ж���ƬԪ��ɫ�������ơ�
            #pragma fragment frag

            // Core.hlsl �ļ��������õ� HLSL ���
            // �����Ķ��壬������������ HLSL �ļ�������
            // Common.hlsl��SpaceTransforms.hlsl �ȣ��� #include ���á�
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // �ṹ���彫������������Щ������
            // ��ʾ��ʹ�� Attributes �ṹ��Ϊ������ɫ���е�
            // ����ṹ��
            struct Attributes
            {
                // positionOS ������������ռ��еĶ���
                // λ�á�
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                // �˽ṹ�е�λ�ñ������ SV_POSITION ���塣
                float4 positionHCS  : SV_POSITION;
            };

            // ������ɫ����������� Varyings �ṹ�ж����
            // ���ԡ�vert ���������ͱ����������ص����ͣ��ṹ��
            // ƥ�䡣
            Varyings vert(Attributes IN)
            {
                // ʹ�� Varyings �ṹ����������� (OUT)��
                Varyings OUT;
                // TransformObjectToHClip ����������λ��
                // �Ӷ���ռ�任����βü��ռ䡣
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                // ���������
                return OUT;
            }

            // ƬԪ��ɫ�����塣
            half4 frag() : SV_Target
            {
                // ������ɫ��������������
                half4 customColor = half4(0.5, 0, 0, 1);
                return customColor;
            }
            ENDHLSL
        }
    }
}