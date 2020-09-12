using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DW_Gameplay
{
    [System.Serializable]
    public struct MotionMatchingEventMarker 
    {
        [SerializeField]
        private string name;
        [SerializeField]
        private float time;

        public MotionMatchingEventMarker(string name, float time)
        {
            this.name = name;
            this.time = time;
        }

        public void SetTime(float time)
        {
            this.time = time;
        }

        public void SetName(string name)
        {
            this.name = name;
        }

        public float GetTime()
        {
            return time;
        }

        public string GetName()
        {
            return name;
        }

    }
}
