using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEditor;
using UnityEngine;

namespace ATOcean
{
    public class AT_OceanGPU_FFT : AT_OceanGPU
    {

        [BoxGroup("AT_Ocean")]
        [BoxGroup("AT_Ocean/Settings")]
        public int renderResolution = 256;


        [BoxGroup("AT_Ocean")]
        [BoxGroup("AT_Ocean/Settings")]
        [ReadOnly]
        public int materialLevel = 3;

        [BoxGroup("AT_Ocean/Settings")]
        [InlineEditor]
        public ATO_FFTOceanData waveSettings;

        [BoxGroup("AT_Ocean/Cascade")]
        [ReadOnly]
        public List<ATO_FFTOceanCascade> waveCascade;

        [BoxGroup("AT_Ocean/Settings")]
        public bool showLODs;
        [BoxGroup("AT_Ocean/Settings")]
        public bool syncMaterial;

        [BoxGroup("AT_Ocean/NoiseTexture")]
        public Texture2D noiseTexture;

        [SerializeField]
        float lengthScale0 = 250;
        [SerializeField]
        float lengthScale1 = 17;
        [SerializeField]
        float lengthScale2 = 5;


        [BoxGroup("AT_Ocean/ComputeShader")]
        [SerializeField]
        ComputeShader initialSpectrumShader;
        [BoxGroup("AT_Ocean/ComputeShader")]
        [SerializeField]
        ComputeShader timeDependentSpectrumShader;
        [BoxGroup("AT_Ocean/ComputeShader")]
        [SerializeField]
        ComputeShader texturesMergerShader;

        [Button]
        public void GenerateNoiseTexture()
        {
            var noise = AT_OceanUtiliy.GenerateNoiseTexture(renderResolution, true);
            noiseTexture = noise;

            EditorGUIUtility.PingObject(noise);
        }

        [Button]
        public void GenerateH0Texture()
        {
            var gaussianNoise = noiseTexture;
            // init cascade 
            var tempCascade = new ATO_FFTOceanCascade(renderResolution, 
                initialSpectrumShader,
                gaussianNoise);

            // run and save data to rt 
            float boundary1 = 2 * Mathf.PI / lengthScale1 * 6f;
            tempCascade.RunInitials(waveSettings, lengthScale0, 0.0001f, boundary1);


            var tex2D = AT_OceanUtiliy.SaveTextureToDisk(
                tempCascade.InitialSpectrum,
                AT_OceanUtiliy.TempTexturePath,
                AT_OceanUtiliy.H0TextureName,
                renderResolution
                );

            EditorGUIUtility.PingObject(tex2D);


            AT_OceanUtiliy.SaveTextureToDisk(
                tempCascade.PrecomputedData,
                AT_OceanUtiliy.TempTexturePath,
                AT_OceanUtiliy.precomputeTextureName,
                renderResolution
                );
        }


        public override void InitMesh()
        {
            base.InitMesh();

            InitMaterial();
        }


        void InitialiseCascades()
        {
            if (waveCascade.Count < 3)
                return;

            float boundary1 = 2 * Mathf.PI / lengthScale1 * 6f;
            float boundary2 = 2 * Mathf.PI / lengthScale2 * 6f;
            waveCascade[0].CalculateInitials(waveSettings, lengthScale0, 0.0001f, boundary1);
            waveCascade[1].CalculateInitials(waveSettings, lengthScale1, boundary1, boundary2);
            waveCascade[2].CalculateInitials(waveSettings, lengthScale2, boundary2, 9999);

            Shader.SetGlobalFloat("LengthScale0", lengthScale0);
            Shader.SetGlobalFloat("LengthScale1", lengthScale1);
            Shader.SetGlobalFloat("LengthScale2", lengthScale2);
        }


        public void InitMaterial()
        {
            waveCascade = new List<ATO_FFTOceanCascade>();


            // var gaussianNoise = AT_OceanUtiliy.GetNoiseTexture(resolution);

            var gaussianNoise = noiseTexture;

            for (int i = 0; i < materialLevel; ++i)
            {
                var lod = new ATO_FFTOceanCascade(renderResolution,
                    initialSpectrumShader,
                    timeDependentSpectrumShader,
                    texturesMergerShader,
                    gaussianNoise
                    );
                lod.InitMaterial(material);
                waveCascade.Add(lod);
            }

            centerMesh.material = waveCascade[0].material;

            for ( int i = 0; i < meshClips.Count; ++ i )
            {
                meshClips[i].renderer.material = waveCascade[ClipLevelToMaterialLevel(i)].material;
            }
        }

        public int ClipLevelToMaterialLevel( int _clipLevel )
        {
            int level = Mathf.FloorToInt(Mathf.Lerp(0, materialLevel, 1f * _clipLevel / clipLevels) );

            Debug.Log("Input " + _clipLevel + " out " + level);

            return level;
        }

        #region Update

        public void Update()
        {
            UpdateMaterial();
        }


        // 常用颜色常量数组
        UnityEngine.Color[] commonColors = new UnityEngine.Color[]
        {
                UnityEngine.Color.red,        // 红色
                UnityEngine.Color.green,      // 绿色
                UnityEngine.Color.blue,       // 蓝色
                UnityEngine.Color.white,      // 白色
                UnityEngine.Color.black,      // 黑色
                UnityEngine.Color.yellow,     // 黄色
                UnityEngine.Color.cyan,       // 青色
                UnityEngine.Color.magenta,    // 洋红色
                UnityEngine.Color.gray,       // 灰色 (0.5, 0.5, 0.5)
                UnityEngine.Color.grey,       // 灰色（英式拼写，同 gray）
                UnityEngine.Color.clear,      // 透明色 (0,0,0,0)
        };


        public void UpdateMaterial()
        {
            if (syncMaterial)
            {
                for (int i = 0; i < waveCascade.Count; ++i)
                {
                    waveCascade[i].material.CopyPropertiesFromMaterial(material);

                }
            }
        }

        #endregion

    }

}