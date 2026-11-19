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

    private bool playingBackward = true;
    private const float PlaybackSpeed = 1.0f;

    private void Update()
    {
        if (!playingBackward)
            return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        float normalizedTime = state.normalizedTime;
        normalizedTime -= Time.deltaTime * PlaybackSpeed / state.length;
        normalizedTime = Mathf.Max(0f, normalizedTime);

        animator.Play(state.fullPathHash, 0, normalizedTime);
    }

    public void InvertForward()
    {
        playingBackward = false;
        animator.Play(AnimationName, 0, 0f);
    }

    public void InvertBackward()
    {
        playingBackward = true;
    }
}
