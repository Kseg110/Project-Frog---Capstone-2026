// AnchorFire
// Fire-type anchor component. Reads all stats from the shared FireAnchorData asset.
// Upgrading the FireAnchorData asset affects every AnchorFire in the scene.

using UnityEngine;

public class AnchorFire : AnchorBase
{
    [SerializeField] private AnchorFireData data;

    public AnchorFireData Data => data;
    public override AnchorData BaseData => data;
    public override AnchorElement Element => AnchorElement.Fire;
}