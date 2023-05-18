using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GibsOcean
{
    public class OceanographicSpectra : MonoBehaviour
    {
        private const int LOCAL_WORK_GROUPS_X = 8;
        private const int LOCAL_WORK_GROUPS_Y = 8;
        
        readonly int size;
        
        readonly ComputeShader initSpectrum;
        readonly ComputeShader timeAmplitudeSpectrum;
        
        public OceanographicSpectra(int size, ComputeShader initSpectrum, ComputeShader timeAmplitudeSpectrum)
        {
            this.size = size;
            this.initSpectrum = initSpectrum;
            this.timeAmplitudeSpectrum = timeAmplitudeSpectrum;
            Utils.Kernels.DefineSpectrumKernels(initSpectrum, timeAmplitudeSpectrum);
        }

        public void InitialSpectrumCalculation(WaveSettings waveSettings, float lengthScale, float cutoffLow,
            float cutoffHigh)
        {
            //lambda = waveSettings.lambda;

            initSpectrum.SetInt(Utils.ShaderProperties.Size, size);
            initSpectrum.SetFloat(Utils.ShaderProperties.LengthScale, lengthScale);
            initSpectrum.SetFloat(Utils.ShaderProperties.CutoffHigh, cutoffHigh);
            initSpectrum.SetFloat(Utils.ShaderProperties.CutoffLow, cutoffLow);
            //waveSettings.SetParametersToShader(initSpectrum, Utils.Kernels.InitSpectrum, paramsBuffer);
            
            //initSpectrum.SetTexture(Utils.Kernels.InitSpectrum, Utils.ShaderProperties.H0K, buffer);
            //initSpectrum.SetTexture(Utils.Kernels.InitSpectrum, Utils.ShaderProperties.WaveData, waveData);
            //initSpectrum.SetTexture(Utils.Kernels.InitSpectrum, Utils.ShaderProperties.Noise, gaussianNoise);
            initSpectrum.Dispatch(Utils.Kernels.InitSpectrum, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);
            
            //initSpectrum.SetTexture(Utils.Kernels.ConjugateSpectrum, Utils.ShaderProperties.H0, initSpectrum);
            //initSpectrum.SetTexture(Utils.Kernels.ConjugateSpectrum, Utils.ShaderProperties.H0K, buffer);
            initSpectrum.Dispatch(Utils.Kernels.ConjugateSpectrum, size / LOCAL_WORK_GROUPS_X, size / LOCAL_WORK_GROUPS_Y, 1);
        }
    }
}