using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace DW_Gameplay
{
    [System.Serializable]
    public class MM_DataSection
    {
        [SerializeField]
        public string sectionName;
        [SerializeField]
        public List<float2> timeIntervals;
#if UNITY_EDITOR
        [SerializeField]
        public bool fold = false;
#endif

        public MM_DataSection()
        {
            timeIntervals = new List<float2>();
            sectionName = string.Empty;
        }

        public MM_DataSection(string name)
        {
            this.sectionName = name;
            timeIntervals = new List<float2>();
        }

        public bool SetTimeIntervalWithCheck(int index, float2 change)
        {
            float2 buffor = timeIntervals[index];

            timeIntervals[index] = change;

            if (timeIntervals[index].x == buffor.x && timeIntervals[index].y == buffor.y)
            {
                return false;
            }

            return true;
        }

        public void SetTimeInterval(int index, float2 interval)
        {
            timeIntervals[index] = interval;
        }

        public bool Contain(float localTime)
        {
            for (int i = 0; i < timeIntervals.Count; i++)
            {
                if (localTime >= timeIntervals[i].x && localTime <= timeIntervals[i].y)
                {
                    return true;
                }
            }

            return false;
        }

        public float GetSectionTime()
        {
            float time = 0;
            for (int i = 0; i < timeIntervals.Count; i++)
            {
                time += (timeIntervals[i].y - timeIntervals[i].x);
            }

            return time;
        }
    }
}