using System.Runtime.CompilerServices;
using UnityEngine;

namespace GibsOcean
{
    public class ComputeIFFT : MonoBehaviour
    {
        private const int LOCAL_WORK_GROUPS_X = 8;
        private const int LOCAL_WORK_GROUPS_Y = 8;
        
        readonly int size;
        private readonly int logSize;
        readonly ComputeShader ifftCompute;
        readonly RenderTexture precomputedData;
        
        public ComputeIFFT(int size, ComputeShader ifftCompute)
        {
            this.size = size;
            logSize = (int)Mathf.Log(size, 2);
            this.ifftCompute = ifftCompute;
            Utils.Kernels.DefineIFFTKernels(ifftCompute);

            
            precomputedData = Precompute();
            
            

        }

        RenderTexture Precompute()
        {
            RenderTexture rt = new RenderTexture(logSize, size, 0, RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear);
            rt.filterMode = FilterMode.Point;
            rt.wrapMode = TextureWrapMode.Repeat;
            rt.enableRandomWrite = true;
            rt.Create();
            
            ifftCompute.SetInt(Utils.ShaderProperties.Size, size);
            ifftCompute.SetTexture(Utils.Kernels.Precompute, Utils.ShaderProperties.PrecomputeBuffer, rt);
            ifftCompute.Dispatch(Utils.Kernels.Precompute, logSize, size / 2 / LOCAL_WORK_GROUPS_Y, 1);
            return rt;
        }

        public void IFFT2D(RenderTexture input, RenderTexture buffer, bool outputToInput = false, bool scale = true,
            bool permute = false)
        {
            bool pingPong = false;
            ifftCompute.SetTexture(Utils.Kernels.IFFTHorizontalStep, Utils.ShaderProperties.PrecomputedData, precomputedData);
            ifftCompute.SetTexture(Utils.Kernels.IFFTHorizontalStep, Utils.ShaderProperties.Buffer0, input);
            ifftCompute.SetTexture(Utils.Kernels.IFFTHorizontalStep, Utils.ShaderProperties.Buffer1, buffer);
            for (int step = 0; step < logSize; step++)
            {
                pingPong = !pingPong;
                ifftCompute.SetInt(Utils.ShaderProperties.Step, step);
                ifftCompute.SetBool(Utils.ShaderProperties.PingPong, pingPong);
                ifftCompute.Dispatch(Utils.Kernels.IFFTHorizontalStep, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);
            }
            
            ifftCompute.SetTexture(Utils.Kernels.IFFTVerticalStep, Utils.ShaderProperties.PrecomputedData, precomputedData);
            ifftCompute.SetTexture(Utils.Kernels.IFFTVerticalStep, Utils.ShaderProperties.Buffer0, input);
            ifftCompute.SetTexture(Utils.Kernels.IFFTVerticalStep, Utils.ShaderProperties.Buffer1, buffer);

            for (int step = 0; step < logSize; step++)
            {
                pingPong = !pingPong;
                ifftCompute.SetInt(Utils.ShaderProperties.Step, step);
                ifftCompute.SetBool(Utils.ShaderProperties.PingPong, pingPong);
                ifftCompute.Dispatch(Utils.Kernels.IFFTVerticalStep, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);
            }
            
            if (pingPong && outputToInput)
                Graphics.Blit(buffer, input);
            
            if (!pingPong && !outputToInput)
                Graphics.Blit(input, buffer);

            if (permute)
            {
                ifftCompute.SetInt(Utils.ShaderProperties.Size, size);
                ifftCompute.SetTexture(Utils.Kernels.Permute, Utils.ShaderProperties.Buffer0, outputToInput ? input : buffer);
                ifftCompute.Dispatch(Utils.Kernels.Permute, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);
            }

            if (scale)
            {
                ifftCompute.SetInt(Utils.ShaderProperties.Size, size);
                ifftCompute.SetTexture(Utils.Kernels.Scale, Utils.ShaderProperties.Buffer0, outputToInput ? input : buffer);
                ifftCompute.Dispatch(Utils.Kernels.Scale, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);
            }
        }


    }
}