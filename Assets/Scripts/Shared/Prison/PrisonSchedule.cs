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
            new ScheduleEntry { eventType = PrisonEventType.MorningRollCall, startTimeMinutes = 300, durationMinutes = 60 },
            new ScheduleEntry { eventType = PrisonEventType.Breakfast, startTimeMinutes = 360, durationMinutes = 60 },
            new ScheduleEntry { eventType = PrisonEventType.FreeTime, startTimeMinutes = 420, durationMinutes = 60 },
            new ScheduleEntry { eventType = PrisonEventType.WorkProgram, startTimeMinutes = 480, durationMinutes = 210 },
            new ScheduleEntry { eventType = PrisonEventType.MiddayCount, startTimeMinutes = 690, durationMinutes = 30 },
            new ScheduleEntry { eventType = PrisonEventType.Lunch, startTimeMinutes = 720, durationMinutes = 30 },
            new ScheduleEntry { eventType = PrisonEventType.FreeTime, startTimeMinutes = 750, durationMinutes = 30 },
            new ScheduleEntry { eventType = PrisonEventType.WorkProgram, startTimeMinutes = 780, durationMinutes = 180 },
            new ScheduleEntry { eventType = PrisonEventType.EveningCount, startTimeMinutes = 960, durationMinutes = 30 },
            new ScheduleEntry { eventType = PrisonEventType.Dinner, startTimeMinutes = 990, durationMinutes = 30 },
            new ScheduleEntry { eventType = PrisonEventType.FreeTime, startTimeMinutes = 1020, durationMinutes = 240 },
            new ScheduleEntry { eventType = PrisonEventType.NightRollCall, startTimeMinutes = 1260, durationMinutes = 60 },
            new ScheduleEntry { eventType = PrisonEventType.LightsOut, startTimeMinutes = 1320, durationMinutes = 420 },
        };

        [Header("Time Scale")]
        [Tooltip("Game minutes that pass per real second (1 = full 24h day in 24 real minutes)")]
        public float minutesPerRealSecond = 1f;
    }
}
