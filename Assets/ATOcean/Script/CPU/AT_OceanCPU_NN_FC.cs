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

    public class AT_OceanCPU_NN_FC : AT_OceanCPU_NN
    {
        public enum NetworkType
        {
            SingleNetwork,
            Batch,
        }

        [BoxGroup("ATOcean/OceanNN/FC")]
        public NetworkType type;

        [BoxGroup("ATOcean/OceanNN/FC")]
        public int batchSize = 1024;

        public override void LoadModel()
        {
            base.LoadModel();

            if ( runtimeModel != null )
            {
                batchSize = runtimeModel.inputs[0].shape[2];

            }
        }


        public override void EvalulateWave(float t, float dt)
        {
            if  ( type == NetworkType.SingleNetwork )
            {
                base.EvalulateWave(t, dt);
            }else{
                int batchCount = Mathf.CeilToInt(1.0f * (resolution * resolution) / batchSize);

                for (int batch  = 0; batch  < batchCount; batch++)
                {
                    float[] inputData = new float[batchSize * 7];

                    for (int k = 0; k < batchSize; k++)
                    {
                        int index = batch * batchSize + k;

                        if (index < resolution * resolution)
                        {

                            var currentIndex = index;
                            var position = vertices[currentIndex];
                            var UnitWidth = domainSize / (resolution - 1);
                            // position to uv
                            var uv = new Vector2(
                                (position.x + domainSize * 0.5f) / UnitWidth / resolution,
                                (position.y + 0.5f) / UnitWidth / resolution);
                            var preVertexData = vertUpdate[currentIndex];
                            var displacement = new Vector4(
                                preVertexData.x - position.x,
                                preVertexData.y - position.y,
                                preVertexData.z - position.z,
                                1.0f
                            );
                            inputData[k * 7 + 0] = displacement.x;
                            inputData[k * 7 + 1] = displacement.y;
                            inputData[k * 7 + 2] = displacement.z;
                            inputData[k * 7 + 3] = displacement.w;
                            inputData[k * 7 + 4] = uv.x;
                            inputData[k * 7 + 5] = uv.y;
                            inputData[k * 7 + 6] = dt;
                        }
                        else
                        {
                            inputData[k * 7 + 0] = 0.0f;
                            inputData[k * 7 + 1] = 0.0f;
                            inputData[k * 7 + 2] = 0.0f;
                            inputData[k * 7 + 3] = 0.0f;
                            inputData[k * 7 + 4] = 0.0f;
                            inputData[k * 7 + 5] = 0.0f;
                            inputData[k * 7 + 6] = 0.0f;
                        }

                    }

                    Tensor inputTensor = new Tensor( batchSize,1, 7, 1, inputData, inputName);
                    worker.Execute(inputTensor);
                    var outputTensor = worker.PeekOutput(outputName);
                    float[] outputData = outputTensor.AsFloats();

                    for (int k = 0; k < batchSize; k++)
                    {
                        int index = batch * batchSize + k;
                        if (index < resolution * resolution)
                        {
                            var currentIndex = index;
                            var position = vertices[currentIndex];

                            vertUpdate[currentIndex] = new Vector3(
                                position.x + outputData[k * 4 + 0],
                                position.y + outputData[k * 4 + 1],
                                position.z + outputData[k * 4 + 2]
                            );
                        }
                    }

                    inputTensor.Dispose();
                    outputTensor.Dispose();
                } // batch

                mesh.SetVertices(vertUpdate);
                mesh.SetNormals(normals);
                mesh.SetColors(colors);

                if ( recalculateNormal )
                    mesh.RecalculateNormals();
            }
        }


        public override void EvaluateMesh(int i, int j, float t, float dt)
        {

            if (worker == null)
                return;

            var currentIndex = GetCurrentIndex(i, j);
            try
            {
                var position = vertices[currentIndex];

                if ( i == 2 && j == 2 )
                {
                    Debug.Log("Position : " + position);
                }

                var UnitWidth = domainSize / (resolution - 1);
                // position to uv
                var uv = new Vector2( 
                    (position.x + domainSize * 0.5f) / UnitWidth / resolution  , 
                    (position.y + 0.5f ) / UnitWidth / resolution);

                var preVertexData = vertUpdate[currentIndex];


                var displacement = new Vector4(
                    preVertexData.x - position.x,
                    preVertexData.y - position.y,
                    preVertexData.z - position.z,
                    1.0f
                );

                inputData = new float[7];
                inputData[0] = displacement.x;
                inputData[1] = displacement.y;
                inputData[2] = displacement.z;
                inputData[3] = displacement.w;
                inputData[4] = uv.x;
                inputData[5] = uv.y;
                inputData[6] = dt;


                Tensor inputTensor = new Tensor(1, 1 , 7 , 1 , inputData, inputName);


                worker.Execute(inputTensor);

                Tensor outputTensor = worker.PeekOutput(outputName);

                if (outputTensor == null)
                {
                    Debug.LogError("Output tensor is null");
                    return;
                }


                float[] outDisplacement = outputTensor.AsFloats();


                vertUpdate[currentIndex] = new Vector3(
                    position.x + outDisplacement[0],
                    position.y + outDisplacement[1],
                    position.z + outDisplacement[2]
                );

                inputTensor.Dispose();
                outputTensor.Dispose();


            }
            catch (Exception e)
            {
                Debug.LogError("Error Tensor: " + e.Message);
            }

        }
    }

}