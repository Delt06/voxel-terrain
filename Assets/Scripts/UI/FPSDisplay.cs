using System.Text;
using TMPro;
using UnityEngine;

namespace UI
{
    public class FPSDisplay : MonoBehaviour
    {
        [SerializeField, Min(1)] private int _updateFrequencyInFrames = 5;

        private void Update()
        {
            if (Time.frameCount % _updateFrequencyInFrames != 0) return;
            var fps = Mathf.RoundToInt(1f / Time.unscaledDeltaTime);
            _stringBuilder.Clear().Append(fps);
            _text.SetText(_stringBuilder);
        }

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private TMP_Text _text;
        private readonly StringBuilder _stringBuilder = new StringBuilder();
    }
}