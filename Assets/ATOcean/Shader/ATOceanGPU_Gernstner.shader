Shader "ATOcean/ATOceanWaterGPU_Gernstner"
{
    Properties
    {
        [Header(Diffuse)]
        _Color("Base Color", Color) = (0.1, 0.56, 0.89, 1)
        _DiffuseColor0("Diffuse Dark Color", Color) = (0.1, 0.56, 0.89, 1)
        _DiffuseColor1("Diffuse Light Color", Color) = (0.3, 0.86, 0.89, 1)
        _DiffuseRange("Diffuse Range", Vector) = (-2,2,0,0)
        
        [Header(Specular)]
        _Smoothness("Smoothness", Range(0, 1)) = 0.9 // 控制水面的粗糙度 (1=光滑, 0=粗糙)
        _SpecularColor("Specular Color", Color) = (1, 1, 1, 1) // 水的高光颜色 (通常接近白色)
        
        [Header(Normal)]
        _NormalMap("Normal Map", 2D) = "bump" {} // 法线贴图
        
        _DetailNormalStrength("Detail Normal Strength", Range(0, 1)) = 1
        _DetailNormalVelocity("Detail Normal Velocity", Vector) = ( 0.1,0.05,0.03,-0.15)
        
        [Header(Environment)]
        _EnvironmentMap("Environment Map", CUBE) = "" {} // 环境贴图
        _EnvironmentSpecularFallOff("Environment Specular Fall Off", Range(0, 1.0)) = 0.1
        
        [Header(SSS)]
        _SubSurfaceSunFallOff("SubSurface  Sun Fall Off" , Range(0, 5.0)) = 3.0
        _SubSurfaceBase("SubSurface Base", Range(0, 1.0)) = 0.2
        _SubSurfaceSun("SubSurface Sun ", Range(0, 1.0)) = 0.5
        _SubSurfaceColor("SubSurface Color", Color) = (0.3, 0.86, 0.89, 1)
        
        [Header(LevelOfDetail)]
        _TessellationFactor("Tessellation Factor", Range(1, 64)) = 16 // 曲面细分因子

        _LOD_scale("LOD_scale", Range(1,10)) = 0

        [Header(Cascade 0)]
        _Displacement_c0("Displacement C0", 2D) = "black" {}
        _Normal_c0("Normal C0", 2D) = "black" {}
        [HideInInspector]_Turbulence_c0("Turbulence C0", 2D) = "white" {}
        _LengthScale0("LengthScale C0",float) = 40.0
        [Header(Cascade 1)]
        _Displacement_c1("Displacement C1", 2D) = "black" {}
        _Normal_c1("Normal C1", 2D) = "black" {}
        [HideInInspector]_Turbulence_c1("Turbulence C1", 2D) = "white" {}
        _LengthScale1("LengthScale C1",float) = 160.0
        [Header(Cascade 2)]
        _Displacement_c2("Displacement C2", 2D) = "black" {}
        _Normal_c2("Normal C2", 2D) = "black" {}
        [HideInInspector]_Turbulence_c2("Turbulence C2", 2D) = "white" {}
        _LengthScale2("LengthScale C2",float) = 640.0

        [Toggle(_EXTENDED_WAVE)] _IsExtended("Is Extended", Float) = 0
        // [Toggle(LOD0)] _IsExtended("LOD0", Float) = 0
        // [Toggle(LOD1)] _IsExtended("LOD1", Float) = 0
        // [Toggle(LOD2)] _IsExtended("LOD2", Float) = 0

        [HideInInspector]_TipsColor("Tips Color", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" } // 推荐用于URP
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 5.0

            #pragma multi_compile _ LOD0 LOD1 LOD2

            #pragma vertex vert
            #pragma fragment frag
            #pragma hull hull
            #pragma domain domain
            #pragma shader_feature _EXTENDED_WAVE

            #define UNITY_PI 3.14159265359

            // 包含必要的 URP 和光照库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURECUBE(_EnvironmentMap);
            SAMPLER(sampler_EnvironmentMap);

            // 不同Cascade 的 Displacement 贴图
            TEXTURE2D(_Displacement_c0);
            SAMPLER(sampler_Displacement_c0);

            TEXTURE2D(_Displacement_c1);
            SAMPLER(sampler_Displacement_c1);
            TEXTURE2D(_Displacement_c2);
            SAMPLER(sampler_Displacement_c2);

            
            // 不同Cascade 的 Derivatives 贴图
            TEXTURE2D(_Normal_c0);
            SAMPLER(sampler_Normal_c0);
            TEXTURE2D(_Normal_c1);
            SAMPLER(sampler_Normal_c1);
            texture2D _Normal_c2;
            SAMPLER(sampler_Normal_c2);

            texture2D _Turbulence_c0;
            SAMPLER(sampler_Turbulence_c0);
            TEXTURE2D(_Turbulence_c1);
            SAMPLER(sampler_Turbulence_c1);
            TEXTURE2D(_Turbulence_c2);
            SAMPLER(sampler_Turbulence_c2);


            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _DiffuseColor0;
                float4 _DiffuseColor1;
                float4 _DiffuseRange;
                float _Smoothness;
                float4 _SpecularColor;
                float4 _NormalMap_ST; // 法线贴图的缩放和平移
                float4 _EnvironmentMap_ST;
                float _EnvironmentSpecularFallOff;
                float _DetailNormalStrength;
                float4 _DetailNormalVelocity;
                float _SubSurfaceSunFallOff;
                float _SubSurfaceSun;
                float _SubSurfaceBase;
                float3 _SubSurfaceColor;

                
                float3 _WaveWorldPos;
		        float3 _ViewOrigin;
		        float _DomainSize;
		        float _InvDomainSize;
                
                float _TessellationFactor;
                
                float _LOD_scale;
                float _LengthScale0;
                float _LengthScale1;
                float _LengthScale2;

                float4 _TipsColor;
                
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT; // 切线信息，用于计算TBN矩阵
                float2 uv : TEXCOORD0;
                float4 color : COLOR;

            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float4 tangentWS : TEXCOORD4;
                float3 positionOriginalWS: TEXCOORD5;
            };

            float2 WorldPosition2UV( float3 worldPosition , float lengthScale )
            {
                float2 worldUV = worldPosition.xz;
                
                float2 uv = worldUV / lengthScale + float2(0.5,0.5);

                return uv;
            }

            float WorldPosition2Importance( float3 worldPosition , float lengthScale )
            {
                float2 worldUV = worldPosition.xz;
                // 计算worldPos 距离矩形lengthScale 的距离
                
                float2 distance = abs( worldUV / lengthScale);

                float importance = 1;
                if ( distance.x > 0.5 || distance.y > 0.5)
                {
                    importance = 0;
                }

                return importance;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                // 转换到裁剪空间
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                // 转换到世界空间
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 worldTangent = TransformObjectToWorldDir(input.tangentOS.xyz);
                output.tangentWS = float4(worldTangent,input.tangentOS.w);
                // 获取世界空间下的位置
                output.positionWS = TransformObjectToWorld(input.positionOS);
                
                // 计算世界空间下的观察方向
                output.viewDirWS = GetWorldSpaceViewDir(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _NormalMap);

                output.color = input.color;

                output.positionOriginalWS = output.positionWS;

                return output;
            }
            

            struct TessellationFactors
            {
                 float edge[3] : SV_TessFactor;
                 float inside : SV_InsideTessFactor;
            };    
            
            
            // Patch Constant Function
            float3 PatchConstantFunction(InputPatch<Varyings, 3> patch)
            {
                float3 edgeLengths = float3(
                    distance(patch[0].positionHCS, patch[1].positionHCS),
                    distance(patch[1].positionHCS, patch[2].positionHCS),
                    distance(patch[2].positionHCS, patch[0].positionHCS)
                );
                float3 tessFactors = _TessellationFactor / edgeLengths;
                return tessFactors;
            }
            
            // Patch Constant Function
            TessellationFactors PatchConstantFunction1(InputPatch<Varyings, 3> patch)
            {
                TessellationFactors f;
                // float3 edgeLengths = float3(
                //     distance(patch[0].positionHCS, patch[1].positionHCS),
                //     distance(patch[1].positionHCS, patch[2].positionHCS),
                //     distance(patch[2].positionHCS, patch[0].positionHCS)
                // );
                // float3 tessFactors = _TessellationFactor / edgeLengths;
                float3 edgeCenter0 =  ( patch[0].positionWS + patch[1].positionWS ) * 0.5;
                float3 edgeCenter1 = ( patch[1].positionWS + patch[2].positionWS) * 0.5;
                float3 edgeCenter2 = ( patch[2].positionWS + patch[0].positionWS) * 0.5;


                // float3 center = _WorldSpaceCameraPos;
                float3 center = float3(0,0,0);
                float3 distanceToCenter = float3(
                    distance(center, edgeCenter0),
                    distance(center, edgeCenter1),
                    distance(center, edgeCenter2)
                );

                float3 tessFactors = max( 3.0 , _TessellationFactor * exp( - distanceToCenter * 0.02 ));

                
                #if _EXTENDED_WAVE
                f.edge[0] = 1.0;
                f.edge[1] = 1.0;
                f.edge[2] = 1.0;
                f.inside = 1.0;

                #else
                f.edge[0] = tessFactors.x;
                f.edge[1] = tessFactors.y;
                f.edge[2] = tessFactors.z;
                f.inside = (tessFactors.x + tessFactors.y + tessFactors.z) / 3.0;

                #endif

                return f;
            }
             
            // Hull Shader
            [domain("tri")]
            [partitioning("fractional_odd")]
            [outputtopology("triangle_cw")]
            [patchconstantfunc("PatchConstantFunction1")]
            [outputcontrolpoints(3)]
            Varyings hull(InputPatch<Varyings, 3> patch, uint id : SV_OutputControlPointID)
            {
                return patch[id];
            }


            // Domain Shader
            [domain("tri")]//Hull着色器和Domain着色器都作用于相同的域，即三角形。我们通过domain属性再次发出信号
            Varyings domain(TessellationFactors factors, OutputPatch<Varyings, 3> patch, float3 barycentricCoordinates : SV_DomainLocation)
            {
                Varyings output;
                output.positionHCS = patch[0].positionHCS * barycentricCoordinates.x +
                                     patch[1].positionHCS * barycentricCoordinates.y +
                                     patch[2].positionHCS * barycentricCoordinates.z;

                output.normalWS = normalize(patch[0].normalWS * barycentricCoordinates.x +
                                            patch[1].normalWS * barycentricCoordinates.y +
                                            patch[2].normalWS * barycentricCoordinates.z);

                output.viewDirWS = normalize(patch[0].viewDirWS * barycentricCoordinates.x +
                                             patch[1].viewDirWS * barycentricCoordinates.y +
                                             patch[2].viewDirWS * barycentricCoordinates.z);

                output.uv = patch[0].uv * barycentricCoordinates.x +
                            patch[1].uv * barycentricCoordinates.y +
                            patch[2].uv * barycentricCoordinates.z;

                output.tangentWS = normalize(patch[0].tangentWS * barycentricCoordinates.x +
                                             patch[1].tangentWS * barycentricCoordinates.y +
                                             patch[2].tangentWS * barycentricCoordinates.z);

                // output.bitangentWS = normalize(patch[0].bitangentWS * barycentricCoordinates.x +
                //                                patch[1].bitangentWS * barycentricCoordinates.y +
                //                                patch[2].bitangentWS * barycentricCoordinates.z);

                output.positionWS = patch[0].positionWS * barycentricCoordinates.x +
                                  patch[1].positionWS * barycentricCoordinates.y +
                                  patch[2].positionWS * barycentricCoordinates.z;

                                  
                output.positionOriginalWS = patch[0].positionOriginalWS * barycentricCoordinates.x +
                                  patch[1].positionOriginalWS * barycentricCoordinates.y +
                                  patch[2].positionOriginalWS * barycentricCoordinates.z;

                float3 worldPos = output.positionWS.xyz;
                float3 vertexPos = output.positionHCS.xyz;
                                  
                float3 viewVector = _WorldSpaceCameraPos.xyz - worldPos;
                float viewDist = length(viewVector);

                // float lod_c0 = min(_LOD_scale * _LengthScale0 / viewDist, 1);
                // float lod_c1 = min(_LOD_scale * _LengthScale1 / viewDist, 1);
                // float lod_c2 = min(_LOD_scale * _LengthScale2 / viewDist, 1);
                
                float3 displacement = float3(0,0,0);
                float3 normal = float3(0,0.01,0);

                #if defined(LOD0) 
                
                float2 uv0 = WorldPosition2UV(output.positionWS,_LengthScale0);
                float importance0 = WorldPosition2Importance(output.positionWS,_LengthScale0);

                displacement += SAMPLE_TEXTURE2D_LOD(
                    _Displacement_c0, 
                    sampler_Displacement_c0, 
                    uv0,0) * importance0;

                normal += normalize( SAMPLE_TEXTURE2D_LOD(
                    _Normal_c0, 
                    sampler_Normal_c0, 
                    uv0,0)) * importance0;
                #endif
                
                #if defined(LOD0) || defined(LOD1)
                
                float2 uv1 = WorldPosition2UV(output.positionWS,_LengthScale1);
                float importance1 = WorldPosition2Importance(output.positionWS,_LengthScale1);

                displacement += SAMPLE_TEXTURE2D_LOD(
                    _Displacement_c1, 
                    sampler_Displacement_c1, 
                    uv1,0) * importance1;

                normal += normalize(SAMPLE_TEXTURE2D_LOD(
                    _Normal_c1, 
                    sampler_Normal_c1, 
                    uv1,0)) * importance1;
                #endif

                
                #if defined(LOD0) || defined(LOD1) || defined(LOD2)
                
                float2 uv2 = WorldPosition2UV(output.positionWS,_LengthScale2);
                float importance2 = WorldPosition2Importance(output.positionWS,_LengthScale2);

                displacement += SAMPLE_TEXTURE2D_LOD(
                    _Displacement_c2, 
                    sampler_Displacement_c2, 
                    uv2,0) * importance2;

                normal += normalize(SAMPLE_TEXTURE2D_LOD(
                    _Normal_c2, 
                    sampler_Normal_c2, 
                    uv2,0)) * importance2;
                #endif
                 
                worldPos = float3( worldPos.x + displacement.x , displacement.y , worldPos.z + displacement.z);
                output.normalWS = normalize(normal);
                output.positionWS = worldPos;
                output.positionHCS = TransformWorldToHClip(worldPos);
                

                return output;
            }


            // Fresnel Schlick 近似 (用于水)
            float3 FresnelSchlick(float cosTheta, float3 F0)
            {
                return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
            }

            // Normal Distribution Function (GGX/Trowbridge-Reitz)
            float DistributionGGX(float NdotH, float roughness)
            {
                float a = roughness * roughness;
                float a2 = a * a;
                float NdotH2 = NdotH * NdotH;
                float numerator = a2;
                float denominator = NdotH2 * (a2 - 1.0) + 1.0;
                denominator = UNITY_PI * denominator * denominator;
                return numerator / denominator;
            }

            // Geometry Function (Smith's method with GGX)
            float GeometrySchlickGGX(float NdotV, float roughness)
            {
                float r = (roughness + 1.0);
                float k = (r * r) / 8.0;
                float num = NdotV;
                float denom = NdotV * (1.0 - k) + k;
                return num / denom;
            }

            float GeometrySmith(float NdotV, float NdotL, float roughness)
            {
                float ggx1 = GeometrySchlickGGX(NdotV, roughness);
                float ggx2 = GeometrySchlickGGX(NdotL, roughness);
                return ggx1 * ggx2;
            }
             
            half3 WaterDiffuse( half3 posW )
            {
                float waterDepth = min( 1.0 , max( 0 , posW.y - _DiffuseRange.x ) / ( _DiffuseRange.y - _DiffuseRange.x)  );
                return lerp( _DiffuseColor0 , _DiffuseColor1, waterDepth );
            }

            half GetSimpleSSSIndensity( float3 worldPos )
            {
                return min( 1.0 , max( 0 , worldPos.y - _DiffuseRange.x ) / ( _DiffuseRange.y - _DiffuseRange.x)  );
            }

            half GetSSSIndensity(float4 color)
            {
                return color.r;
            }

            half3 SimpleSSS( float3 viewDir , float3 lightDir , float3 lightColor, float sssIndensity)
            {
                float v = abs(viewDir.y);

                half towardsSun = pow(max(0., dot(lightDir, -viewDir)),_SubSurfaceSunFallOff);


                half3 subsurface = (_SubSurfaceBase + _SubSurfaceSun * towardsSun) *_SubSurfaceColor.rgb * lightColor.rgb;

                subsurface *= (1.0 - v * v) * sssIndensity;

                return subsurface;
            }

            half3 ClampFinalLight(float3 specular , float3 viewDir)
            {
                float3 clampedSpecular = max(specular,0);

                float depth = length(viewDir) * 0.005;
                float maxIntensity = depth * 500.0 * depth + 1.0;
                float inverseIntensity = 1.0 / maxIntensity;

                clampedSpecular =  saturate( specular * inverseIntensity) * maxIntensity;

                return clampedSpecular;

            }
             

            float3 CalculateNormalWithDetail( float2 uv,  float3 normalWS, float4 tangentWS )
            {
                // 采样法线贴图并转换至世界空间
                // 获取时间
                float time = _Time.y;
                // 计算法线贴图偏移
                float2 normalUV1 = uv + _DetailNormalVelocity.xy * time;
                float2 normalUV2 = uv + _DetailNormalVelocity.zw * time;
                float3 normalTex1 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap,normalUV1));
                float3 normalTex2 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap,normalUV2));
                float3 biTangent = cross(normalWS, tangentWS.xyz) * tangentWS.w; 
                float3 normalDetail1 = normalize(
                    normalTex1.x * tangentWS.xyz +
                    normalTex1.y * biTangent +
                    normalTex1.z * normalWS
                );
                float3 normalDetail2 = normalize(
                    normalTex2.x * tangentWS.xyz +
                    normalTex2.y * biTangent +
                    normalTex2.z * normalWS
                );

                float3 normalDetailed = normalize(normalWS + ( normalDetail1 + normalDetail2 * 0.5) * _DetailNormalStrength);
                
                return normalDetailed;
            }


            half4 frag(Varyings input) : SV_Target
            {

                float2 uv = WorldPosition2UV(input.positionOriginalWS,_LengthScale0);

                float3 normalDetailed = CalculateNormalWithDetail(uv,input.normalWS,input.tangentWS);

                // float4 d = SAMPLE_TEXTURE2D_LOD(
                //     _Displacement_c0, 
                //     sampler_Displacement_c0, 
                //     uv,0);

                // float4 n = SAMPLE_TEXTURE2D_LOD(
                //     _Normal_c0, 
                //     sampler_Normal_c0, 
                //     uv,0) ;

                // d.y = saturate( d.y * 0.5 + 1.0 );
                // n = normalize(n);
                // return half4( n.z,0 ,0  ,0);

                // half sssIndensity = GetSSSIndensity(input.color) * 0.05;
                half sssIndensity = GetSimpleSSSIndensity(input.positionWS);

                // 获取当前帧的光照数据 (方向光)
                Light light = GetMainLight();

                float3 N = normalDetailed;
                float3 V = normalize(input.viewDirWS);

                
                // #if _EXTENDED_WAVE
                // view dir 的计算有误差
                    V = normalize(_WorldSpaceCameraPos -  input.positionWS );
                // #endif

                float3 L = normalize(light.direction);
                float3 H = normalize(V + L); // 半角向量


                float3 albedo = _Color.rgb;

                float roughness = 1.0 - _Smoothness; // 粗糙度是光滑度的反面
                float3 F0 = _SpecularColor.rgb; // 水的基础反射率

                // 计算 BRDF 项
                float NdotL = max(dot(N, L), 0.001); // 防止除零
                float NdotV = max(dot(N, V), 0.001);
                float NdotH = max(dot(N, H), 0.0);
                float VdotH = max(dot(V, H), 0.0);

                float3 F = FresnelSchlick(VdotH, F0);
                float D = DistributionGGX(NdotH, roughness);
                float G = GeometrySmith(NdotV, NdotL, roughness);

                float3 numerator = D * G * F;
                float denominator = 4.0 * NdotV * NdotL;

                float3 specular = numerator / denominator;
                // 把镜面反射的强度限制在20以适配bloom
                // specular = saturate( specular * 0.05 ) * 20.0;
                

                // return half4(specular,1.0);

                half3 waterDiffuse = WaterDiffuse(input.positionWS) * albedo;

                // 计算漫反射 (这里我们用一个非常简单的 Lambert，实际水几乎没有漫反射)
                float3 kD = float3(1.0,1.0,1.0) - F; // 能量守恒: 漫反射 = 1 - 反射
                float3 diffuse = kD * waterDiffuse / UNITY_PI;


                // 最终光照 = (漫反射 + 镜面反射) * 光照强度
                float3 radiance = light.color * light.shadowAttenuation; // 简化光照
                float3 Lo = (diffuse + specular) * radiance * NdotL;

                Lo = ClampFinalLight(Lo,input.viewDirWS);

                // 添加环境光 (天光) - 使用 Unity 的环境光照
                float3 ambient = 0.3 * waterDiffuse; // 简化环境光，实际应使用 SH 或 Cubemap
                
                // 添加环境光 (天光) - 使用 Environment Map
                float mipLevel = roughness * UNITY_SPECCUBE_LOD_STEPS; // 根据粗糙度选择适当的Mip级别
                float3 reflectionVector = reflect(-V, normalDetailed); // 反射向量
                float3 environmentLight = SAMPLE_TEXTURECUBE_LOD(_EnvironmentMap, sampler_EnvironmentMap, reflectionVector, mipLevel).rgb;
                float3 environmentSpecular = environmentLight * _SpecularColor.rgb * _EnvironmentSpecularFallOff;


                // float3 ambient = SampleSH(normalize(input.normalWS)) * albedo;
                half3 sss = SimpleSSS(V , L , light.color , sssIndensity );

                float3 color = Lo + ambient + sss +  environmentSpecular;

                // 应用菲涅尔效应到整体反射强度 (视角依赖)
                float fresnel = FresnelSchlick(NdotV, F0).r; // 取一个分量
                color = lerp(waterDiffuse, color, fresnel); // 视角越斜，反射越强

                // show tips Color 
                color += _TipsColor.xyz;

                
                

                return half4(color, 1.0);
            }
            ENDHLSL
        }


        /*
        // This pass is used when drawing to a _CameraNormalsTexture texture
        Pass
        {
            Name "DepthNormals"
            Tags{"LightMode" = "DepthNormals"}

            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }
        */

    }
    FallBack "Diffuse"
}