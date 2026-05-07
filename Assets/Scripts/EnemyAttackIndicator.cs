using System.Collections;
using UnityEngine;


// Current version:
// - Uses a timed approach because attack animations are not implemented yet.

// Intended use:
// - Attach this script to enemy prefabs.
// - Assign a small object as the indicatorObject.
// - Enemy attack scripts should call TriggerAttackIndicator() before attacking.
//
// Future animation implementation:

//  Animation Event calls TriggerAttackIndicator().

// Example future animation event:
//
// public void Animation_StartTelegraph()
// {
//     attackIndicator.TriggerAttackIndicator();
// }

public class EnemyAttackIndicator : MonoBehaviour
{
    [Header("Indicator Object")]
    // Visual object used for the attack warning.
    public GameObject indicatorObject;

    [Header("Timing")]
    // How long the indicator stays yellow before turning red.
    public float windUpTime = 1f;

    // How long the indicator stays red before disappearing.
    public float redTime = 0.3f;

    
    private Renderer indicatorRenderer;

    // Prevents multiple attack telegraphs from overlapping.
    private bool isAttacking;

    private void Start()
    {
        indicatorRenderer = indicatorObject.GetComponent<Renderer>();

        indicatorObject.SetActive(false);
    }

    // Public function that other enemy scripts can call.
    // Example:
    // attackIndicator.TriggerAttackIndicator();
    public void TriggerAttackIndicator()
    {
        if (!isAttacking)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    // Handles the full attack telegraph sequence.
    private IEnumerator AttackRoutine()
    {
        
        isAttacking = true;

        indicatorObject.SetActive(true);

       
        indicatorRenderer.material.color = Color.yellow;

        
        yield return new WaitForSeconds(windUpTime);

        
        indicatorRenderer.material.color = Color.red;

       
        yield return new WaitForSeconds(redTime);

        
        indicatorObject.SetActive(false);

        isAttacking = false;
    }
}