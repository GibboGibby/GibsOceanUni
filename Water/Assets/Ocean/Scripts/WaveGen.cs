using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GibsOcean
{
    public class WaveGen : MonoBehaviour
    {
        public WaveCascade cascade0;
        public WaveCascade cascade1;
        public WaveCascade cascade2;

        [SerializeField] private int size = 256;
        [SerializeField] private WaveSettings waveSettings;
        [SerializeField] private bool alwaysRecalculateInitialSpectrum;
        [SerializeField] private float lengthScale0 = 250;
        [SerializeField] private float lengthScale1 = 17;
        [SerializeField] private float lengthScale2 = 5;

        [SerializeField] private ComputeShader ifftCompute;
        [SerializeField] private ComputeShader initSpectrumCompute;
        [SerializeField] private ComputeShader timeAmplitudeCompute;
        [SerializeField] private ComputeShader foamSimCompute;
        [SerializeField] private ComputeShader fillTexturesCompute;

        private Texture2D gaussianNoise;
        private ComputeIFFT ifft;
        private Texture2D physicsReadback;

        private void Awake()
        {
            Application.targetFrameRate = -1;
            ifft = new ComputeIFFT(size, ifftCompute);
            gaussianNoise = GetNoiseTexture(size);

            cascade0 = new WaveCascade(size, ifft, gaussianNoise, initSpectrumCompute, timeAmplitudeCompute,
                foamSimCompute,
                fillTexturesCompute);

            cascade1 = new WaveCascade(size, ifft, gaussianNoise, initSpectrumCompute, timeAmplitudeCompute,
                foamSimCompute, fillTexturesCompute);
            
            cascade2 = new WaveCascade(size, ifft, gaussianNoise, initSpectrumCompute, timeAmplitudeCompute,
                foamSimCompute, fillTexturesCompute);

            InitialiseCascades();
            
            physicsReadback = new Texture2D(size, size, TextureFormat.RGBAFloat, false);
        }

        public WaveSettings GetWaveSettings()
        {
            return waveSettings;
        }

        void InitialiseCascades()
        {
            float boundary1 = 2 * Mathf.PI / lengthScale1 * 6f;
            float boundary2 = 2 * Mathf.PI / lengthScale2 * 6f;
            cascade0.CalculateInitialSpectrum(waveSettings, lengthScale0, 0.0001f, boundary1);
            cascade1.CalculateInitialSpectrum(waveSettings, lengthScale1, boundary1, boundary2);
            cascade2.CalculateInitialSpectrum(waveSettings, lengthScale2, boundary2, 9999);

            Shader.SetGlobalFloat("LengthScale0", lengthScale0);
            Shader.SetGlobalFloat("LengthScale1", lengthScale1);
            Shader.SetGlobalFloat("LengthScale2", lengthScale2);
        }

        private void Update()
        {
            if (alwaysRecalculateInitialSpectrum)
                InitialiseCascades();

            cascade0.WaveUpdate(Time.time);
            RequestReadback();
            cascade1.WaveUpdate(Time.time);
            cascade2.WaveUpdate(Time.time);
            
        }

        public void UpdateWaveSettings(WaveSettings newSettings)
        {
            waveSettings = newSettings;
            InitialiseCascades();
        }

        Texture2D GetNoiseTexture(int size)
        {
            string filename = "GaussianNoiseTexture" + size.ToString() + "x" + size.ToString();
            Texture2D noise = Resources.Load<Texture2D>("GaussianNoiseTextures/" + filename);
            return noise ? noise : GenerateNoiseTexture(size, true);
        }

        Texture2D GenerateNoiseTexture(int size, bool saveIntoAssetFile)
        {
            Texture2D noise = new Texture2D(size, size, TextureFormat.RGFloat, false, true);
            noise.filterMode = FilterMode.Point;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    noise.SetPixel(i, j, new Vector4(NormalRandom(), NormalRandom()));
                }
            }

            noise.Apply();
#if UNITY_EDITOR
            if (saveIntoAssetFile)
            {
                string filename = "GaussianNoiseTexture" + size.ToString() + "x" + size.ToString();
                string path = "Assets/Resources/GaussianNoiseTextures/";
                AssetDatabase.CreateAsset(noise, path + filename + ".asset");

            }
#endif
            return noise;
        }

        float NormalRandom()
        {
            return Mathf.Cos(2 * Mathf.PI * Random.value) * Mathf.Sqrt(-2 * Mathf.Log(Random.value));
        }

        private void OnDestroy()
        {
            cascade0.Dispose();
            cascade1.Dispose();
            cascade2.Dispose();
        }

        public float GetWaterHeight(Vector3 position)
        {
            Vector3 displacement = GetWaterDisplacement(position);
            displacement = GetWaterDisplacement(position - displacement);
            displacement = GetWaterDisplacement(position - displacement);

            return GetWaterDisplacement(position - displacement).y;
        }

        public Vector3 GetWaterDisplacement(Vector3 position)
        {
            Color c = physicsReadback.GetPixelBilinear(position.x / lengthScale0, position.z / lengthScale0);
            return new Vector3(c.r, c.g, c.b);
        }

        void RequestReadback()
        {
            AsyncGPUReadback.Request(cascade0.Displacement, 0, TextureFormat.RGBAFloat, OnCompleteReadback);
        }

        void OnCompleteReadback(AsyncGPUReadbackRequest request) => OnCompleteReadback(request, physicsReadback);

        void OnCompleteReadback(AsyncGPUReadbackRequest request, Texture2D result)
        {
            if (request.hasError)
            {
                Debug.LogError("GPU readback error detected.");
                return;
            }
            if (result != null)
            {
                physicsReadback.LoadRawTextureData(request.GetData<Color>());
                physicsReadback.Apply();
            }
        }
    }
}