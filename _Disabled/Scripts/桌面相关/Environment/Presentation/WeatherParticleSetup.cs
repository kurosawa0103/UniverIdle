using UnityEngine;

namespace DesktopPet.Environment
{
    /// <summary>
    /// 天气粒子落到地面 Y 以下则消掉。Sorting Layer 以预制体为准（Weather）。
    /// </summary>
    [DefaultExecutionOrder(15)]
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class WeatherParticleSetup : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("相对 ResolveGroundY 的偏移，正值=略高于地面就消失")]
        private float groundYOffset;

        private ParticleSystem _ps;
        private ParticleSystem.Particle[] _buffer;

        private void Awake()
        {
            _ps = GetComponent<ParticleSystem>();
        }

        private void LateUpdate()
        {
            if (_ps == null)
                return;

            int count = _ps.particleCount;
            if (count <= 0)
                return;

            if (_buffer == null || _buffer.Length < count)
                _buffer = new ParticleSystem.Particle[Mathf.NextPowerOfTwo(Mathf.Max(64, count))];

            int n = _ps.GetParticles(_buffer);
            if (n <= 0)
                return;

            float groundY = DesktopPetServices.ResolveGroundY() + groundYOffset;
            bool worldSpace = _ps.main.simulationSpace == ParticleSystemSimulationSpace.World;
            bool dirty = false;

            for (int i = 0; i < n; i++)
            {
                Vector3 worldPos = worldSpace
                    ? _buffer[i].position
                    : _ps.transform.TransformPoint(_buffer[i].position);

                if (worldPos.y > groundY)
                    continue;

                _buffer[i].remainingLifetime = -1f;
                dirty = true;
            }

            if (dirty)
                _ps.SetParticles(_buffer, n);
        }
    }
}
