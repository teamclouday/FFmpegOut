using System.Collections;
using System.IO;
using UnityEngine;

namespace FFmpegOut
{
    [AddComponentMenu("FFmpegOut/Video Capture"), DefaultExecutionOrder(10000)]
    public sealed class VideoCapture : MonoBehaviour
    {
        #region Public properties

        [SerializeField] int _width = 1920;

        public int width
        {
            get { return _width; }
            set { _width = value; }
        }

        [SerializeField] int _height = 1080;

        public int height
        {
            get { return _height; }
            set { _height = value; }
        }

        [SerializeField] FFmpegPreset _preset;

        public FFmpegPreset preset
        {
            get { return _preset; }
            set { _preset = value; }
        }

        [SerializeField] float _frameRate = 60;

        public float frameRate
        {
            get { return _frameRate; }
            set { _frameRate = value; }
        }

        [SerializeField] string _folderPath = "";

        public string folderPath
        {
            get { return _folderPath; }
            set { _folderPath = value; }
        }

        #endregion

        #region Private members

        FFmpegSession _session;
        RenderTexture _tempRT;

        private void CheckRenderTexture()
        {
            if (_tempRT == null)
            {
                if (_tempRT != null)
                {
                    Destroy(_tempRT);
                }
                _tempRT = new RenderTexture(_width, _height, 0, RenderTextureFormat.ARGB32);
            }
        }

        private void CheckCaptureSession()
        {
            if (_session == null)
            {
                if (!string.IsNullOrEmpty(_folderPath))
                {
                    Directory.CreateDirectory(_folderPath);
                }

                _session = FFmpegSession.Create(
                    Path.Combine(string.IsNullOrEmpty(_folderPath) ? "." : _folderPath, gameObject.name),
                    _width, _height, _frameRate, preset
                );

                _startTime = Time.time;
                _frameCount = 0;
                _frameDropCount = 0;
            }
        }

        #endregion

        #region Time-keeping variables

        int _frameCount;
        float _startTime;
        int _frameDropCount;

        float FrameTime
        {
            get { return _startTime + (_frameCount - 0.5f) / _frameRate; }
        }

        void WarnFrameDrop()
        {
            if (++_frameDropCount != 10) return;

            Debug.LogWarning(
                "Significant frame droppping was detected. This may introduce " +
                "time instability into output video. Decreasing the recording " +
                "frame rate is recommended."
            );
        }

        #endregion

        #region MonoBehaviour implementation

        void OnValidate()
        {
            _width = Mathf.Max(8, _width);
            _height = Mathf.Max(8, _height);
        }

        void OnDisable()
        {
            if (_session != null)
            {
                // Close and dispose the FFmpeg session.
                _session.Close();
                _session.Dispose();
                _session = null;
            }

            if (_tempRT != null)
            {
                // Dispose the frame texture.
                GetComponent<Camera>().targetTexture = null;
                Destroy(_tempRT);
                _tempRT = null;
            }
        }

        IEnumerator Start()
        {
            // Sync with FFmpeg pipe thread at the end of every frame.
            for (var eof = new WaitForEndOfFrame(); ;)
            {
                yield return eof;
                _session?.CompletePushFrames();
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            CheckRenderTexture();
            CheckCaptureSession();

            Graphics.Blit(source, _tempRT);
            Graphics.Blit(source, destination);

            var gap = Time.time - FrameTime;
            var delta = 1 / _frameRate;

            if (gap < 0)
            {
                // Update without frame data.
                _session.PushFrame(null);
            }
            else if (gap < delta)
            {
                // Single-frame behind from the current time:
                // Push the current frame to FFmpeg.
                _session.PushFrame(_tempRT);
                _frameCount++;
            }
            else if (gap < delta * 2)
            {
                // Two-frame behind from the current time:
                // Push the current frame twice to FFmpeg. Actually this is not
                // an efficient way to catch up. We should think about
                // implementing frame duplication in a more proper way. #fixme
                _session.PushFrame(_tempRT);
                _session.PushFrame(_tempRT);
                _frameCount += 2;
            }
            else
            {
                // Show a warning message about the situation.
                WarnFrameDrop();

                // Push the current frame to FFmpeg.
                _session.PushFrame(_tempRT);

                // Compensate the time delay.
                _frameCount += Mathf.FloorToInt(gap * _frameRate);
            }
        }

        #endregion
    }
}