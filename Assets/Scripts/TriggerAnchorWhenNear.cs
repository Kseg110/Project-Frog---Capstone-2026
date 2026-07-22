using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Animator))]
public class TriggerAnchorWhenNear : MonoBehaviour
{
	[Header("Animator")]
	[SerializeField] private string boolParameter = "activated";

	[Header("Line Renderer")]
	[SerializeField] private LineRenderer line;
	[Tooltip("If set, this transform is used for point 0. Otherwise the current value is left alone.")]
	[SerializeField] private Transform lineStart; // optional

	[Header("Target")]
	[SerializeField] private string playerLayerName = "Player";
	[SerializeField] private Vector3 playerOffset = Vector3.zero;
	[SerializeField] private bool usePlayerRootTransform = true;

	Animator animator;
	int playerLayer;
	int paramHash;

	int playersInside = 0;
	Transform trackedPlayer;

	void Awake()
	{
		animator = GetComponent<Animator>();
		playerLayer = LayerMask.NameToLayer(playerLayerName);
		paramHash = Animator.StringToHash(boolParameter);

		GetComponent<SphereCollider>().isTrigger = true;

		if (line != null && line.positionCount < 2)
			line.positionCount = 2;
	}

	void Update()
	{
		if (trackedPlayer == null || line == null)
			return;

		Vector3 worldTarget = trackedPlayer.position + playerOffset;

		if (line.useWorldSpace)
			line.SetPosition(1, worldTarget);
		else
			line.SetPosition(1, line.transform.InverseTransformPoint(worldTarget));
	}

	void OnTriggerEnter(Collider other)
	{
		if (playerLayer < 0)
			return; // layer name not found

		// Use the collider's root if desired (common when colliders are on child objects)
		Transform candidate = usePlayerRootTransform ? other.transform.root : other.transform;

		if (candidate.gameObject.layer != playerLayer)
			return;

		playersInside++;

		// Choose a tracked player if we don't have one yet
		if (trackedPlayer == null)
			trackedPlayer = candidate;

		if (playersInside == 1)
			animator.SetBool(paramHash, true);
	}

	void OnTriggerExit(Collider other)
	{
		if (playerLayer < 0)
			return;

		Transform candidate = usePlayerRootTransform ? other.transform.root : other.transform;

		if (candidate.gameObject.layer != playerLayer)
			return;

		playersInside = Mathf.Max(0, playersInside - 1);

		// If the tracked player left, clear it (or you could pick another if one remains)
		if (trackedPlayer == candidate)
			trackedPlayer = null;

		if (playersInside == 0)
		{
			animator.SetBool(paramHash, false);
			line.SetPosition(1, new Vector3(0,0,0));
			// Optional: stop drawing by snapping to start or clearing the line
			// if (line != null && lineStart != null) line.SetPosition(1, lineStart.position);
		}
		else
		{
			// Optional: if someone else is still inside, you can re-acquire a player here if you want.
			// (Requires tracking a HashSet<Transform> of players inside.)
		}
	}
}
