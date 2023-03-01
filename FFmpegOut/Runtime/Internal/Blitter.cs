// FFmpegOut - FFmpeg video encoding plugin for Unity
// https://github.com/keijiro/KlakNDI

using UnityEngine;
using UnityEngine.Rendering;

namespace FFmpegOut
{
    sealed class Blitter : MonoBehaviour
    {
        #region Factory method

        static readonly System.Type[] _initialComponents = { typeof(Camera), typeof(Blitter) };

        public static GameObject CreateInstance(Camera source)
        {
            var go = new GameObject("Blitter", _initialComponents)
            {
                hideFlags = HideFlags.HideInHierarchy
            };

            var camera = go.GetComponent<Camera>();
            camera.cullingMask = 1 << UILayer;
            camera.targetDisplay = source.targetDisplay;

            var blitter = go.GetComponent<Blitter>();
            blitter._sourceTexture = source.targetTexture;

            return go;
        }

        #endregion

        #region Private members

        // Assuming that the 5th layer is "UI". #badcode
        const int UILayer = 5;

        Texture _sourceTexture;
        Mesh _mesh;
        Material _material;

        void PreCull(Camera camera)
        {
            if (_mesh == null || camera != GetComponent<Camera>()) return;

            Graphics.DrawMesh(
                _mesh, transform.localToWorldMatrix,
                _material, UILayer, camera
            );
        }

        void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            PreCull(camera);
        }

        #endregion

        #region MonoBehaviour implementation

        void Update()
        {
            if (_mesh == null)
            {
                // Index-only triangle mesh
                _mesh = new Mesh
                {
                    vertices = new Vector3[3],
                    triangles = new int[] { 0, 1, 2 },
                    bounds = new Bounds(Vector3.zero, Vector3.one)
                };
                _mesh.UploadMeshData(true);

                // Blitter shader material
                var shader = Shader.Find("Hidden/FFmpegOut/Blitter");
                _material = new Material(shader);
                _material.SetTexture("_MainTex", _sourceTexture);

                // Register the camera render callback.
                RenderPipelineManager.beginCameraRendering += BeginCameraRendering; // SRP
                Camera.onPreCull += PreCull; // Legacy
            }
        }

        void OnDisable()
        {
            if (_mesh != null)
            {
                // Unregister the camera render callback.
                RenderPipelineManager.beginCameraRendering -= BeginCameraRendering; // SRP
                Camera.onPreCull -= PreCull; // Legacy

                // Destroy temporary objects.
                Destroy(_mesh);
                Destroy(_material);
                _mesh = null;
                _material = null;
            }
        }

        #endregion
    }
}
