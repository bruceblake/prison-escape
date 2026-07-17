using Prison;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prison.Career
{
    /// <summary>
    /// Confirm dialog for quitting a facility run to the Prison Select hub: globals save,
    /// the local run is abandoned and cannot be resumed. Runtime-built overlay.
    /// </summary>
    public class CareerQuitConfirmUI : MonoBehaviour
    {
        public static void Show(System.Action onConfirm)
        {
            if (FindAnyObjectByType<CareerQuitConfirmUI>() != null) return;

            var root = new GameObject("CareerQuitConfirm");
            var ui = root.AddComponent<CareerQuitConfirmUI>();
            ui.Build(onConfirm);
        }

        private void Build(System.Action onConfirm)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5500;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            gameObject.AddComponent<GraphicRaycaster>();

            var dim = EscapeEndScreenUI.CreatePanel(transform, "Dim", new Color(0f, 0f, 0f, 0.72f));
            EscapeEndScreenUI.Stretch(dim.rectTransform);

            var panel = EscapeEndScreenUI.CreatePanel(transform, "Panel", new Color(0.07f, 0.09f, 0.11f, 0.98f));
            var prt = panel.rectTransform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(760f, 300f);
            panel.gameObject.AddComponent<Outline>().effectColor = PrisonUITheme.HazardRed;

            var title = EscapeEndScreenUI.CreateText(panel.transform, "Title", "QUIT TO PRISON SELECT?",
                34f, PrisonUITheme.HazardRed, new Vector2(0.5f, 0.8f), FontStyles.Bold);
            title.rectTransform.sizeDelta = new Vector2(700f, 50f);

            var body = EscapeEndScreenUI.CreateText(panel.transform, "Body",
                "Your career (cash, respect, stats, unlocks) is saved.\nThis facility run is abandoned — re-entry starts from Day 1.",
                22f, new Color(0.8f, 0.8f, 0.82f), new Vector2(0.5f, 0.55f), FontStyles.Normal);
            body.rectTransform.sizeDelta = new Vector2(700f, 90f);

            var buttonSize = new Vector2(300f, 66f);
            EscapeEndScreenUI.CreateButton(panel.transform, "ABANDON RUN", new Vector2(0.27f, 0.18f), () =>
            {
                Destroy(gameObject);
                onConfirm?.Invoke();
            }, buttonSize);
            EscapeEndScreenUI.CreateButton(panel.transform, "CANCEL", new Vector2(0.73f, 0.18f), () =>
            {
                Destroy(gameObject);
            }, buttonSize);
        }
    }
}
