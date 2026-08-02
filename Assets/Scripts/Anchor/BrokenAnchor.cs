using UnityEngine;

public class BrokenAnchor : AnchorBase
{
    [SerializeField] private BrokenAnchorData data;

    public BrokenAnchorData Data => data;
    public override AnchorData BaseData => data;
    public override AnchorElement Element => AnchorElement.Broken;

    //public override void Activate()
    //{
    //    // No special behavior.
    //    // Exists only so the player can tether to this anchor.
    //    //base.Activate();
    //}
}