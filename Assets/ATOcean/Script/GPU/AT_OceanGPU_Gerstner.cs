using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ATOcean
{
    public class AT_OceanGPU_Gerstner : AT_OceanGPU
    {
        [BoxGroup("AT_Ocean")]
        [BoxGroup("AT_Ocean/Gerstner")]
        [InlineEditor]
        public List<AT_OceanWaveData> waveData;

        [BoxGroup("AT_Ocean/Gerstner")]
        [ReadOnly]
        public List<ATO_GerstnerWaveCascade> waveCascade;


        [BoxGroup("AT_Ocean/Gerstner")]
        [SerializeField]
        ComputeShader gerstnerWaveShader;


        [BoxGroup("AT_Ocean/Gerstner")]
        [ReadOnly]
        public float timer;


        [BoxGroup("AT_Ocean/Debug")]
        public bool visualizeRT;

        [BoxGroup("AT_Ocean/Visual")]
        public ATO_Visual visual;


        #region Function

        [BoxGroup("AT_Ocean/Gerstner")]
        [HorizontalGroup("AT_Ocean/Gerstner/Buttons", 0.5f)]
        [GUIColor(1.0f, 1.0f, 0.2f)]
        [Button(ButtonSizes.Medium)]
        void CreateNewWaveData()
        {
            waveData = new List<AT_OceanWaveData>();

            for (int i = 0; i < renderCascades.Count; i++)
            {
                waveData.Add(AT_OceanUtiliy.GernateWaveData());
            }
            // waveData = AT_OceanUtiliy.GernateWaveData();
        }


        public static string TempTexturePath = "Assets/ATOcean/Result/GersterGPU";

        public static string Displacement = "Displacement";
        public static string Normal = "Normal";


        [BoxGroup("AT_Ocean/Gerstner")]
        [HorizontalGroup("AT_Ocean/Gerstner/Buttons")]
        [GUIColor(1.0f, 1.0f, 0.2f)]
        [Button(ButtonSizes.Medium)]
        public void RandomizeWaveData()
        {
            for( int i = 0; i < waveData.Count; ++ i )
            {
                waveData[i].SetupWaves();
            }
        }

        [BoxGroup("AT_Ocean/Gerstner")]
        [GUIColor(1.0f, 1.0f, 0.2f)]
        [Button(ButtonSizes.Medium)]
        void GenerateDisplaceNormalMap()
        {
            // init cascade 
            var tempCascade = new ATO_GerstnerWaveCascade(
                renderResolution,
                gerstnerWaveShader,
                renderCascades[0].lengthScale,
                waveData[0]

                );

            float time = 0.0f;
            tempCascade.RunOnce(
                time
                );

            var tex2D = AT_OceanUtiliy.SaveTextureToDisk(
                tempCascade.DisplacementRT,
                TempTexturePath,
                Displacement,
                (int)renderCascades[0].lengthScale
                );
#if UNITY_EDITOR
            UnityEditor.EditorGUIUtility.PingObject(tex2D);
#endif

            AT_OceanUtiliy.SaveTextureToDisk(
                tempCascade.NormalRT,
                TempTexturePath,
                Normal,
                (int)renderCascades[0].lengthScale
                );


        }


        [BoxGroup("AT_Ocean/Export")]
        public string outputDirectory = "Assets/ATOcean/Output";

        [BoxGroup("AT_Ocean/Export")]
        public string folderName = "Temp001";

        [BoxGroup("AT_Ocean/Export")]
        public string fileName = "Gerstner";

        [BoxGroup("AT_Ocean/Export")]
        [MinMaxSlider(0, 10000, true)]
        [InfoBox("The time range to export")]
        public Vector2 frameRange = new Vector2(0,100);

        [BoxGroup("AT_Ocean/Export")]
        [DisplayAsString]
        [ShowInInspector]
        string realTimeRange => $"{frameRange.x * frameDeltaTime} ~ {frameRange.y * frameDeltaTime}";

        
        [BoxGroup("AT_Ocean/Export")]
        [Min(0.0001f)]
        public float frameDeltaTime = 0.016f;
        
        [BoxGroup("AT_Ocean/Export")]
        [GUIColor(0.2f, 1.0f, 0.9f)]
        [Button("UpdateFolderName",ButtonSizes.Large)]
        public void UpdateFolderName()
        {
            folderName = "Gerstner_" + System.DateTime.Now.ToString("MM-dd_HH-mm-ss");

        }


        [BoxGroup("AT_Ocean/Export")]
        [GUIColor(0.2f, 1.0f, 0.9f)]
        [Button("Export",ButtonSizes.Large)]
        public void Export()
        {
            // 设置输出文件夹
            var outputFolder = outputDirectory + "/" + folderName;
            if ( Directory.Exists(outputFolder) )
            {
                // create a new folder
                UpdateFolderName();
                outputFolder = outputDirectory + "/" + folderName;
            }
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }


            // 在frameRange内循环导出
            for (int i = (int)frameRange.x; i < (int)frameRange.y; ++i)
            {
                var time = i * frameDeltaTime ;

                // run cascade 0 
                waveCascade[0].Run(time);

                // save and export 
                AT_OceanUtiliy.SaveTextureToDisk(
                    waveCascade[0].DisplacementRT,
                    outputFolder + "/" + fileName + "_Displacement_" + i.ToString("00000")
                    );

                AT_OceanUtiliy.SaveTextureToDisk(
                    waveCascade[0].NormalRT,
                    outputFolder + "/" + fileName + "_Normal_" + i.ToString("00000")
                    );

            }            

        }



        #endregion

        #region Cascades
        public void CreateCascades()
        {
            waveCascade = new List<ATO_GerstnerWaveCascade>();

            for (int i = 0; i < renderCascades.Count; i++)
            {
                waveCascade.Add(
                    new ATO_GerstnerWaveCascade(
                    (int)renderCascades[i].renderResolution,
                    gerstnerWaveShader,
                    renderCascades[i].lengthScale,
                    waveData[i]
                    ));
            }
        }

        public void InitCascades()
        {
            for (int i = 0; i < waveCascade.Count; i++)
            {
                waveCascade[i].Init();
            }
        }

        public void LinkTextures()
        { 
            for ( int i = 0; i < renderCascades.Count; i++)
            {
                for (int lod = 0; lod < cascadeLevel; ++lod)
                {
                    renderCascades[i].material.SetTexture("_Displacement_c" + lod, waveCascade[lod].DisplacementRT);
                    renderCascades[i].material.SetTexture("_Normal_c" + lod, waveCascade[lod].NormalRT);
                }
            }
        }

        #endregion

        #region Visual 


        public void UpdateVisual()
        {
            if (visual == null)
            {
                visual = transform.GetComponentInChildren<ATO_Visual>();
            }
            if (visual != null)
            {
                if (Input.GetKeyDown(KeyCode.G))
                {
                    visualizeRT  = !visualizeRT;
                }


                if (visualizeRT)
                {
                    visual.Show();
                }
                else
                {
                    visual.Hide();
                }
            }

        }

        public void InitVisual()
        {
            if (visual == null)
            {
                visual = transform.GetComponentInChildren<ATO_Visual>();
            }
            if (visual != null)
            {
                CleanVisual();

                for ( int i = 0; i < renderCascades.Count; i++)
                {
                    visual.AddImageRef(
                        waveCascade[i].DisplacementRT,
                        "Displacement",
                        i,
                        renderCascades[i].renderResolution
                        );
                    visual.AddImageRef(
                        waveCascade[i].NormalRT,
                        "Normal",
                        i,
                        renderCascades[i].renderResolution
                        );
                }

            }

            }

        public void CleanVisual()
        {
            if (visual == null)
            {
                visual = transform.GetComponentInChildren<ATO_Visual>();
            }
            if (visual != null)
                visual.Clear();

        }
        #endregion


        #region Render


        public override void InitRender()
        {
            base.InitRender();

            CreateCascades();

            InitCascades();

            LinkTextures();

            InitVisual();
        }

        public override void Dipose()
        {
            base.Dipose();

            if (waveCascade != null)
            {
                waveCascade.ForEach(x => x.Dispose());
                waveCascade.Clear();
            }

            CleanVisual();
        }


        public override void UpdateRender()
        {
            base.UpdateRender();

            if ( waveCascade == null || waveCascade.Count == 0)
            {
                CreateCascades();
                InitCascades();
            }

            timer += Time.deltaTime;
            for (int i = 0; i < waveCascade.Count; i++)
            {
                waveCascade[i].Run(timer);
            }

            // LinkTextures();

            UpdateVisual();
        }

        #endregion

        public static int DISPLACEMENT_0_PROP = Shader.PropertyToID("_Displacement_c0");
        public static int DISPLACEMENT_1_PROP = Shader.PropertyToID("_Displacement_c1");
        public static int DISPLACEMENT_2_PROP = Shader.PropertyToID("_Displacement_c2");

        public static int NORMAL_0_PROP = Shader.PropertyToID("_Normal_c0");
        public static int NORMAL_1_PROP = Shader.PropertyToID("_Normal_c1");
        public static int NORMAL_2_PROP = Shader.PropertyToID("_Normal_c2");

        public static int TURBULENCE_0_PROP = Shader.PropertyToID("_Turbulence_c0");
        public static int TURBULENCE_1_PROP = Shader.PropertyToID("_Turbulence_c1");
        public static int TURBULENCE_2_PROP = Shader.PropertyToID("_Turbulence_c2");

    }

}