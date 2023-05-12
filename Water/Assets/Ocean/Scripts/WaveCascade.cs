using System.Collections;
using System.Collections.Generic;
using GibsOcean;
using UnityEngine;
using UnityEngine.Diagnostics;

namespace GibsOcean
{
    public class WaveCascade : MonoBehaviour
    {
        private const int LOCAL_WORK_GROUPS_X = 8;
        private const int LOCAL_WORK_GROUPS_Y = 8;
        
        // Define properties for the textures
        public RenderTexture Displacement => displacement;
        
        public RenderTexture Derivatives => derivatives;
        public RenderTexture Turbulence => turbulence;

        public Texture2D GaussianNoise => gaussianNoise;
        public RenderTexture WaveData => waveData;
        public RenderTexture InitialSpectrum => initSpectrum;

        private readonly int size;
        private readonly ComputeShader initSpectrumCompute;
        private readonly ComputeShader timeAmplitudeCompute;
        private readonly ComputeShader foamSimCompute;
        private readonly ComputeShader fillTexturesCompute;

        private readonly ComputeIFFT ifft;
        private readonly Texture2D gaussianNoise;
        private readonly ComputeBuffer paramsBuffer;
        private readonly RenderTexture initSpectrum;
        private readonly RenderTexture waveData;

        private readonly RenderTexture buffer;
        private readonly RenderTexture DxDz;
        private readonly RenderTexture DyDxz;
        private readonly RenderTexture DyxDyz;
        private readonly RenderTexture DxxDzz;

        private readonly RenderTexture displacement;
        private readonly RenderTexture derivatives;
        private readonly RenderTexture turbulence;

        private float lambda;

        public WaveCascade(int size, ComputeIFFT ifft, Texture2D gaussianNoise, ComputeShader initSpectrum, ComputeShader timeAmplitude,
            ComputeShader foamSim, ComputeShader fillTextures)
        {
            this.size = size;
            this.ifft = ifft;
            this.gaussianNoise = gaussianNoise;
            initSpectrumCompute = initSpectrum;
            timeAmplitudeCompute = timeAmplitude;
            foamSimCompute = foamSim;
            fillTexturesCompute = fillTextures;
            
            
            
            Utils.Kernels.DefineSpectrumKernels(initSpectrumCompute, timeAmplitudeCompute);
            Utils.Kernels.DefineTextureFillingKernels(foamSimCompute, fillTexturesCompute);

            this.initSpectrum = Utils.CreateRenderTexture(size, RenderTextureFormat.ARGBFloat);
            waveData = Utils.CreateRenderTexture(size, RenderTextureFormat.ARGBFloat);
            displacement = Utils.CreateRenderTexture(size, RenderTextureFormat.ARGBFloat);
            derivatives = Utils.CreateRenderTexture(size, RenderTextureFormat.ARGBFloat, true);
            turbulence = Utils.CreateRenderTexture(size, RenderTextureFormat.ARGBFloat, true);
            Shader.SetGlobalTexture(Utils.ShaderProperties.Displacementc0, displacement);
            
            paramsBuffer = new ComputeBuffer(1, 8 * sizeof(float));

            buffer = Utils.CreateRenderTexture(size);
            DxDz = Utils.CreateRenderTexture(size);
            DyDxz = Utils.CreateRenderTexture(size);
            DyxDyz = Utils.CreateRenderTexture(size);
            DxxDzz = Utils.CreateRenderTexture(size);
            //foamSimCompute.SetTexture(Utils.Kernels.FoamSimInit, Utils.ShaderProperties.Turbulence, turbulence);
            //foamSimCompute.Dispatch(Utils.Kernels.FoamSimInit, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);
        }

