using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.Player
{
    public class PlayerAnimation : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerAttacks playerAttacks;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private Animator animator;

        // Hash constant animation parameters
        private static readonly int PrimaryAttackHash = Animator.StringToHash("PrimeAttack"); // Bool
        private static readonly int SecondaryAttackHash = Animator.StringToHash("SecAttack"); // Bool
        private static readonly int TongueAttackHash = Animator.StringToHash("TongueAttack"); // Trigger
        private static readonly int DeathHash = Animator.StringToHash("Death"); // Trigger 
        private static readonly int MovementSpeedHash = Animator.StringToHash("MovementSpeed"); // Float
        private static readonly int ForwardSpeedHash = Animator.StringToHash("ForwardSpeed"); // Float 
        private static readonly int TurnSpeedHash = Animator.StringToHash("TurnSpeed"); // Float
        private static readonly int EatFlyHash = Animator.StringToHash("EatFly"); // Trigger
        private static readonly int TetherHash = Animator.StringToHash("Tether"); // Trigger 
        private static readonly int TakeDamageHash = Animator.StringToHash("TakeDamage"); // Trigger
        private static readonly int BloodPactHash = Animator.StringToHash("BloodPact"); // Trigger
        private static readonly int BreakTetherHash = Animator.StringToHash("BreakTether"); // Trigger
        private static readonly int IsDashingHash = Animator.StringToHash("IsDashing"); // Bool
        private static readonly int IsTetheredHash = Animator.StringToHash("IsTethered"); // Bool
        private static readonly int Health = Animator.StringToHash("Health");// float

        [Header("Animation Events")]
        // UnityEvents that can be subscribed to and or set in inspector
        public UnityEvent OnPrimeProjectileSpawn;
        public UnityEvent OnSecProjectileSpawn;
        public UnityEvent OnFlyEaten;
        public UnityEvent OnTongueRelease;
        public UnityEvent OnAnimationComplete;

        // Tracks if player is holding fire input
        public bool isHoldingPrimaryAttack = false;
        public bool isHoldingSecondaryAttack = false;
        private int pauseFrame = -1;
        private Health playerHealth;
        public bool PausedThisFrame => pauseFrame == Time.frameCount;


        private void Awake()
        {
            if (playerAttacks == null)
                playerAttacks = GetComponent<PlayerAttacks>();

            if (playerMovement == null)
                playerMovement = GetComponent<PlayerMovement>();

            if (animator == null)
                animator = GetComponent<Animator>();

            if (playerHealth == null)
                playerHealth = GetComponentInParent<Health>();

            if (animator == null)
            {
                Debug.LogError($"[{gameObject.name}] Animator missing!", this);
                return;
            }
        }

        private void Update()
        {
            if (animator == null)
                return;

            UpdateMovementAnimations();

            float currentHealth = playerHealth != null ? playerHealth.CurrentHealth : 0f;
            animator.SetFloat(Health, currentHealth);
        }

        private void UpdateMovementAnimations()
        {
            float movement = 0f;
            if (playerMovement != null)
                movement = playerMovement.GetMovementFraction();
            const float dampTime = 0.1f;
            animator.SetFloat(ForwardSpeedHash, movement, dampTime, Time.deltaTime);
            //animator.SetFloat(TurnSpeedHash, playerMovement.speed);
        }

        private bool IsAnimatorValid()
        {
            return animator != null;
        }

        #region Public animaton methods to call from other scripts
        /// <summary>
        /// Plays Primary, Secondary, Tongue animations. Projectile/ tongue spawns via animation event.
        /// </summary>
        public void PlayPrimaryAttack()
        {
            if (!IsAnimatorValid()) return;
            if (isHoldingPrimaryAttack) return;
            animator.SetBool(PrimaryAttackHash, true);
            //animator.SetFloat("AttackSpeed", 1f);
        }

        public void StopPrimaryAttack()
        {
            if (!IsAnimatorValid()) return;

                isHoldingPrimaryAttack = false;
                animator.SetFloat("AttackSpeed", 1f);
                animator.SetBool(PrimaryAttackHash, false);
        }

        public void PlaySecondaryAttack()
        {
            if (!IsAnimatorValid()) return;
            animator.SetBool(SecondaryAttackHash, true);
        }

        public void StopSecondaryAttack()
        {
            if (!IsAnimatorValid()) return;
            animator.SetBool(SecondaryAttackHash, false);
        }

        public void PlayTongueAttack()
        {
            if (!IsAnimatorValid()) return;
            animator.SetTrigger(TongueAttackHash);
        }

        /// <summary>
        /// Plays animation immediately without requiring animation event
        /// </summary>
        public void PlayDash()
        {
            if (!IsAnimatorValid()) return;
            animator.SetBool(IsDashingHash, true);
        }
        public void StopDash()
        {
            if (!IsAnimatorValid()) return;
            animator.SetBool(IsDashingHash, false);
        }

        public void PlayTakeDamage()
        {
            if (!IsAnimatorValid()) return;
            animator.SetTrigger(TakeDamageHash);
        }

        public void PlayTether()
        {
            if (!IsAnimatorValid()) return;
            animator.SetTrigger(TetherHash);
            animator.SetBool(IsTetheredHash, true);
        }

        public void StopTether()
        {
            if (!IsAnimatorValid()) return;
            animator.SetBool(IsTetheredHash, false);
        }

        public void PlayBeakTether()
        {
            if (!IsAnimatorValid()) return;
            animator.SetTrigger(BreakTetherHash);
            animator.SetBool(IsTetheredHash, false);
        }

        public void PlayDeath()
        {
            if (!IsAnimatorValid()) return;
            animator.SetTrigger(DeathHash);
        }

        public void PlayBloodPact()
        {
            if (!IsAnimatorValid()) return;
            animator.SetTrigger(BloodPactHash);
        }

        /// <summary>
        /// Play fly eating animation and heal player with animation event
        /// </summary>
        public void PlayEatFly()
        {
            if (!IsAnimatorValid()) return;
            animator.SetTrigger(EatFlyHash);
        }

        #endregion

        #region Animation Event Callbacks that are called by Unity Animator.
        /// <summary>
        /// Called the exact frame where projectile/ tongue should spawn during attack
        /// Set the relevant method in animation window on the desired frame
        /// </summary>
        public void AnimEvent_SpawnPrimeProjectile()
        {
            OnPrimeProjectileSpawn?.Invoke();
            bool shouldHold = playerAttacks != null && playerAttacks.IsPrimaryInputHeld();
            if (shouldHold)
            {
                isHoldingPrimaryAttack = true;

                animator.SetFloat("AttackSpeed", 0f);
            }
            else
            {
                animator.SetBool(PrimaryAttackHash, false);
                animator.SetFloat("AttackSpeed", 2f);
            }
        }

        public void AnimEvent_SpawnSecProjectile()
        {
            OnSecProjectileSpawn?.Invoke();
        }

        public void AnimEvent_SpawnTongue()
        {
            OnTongueRelease?.Invoke();
        }    

        /// <summary>
        /// Trigger healing event at end of fly eating
        /// </summary>
        public void AnimEvenet_FlyEaten()
        {
            OnFlyEaten?.Invoke();
        }

        // Generic animation completion callback
        public void AnimEvent_AnimationComplete()
        {
            OnAnimationComplete?.Invoke();
        }
        #endregion

        // Animator speed helped funciton to hold pose easily without extra events
        public void SetAnimatorSpeed(float speed)
        {
            if (!IsAnimatorValid()) return;
            animator.speed = speed;
        }

    }
}
