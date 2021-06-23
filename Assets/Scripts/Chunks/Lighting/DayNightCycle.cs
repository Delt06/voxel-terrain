using UnityEngine;

namespace Chunks.Lighting
{
    public class DayNightCycle : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _dayDuration = 60f;
        [SerializeField, Min(0f)] private float _nightDuration = 30f;
        [SerializeField] private Gradient _sunlightColor = default;
        [SerializeField] private Gradient _skyColor = default;

        private void Update()
        {
            _time += Time.deltaTime;
            if (_day && _time >= _dayDuration)
            {
                _time = 0f;
                _day = false;
            }

            if (!_day && _time >= _nightDuration)
            {
                _time = 0f;
                _day = true;
            }

            var normalizedTimeOfDay = CalculateNormalizedTimeOfDay();
            Shader.SetGlobalFloat(NormalizedTimeOfDayId, normalizedTimeOfDay);
            var sunlightColor = _sunlightColor.Evaluate(normalizedTimeOfDay);
            Shader.SetGlobalColor(SunlightColorId, sunlightColor);
            var skyColor = _skyColor.Evaluate(normalizedTimeOfDay);
            Shader.SetGlobalColor(SkyColorId, skyColor);
        }

        private void OnDestroy()
        {
            Shader.SetGlobalFloat(NormalizedTimeOfDayId, 0);
            Shader.SetGlobalColor(SunlightColorId, Color.white);
            Shader.SetGlobalColor(SkyColorId, Color.white);
        }

        private float CalculateNormalizedTimeOfDay()
        {
            const float half = 0.5f;
            if (_day)
                return Mathf.Clamp01(_time / _dayDuration) * half;
            return Mathf.Clamp01(_time / _nightDuration) * half + half;
        }

        private float _time;
        private bool _day = true;
        private static readonly int SunlightColorId = Shader.PropertyToID("_SunlightColor");
        private static readonly int SkyColorId = Shader.PropertyToID("_SkyColor");
        private static readonly int NormalizedTimeOfDayId = Shader.PropertyToID("_NormalizedTimeOfDay");
    }
}