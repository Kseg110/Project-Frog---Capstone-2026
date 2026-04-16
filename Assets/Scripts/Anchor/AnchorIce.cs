// AnchorIce
// Ice-type anchor component. Reads all stats from the shared IceAnchorData asset.
// Upgrading the IceAnchorData asset affects every AnchorIce in the scene.

using UnityEngine;

public class AnchorIce : AnchorBase
{
    [SerializeField] private AnchorIceData data;

    public AnchorIceData Data => data;
    public override AnchorData BaseData => data;
}