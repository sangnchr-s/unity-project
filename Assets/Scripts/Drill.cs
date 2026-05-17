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
            var effectivePower = configuredPower > 0 ? configuredPower : power;
            terrain.Dig(transform.position, radius, effectivePower);
        }
    }
}
