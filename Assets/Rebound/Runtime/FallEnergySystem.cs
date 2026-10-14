using UnityEngine;

namespace Rebound
{
    public sealed class FallEnergySystem : MonoBehaviour
    {
        public PrototypeSettings settings;
        public float Energy { get; private set; }
        public bool IsReady => Energy >= settings.reboundThreshold;
        float lastHeight;
        bool tracking;
        public void ObserveHeight(float height)
        {
            if (tracking) Energy = Mathf.Clamp(Energy + Mathf.Max(0f, lastHeight - height) * settings.energyPerUnit, 0f, 100f);
            lastHeight = height;
            tracking = true;
        }
        public void ResetTracking(float height, bool clearEnergy)
        {
            lastHeight = height;
            tracking = true;
            if (clearEnergy) Energy = 0f;
        }
        public bool TryConsume()
        {
            if (!IsReady) return false;
            Energy = 0f;
            return true;
        }
    }
}
