using UnityEngine;

[RequireComponent(typeof(Animator))]
public class NeckerCubeGFXController : MonoBehaviour
{
    private Animator animator;

    private const string AnimationName = @"Necker_Cube|Transition";

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void Invert()
    {
        animator.speed = 1.0f;
        animator.Play(AnimationName, 0, 0.0f);
    }

    public void InvertBack()
    {
        animator.speed = -1.0f;
        animator.Play(AnimationName, 0, 1.0f);
    }
}
