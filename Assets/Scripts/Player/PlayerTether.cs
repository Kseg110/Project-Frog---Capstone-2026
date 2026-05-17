//// PlayerTether
//// Manages a physics-based Rope between the player's fire point and the
//// currently grappled anchor. The Rope component handles spring-damper sag
//// and the RopeMesh component generates a 3D tube mesh each frame.
////
//// SETUP:
////   1. Create a child GameObject under the Player (e.g. "TetherRope")
////   2. Add Rope + RopeMesh + LineRenderer to that child
////      - Leave Rope's StartPoint / EndPoint blank — this script sets them at runtime
////      - Configure Rope settings (stiffness, damping, ropeWidth, linePoints, midPointWeight)
////      - Configure RopeMesh settings (OverallDivision, ropeWidth, radialDivision, material)
////      - If you only want the 3D mesh, set LineRenderer width to 0 or disable it
////   3. Drag references into this component's inspector fields

//using UnityEngine;

//public class PlayerTether : MonoBehaviour
//{
//    [Header("References")]
//    [SerializeField] private PlayerAnchor playerAnchor;
//    [SerializeField] private Transform firePoint;
//    [SerializeField] private Rope rope;
//    [SerializeField] private RopeMesh ropeMesh;

//    [Header("Rope Tuning")]
//    [Tooltip("Extra slack added to the straight-line distance so the rope visibly sags")]
//    [SerializeField] private float slackAmount = 2f;

//    [Tooltip("How fast ropeLength adjusts when the distance changes (0 = instant)")]
//    [SerializeField] private float lengthSmoothSpeed = 0f;

//    private bool isRopeActive;

//    private void Awake()
//    {
//        // Start hidden — rope activates when the tether connects
//        SetRopeVisible(false);
//    }

//    private void Update()
//    {
//        HandleTether();
//    }

//    private void HandleTether()
//    {
//        if (playerAnchor == null || firePoint == null || rope == null)
//            return;

//        bool shouldBeActive = playerAnchor.IsTethered && playerAnchor.CurrentAnchor != null;

//        // ── Tether just connected ──
//        if (shouldBeActive && !isRopeActive)
//        {
//            ActivateRope(playerAnchor.CurrentAnchor.transform);
//        }
//        // ── Tether just disconnected ──
//        else if (!shouldBeActive && isRopeActive)
//        {
//            DeactivateRope();
//        }

//        // ── While active, keep rope length in sync with distance ──
//        if (isRopeActive && playerAnchor.CurrentAnchor != null)
//        {
//            UpdateRopeLength(playerAnchor.CurrentAnchor.transform.position);
//        }
//    }

//    private void ActivateRope(Transform anchorTransform)
//    {
//        // Point the rope at our two endpoints
//        rope.SetStartPoint(firePoint, instantAssign: true);
//        rope.SetEndPoint(anchorTransform, instantAssign: true);

//        // Set initial length so the rope doesn't start fully taut
//        float dist = Vector3.Distance(firePoint.position, anchorTransform.position);
//        rope.ropeLength = dist + slackAmount;
//        rope.RecalculateRope();

//        SetRopeVisible(true);
//        isRopeActive = true;
//    }

//    private void DeactivateRope()
//    {
//        SetRopeVisible(false);
//        rope.SetStartPoint(null, instantAssign: true);
//        rope.SetEndPoint(null, instantAssign: true);
//        isRopeActive = false;
//    }

//    private void UpdateRopeLength(Vector3 anchorPos)
//    {
//        float dist = Vector3.Distance(firePoint.position, anchorPos);
//        float targetLength = dist + slackAmount;

//        if (lengthSmoothSpeed > 0f)
//            rope.ropeLength = Mathf.Lerp(rope.ropeLength, targetLength, Time.deltaTime * lengthSmoothSpeed);
//        else
//            rope.ropeLength = targetLength;
//    }

//    private void SetRopeVisible(bool visible)
//    {
//        if (rope != null)
//            rope.gameObject.SetActive(visible);
//    }
//}
