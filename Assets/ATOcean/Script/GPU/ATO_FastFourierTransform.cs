using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ATO_FastFourierTransform 
{
    const int LOCAL_WORK_GROUPS_X = 8;
    const int LOCAL_WORK_GROUPS_Y = 8;


    readonly int resolution;
    readonly ComputeShader fftShader;
    readonly RenderTexture precomputedData;

    public RenderTexture PrecomputedData => precomputedData;



    public ATO_FastFourierTransform(int resolution, ComputeShader fftShader)
    {
        this.resolution = resolution;
        this.fftShader = fftShader;
        precomputedData = PrecomputeTwiddleFactorsAndInputIndices();

        KERNEL_PRECOMPUTE = fftShader.FindKernel("PrecomputeTwiddleFactorsAndInputIndices");
        KERNEL_HORIZONTAL_STEP_FFT = fftShader.FindKernel("HorizontalStepFFT");
        KERNEL_VERTICAL_STEP_FFT = fftShader.FindKernel("VerticalStepFFT");
        KERNEL_HORIZONTAL_STEP_IFFT = fftShader.FindKernel("HorizontalStepInverseFFT");
        KERNEL_VERTICAL_STEP_IFFT = fftShader.FindKernel("VerticalStepInverseFFT");
        KERNEL_SCALE = fftShader.FindKernel("Scale");
        KERNEL_PERMUTE = fftShader.FindKernel("Permute");
    }

    /// <summary>
    /// Precompute twiddle factors and input indices.
    /// the dimension of precomputed data is [log2(resolution), resolution, 4]
    /// the output is [Wreal, W_imag, input0_index, input1_index]
    /// (see FastFourierTransform.compute )
    /// </summary>
    /// <returns></returns>
    RenderTexture PrecomputeTwiddleFactorsAndInputIndices()
    {
        int logSize = (int)Mathf.Log(resolution, 2);
        RenderTexture rt = new RenderTexture(logSize, resolution, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        rt.filterMode = FilterMode.Point;
        rt.wrapMode = TextureWrapMode.Repeat;
        rt.enableRandomWrite = true;
        rt.Create();

        fftShader.SetInt(PROP_ID_SIZE, resolution);
        fftShader.SetTexture(KERNEL_PRECOMPUTE, PROP_ID_PRECOMPUTE_BUFFER, rt);
        fftShader.Dispatch(KERNEL_PRECOMPUTE, logSize, resolution / 2 / LOCAL_WORK_GROUPS_Y, 1);
        return rt;
    }



    public void FFT2D(RenderTexture input, RenderTexture buffer, bool outputToInput = false)
    {
        // TODO 
    }



    public void IFFT2D(RenderTexture input, RenderTexture buffer, bool outputToInput = false, bool scale = true, bool permute = false)
    {
        int logSize = (int)Mathf.Log(resolution, 2);
        bool pingPong = false;

        fftShader.SetTexture(KERNEL_HORIZONTAL_STEP_IFFT, PROP_ID_PRECOMPUTED_DATA, precomputedData);
        fftShader.SetTexture(KERNEL_HORIZONTAL_STEP_IFFT, PROP_ID_BUFFER0, input);
        fftShader.SetTexture(KERNEL_HORIZONTAL_STEP_IFFT, PROP_ID_BUFFER1, buffer);
        for (int i = 0; i < logSize; i++)
        {
            pingPong = !pingPong;
            fftShader.SetInt(PROP_ID_STEP, i);
            fftShader.SetBool(PROP_ID_PINGPONG, pingPong);
            fftShader.Dispatch(KERNEL_HORIZONTAL_STEP_IFFT, resolution / LOCAL_WORK_GROUPS_X, resolution / LOCAL_WORK_GROUPS_Y, 1);
        }

        fftShader.SetTexture(KERNEL_VERTICAL_STEP_IFFT, PROP_ID_PRECOMPUTED_DATA, precomputedData);
        fftShader.SetTexture(KERNEL_VERTICAL_STEP_IFFT, PROP_ID_BUFFER0, input);
        fftShader.SetTexture(KERNEL_VERTICAL_STEP_IFFT, PROP_ID_BUFFER1, buffer);
        for (int i = 0; i < logSize; i++)
        {
            pingPong = !pingPong;
            fftShader.SetInt(PROP_ID_STEP, i);
            fftShader.SetBool(PROP_ID_PINGPONG, pingPong);
            fftShader.Dispatch(KERNEL_VERTICAL_STEP_IFFT, resolution / LOCAL_WORK_GROUPS_X, resolution / LOCAL_WORK_GROUPS_Y, 1);
        }

        if (pingPong && outputToInput)
        {
            Graphics.Blit(buffer, input);
        }

        if (!pingPong && !outputToInput)
        {
            Graphics.Blit(input, buffer);
        }

        if (permute)
        {
            fftShader.SetInt(PROP_ID_SIZE, resolution);
            fftShader.SetTexture(KERNEL_PERMUTE, PROP_ID_BUFFER0, outputToInput ? input : buffer);
            fftShader.Dispatch(KERNEL_PERMUTE, resolution / LOCAL_WORK_GROUPS_X, resolution / LOCAL_WORK_GROUPS_Y, 1);
        }
        
        if (scale)
        {
            fftShader.SetInt(PROP_ID_SIZE, resolution);
            fftShader.SetTexture(KERNEL_SCALE, PROP_ID_BUFFER0, outputToInput ? input : buffer);
            fftShader.Dispatch(KERNEL_SCALE, resolution / LOCAL_WORK_GROUPS_X, resolution / LOCAL_WORK_GROUPS_Y, 1);
        }
    }


    // Kernel IDs:
    readonly int KERNEL_PRECOMPUTE;
    readonly int KERNEL_HORIZONTAL_STEP_FFT;
    readonly int KERNEL_VERTICAL_STEP_FFT;
    readonly int KERNEL_HORIZONTAL_STEP_IFFT;
    readonly int KERNEL_VERTICAL_STEP_IFFT;
    readonly int KERNEL_SCALE;
    readonly int KERNEL_PERMUTE;



    // Property IDs:
    readonly int PROP_ID_PRECOMPUTE_BUFFER = Shader.PropertyToID("PrecomputeBuffer");
    readonly int PROP_ID_PRECOMPUTED_DATA = Shader.PropertyToID("PrecomputedData");
    readonly int PROP_ID_BUFFER0 = Shader.PropertyToID("Buffer0");
    readonly int PROP_ID_BUFFER1 = Shader.PropertyToID("Buffer1");
    readonly int PROP_ID_SIZE = Shader.PropertyToID("Size");
    readonly int PROP_ID_STEP = Shader.PropertyToID("Step");
    readonly int PROP_ID_PINGPONG = Shader.PropertyToID("PingPong");


}
