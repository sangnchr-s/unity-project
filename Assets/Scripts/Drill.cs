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
            var heatMul = overheat != null ? overheat.EffectivePowerMultiplier : 1f;
            var bit = DrillBitSystem.Instance;
            var bitMul = bit != null ? bit.EffectivePowerMultiplier : 1f;
            var effectivePower = basePower * heatMul * bitMul;
            terrain.Dig(transform.position, radius, effectivePower);
        }
    }
}
