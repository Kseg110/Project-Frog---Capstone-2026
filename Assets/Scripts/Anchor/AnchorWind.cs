// AnchorWind
// Wind-type anchor component. Reads all stats from the shared WindAnchorData asset.
// Upgrading the WindAnchorData asset affects every AnchorWind in the scene.

using UnityEngine;

public class AnchorWind : AnchorBase
{
    [SerializeField] private AnchorWindData data;

    public AnchorWindData Data => data;
    public override AnchorData BaseData => data;
    public override AnchorElement Element => AnchorElement.Wind;

    public override void Activate()
    {
        base.Activate();
    }
    
}