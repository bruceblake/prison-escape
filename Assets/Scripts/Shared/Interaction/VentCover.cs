using UnityEngine;
using System.Collections;

public class VentCover : MonoBehaviour
{
    [Header("Screw References")]
    public InteractableScrew[] screws;

    [Header("Vent State")]
    public Collider passageCollider;
    public Transform ventCoverTransform;
    [Tooltip("If set, slides the vent cover. Otherwise uses ventCoverTransform.")]
    public GameObject ventCoverVisual;
    public Animator animator;

    [Header("Vent Cover Animation")]
    [Tooltip("How far the vent cover slides when opened (in local space)")]
    public float slideDistance = 1f;
    [Tooltip("Direction to slide. (0,0,1) = forward, (0,1,0) = up")]
    public Vector3 slideAxis = Vector3.forward;
    public float slideDuration = 0.5f;

    private int screwsRemaining;
    private bool isOpen;
    private Vector3 ventCoverStartPos;

    void Start()
    {
        screwsRemaining = screws.Length;
        isOpen = false;

        if (passageCollider != null)
            passageCollider.enabled = false;

        Transform vent = ventCoverTransform != null ? ventCoverTransform : (ventCoverVisual != null ? ventCoverVisual.transform : null);
        if (vent != null)
            ventCoverStartPos = vent.localPosition;
    }

    public void OnScrewRemoved(InteractableScrew screw)
    {
        screwsRemaining--;
        Debug.Log($"[VentCover] Screw removed. {screwsRemaining} remaining.");

        if (screwsRemaining <= 0)
            OpenVent();
    }

    private void OpenVent()
    {
        if (isOpen) return;
        isOpen = true;

        Debug.Log("[VentCover] All screws removed. Vent is now open!");

        if (passageCollider != null)
            passageCollider.enabled = true;

        Transform vent = ventCoverTransform != null ? ventCoverTransform : (ventCoverVisual != null ? ventCoverVisual.transform : null);
        if (vent != null && slideDuration > 0f)
        {
            StartCoroutine(SlideVentCover(vent));
        }
        else if (ventCoverVisual != null)
        {
            ventCoverVisual.SetActive(false);
        }

        if (animator != null)
            animator.SetTrigger("Open");
    }

    private IEnumerator SlideVentCover(Transform vent)
    {
        Vector3 endPos = ventCoverStartPos + slideAxis.normalized * slideDistance;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            t = t * t * (3f - 2f * t);
            vent.localPosition = Vector3.Lerp(ventCoverStartPos, endPos, t);
            yield return null;
        }

        vent.localPosition = endPos;
    }
}
