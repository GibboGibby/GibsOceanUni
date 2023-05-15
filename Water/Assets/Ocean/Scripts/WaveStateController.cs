using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace GibsOcean
{
    [System.Serializable]
    public struct WaveState
    {
        public string Name;
        public WaveSettings wsEnd;
        public Material matEnd;
    }
    
    public class WaveStateController : MonoBehaviour
    {
        [SerializeField] WaveGen waveGen;
        [SerializeField] private OceanRenderer oceanRenderer;
        
        [SerializeField] private static Material oceanMaterial;

        [SerializeField] private Tuple<WaveSettings, Material> calmState;
        
        
        
        //[SerializeField] private WaveSettings notCalm;

        //public WaveSettings wsStart;
        //public WaveSettings wsEnd;

        //[SerializeField] public Material mStart;
        //[SerializeField] public Material mEnd;

        [SerializeField] private Material bloodOcean;

        [SerializeField]public List<WaveState> WaveStates;
        private int waveStateIndex = 0;
        

        [SerializeField] private float totalTime;

        private void Awake()
        {
            //waveGen.UpdateWaveSettings(wsStart);
            WaveStates state = WaveStates[waveStateIndex];
			StartCoroutine(WaveLerp(state.matEnd, state.wsEnd), -1, 0);
			waveStateIndex++;
			if (waveStateIndex == WaveStates.Count) waveStateIndex = 0;
        }


        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                
                Debug.Log(state.Name);
                waveStateIndex++;
                if (waveStateIndex >= WaveStates.Count) waveStateIndex = WaveStates.Count - 1;
				WaveState state = WaveStates[waveStateIndex];
                StartCoroutine(WaveLerp(state.matEnd, state.wsEnd));
            }
			if (Input.GetKeyDown(KeyCode.T))
			{
				waveStateIndex--;
				if (waveStateIndex < 0) waveStateIndex = 0;
				WaveState state = WaveStates[waveStateIndex];
				StartCoroutine(WaveLerp(state.matEnd, state.wsEnd));
			}
        }

        public IEnumerator WaveLerp(Material material = null, WaveSettings waveSettings = null, int matNumber = -1, float _totalTime = totalTime)
        {
            float timeElapsed = 0;
            bool waveSettingsPresent = (waveSettings != null);
            bool materialPresent = (material != null);
            Material matStart = new Material(oceanRenderer.GetMaterial());
            WaveSettings wsStart = waveGen.GetWaveSettings();
            while (timeElapsed < _totalTime)
            {
                if (waveSettingsPresent)
                {
                    WaveSettings newWave = WaveSettingsLerp(wsStart, waveSettings, timeElapsed / _totalTime);
                    waveGen.UpdateWaveSettings(newWave);
                }

                if (materialPresent)
                {
                    Material mat = new Material( WaveMaterialLerp(matStart, new Material(material), timeElapsed / _totalTime));
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
                    oceanRenderer.SetMaterials(new Material(WaveMaterialLerp(matStart, new Material(material), 1f)));
                else
                    oceanRenderer.SetMaterial(new Material(WaveMaterialLerp(matStart, new Material(material), 1f)), matNumber);
            }
aaaaa
            if (waveSettingsPresent)
                waveGen.UpdateWaveSettings(waveSettings);
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
            temp.SetColor("_Color", Color.Lerp(temp.GetColor("_Color"), target.GetColor("_Color"), interpVal));
            temp.SetColor("_SSSColor", Color.Lerp(temp.GetColor("_SSSColor"), target.GetColor("_SSSColor"), interpVal));
            
            temp.SetColor("_FoamColor", Color.Lerp(temp.GetColor("_FoamColor"), target.GetColor("_FoamColor"), interpVal));
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