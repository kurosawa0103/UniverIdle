using System;
using UnityEngine;

namespace DesktopPet.Environment
{
    public sealed class DayNightSystem
    {
        private readonly DayNightConfig _config;
        private DayNightPhase _phase = DayNightPhase.Day;
        private float _elapsedInPhase;
        private bool _autoCycleEnabled = true;

        public DayNightPhase CurrentPhase => _phase;
        public bool AutoCycleEnabled => _autoCycleEnabled;
        public float ElapsedInPhase => _elapsedInPhase;

        public event Action<DayNightPhase> PhaseChanged;

        public DayNightSystem(DayNightConfig config)
        {
            _config = config;
        }

        public void Tick(float deltaTime)
        {
            if (!_autoCycleEnabled || _config == null)
                return;

            _elapsedInPhase += deltaTime;
            if (_elapsedInPhase < _config.GetDuration(_phase))
                return;

            AdvanceToNextPhase();
        }

        public void SetPhase(DayNightPhase phase, bool fromManual)
        {
            if (_phase == phase && !fromManual)
                return;

            _phase = phase;
            _elapsedInPhase = 0f;

            if (fromManual)
                _autoCycleEnabled = false;

            PhaseChanged?.Invoke(_phase);
        }

        public void SetAutoCycleEnabled(bool enabled)
        {
            _autoCycleEnabled = enabled;
            if (enabled)
                _elapsedInPhase = 0f;
        }

        public void RestoreState(DayNightPhase phase, bool autoCycleEnabled, float elapsedInPhase)
        {
            float elapsed = Mathf.Max(0f, elapsedInPhase);
            bool changed = _phase != phase
                           || _autoCycleEnabled != autoCycleEnabled
                           || !Mathf.Approximately(_elapsedInPhase, elapsed);

            _phase = phase;
            _autoCycleEnabled = autoCycleEnabled;
            _elapsedInPhase = elapsed;

            if (changed)
                PhaseChanged?.Invoke(_phase);
        }

        private void AdvanceToNextPhase()
        {
            _phase = _config.GetNextPhase(_phase);
            _elapsedInPhase = 0f;
            PhaseChanged?.Invoke(_phase);
        }
    }
}
