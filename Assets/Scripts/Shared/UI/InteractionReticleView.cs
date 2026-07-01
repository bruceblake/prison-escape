using UnityEngine;
using UnityEngine.UI;

/// <summary>Center reticle: small dot in idle, bracket grows when the player is aiming at an interactable.</summary>
[DisallowMultipleComponent]
public class InteractionReticleView : MonoBehaviour
{
    [SerializeField] private Image dotImage;
    [SerializeField] private Image bracketImage;
    [Tooltip("Filled Image (radial 360), fill clockwise; shows hold-to-complete progress")]
    [SerializeField] private Image holdProgressRing;
    [Tooltip("Pixel size of the reticle in idle / when aiming at a target")]
    public float idleSize = 6f;
    public float targetSize = 20f;
    [SerializeField] private float lerp = 10f;
    [SerializeField] private PlayerInteractor interactor;

    private void Reset()
    {
        interactor = FindFirstObjectByType<PlayerInteractor>();
    }

    private void LateUpdate()
    {
        if (interactor == null) interactor = FindFirstObjectByType<PlayerInteractor>();
        bool has = interactor != null && interactor.HasCurrentInteractable;
        float want = has ? targetSize : idleSize;
        if (bracketImage != null)
        {
            var rt = bracketImage.rectTransform;
            float s = rt.sizeDelta.x;
            s = Mathf.Lerp(s, want, 1f - Mathf.Exp(-lerp * Time.deltaTime));
            rt.sizeDelta = new Vector2(s, s);
            bracketImage.enabled = has;
        }
        if (dotImage != null) dotImage.enabled = !has;

        if (holdProgressRing != null && interactor != null)
        {
            float h = interactor.HoldProgress01;
            holdProgressRing.gameObject.SetActive(h > 0.01f);
            holdProgressRing.fillAmount = h;
        }
    }
}
