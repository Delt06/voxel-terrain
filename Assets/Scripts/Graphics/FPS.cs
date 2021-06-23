using UnityEngine;

namespace Graphics
{
    public sealed class FPS : MonoBehaviour
    {
        [SerializeField, Min(30)] private int _editorTargetFps = 120;
        [SerializeField, Min(30)] private int _targetFps = 60;

        private void Awake()
        {
            Application.targetFrameRate = GetTargetFPS();
        }

        private int GetTargetFPS() => Application.isEditor ? _editorTargetFps : _targetFps;
    }
}