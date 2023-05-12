using UnityEngine;

namespace GibsOcean
{
    class Utils
    {
        public static RenderTexture CreateRenderTexture(int size, RenderTextureFormat format = RenderTextureFormat.RGFloat, bool useMips = false)
        {
            RenderTexture rt = new RenderTexture(size, size, 0, format, RenderTextureReadWrite.Linear);
            rt.useMipMap = useMips;
            rt.autoGenerateMips = false;
            rt.anisoLevel = 6;
            rt.filterMode = FilterMode.Trilinear;
            rt.wrapMode = TextureWrapMode.Repeat;
            rt.enableRandomWrite = true;
            rt.Create();
            return rt;
        }
        
        public static class Kernels
        {
            // IFFT
            public static int Precompute;
            public static int IFFTHorizontalStep;
            public static int IFFTVerticalStep;
            public static int Scale;
            public static int Permute;

            // Spectrum
            public static int InitSpectrum;
            public static int ConjugateSpectrum;

            // Calculating Amplitudes
            public static int TimeAmplitudeSpectrum;
            
            // Foam Simulation
            public static int FoamSim;
            public static int FoamSimInit;
            
            //Filling Textures
            public static int FillDisplacementAndDerivatives;
            
            

            public static void DefineIFFTKernels(ComputeShader ifftCompute)
            {
                Precompute = ifftCompute.FindKernel("Precompute");
                IFFTHorizontalStep = ifftCompute.FindKernel("IFFTHorizontalStep");
                IFFTVerticalStep = ifftCompute.FindKernel("IFFTVerticalStep");
                Scale = ifftCompute.FindKernel("Scale");
                Permute = ifftCompute.FindKernel("Permute");
            }

            public static void DefineSpectrumKernels(ComputeShader initSpectrum, ComputeShader timeAmpSpectrum)
            {
                InitSpectrum = initSpectrum.FindKernel("InitSpectrum");
                ConjugateSpectrum = initSpectrum.FindKernel("ConjugateSpectrum");
                TimeAmplitudeSpectrum = timeAmpSpectrum.FindKernel("TimeAmplitudeSpectrum");
            }

            public static void DefineTextureFillingKernels(ComputeShader foamSim,
                ComputeShader displacementAndDerivative)
            {
                FoamSimInit = foamSim.FindKernel("Init");
                FoamSim = foamSim.FindKernel("FoamSim");
                FillDisplacementAndDerivatives = displacementAndDerivative.FindKernel("FillDisplacementAndDerivatives");
            }
        }

        public static class ShaderProperties
        {
            // Propertie IDs for each Kernel
            // IFFT
            public static readonly int PrecomputeBuffer = Shader.PropertyToID("PrecomputeBuffer");
            public static readonly int PrecomputedData = Shader.PropertyToID("PrecomputedData");
            public static readonly int Buffer0 = Shader.PropertyToID("Buffer0");
            public static readonly int Buffer1 = Shader.PropertyToID("Buffer1");
            public static readonly int Size = Shader.PropertyToID("Size");
            public static readonly int Step = Shader.PropertyToID("Step");
            public static readonly int PingPong = Shader.PropertyToID("PingPong");

            public static readonly int DxDz = Shader.PropertyToID("DxDz");
            public static readonly int DyDxz = Shader.PropertyToID("DyDxz");
            public static readonly int DyxDyz = Shader.PropertyToID("DyxDyz");
            public static readonly int DxxDzz = Shader.PropertyToID("DxxDzz");
            // Foam Sim
            public static readonly int DeltaTime = Shader.PropertyToID("DeltaTime");
            public static readonly int FoamDecayRate = Shader.PropertyToID("FoamDecayRate");
            // Fill Displacement and Derivatives
            public static readonly int Lambda = Shader.PropertyToID("Lambda");
            public static readonly int Turbulence = Shader.PropertyToID("Turbulence");

            public static readonly int Displacement = Shader.PropertyToID("Displacement");
            public static readonly int Derivatives = Shader.PropertyToID("Derivatives");
            // Init Spectrum
            public static readonly int Time = Shader.PropertyToID("Time");
            public static readonly int LengthScale = Shader.PropertyToID("LengthScale");
            public static readonly int CutoffHigh = Shader.PropertyToID("CutoffHigh");
            public static readonly int CutoffLow = Shader.PropertyToID("CutoffLow");
            public static readonly int Noise = Shader.PropertyToID("Noise");
            public static readonly int H0 = Shader.PropertyToID("H0");
            public static readonly int H0K = Shader.PropertyToID("H0K");
            public static readonly int WaveData = Shader.PropertyToID("WaveData");
            public static readonly int Gravity = Shader.PropertyToID("GravityAcceleration");
            public static readonly int Depth = Shader.PropertyToID("Depth");
            public static readonly int Spectrum = Shader.PropertyToID("Spectrum");
            
            // Shader itself
            public static readonly int Displacementc0 = Shader.PropertyToID("Displacement0");

        }
    }
}
