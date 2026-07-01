using UnityEngine;
using Prison;

/// <summary>
/// Placed on the bed to satisfy night bed presence checks for this cell.
/// Destroyed during morning line-up (dummy "discovered").
/// </summary>
public class FakeBedDummy : MonoBehaviour
{
    [Tooltip("Must match the cell this bed belongs to.")]
    public int cellIndex;

    public int CellIndex => cellIndex;

    private void OnEnable()
    {
        if (PrisonTimeManager.Instance == null) return;
        PrisonTimeManager.Instance.OnEventChanged += OnScheduleChanged;
        if (PrisonEventExtensions.IsMorningLineUp(PrisonTimeManager.Instance.CurrentEvent))
            DestroyDummyDiscovered();
    }

    private void OnDisable()
    {
        if (PrisonTimeManager.Instance != null)
            PrisonTimeManager.Instance.OnEventChanged -= OnScheduleChanged;
    }

    private void OnScheduleChanged(PrisonEventType evt)
    {
        if (PrisonEventExtensions.IsMorningLineUp(evt))
            DestroyDummyDiscovered();
    }

    private void DestroyDummyDiscovered()
    {
        PrisonSecurityAlerts.RaiseSuspicion($"Fake bed dummy found in cell {cellIndex} during morning roll call.");
        Destroy(gameObject);
    }
}
