using UnityEngine;

namespace Assets.Scripts.Player
{
    public class playerAnimation : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerAttacks playerAttacks;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private Animator animator;

    [Header("Animator Parameters")]
        [SerializeField] private string isAttackingParameter = "IsAttacking";
        [SerializeField] private string movementSpeedParameter = "MovementSpeed";

        [SerializeField] private float movespeed;

        private int isAttackingHash;
        private bool hasIsAttacking;

        private void Awake()
        {
            if (playerAttacks == null)
                playerAttacks = GetComponent<PlayerAttacks>();

            if (playerMovement == null)
                playerMovement = GetComponent<PlayerMovement>();

            if (animator == null)
                animator = GetComponent<Animator>();

            if (animator == null)
            {
                Debug.LogError($"[{gameObject.name}] Animator missing!", this);
                return;
            }

            isAttackingHash = Animator.StringToHash(isAttackingParameter);

            hasIsAttacking = HasAnimatorParameter(
                isAttackingParameter,
                AnimatorControllerParameterType.Bool
            );

            if (!HasAnimatorParameter(
                movementSpeedParameter,
                AnimatorControllerParameterType.Float))
            {
                Debug.LogError(
                    $"Animator needs a FLOAT parameter called '{movementSpeedParameter}'!",
                    animator
                );
            }
        }

        private void Update()
        {
            if (playerMovement != null)
            {
                GetSpeedForAnimation();
            }

            if (animator == null)
                return;

            // SET MOVEMENT FLOAT
            animator.SetFloat(
                movementSpeedParameter,
                movespeed
            );

            // SET ATTACK BOOL
            if (playerAttacks != null && hasIsAttacking)
            {
                animator.SetBool(
                    isAttackingHash,
                    playerAttacks.IsAttacking
                );
            }
        }

        private void GetSpeedForAnimation()
        {
            movespeed = playerMovement.speed;
        }

        private bool HasAnimatorParameter(
            string parameterName,
            AnimatorControllerParameterType expectedType)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.name == parameterName)
                {
                    if (parameter.type != expectedType)
                    {
                        Debug.LogError(
                            $"Animator parameter '{parameterName}' is not a {expectedType}.",
                            animator
                        );

                        return false;
                    }

                    return true;
                }
            }

            return false;
        }
    }

}
