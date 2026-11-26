using UnityEngine;

[RequireComponent(typeof(Animator))]
public class NeckerCubeGFXController : MonoBehaviour
{
    private const string ClipName = "Necker_Cube|Transition";
    private static readonly int State = Animator.StringToHash("Base Layer." + ClipName);

    private Animator animator;
    private float duration = 1f;
    private float progress;
    private float target;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        animator.speed = 0f;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == ClipName)
                duration = Mathf.Max(0.01f, clip.length);
        }
    }

    private void Start()
    {
        ApplyPose();
    }

    public void InvertForward() => ShowMode(true, false);
    public void InvertBackward() => ShowMode(false, false);

    public void ShowMode(bool modeB, bool immediately)
    {
        target = modeB ? 1f : 0f;
        if (immediately)
        {
            progress = target;
            ApplyPose();
        }
    }

    private void Update()
    {
        if (progress == target)
            return;

        progress = Mathf.MoveTowards(progress, target, Time.deltaTime / duration);
        ApplyPose();
    }

    private void ApplyPose()
    {
        animator.Play(State, 0, progress);
        animator.Update(0f);
    }
}
