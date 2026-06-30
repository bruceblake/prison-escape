using UnityEngine;

namespace Prison
{
    [CreateAssetMenu(fileName = "PrisonSchedule", menuName = "Prison/Schedule")]
    public class PrisonSchedule : ScriptableObject
    {
        [System.Serializable]
        public struct ScheduleEntry
        {
            public PrisonEventType eventType;
            [Tooltip("Start time in minutes from midnight (0-1440)")]
            public float startTimeMinutes;
            [Tooltip("Duration in minutes")]
            public float durationMinutes;
        }

        [Header("Schedule")]
        public ScheduleEntry[] entries = new ScheduleEntry[]
        {
            new ScheduleEntry { eventType = PrisonEventType.RollCall, startTimeMinutes = 360, durationMinutes = 15 },
            new ScheduleEntry { eventType = PrisonEventType.Breakfast, startTimeMinutes = 375, durationMinutes = 30 },
            new ScheduleEntry { eventType = PrisonEventType.FreeTime, startTimeMinutes = 405, durationMinutes = 60 },
            new ScheduleEntry { eventType = PrisonEventType.Lunch, startTimeMinutes = 465, durationMinutes = 30 },
            new ScheduleEntry { eventType = PrisonEventType.FreeTime, startTimeMinutes = 495, durationMinutes = 90 },
            new ScheduleEntry { eventType = PrisonEventType.Dinner, startTimeMinutes = 585, durationMinutes = 30 },
            new ScheduleEntry { eventType = PrisonEventType.FreeTime, startTimeMinutes = 615, durationMinutes = 120 },
            new ScheduleEntry { eventType = PrisonEventType.LightsOut, startTimeMinutes = 735, durationMinutes = 360 },
            new ScheduleEntry { eventType = PrisonEventType.RollCall, startTimeMinutes = 1095, durationMinutes = 15 },
        };

        [Header("Time Scale")]
        [Tooltip("Game minutes that pass per real second (e.g. 0.1 = 1 game min per 10 real sec)")]
        public float minutesPerRealSecond = 0.1f;
    }
}
