using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;


namespace DW_Gameplay
{
    [System.Serializable]
    public class MotionDataGroup
    {
        [SerializeField]
        public string name;
        [SerializeField]
        public List<MotionMatchingData> animationData;

        // Not serialized Fields:
        [System.NonSerialized]
        public int jobsCount;
        [System.NonSerialized]
        public int calculatedFramesCount;

        public NativeArray<TrajectoryPoint>[] trajectoryPointsPerJob;
        public NativeArray<BoneData>[] bonesPerJob;
        public NativeArray<FrameDataInfo>[] framesInfoPerJob;
        public NativeArray<FrameContact>[] contactPointsPerJob;

#if UNITY_EDITOR
        [SerializeField]
        public bool fold = false;
#endif

        public MotionDataGroup(string groupName)
        {
            this.name = groupName;
            animationData = new List<MotionMatchingData>();
        }

        public void Dispose()
        {
            for (int i = 0; i < jobsCount; i++)
            {
                trajectoryPointsPerJob[i].Dispose();
                bonesPerJob[i].Dispose();
                framesInfoPerJob[i].Dispose();
                if (contactPointsPerJob != null)
                {
                    contactPointsPerJob[i].Dispose();
                }
            }
        }
    }
}
