// GrappleTowerManager
// Represents a grapple tower in the world. Stores the tower's type (Fire, Ice, Wind),
// its grapple range, and the associated type-specific field data. Exposes all values
// as read-only properties for use by other systems such as PlayerGrapple and AnchorDamageManager.

using UnityEngine;

public class GrappleTowerManager : MonoBehaviour
{
    [SerializeField] private TowerType towerType;

    [Header("Grapple Settings")]
    [SerializeField] private float grappleRange = 15f;

    [Header("Tower Fields")]
    [SerializeField] private FireTowerFields fireFields = new FireTowerFields();
    [SerializeField] private IceTowerFields iceFields = new IceTowerFields();
    [SerializeField] private WindTowerFields windFields = new WindTowerFields();

    public TowerType TowerType => towerType;
    public float GrappleRange => grappleRange;
    public FireTowerFields FireFields => fireFields;
    public IceTowerFields IceFields => iceFields;
    public WindTowerFields WindFields => windFields;

    // Optional: visualize range in Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, grappleRange);
    }
}