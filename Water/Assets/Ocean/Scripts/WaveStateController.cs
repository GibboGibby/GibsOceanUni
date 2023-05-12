using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace GibsOcean
{
    public class WaveStateController : MonoBehaviour
    {
        [SerializeField] WaveGen waveGen;
        [SerializeField] private OceanRenderer oceanRenderer;
        
        [SerializeField] private static Material oceanMaterial;
        
        
        //[SerializeField] private WaveSettings notCalm;

        public WaveSettings wsStart;
        public WaveSettings wsEnd;

        [SerializeField] public Material mStart;
        [SerializeField] public Material mEnd;

        [SerializeField] private Material bloodOcean;
        

        [SerializeField] private float totalTime;

        private void Awake()
        {
            //waveGen.UpdateWaveSettings(wsStart);
            
        }


        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.O))
            {
                wsStart = waveGen.GetWaveSettings();
                StartCoroutine(WaveLerp(null, wsEnd));
            }
            
            if (Input.GetKeyDown(KeyCode.P))
            {
                wsStart = waveGen.GetWaveSettings();
                mStart = new Material(oceanRenderer.GetMaterial());
                StartCoroutine(WaveLerp(mEnd, wsEnd));
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                mStart = new Material(oceanRenderer.GetMaterial());
                StartCoroutine(WaveLerp(mEnd));
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                mStart = new Material(oceanRenderer.GetMaterial());
                StartCoroutine(WaveLerp(mEnd, null, 0));
                StartCoroutine(WaveLerp(mEnd, null, 1));
                StartCoroutine(WaveLerp(bloodOcean, null, 2));
            }
        }

        public IEnumerator WaveLerp(Material material = null, WaveSettings waveSettings = null, int matNumber = -1)
        {
            float timeElapsed = 0;
            bool waveSettingsPresent = (waveSettings != null);
            bool materialPresent = (material != null);
            while (timeElapsed < totalTime)
            {
                if (waveSettingsPresent)
                {
                    WaveSettings newWave = WaveSettingsLerp(wsStart, waveSettings, timeElapsed / totalTime);
                    waveGen.UpdateWaveSettings(newWave);
                }

                if (materialPresent)
                {
                    Material mat = new Material( WaveMaterialLerp(mStart, new Material(material), timeElapsed / totalTime));
                    if (matNumber == -1)
                        oceanRenderer.SetMaterials(mat);
                    else
                        oceanRenderer.SetMaterial(mat, matNumber);
                }
                timeElapsed += Time.deltaTime;
                //TODO: change materials as well
                yield return null;
            }

            if (materialPresent)
            {
                if (matNumber == -1)
                    oceanRenderer.SetMaterials(new Material(WaveMaterialLerp(mStart, mEnd, 1f)));
                else
                    oceanRenderer.SetMaterial(new Material(WaveMaterialLerp(mStart, mEnd, 1f)), matNumber);
            }

            if (waveSettingsPresent)
                waveGen.UpdateWaveSettings(wsEnd);
        }

        public static WaveSettings WaveSettingsLerp(WaveSettings input, WaveSettings target, float interpVal)
        {
            WaveSettings temp = ScriptableObject.CreateInstance<WaveSettings>();
            temp.g = input.g + ((target.g - input.g) * interpVal);
            temp.depth = input.depth + ((target.depth - input.depth) * interpVal);
            temp.lambda = input.lambda + ((target.lambda - input.lambda) * interpVal);
            temp.waveSettings.scale = input.waveSettings.scale + ((target.waveSettings.scale - input.waveSettings.scale) * interpVal);
            temp.waveSettings.windSpeed = input.waveSettings.windSpeed + ((target.waveSettings.windSpeed - input.waveSettings.windSpeed) * interpVal);
            temp.waveSettings.windDirection = input.waveSettings.windDirection + ((target.waveSettings.windDirection - input.waveSettings.windDirection) * interpVal);
            temp.waveSettings.fetch = input.waveSettings.fetch + ((target.waveSettings.fetch - input.waveSettings.fetch) * interpVal);
            temp.waveSettings.spreadBlend = input.waveSettings.spreadBlend + ((target.waveSettings.spreadBlend - input.waveSettings.spreadBlend) * interpVal);
            temp.waveSettings.swell = input.waveSettings.swell + ((target.waveSettings.swell - input.waveSettings.swell) * interpVal);
            temp.waveSettings.peakEnhancement = input.waveSettings.peakEnhancement + ((target.waveSettings.peakEnhancement - input.waveSettings.peakEnhancement) * interpVal);
            temp.waveSettings.shortWavesFade = input.waveSettings.shortWavesFade + ((target.waveSettings.shortWavesFade - input.waveSettings.shortWavesFade) * interpVal);
            return temp;
        }

        public static Material WaveMaterialLerp(Material input, Material target, float interpVal)
        {
            Material temp = new Material(input);
            temp.SetColor("_Color", Color.Lerp(input.GetColor("_Color"), target.GetColor("_Color"), interpVal));
            temp.SetColor("_SSSColor", Color.Lerp(input.GetColor("_SSSColor"), target.GetColor("_SSSColor"), interpVal));
            
            temp.SetColor("_FoamColor", Color.Lerp(input.GetColor("_FoamColor"), target.GetColor("_FoamColor"), interpVal));
            temp = ShaderFloatLerp(temp, input, target, "_SSSStrength", interpVal);
            temp = ShaderFloatLerp(temp, input, target, "_SSSScale", interpVal);
            
            temp = ShaderFloatLerp(temp, input, target, "_SSSBase", interpVal);
            temp = ShaderFloatLerp(temp, input, target, "_LOD_scale", interpVal);
            temp = ShaderFloatLerp(temp, input, target, "_MaxGloss", interpVal);
            temp = ShaderFloatLerp(temp, input, target, "_Roughness", interpVal);
            temp = ShaderFloatLerp(temp, input, target, "_RoughnessScale", interpVal);
            temp = ShaderFloatLerp(temp, input, target, "_FoamBiasLOD0", interpVal);
            temp = ShaderFloatLerp(temp, input, target, "_FoamBiasLOD1", interpVal);
            temp = ShaderFloatLerp(temp, input, target, "_FoamBiasLOD2", interpVal);
            temp = ShaderFloatLerp(temp, input, target, "_FoamScale", interpVal);
            temp = ShaderFloatLerp(temp, input, target, "_ContactFoam", interpVal);
            
            return temp;

        }

        public static Material ShaderFloatLerp(Material temp, Material input, Material target, string valueName, float interpVal)
        {
            temp.SetFloat(valueName, input.GetFloat(valueName) + ((target.GetFloat(valueName) - input.GetFloat(valueName)) * interpVal));
            return temp;
        }
    }
}