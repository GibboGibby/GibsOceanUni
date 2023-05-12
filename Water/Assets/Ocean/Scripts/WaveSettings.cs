using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GibsOcean
{

    public struct SpectrumSettings
    {
        public float scale;
        public float angle;
        public float spreadBlend;
        public float swell;
        public float alpha;
        public float peakOmega;
        public float gamma;
        public float shortWavesFade;
    }

    [System.Serializable]
    public struct DisplaySpectrumSettings
    {
        [Range(0, 1)] public float scale;
        public float windSpeed;
        public float windDirection;
        public float fetch;
        [Range(0, 1)] public float spreadBlend;
        [Range(0, 1)] public float swell;
        public float peakEnhancement;
        public float shortWavesFade;
    }
    
    [CreateAssetMenu(fileName ="New Wave Settings", menuName = "Ocean/Wave Settings")]
    public class WaveSettings : ScriptableObject
    {
        public float g;
        public float depth;
        [Range(0, 1)] public float lambda;
        public DisplaySpectrumSettings waveSettings;

        private SpectrumSettings[] spectrum = new SpectrumSettings[1];

        public void SetParametersToShader(ComputeShader shader, int kernelIndex, ComputeBuffer paramsBuffer)
        {
            shader.SetFloat(Utils.ShaderProperties.Gravity, g);
            shader.SetFloat(Utils.ShaderProperties.Depth, depth);

            spectrum[0].scale = waveSettings.scale;
            spectrum[0].angle = waveSettings.windDirection / 180 * Mathf.PI;
            spectrum[0].spreadBlend = waveSettings.spreadBlend;
            spectrum[0].swell = Mathf.Clamp(waveSettings.swell, 0.01f, 1);
            spectrum[0].alpha = (0.076f * Mathf.Pow(waveSettings.windSpeed * waveSettings.fetch / g / g, -0.22f));
            spectrum[0].peakOmega = (22 * Mathf.Pow(waveSettings.windSpeed * waveSettings.fetch / g / g, -0.33f));
            spectrum[0].gamma = waveSettings.peakEnhancement;
            spectrum[0].shortWavesFade = waveSettings.shortWavesFade;
            
            paramsBuffer.SetData(spectrum);
            shader.SetBuffer(kernelIndex, Utils.ShaderProperties.Spectrum, paramsBuffer);
        }
    }
}