using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

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

        public class MeshUtil
        {
            public class MeshTransform
            {
                public Transform Transform;
                public MeshRenderer MeshRenderer;

                public MeshTransform(Transform transform, MeshRenderer meshRenderer)
                {
                    Transform = transform;
                    MeshRenderer = meshRenderer;
                }
            }

            public static MeshTransform InstantiateMeshTransform(string name, Material mat, Transform transform, Mesh mesh)
            {
                GameObject gameObject = new GameObject();
                gameObject.name = name;
                gameObject.transform.SetParent(transform);
                gameObject.transform.localPosition = Vector3.zero;
                MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
                meshFilter.mesh = mesh;
                MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = true;
                meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.Camera;
                meshRenderer.material = mat;
                meshRenderer.allowOcclusionWhenDynamic = false;
                return new MeshTransform(gameObject.transform, meshRenderer);
            }

            public static Mesh BuildSkirt(int n, float outerBorderScale)
            {
                Mesh mesh = new Mesh();
                mesh.name = "Skirt";
                CombineInstance[] combine = new CombineInstance[8];
                
                Mesh quad = CreatePlane(1, 1, 1);
                Mesh horizontalStrip = CreatePlane(n, 1, 1);
                Mesh verticalStrip = CreatePlane(1, n, 1);
                
                Vector3 cornerQuadScale = new Vector3(outerBorderScale, 1, outerBorderScale);
                Vector3 midQuadScaleVertical = new Vector3(1f / n, 1, outerBorderScale);
                Vector3 midQuadScaleHorizontal = new Vector3(outerBorderScale, 1, 1f / n);
                
                combine[0].transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, cornerQuadScale);
                combine[0].mesh = quad;

                combine[1].transform = Matrix4x4.TRS(Vector3.right * outerBorderScale, Quaternion.identity, midQuadScaleVertical);
                combine[1].mesh = horizontalStrip;

                combine[2].transform = Matrix4x4.TRS(Vector3.right * (outerBorderScale + 1), Quaternion.identity, cornerQuadScale);
                combine[2].mesh = quad;

                combine[3].transform = Matrix4x4.TRS(Vector3.forward * outerBorderScale, Quaternion.identity, midQuadScaleHorizontal);
                combine[3].mesh = verticalStrip;

                combine[4].transform = Matrix4x4.TRS(Vector3.right * (outerBorderScale + 1)
                                                     + Vector3.forward * outerBorderScale, Quaternion.identity, midQuadScaleHorizontal);
                combine[4].mesh = verticalStrip;

                combine[5].transform = Matrix4x4.TRS(Vector3.forward * (outerBorderScale + 1), Quaternion.identity, cornerQuadScale);
                combine[5].mesh = quad;

                combine[6].transform = Matrix4x4.TRS(Vector3.right * outerBorderScale
                                                     + Vector3.forward * (outerBorderScale + 1), Quaternion.identity, midQuadScaleVertical);
                combine[6].mesh = horizontalStrip;

                combine[7].transform = Matrix4x4.TRS(Vector3.right * (outerBorderScale + 1)
                                                     + Vector3.forward * (outerBorderScale + 1), Quaternion.identity, cornerQuadScale);

                combine[7].mesh = quad;
                mesh.CombineMeshes(combine, true);
                return mesh;
            }
            [System.Flags]
            public enum Seams
            {
                None = 0,
                Left = 1,
                Right = 2,
                Top = 4,
                Bottom = 8,
                All = Left | Right | Top | Bottom
            };

            public static Mesh CreatePlane(int x, int y, float lengthScale, Seams seams = Seams.None,
                int triangleShift = 0)
            {
                Mesh mesh = new Mesh();
                mesh.name = "Plane";
                if ((x + 1) * (y + 1) >= 256 * 256)
                    mesh.indexFormat = IndexFormat.UInt32;
                Vector3[] vertices = new Vector3[(x + 1) * (y + 1)];
                int[] triangles = new int[x * y * 2 * 3];
                Vector3[] normals = new Vector3[(x + 1) * (y + 1)];

                for (int i = 0; i < y + 1; i++)
                {
                    for (int j = 0; j < x + 1; j++)
                    {
                        int tempX = j;
                        int tempZ = i;

                        if ((i == 0 && seams.HasFlag(Seams.Bottom)) || (i == y && seams.HasFlag(Seams.Top)))
                            tempX = tempX / 2 * 2;
                        if ((j == 0 && seams.HasFlag(Seams.Left)) || (j == y && seams.HasFlag(Seams.Right)))
                            tempZ = tempZ / 2 * 2;

                        vertices[j + i * (x + 1)] = new Vector3(tempX, 0, tempZ) * lengthScale;
                        normals[j + i * (x + 1)] = Vector3.up;

                    }
                }

                int triangleCount = 0;
                for (int i = 0; i < y; i++)
                {
                    for (int j = 0; j < x; j++)
                    {
                        int k = j + i * (x + 1);
                        if ((i + j + triangleShift) % 2 == 0)
                        {
                            triangles[triangleCount++] = k;
                            triangles[triangleCount++] = k + x + 1;
                            triangles[triangleCount++] = k + x + 2;

                            triangles[triangleCount++] = k;
                            triangles[triangleCount++] = k + x + 2;
                            triangles[triangleCount++] = k + 1;
                        }
                        else
                        {
                            triangles[triangleCount++] = k;
                            triangles[triangleCount++] = k + x + 1;
                            triangles[triangleCount++] = k + 1;

                            triangles[triangleCount++] = k + 1;
                            triangles[triangleCount++] = k + x + 1;
                            triangles[triangleCount++] = k + x + 2;
                        }
                    }
                }

                mesh.vertices = vertices;
                mesh.triangles = triangles;
                mesh.normals = normals;
                return mesh;
            }

            public static Mesh CreateRing(int n, float lengthScale)
            {
                Mesh mesh = new Mesh();
                mesh.name = "Ring";
                if ((2 * n + 1) * (2 * n + 1) >= 256 * 256)
                    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                
                CombineInstance[] combine = new CombineInstance[4];

                combine[0].mesh = CreatePlane(2 * n, (n - 1) / 2, lengthScale, Seams.Bottom | Seams.Right | Seams.Left);
                combine[0].transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);

                combine[1].mesh = CreatePlane(2 * n, (n - 1) / 2, lengthScale, Seams.Top | Seams.Right | Seams.Left);
                combine[1].transform = Matrix4x4.TRS(new Vector3(0, 0, n + 1 + (n - 1) / 2) * lengthScale, Quaternion.identity, Vector3.one);

                combine[2].mesh = CreatePlane((n - 1) / 2, n + 1, lengthScale, Seams.Left);
                combine[2].transform = Matrix4x4.TRS(new Vector3(0, 0, (n - 1) / 2) * lengthScale, Quaternion.identity, Vector3.one);

                combine[3].mesh = CreatePlane((n - 1) / 2, n + 1, lengthScale, Seams.Right);
                combine[3].transform = Matrix4x4.TRS(new Vector3(n + 1 + (n - 1) / 2, 0, (n - 1) / 2) * lengthScale, Quaternion.identity, Vector3.one);

                mesh.CombineMeshes(combine, true);
                return mesh;
            }

            public static Mesh CreateTrim(int n, float lengthScale)
            {
                Mesh mesh = new Mesh();
                mesh.name = "Trim";
                CombineInstance[] combine = new CombineInstance[2];

                combine[0].mesh = CreatePlane(n + 1, 1, lengthScale, triangleShift: 1);
                combine[0].transform = Matrix4x4.TRS(new Vector3(-n - 1, 0, -1) * lengthScale, Quaternion.identity, Vector3.one);

                combine[1].mesh = CreatePlane(1, n, lengthScale, triangleShift: 1);
                combine[1].transform = Matrix4x4.TRS(new Vector3(-1, 0, -n - 1) * lengthScale, Quaternion.identity, Vector3.one);
                
                mesh.CombineMeshes(combine, true);
                return mesh;
            }
            
            

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
