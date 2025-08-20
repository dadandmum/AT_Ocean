using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;
using SharpEXR;
using Unity.VisualScripting;

namespace ATOcean
{

    public class AT_OceanCPU_NN : AT_OceanCPU
    {
        [BoxGroup("ATOcean/OceanNN")]
        public NNModel modelAsset;

        [BoxGroup("ATOcean/OceanNN")]
        public string inputName = "input";

        [BoxGroup("ATOcean/OceanNN")]
        public string outputName = "output";


        [BoxGroup("ATOcean/OceanNN")]
        public WorkerFactory.Type workerType = WorkerFactory.Type.CSharp;

        [BoxGroup("ATOcean/OceanNN")]
        [ReadOnly]
        public Model runtimeModel;



        [BoxGroup("ATOcean/OceanNN")]
        [ReadOnly]
        public IWorker worker;

        [BoxGroup("ATOcean/OceanNN")]
        [ReadOnly]
        public float[] inputData = new float[7];


        [BoxGroup("ATOcean/OceanNN")]
        [ReadOnly]
        [HideInInspector]
        public float[] initData = null;



        [BoxGroup("ATOcean/OceanNN")]
        public Texture initTex;

        [BoxGroup("ATOcean/OceanNN")]
        [Button]
        public void SetupByInitTex()
        {
            if (initTex == null)
                return;

            int width = (int)initTex.width;
            int height = (int)initTex.height;

#if UNITY_EDITOR
            if ( !Application.isPlaying)
            {

                float[] datas = new float[width * height * 4];
                // get file path of initText
                var assetPath = UnityEditor.AssetDatabase.GetAssetPath(initTex);
                // turn to absolute path
                var absolutePath = Application.dataPath + "/" + assetPath.Replace("Assets/", "");

                // load texture
                var exrFile = EXRFile.FromFile(absolutePath);
                // get texture data
                var part = exrFile.Parts[0];
                part.Open(absolutePath);

                datas = part.GetFloats(ChannelConfiguration.RGB, true, GammaEncoding.Linear, true);

                part.Close();

                // copy data to initData
                initData = new float[width * height * 4];
                Array.Copy(datas, initData, datas.Length);
            }

#endif 



            // set the vertex 
            for (int i = 0; i < resolution; i++)
            {
                for (int j = 0; j < resolution; j++)
                {
                    // convert to uv
                    var uv = new Vector2(
                        (i + 0.5f) / resolution,
                        (j + 0.5f) / resolution
                    );

                    // convert to image data 
                    var ii = (int)(uv.x * width);
                    var jj = (int)(uv.y * height);
                    var index = (ii + jj * width) * 4;
                    // get data from image 
                    var displacement = new Vector4(
                        initData[index],
                        initData[index + 1],
                        initData[index + 2],
                        1.0f
                    );


                    int currentIndex = GetCurrentIndex(i, j);
                    // get current vertex
                    var vertex = vertices[currentIndex];
                    // set the offset 
                    vertUpdate[currentIndex] = new Vector3(
                        vertex.x + displacement.x,
                        vertex.y + displacement.y,
                        vertex.z + displacement.z
                    );

                    // TODO: set normal
                }
            }


            mesh.SetVertices(vertUpdate);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);

            if (recalculateNormal)
                mesh.RecalculateNormals();

        }



        virtual public void LoadModel()
        {
            try
            {
                // 加载模型（Barracuda 自动解析 ONNX）
                runtimeModel = ModelLoader.Load(modelAsset);
                Debug.Log("Load Model " + modelAsset.name); 
                Debug.Log("Model Inputs: " + runtimeModel.inputs[0].shape[0] + " " + runtimeModel.inputs[0].shape[1] + " " + runtimeModel.inputs[0].shape[2] + " " + runtimeModel.inputs[0].shape[3]);

                Debug.Log("Model Outputs: " + runtimeModel.outputs[0]);



                // 创建推理 Worker（使用 GPU Compute Shader 后端）
                // 可选：WorkerFactory.Type.CPU, WorkerFactory.Type.ComputePrecompiled, WorkerFactory.Type.Auto
                worker = WorkerFactory.CreateWorker(workerType, runtimeModel);

                Debug.Log("Barracuda model loaded successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to load Barracuda model: " + e.Message);
            }
        }

        public override void InitMesh()
        {
            base.InitMesh();

            LoadModel();

            SetupByInitTex();
        }

    }
}