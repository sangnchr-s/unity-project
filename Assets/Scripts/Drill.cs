using UnityEngine;

public class Drill : MonoBehaviour
{
    public VoxelTerrain terrain;
    public float radius = 2.5f;
    public float power = 6f;

    void Update()
    {
        if (DrillStateManager.instance.canDrill)
        {
            var configuredPower = DrillPracticeTelemetry.DrillPower;
            var basePower = configuredPower > 0 ? configuredPower : power;
            var overheat = DrillOverheatSystem.Instance;
            var multiplier = overheat != null ? overheat.EffectivePowerMultiplier : 1f;
            var effectivePower = basePower * multiplier;
            terrain.Dig(transform.position, radius, effectivePower);
        }
    }
}