        public void CalculateInitialSpectrum(WaveSettings waveSettings, float lengthScale, float cutoffLow,
            float cutoffHigh)
        {
            lambda = waveSettings.lambda;

            initSpectrumCompute.SetInt(Utils.ShaderProperties.Size, size);
            initSpectrumCompute.SetFloat(Utils.ShaderProperties.LengthScale, lengthScale);
            initSpectrumCompute.SetFloat(Utils.ShaderProperties.CutoffHigh, cutoffHigh);
            initSpectrumCompute.SetFloat(Utils.ShaderProperties.CutoffLow, cutoffLow);
            waveSettings.SetParametersToShader(initSpectrumCompute, Utils.Kernels.InitSpectrum, paramsBuffer);
            
            initSpectrumCompute.SetTexture(Utils.Kernels.InitSpectrum, Utils.ShaderProperties.H0K, buffer);
            initSpectrumCompute.SetTexture(Utils.Kernels.InitSpectrum, Utils.ShaderProperties.WaveData, waveData);
            initSpectrumCompute.SetTexture(Utils.Kernels.InitSpectrum, Utils.ShaderProperties.Noise, gaussianNoise);
            initSpectrumCompute.Dispatch(Utils.Kernels.InitSpectrum, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);
            
            initSpectrumCompute.SetTexture(Utils.Kernels.ConjugateSpectrum, Utils.ShaderProperties.H0, initSpectrum);
            initSpectrumCompute.SetTexture(Utils.Kernels.ConjugateSpectrum, Utils.ShaderProperties.H0K, buffer);
            initSpectrumCompute.Dispatch(Utils.Kernels.ConjugateSpectrum, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);
        }


        public void WaveUpdate(float time)
        {
            timeAmplitudeCompute.SetTexture(Utils.Kernels.TimeAmplitudeSpectrum, Utils.ShaderProperties.DxDz, DxDz);
            timeAmplitudeCompute.SetTexture(Utils.Kernels.TimeAmplitudeSpectrum, Utils.ShaderProperties.DyDxz, DyDxz);
            timeAmplitudeCompute.SetTexture(Utils.Kernels.TimeAmplitudeSpectrum, Utils.ShaderProperties.DyxDyz, DyxDyz);
            timeAmplitudeCompute.SetTexture(Utils.Kernels.TimeAmplitudeSpectrum, Utils.ShaderProperties.DxxDzz, DxxDzz);
            timeAmplitudeCompute.SetTexture(Utils.Kernels.TimeAmplitudeSpectrum, Utils.ShaderProperties.H0, initSpectrum);
            timeAmplitudeCompute.SetTexture(Utils.Kernels.TimeAmplitudeSpectrum, Utils.ShaderProperties.WaveData, waveData);
            timeAmplitudeCompute.SetFloat(Utils.ShaderProperties.Time, time);
            timeAmplitudeCompute.Dispatch(Utils.Kernels.TimeAmplitudeSpectrum, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);
            
            ifft.IFFT2D(DxDz, buffer, true, false, true);
            ifft.IFFT2D(DyDxz, buffer, true, false, true);
            ifft.IFFT2D(DyxDyz, buffer, true, false, true);
            ifft.IFFT2D(DxxDzz, buffer, true, false, true);
            
            foamSimCompute.SetFloat(Utils.ShaderProperties.DeltaTime, Time.deltaTime);
            foamSimCompute.SetFloat(Utils.ShaderProperties.Lambda, lambda);
            foamSimCompute.SetTexture(Utils.Kernels.FoamSim, Utils.ShaderProperties.DyDxz, DyDxz);
            foamSimCompute.SetTexture(Utils.Kernels.FoamSim, Utils.ShaderProperties.DxxDzz, DxxDzz);
            foamSimCompute.SetTexture(Utils.Kernels.FoamSim, Utils.ShaderProperties.Turbulence, turbulence);
            foamSimCompute.Dispatch(Utils.Kernels.FoamSim, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);
            
            fillTexturesCompute.SetTexture(Utils.Kernels.FillDisplacementAndDerivatives, Utils.ShaderProperties.DxDz, DxDz);
            fillTexturesCompute.SetTexture(Utils.Kernels.FillDisplacementAndDerivatives, Utils.ShaderProperties.DyDxz, DyDxz);
            fillTexturesCompute.SetTexture(Utils.Kernels.FillDisplacementAndDerivatives, Utils.ShaderProperties.DyxDyz, DyxDyz);
            fillTexturesCompute.SetTexture(Utils.Kernels.FillDisplacementAndDerivatives, Utils.ShaderProperties.DxxDzz, DxxDzz);
            fillTexturesCompute.SetTexture(Utils.Kernels.FillDisplacementAndDerivatives, Utils.ShaderProperties.Displacement, displacement);
            fillTexturesCompute.SetTexture(Utils.Kernels.FillDisplacementAndDerivatives, Utils.ShaderProperties.Derivatives, derivatives);
            fillTexturesCompute.SetFloat(Utils.ShaderProperties.Lambda, lambda);
            fillTexturesCompute.Dispatch(Utils.Kernels.FillDisplacementAndDerivatives, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);
            
            derivatives.GenerateMips();
            turbulence.GenerateMips();
        }
        

        public void Dispose()
        {
            paramsBuffer?.Release();
        }
    }
}