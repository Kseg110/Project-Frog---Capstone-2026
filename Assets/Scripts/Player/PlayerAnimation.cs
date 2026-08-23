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
        private static readonly int TurnSpeedHash = Animator.StringToHash("TrunSpeed"); // Float
        private static readonly int EatFlyHash = Animator.StringToHash("EatFly"); // Trigger
        private static readonly int TetherHash = Animator.StringToHash("Tether"); // Trigger 
        private static readonly int TakeDamageHash = Animator.StringToHash("TakeDamage"); // Trigger
        private static readonly int BloodPactHash = Animator.StringToHash("BloodPact"); // Trigger
        private static readonly int BreakTetherHash = Animator.StringToHash("BreakTether"); // Trigger
        private static readonly int IsDashingHash = Animator.StringToHash("IsDashing"); // Bool
        private static readonly int IsTetheredHash = Animator.StringToHash("IsTethered"); // Bool

        [Header("Animation Events")]
        // UnityEvents that can be subscribed to and or set in inspector
        public UnityEvent OnPrimeProjectileSpawn;
        public UnityEvent OnSecProjectileSpawn;
        public UnityEvent OnFlyEaten;
        public UnityEvent OnTongueRelease;
        public UnityEvent OnAnimationComplete;


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
        }

        private void Update()
        {
            if (animator == null)
                return;

            UpdateMovementAnimations();
        }

        private void UpdateMovementAnimations()
        {
            animator.SetFloat(ForwardSpeedHash, playerMovement.speed);
            animator.SetFloat(TurnSpeedHash, playerMovement.speed);
        }

        private void AnimatorHelper()
        {
            if (animator != null) return;
        }

        #region Public animaton methods to call from other scripts
        /// <summary>
        /// Plays Primary, Secondary, Tongue animations. Projectile/ tongue spawns via animation event.
        /// </summary>
        public void PlayPrimaryAttack()
        {
            AnimatorHelper();
            animator.SetBool(PrimaryAttackHash, true);
        }

        public void StopPrimaryAttack()
        {
            AnimatorHelper();
            animator.SetBool(PrimaryAttackHash, false);
        }

        public void PlaySecondaryAttack()
        {
            AnimatorHelper();
            animator.SetBool(SecondaryAttackHash, true);
        }

        public void StopSecondaryAttack()
        {
            AnimatorHelper();
            animator.SetBool(SecondaryAttackHash, false);
        }

        public void PlayTongueAttack()
        {
            AnimatorHelper();
            animator.SetTrigger(TongueAttackHash);
        }

        /// <summary>
        /// Plays animation immediately without requiring animation event
        /// </summary>
        public void PlayDash()
        {
            AnimatorHelper();
            animator.SetBool(IsDashingHash, true);
        }
        public void StopDash()
        {
            AnimatorHelper();
            animator.SetBool(IsDashingHash, false);
        }

        public void PlayTakeDamage()
        {
            AnimatorHelper();
            animator.SetTrigger(TakeDamageHash);
        }

        public void PlayTether()
        {
            AnimatorHelper();
            animator.SetTrigger(TetherHash);
            animator.SetBool(IsTetheredHash, true);
        }

        public void StopTether()
        {
            AnimatorHelper();
            animator.SetBool(IsTetheredHash, false);
        }

        public void PlayBeakTether()
        {
            AnimatorHelper();
            animator.SetTrigger(BreakTetherHash);
            animator.SetBool(IsTetheredHash, false);
        }

        public void PlayDeath()
        {
            AnimatorHelper();
            animator.SetTrigger(DeathHash);
        }

        public void PlayBloodPact()
        {
            AnimatorHelper();
            animator.SetTrigger(BloodPactHash);
        }

        /// <summary>
        /// Play fly eating animation and heal player with animation event
        /// </summary>
        public void PlayEatFly()
        {
            AnimatorHelper();
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

    }
}
