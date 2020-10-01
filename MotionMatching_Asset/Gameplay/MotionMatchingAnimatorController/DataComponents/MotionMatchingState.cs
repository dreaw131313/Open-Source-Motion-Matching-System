using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace DW_Gameplay
{
    [System.Serializable]
    public enum MotionMatchingStateType : int
    {
        MotionMatching,
        SingleAnimation,
        ContactAnimationState
    }

    [System.Serializable]
    public class MotionMatchingState : ISerializationCallbackReceiver
    {
        [SerializeField]
        public int nodeID;

        #region Common features
        [SerializeField]
        protected string name;
        [SerializeField]
        private int index;
        [SerializeField]
        private MotionMatchingStateType stateType;
        [SerializeField]
        public PoseCostType poseCostType;
        [SerializeField]
        public TrajectoryCostType trajectoryCostType;
        [SerializeField]
        public int startClipIndex;
        [SerializeField]
        public float startClipTime;
        [SerializeField]
        public List<Transition> transitions;
        [SerializeField]
        public SectionsDependencies sectionsDependencies;
        [SerializeField]
        public int startSection = 0;
        [SerializeField]
        [Range(0.01f, 1f)]
        public float trajectoryCostWeight = 1f;
        [SerializeField]
        [Range(0.01f, 1f)]
        public float poseCostWeight = 1f;
        [SerializeField]
        public List<float2> whereCanFindingNextPose;
        #endregion

        #region Trajectory
        [SerializeField]
        public bool trajectoryCorrection;
        #endregion

        #region Motion Data Groups
        [SerializeField]
        public string startMotionDataGroup;
        [SerializeField]
        public List<MotionDataGroup> motionDataGroups;

        public Dictionary<string, int> motionDataGroupsIndexes; // for selecting groups by names;
        #endregion

        [SerializeField]
        public MotionMatchingStateFeatures mmFeatures;
        [SerializeField]
        public SingleAnimationStateFeatures saFeatures;
        [SerializeField]
        public ContactStateFeatures csFeatures;

        [SerializeField]
        public float speedMultiplier = 1f;

        public int maxJobsCount { get; private set; }

        [System.NonSerialized]
        private int UsersCount = 0;

        public MotionMatchingState(string name, MotionMatchingStateType type, int index, int stateID)
        {
            this.name = name;
            this.stateType = type;
            this.index = index;
            this.nodeID = stateID;
            poseCostType = PoseCostType.Position;
            trajectoryCostType = TrajectoryCostType.PositionVelocityOrientation;
            trajectoryCostWeight = 1f;
            poseCostWeight = 1f;
            transitions = new List<Transition>();
            speedMultiplier = 1f;
            whereCanFindingNextPose = new List<float2>();
            motionDataGroups = new List<MotionDataGroup>();
            motionDataGroups.Add(new MotionDataGroup("MotionGroup"));

            switch (this.stateType)
            {
                case MotionMatchingStateType.MotionMatching:
                    mmFeatures = new MotionMatchingStateFeatures();
                    break;
                case MotionMatchingStateType.SingleAnimation:
                    saFeatures = new SingleAnimationStateFeatures();
                    break;
                case MotionMatchingStateType.ContactAnimationState:
                    csFeatures = new ContactStateFeatures();
                    break;
            }
        }

        #region Getters and Setters
        public void SetStateName(string name)
        {
            this.name = name;
        }

        public string GetName()
        {
            return name;
        }

        public void SetIndex(int index)
        {
            this.index = index;
        }

        public int GetIndex()
        {
            return index;
        }

        public MotionMatchingStateType GetStateType()
        {
            return this.stateType;
        }

        #endregion

        #region ISerializationCallbackReceiver implementation
        public void OnBeforeSerialize()
        {

        }

        public void OnAfterDeserialize()
        {
            switch (this.stateType)
            {
                case MotionMatchingStateType.MotionMatching:
                    this.saFeatures = null;
                    break;
                case MotionMatchingStateType.SingleAnimation:
                    this.mmFeatures = null;
                    break;
            }
        }
        #endregion


        #region Creating struct AnimationData
        public void CreateStructureAnimationData(int maxFramesForJob)
        {
            UsersCount += 1;
            if (UsersCount > 1)
            {
                return;
            }
            if (UsersCount > 1)
            {
                Debug.Log(string.Format("Animation data in {0} state was created, something going wrong!", this.name));
            }

            int trajectoryCount = motionDataGroups[0].animationData[0][0].trajectory.Length;
            int poseCount = motionDataGroups[0].animationData[0][0].pose.Count;

            maxJobsCount = 0;

            motionDataGroupsIndexes = new Dictionary<string, int>();
            for (int i = 0; i < motionDataGroups.Count; i++)
            {
                motionDataGroupsIndexes.Add(motionDataGroups[i].name, i);
            }


            if (stateType == MotionMatchingStateType.ContactAnimationState && csFeatures.contactStateType == ContactStateType.Impacts)
            {
                CreateImpactData(motionDataGroups[0], trajectoryCount, poseCount, maxFramesForJob);
                maxJobsCount = motionDataGroups[0].jobsCount;
            }
            else
            {
                for (int i = 0; i < motionDataGroups.Count; i++)
                {
                    CreateFrameDataForMotionGroups(motionDataGroups[i], maxFramesForJob, trajectoryCount, poseCount);

                    if (motionDataGroups[i].jobsCount > maxJobsCount)
                    {
                        maxJobsCount = motionDataGroups[i].jobsCount;
                    }
                }
            }
        }

        private void CreateContactPointsData(MotionDataGroup group, int[] framesPerJob)
        {
            int groupJobsCount = framesPerJob.Length;
            switch (this.csFeatures.contactMovementType)
            {
                case ContactStateMovemetType.StartContact:
                case ContactStateMovemetType.ContactLand:
                    foreach (MotionMatchingData d in group.animationData)
                    {
                        if (group.animationData[0].contactPoints.Count < this.csFeatures.middleContactsCount)
                        {
                            throw new System.Exception(string.Format("In state {0} there are not enough contact points in data {1}", this.name, d.name));
                        }
                    }
                    break;
                case ContactStateMovemetType.StartContactLand:
                    foreach (MotionMatchingData d in group.animationData)
                    {
                        if (group.animationData[0].contactPoints.Count < 1 + this.csFeatures.middleContactsCount)
                        {
                            throw new System.Exception(string.Format("In state {0} there are not enough contact points in data {1}", this.name, d.name));
                        }
                    }
                    break;
                case ContactStateMovemetType.Contact:
                    foreach (MotionMatchingData d in group.animationData)
                    {
                        if (group.animationData[0].contactPoints.Count < this.csFeatures.middleContactsCount)
                        {
                            throw new System.Exception(string.Format("In state {0} there are not enough contact points in data {1}", this.name, d.name));
                        }
                    }
                    break;
                case ContactStateMovemetType.StartLand:
                    foreach (MotionMatchingData d in group.animationData)
                    {
                        if (group.animationData[0].contactPoints.Count < 2)
                        {
                            throw new System.Exception(string.Format("In state {0} there are not enough contact points in data {1}", this.name, d.name));
                        }
                    }
                    break;
            }

            group.contactPointsPerJob = new NativeArray<FrameContact>[framesPerJob.Length];

            switch (this.csFeatures.contactMovementType)
            {
                case ContactStateMovemetType.Contact:
                    for (int i = 0; i < groupJobsCount; i++)
                    {
                        group.contactPointsPerJob[i] = new NativeArray<FrameContact>(
                            framesPerJob[i] * this.csFeatures.middleContactsCount,
                            Allocator.Persistent
                            );
                    }
                    break;
                case ContactStateMovemetType.StartContact:
                case ContactStateMovemetType.ContactLand:
                    for (int i = 0; i < groupJobsCount; i++)
                    {
                        group.contactPointsPerJob[i] = new NativeArray<FrameContact>(
                            framesPerJob[i] * (this.csFeatures.middleContactsCount + 1),
                            Allocator.Persistent
                            );
                    }
                    break;
                case ContactStateMovemetType.StartContactLand:
                    for (int i = 0; i < groupJobsCount; i++)
                    {
                        group.contactPointsPerJob[i] = new NativeArray<FrameContact>(
                            framesPerJob[i] * (this.csFeatures.middleContactsCount + 2),
                            Allocator.Persistent
                            );
                    }
                    break;
                case ContactStateMovemetType.StartLand:
                    for (int i = 0; i < groupJobsCount; i++)
                    {
                        group.contactPointsPerJob[i] = new NativeArray<FrameContact>(
                            framesPerJob[i] * 2,
                            Allocator.Persistent
                            );
                    }
                    break;
            }

            int cpIndex = 0;
            int jobIndex = 0;


            switch (this.csFeatures.contactMovementType)
            {
                case ContactStateMovemetType.Contact:
                    for (int clipIndex = 0; clipIndex < group.animationData.Count; clipIndex++)
                    {
                        for (int frameIndex = 0; frameIndex < group.animationData[clipIndex].numberOfFrames; frameIndex++)
                        {
                            if (group.animationData[clipIndex].CanUseFrame(frameIndex))
                            {
                                for (int i = 1; i <= this.csFeatures.middleContactsCount; i++)
                                {
                                    group.contactPointsPerJob[jobIndex][cpIndex] = new FrameContact(
                                        group.animationData[clipIndex][frameIndex].contactPoints[i].position,
                                        group.animationData[clipIndex][frameIndex].contactPoints[i].normal//,
                                        //group.animationData[clipIndex][frameIndex].contactPoints[i].forward
                                        );
                                    cpIndex++;

                                }

                                if (cpIndex >= framesPerJob[jobIndex] * csFeatures.middleContactsCount)
                                {
                                    cpIndex = 0;
                                    jobIndex++;
                                }
                            }
                        }
                    }
                    break;
                case ContactStateMovemetType.StartContact:
                    for (int clipIndex = 0; clipIndex < group.animationData.Count; clipIndex++)
                    {
                        for (int frameIndex = 0; frameIndex < group.animationData[clipIndex].numberOfFrames; frameIndex++)
                        {
                            if (group.animationData[clipIndex].CanUseFrame(frameIndex))
                            {
                                for (int i = 0; i <= this.csFeatures.middleContactsCount; i++)
                                {
                                    group.contactPointsPerJob[jobIndex][cpIndex] = new FrameContact(
                                        group.animationData[clipIndex][frameIndex].contactPoints[i].position,
                                       group.animationData[clipIndex][frameIndex].contactPoints[i].normal
                                        //group.animationData[clipIndex][frameIndex].contactPoints[i].forward
                                        );
                                    cpIndex++;

                                }

                                if (cpIndex >= framesPerJob[jobIndex] * (csFeatures.middleContactsCount + 1))
                                {
                                    cpIndex = 0;
                                    jobIndex++;
                                }
                            }
                        }
                    }
                    break;
                case ContactStateMovemetType.ContactLand:
                    for (int clipIndex = 0; clipIndex < group.animationData.Count; clipIndex++)
                    {
                        for (int frameIndex = 0; frameIndex < group.animationData[clipIndex].numberOfFrames; frameIndex++)
                        {
                            if (group.animationData[clipIndex].CanUseFrame(frameIndex))
                            {
                                for (int i = 1; i <= this.csFeatures.middleContactsCount; i++)
                                {
                                    group.contactPointsPerJob[jobIndex][cpIndex] = new FrameContact(
                                        group.animationData[clipIndex][frameIndex].contactPoints[i].position,
                                        group.animationData[clipIndex][frameIndex].contactPoints[i].normal
                                        //group.animationData[clipIndex][frameIndex].contactPoints[i].forward
                                        );
                                    cpIndex++;

                                }
                                int lastPoimtIndex = group.animationData[clipIndex][frameIndex].contactPoints.Length - 1;
                                group.contactPointsPerJob[jobIndex][cpIndex] = new FrameContact(
                                        group.animationData[clipIndex][frameIndex].contactPoints[lastPoimtIndex].position,
                                        group.animationData[clipIndex][frameIndex].contactPoints[lastPoimtIndex].normal
                                        //group.animationData[clipIndex][frameIndex].contactPoints[lastPoimtIndex].forward
                                        );
                                cpIndex++;

                                if (cpIndex >= framesPerJob[jobIndex] * (csFeatures.middleContactsCount + 1))
                                {
                                    cpIndex = 0;
                                    jobIndex++;
                                }
                            }
                        }
                    }
                    break;
                case ContactStateMovemetType.StartContactLand:
                    for (int clipIndex = 0; clipIndex < group.animationData.Count; clipIndex++)
                    {
                        for (int frameIndex = 0; frameIndex < group.animationData[clipIndex].numberOfFrames; frameIndex++)
                        {
                            if (group.animationData[clipIndex].CanUseFrame(frameIndex))
                            {
                                for (int i = 0; i <= this.csFeatures.middleContactsCount; i++)
                                {
                                    //Debug.Log(string.Format("clipIndex: {0} frameIndex: {1} jobIndex: {2} cpIndex: {3} ", clipIndex, frameIndex, jobIndex, cpIndex));
                                    group.contactPointsPerJob[jobIndex][cpIndex] = new FrameContact(
                                        group.animationData[clipIndex][frameIndex].contactPoints[i].position,
                                        group.animationData[clipIndex][frameIndex].contactPoints[i].normal
                                        //group.animationData[clipIndex][frameIndex].contactPoints[i].forward
                                        );
                                    cpIndex++;
                                }

                                int lastPoimtIndex = group.animationData[clipIndex][frameIndex].contactPoints.Length - 1;
                                group.contactPointsPerJob[jobIndex][cpIndex] = new FrameContact(
                                        group.animationData[clipIndex][frameIndex].contactPoints[lastPoimtIndex].position,
                                        group.animationData[clipIndex][frameIndex].contactPoints[lastPoimtIndex].normal
                                        //group.animationData[clipIndex][frameIndex].contactPoints[lastPoimtIndex].forward
                                        );
                                cpIndex++;

                                if (cpIndex >= framesPerJob[jobIndex] * (csFeatures.middleContactsCount + 2))
                                {
                                    cpIndex = 0;
                                    jobIndex++;
                                }
                            }
                        }
                    }
                    break;
                case ContactStateMovemetType.StartLand:
                    for (int clipIndex = 0; clipIndex < group.animationData.Count; clipIndex++)
                    {
                        for (int frameIndex = 0; frameIndex < group.animationData[clipIndex].numberOfFrames; frameIndex++)
                        {
                            if (group.animationData[clipIndex].CanUseFrame(frameIndex))
                            {
                                group.contactPointsPerJob[jobIndex][cpIndex] = new FrameContact(
                                    group.animationData[clipIndex][frameIndex].contactPoints[0].position,
                                    group.animationData[clipIndex][frameIndex].contactPoints[0].normal
                                    //group.animationData[clipIndex][frameIndex].contactPoints[0].forward
                                    );
                                cpIndex++;

                                int lastPoimtIndex = group.animationData[clipIndex][frameIndex].contactPoints.Length - 1;
                                group.contactPointsPerJob[jobIndex][cpIndex] = new FrameContact(
                                        group.animationData[clipIndex][frameIndex].contactPoints[lastPoimtIndex].position,
                                        group.animationData[clipIndex][frameIndex].contactPoints[lastPoimtIndex].normal
                                        //group.animationData[clipIndex][frameIndex].contactPoints[lastPoimtIndex].forward
                                        );
                                cpIndex++;

                                if (cpIndex >= framesPerJob[jobIndex] * 2)
                                {
                                    cpIndex = 0;
                                    jobIndex++;
                                }
                            }
                        }
                    }
                    break;
            }

        }

        private void CreateImpactData(
            MotionDataGroup group,
            int trajectoryCount,
            int poseCount,
            int maxFramesForJob
            )
        {
            // Obliczenie ilości wszystkich klatek do sprawdzenia
            group.calculatedFramesCount = 0;
            for (int i = 0; i < group.animationData.Count; i++)
            {
                for (int frameIndex = 0; frameIndex < group.animationData[i].frames.Count; frameIndex++)
                {
                    if (group.animationData[i][frameIndex].contactPoints.Length == 1)
                    {
                        group.calculatedFramesCount++;
                    }
                }
            }

            // Obliczanie odpowiedniej ilości jobów
            int availableThreads = SystemInfo.processorCount;
            if (availableThreads > 4)
            {
                availableThreads = availableThreads - 1;
            }

            int bestThreadsCount = (int)math.ceil((float)group.calculatedFramesCount / (float)maxFramesForJob);

            if (bestThreadsCount >= availableThreads)
            {
                group.jobsCount = availableThreads;
            }
            else
            {
                group.jobsCount = bestThreadsCount;
            }

            group.trajectoryPointsPerJob = new NativeArray<TrajectoryPoint>[group.jobsCount];
            group.bonesPerJob = new NativeArray<BoneData>[group.jobsCount];
            group.framesInfoPerJob = new NativeArray<FrameDataInfo>[group.jobsCount];

            int[] framesPerJob = new int[group.jobsCount];

            int framesForOneJob = Mathf.FloorToInt((float)group.calculatedFramesCount / (float)group.jobsCount);

            for (int i = 0; i < group.jobsCount - 1; i++)
            {
                framesPerJob[i] = framesForOneJob;
            }

            framesPerJob[group.jobsCount - 1] = group.calculatedFramesCount - (framesForOneJob * (group.jobsCount - 1));

            for (int i = 0; i < group.jobsCount; i++)
            {
                group.trajectoryPointsPerJob[i] = new NativeArray<TrajectoryPoint>(framesPerJob[i] * trajectoryCount, Allocator.Persistent);
                group.bonesPerJob[i] = new NativeArray<BoneData>(framesPerJob[i] * poseCount, Allocator.Persistent);
                group.framesInfoPerJob[i] = new NativeArray<FrameDataInfo>(framesPerJob[i], Allocator.Persistent);
            }

            group.contactPointsPerJob = new NativeArray<FrameContact>[group.jobsCount];
            for (int i = 0; i < group.jobsCount; i++)
            {
                group.contactPointsPerJob[i] = new NativeArray<FrameContact>(
                    framesPerJob[i],
                    Allocator.Persistent
                    );
            }

            int jobIndex = 0;
            int frameInfoIndex = 0;
            int jobTrajectoryPointIndex = 0;
            int jobBoneIndex = 0;
            int cpIndex = 0;

            for (int clipIndex = 0; clipIndex < group.animationData.Count; clipIndex++)
            {
                for (int frameIndex = 0; frameIndex < group.animationData[clipIndex].numberOfFrames; frameIndex++)
                {
                    if (group.animationData[clipIndex][frameIndex].contactPoints.Length == 1)
                    {
                        // Making native array trajectory points
                        for (int pointIndex = 0; pointIndex < trajectoryCount; pointIndex++)
                        {
                            group.trajectoryPointsPerJob[jobIndex][jobTrajectoryPointIndex] = group.animationData[clipIndex][frameIndex].trajectory.GetPoint(pointIndex);
                            jobTrajectoryPointIndex++;
                        }

                        // Making native array bones
                        for (int boneIndex = 0; boneIndex < poseCount; boneIndex++)
                        {
                            group.bonesPerJob[jobIndex][jobBoneIndex] = group.animationData[clipIndex][frameIndex].pose.GetBoneData(boneIndex);
                            jobBoneIndex++;
                        }

                        // Making native array Impacts

                        group.contactPointsPerJob[jobIndex][cpIndex] = new FrameContact(
                                        group.animationData[clipIndex][frameIndex].contactPoints[0].position,
                                        group.animationData[clipIndex][frameIndex].contactPoints[0].normal
                                        //group.animationData[clipIndex][frameIndex].contactPoints[0].forward
                                        );
                        cpIndex++;


                        // Making native array frame info
                        group.framesInfoPerJob[jobIndex][frameInfoIndex] = new FrameDataInfo(
                            clipIndex,
                             group.animationData[clipIndex][frameIndex].localTime,
                             group.animationData[clipIndex][frameIndex].sections
                            );
                        frameInfoIndex++;

                        if (frameInfoIndex == framesPerJob[jobIndex])
                        {
                            jobIndex++;
                            frameInfoIndex = 0;
                            jobTrajectoryPointIndex = 0;
                            jobBoneIndex = 0;
                            cpIndex = 0;
                        }
                    }
                }
            }
        }

        //private void CreateImpactData(
        //    int trajectoryCount,
        //    int poseCount,
        //    int maxFramesForJob
        //    )
        //{
        //    // Obliczenie ilości wszystkich klatek do sprawdzenia
        //    allFramesCount = 0;
        //    for (int i = 0; i < animationData.Count; i++)
        //    {
        //        for (int frameIndex = 0; frameIndex < animationData[i].frames.Count; frameIndex++)
        //        {
        //            if (animationData[i][frameIndex].contactPoints.Length == 1)
        //            {
        //                allFramesCount++;
        //            }
        //        }
        //    }

        //    // Obliczanie odpowiedniej ilości jobów
        //    int availableThreads = SystemInfo.processorCount;
        //    if (availableThreads > 4)
        //    {
        //        availableThreads = availableThreads - 1;
        //    }

        //    int bestThreadsCount = (int)math.ceil((float)allFramesCount / (float)maxFramesForJob);

        //    if (bestThreadsCount >= availableThreads)
        //    {
        //        maxJobsCount = availableThreads;
        //    }
        //    else
        //    {
        //        maxJobsCount = bestThreadsCount;
        //    }

        //    trajectoryPointsPerJob = new NativeArray<TrajectoryPoint>[maxJobsCount];
        //    bonesPerJob = new NativeArray<BoneData>[maxJobsCount];
        //    framesInfoPerJob = new NativeArray<FrameDataInfo>[maxJobsCount];

        //    int[] framesPerJob = new int[maxJobsCount];

        //    int framesForOneJob = Mathf.FloorToInt((float)allFramesCount / (float)maxJobsCount);

        //    for (int i = 0; i < maxJobsCount - 1; i++)
        //    {
        //        framesPerJob[i] = framesForOneJob;
        //    }

        //    framesPerJob[maxJobsCount - 1] = allFramesCount - (framesForOneJob * (maxJobsCount - 1));

        //    for (int i = 0; i < maxJobsCount; i++)
        //    {
        //        trajectoryPointsPerJob[i] = new NativeArray<TrajectoryPoint>(framesPerJob[i] * trajectoryCount, Allocator.Persistent);
        //        bonesPerJob[i] = new NativeArray<BoneData>(framesPerJob[i] * poseCount, Allocator.Persistent);
        //        framesInfoPerJob[i] = new NativeArray<FrameDataInfo>(framesPerJob[i], Allocator.Persistent);
        //    }

        //    contactPointsPerJob = new NativeArray<FrameContact>[maxJobsCount];
        //    for (int i = 0; i < maxJobsCount; i++)
        //    {
        //        contactPointsPerJob[i] = new NativeArray<FrameContact>(
        //            framesPerJob[i],
        //            Allocator.Persistent
        //            );
        //    }

        //    int jobIndex = 0;
        //    int frameInfoIndex = 0;
        //    int jobTrajectoryPointIndex = 0;
        //    int jobBoneIndex = 0;
        //    int cpIndex = 0;

        //    for (int clipIndex = 0; clipIndex < animationData.Count; clipIndex++)
        //    {
        //        for (int frameIndex = 0; frameIndex < animationData[clipIndex].numberOfFrames; frameIndex++)
        //        {
        //            if (animationData[clipIndex][frameIndex].contactPoints.Length == 1)
        //            {
        //                // Making native array trajectory points
        //                for (int pointIndex = 0; pointIndex < animationData[clipIndex][frameIndex].trajectory.Length; pointIndex++)
        //                {
        //                    trajectoryPointsPerJob[jobIndex][jobTrajectoryPointIndex] = animationData[clipIndex][frameIndex].trajectory.GetPoint(pointIndex);
        //                    jobTrajectoryPointIndex++;
        //                }

        //                // Making native array bones
        //                for (int boneIndex = 0; boneIndex < animationData[clipIndex][frameIndex].pose.Count; boneIndex++)
        //                {
        //                    bonesPerJob[jobIndex][jobBoneIndex] = animationData[clipIndex][frameIndex].pose.GetBoneData(boneIndex);
        //                    jobBoneIndex++;
        //                }

        //                // Making native array Impacts

        //                contactPointsPerJob[jobIndex][cpIndex] = new FrameContact(
        //                               animationData[clipIndex][frameIndex].contactPoints[0].position,
        //                               animationData[clipIndex][frameIndex].contactPoints[0].normal,
        //                               animationData[clipIndex][frameIndex].contactPoints[0].forward
        //                               );
        //                cpIndex++;


        //                // Making native array frame info
        //                framesInfoPerJob[jobIndex][frameInfoIndex] = new FrameDataInfo(
        //                    clipIndex,
        //                    animationData[clipIndex][frameIndex].localTime,
        //                    animationData[clipIndex][frameIndex].sections
        //                    );
        //                frameInfoIndex++;

        //                if (frameInfoIndex == framesPerJob[jobIndex])
        //                {
        //                    jobIndex++;
        //                    frameInfoIndex = 0;
        //                    jobTrajectoryPointIndex = 0;
        //                    jobBoneIndex = 0;
        //                    cpIndex = 0;
        //                }
        //            }
        //        }
        //    }
        //}

        private void CreateFrameDataForMotionGroups(
            MotionDataGroup group,
            float maxFramesPerJob,
            int trajectoryCount,
            int poseCount
            )
        {
            // Obliczenie ilości wszystkich klatek do sprawdzenia
            group.calculatedFramesCount = 0;
            for (int i = 0; i < group.animationData.Count; i++)
            {
                group.calculatedFramesCount += group.animationData[i].usedFrameCount;
            }

            // Obliczanie odpowiedniej ilości jobów
            int availableThreads = SystemInfo.processorCount;
            if (availableThreads > 4)
            {
                availableThreads = availableThreads - 1;
            }

            int bestThreadsCount = (int)math.ceil((float)group.calculatedFramesCount / (float)maxFramesPerJob);

            if (bestThreadsCount >= availableThreads)
            {
                group.jobsCount = availableThreads;
            }
            else
            {
                group.jobsCount = bestThreadsCount;
            }

            group.trajectoryPointsPerJob = new NativeArray<TrajectoryPoint>[group.jobsCount];
            group.bonesPerJob = new NativeArray<BoneData>[group.jobsCount];
            group.framesInfoPerJob = new NativeArray<FrameDataInfo>[group.jobsCount];

            int[] framesPerJob = new int[group.jobsCount];

            int framesForOneJob = Mathf.FloorToInt((float)group.calculatedFramesCount / (float)group.jobsCount);

            for (int i = 0; i < group.jobsCount - 1; i++)
            {
                framesPerJob[i] = framesForOneJob;
            }

            framesPerJob[group.jobsCount - 1] = group.calculatedFramesCount - (framesForOneJob * (group.jobsCount - 1));
            for (int i = 0; i < group.jobsCount; i++)
            {
                group.trajectoryPointsPerJob[i] = new NativeArray<TrajectoryPoint>(framesPerJob[i] * trajectoryCount, Allocator.Persistent);
                group.bonesPerJob[i] = new NativeArray<BoneData>(framesPerJob[i] * poseCount, Allocator.Persistent);
                group.framesInfoPerJob[i] = new NativeArray<FrameDataInfo>(framesPerJob[i], Allocator.Persistent);
            }

            int jobIndex = 0;
            int frameInfoIndex = 0;
            int jobTrajectoryPointIndex = 0;
            int jobBoneIndex = 0;

            for (int clipIndex = 0; clipIndex < group.animationData.Count; clipIndex++)
            {
                for (int frameIndex = 0; frameIndex < group.animationData[clipIndex].numberOfFrames; frameIndex++)
                {
                    if (group.animationData[clipIndex].CanUseFrame(frameIndex))
                    {
                        // Making native array trajectory points
                        for (int pointIndex = 0; pointIndex < trajectoryCount; pointIndex++)
                        {
                            group.trajectoryPointsPerJob[jobIndex][jobTrajectoryPointIndex] = group.animationData[clipIndex][frameIndex].trajectory.GetPoint(pointIndex);
                            jobTrajectoryPointIndex++;
                        }

                        // Making native array bones
                        for (int boneIndex = 0; boneIndex < poseCount; boneIndex++)
                        {
                            group.bonesPerJob[jobIndex][jobBoneIndex] = group.animationData[clipIndex][frameIndex].pose.GetBoneData(boneIndex);
                            jobBoneIndex++;
                        }

                        // Making native array frame info
                        group.framesInfoPerJob[jobIndex][frameInfoIndex] = new FrameDataInfo(
                            clipIndex,
                            group.animationData[clipIndex][frameIndex].localTime,
                            group.animationData[clipIndex][frameIndex].sections
                            );
                        frameInfoIndex++;

                        if (frameInfoIndex == framesPerJob[jobIndex])
                        {
                            jobIndex++;
                            frameInfoIndex = 0;
                            jobTrajectoryPointIndex = 0;
                            jobBoneIndex = 0;
                        }
                    }
                }
            }


            if (this.stateType == MotionMatchingStateType.ContactAnimationState && this.csFeatures.contactStateType == ContactStateType.NormalContacts)
            {
                CreateContactPointsData(group, framesPerJob);
            }

        }

        private void DisposeFrameDataFromMotionGroup()
        {
            for (int i = 0; i < motionDataGroups.Count; i++)
            {
                motionDataGroups[i].Dispose();
            }
        }

        public void DisposeStructureAnimationData()
        {
            UsersCount -= 1;
            if (UsersCount != 0)
            {
                return;
            }

            DisposeFrameDataFromMotionGroup();
        }

        #endregion

#if UNITY_EDITOR
        [SerializeField]
        public bool animDataFold = false;

        public Transition AddTransitionToState_OLD(MotionMatchingState toState, int nodeID, Rect rect, bool portal = false)
        {
            Transition t = null;// new Transition(TransitionType., toState.GetIndex());

            switch (toState.GetStateType())
            {
                case MotionMatchingStateType.MotionMatching:
                    t = new Transition(MotionMatchingStateType.MotionMatching, toState.GetIndex());
                    break;
                case MotionMatchingStateType.SingleAnimation:
                    t = new Transition(MotionMatchingStateType.SingleAnimation, toState.GetIndex());
                    break;
            }

            t.toPortal = portal;
            if (!portal)
            {
                t.portalRect = Rect.zero;
            }
            else
            {
                t.portalRect = rect;
            }

            t.nodeID = nodeID;
            transitions.Add(t);
            return t;
        }

        public bool AddTransition(MotionMatchingState toState, int nodeID, bool portal = false)
        {
            transitions.Add(new Transition(
                toState.GetStateType(),
                toState.GetIndex()
                ));
            transitions[transitions.Count - 1].nodeID = nodeID;
            transitions[transitions.Count - 1].transitionRect = new Rect();
            transitions[transitions.Count - 1].transitionRect.size = new Vector2(15, 15);
            transitions[transitions.Count - 1].toPortal = portal;
            transitions[transitions.Count - 1].fromStateIndex = this.GetIndex();
            return false;
        }

        public bool RemoveTransition(int toStateIndex)
        {
            for (int i = 0; i < transitions.Count; i++)
            {
                if (transitions[i].nextStateIndex == toStateIndex)
                {
                    transitions.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public void AddMotionMatchingData(MotionMatchingData[] data, MotionDataGroup group)
        {
            group.animationData.AddRange(data);

            switch (this.stateType)
            {
                case MotionMatchingStateType.MotionMatching:
                    break;
                case MotionMatchingStateType.SingleAnimation:
                    for (int i = 0; i < data.Length; i++)
                    {
                        whereCanFindingNextPose.Add(new float2(
                            0f,
                            data[i].contactPoints.Count > 0 ? data[i].contactPoints[0].startTime : data[i].animationLength * 0.5f
                            ));
                    }
                    break;
                case MotionMatchingStateType.ContactAnimationState:
                    for (int i = 0; i < data.Length; i++)
                    {
                        whereCanFindingNextPose.Add(new float2(
                            0f,
                            data[i].contactPoints.Count > 0 ? data[i].contactPoints[0].startTime : data[i].animationLength * 0.5f
                            ));
                    }
                    break;
            }

            UpdateTransitions();
        }

        public void AddMotionMatchingData(MotionMatchingData data, MotionDataGroup group)
        {
            group.animationData.Add(data);

            switch (this.stateType)
            {
                case MotionMatchingStateType.MotionMatching:
                    break;
                case MotionMatchingStateType.SingleAnimation:
                    break;
                case MotionMatchingStateType.ContactAnimationState:
                    whereCanFindingNextPose.Add(new float2(
                        0f,
                        data.contactPoints.Count > 0 ? data.contactPoints[0].startTime : data.animationLength * 0.5f
                        ));
                    break;
            }

            UpdateTransitions();
        }

        public void RemoveMotionMatchingData(int index, MotionDataGroup group)
        {
            group.animationData.RemoveAt(index);

            switch (this.stateType)
            {
                case MotionMatchingStateType.MotionMatching:
                    break;
                case MotionMatchingStateType.SingleAnimation:
                    break;
                case MotionMatchingStateType.ContactAnimationState:
                    whereCanFindingNextPose.RemoveAt(index);
                    break;
            }
            UpdateTransitions(index);
        }

        public void ClearMotionMatchingData(MotionDataGroup group)
        {
            group.animationData.Clear();
            whereCanFindingNextPose.Clear();
            UpdateTransitions();
        }

        public void UpdateTransitions(int removingDataIndex = -1)
        {
            if (stateType == MotionMatchingStateType.MotionMatching)
            {
                return;
            }
            foreach (Transition t in transitions)
            {
                t.fromStateIndex = GetIndex();
                foreach (TransitionOptions o in t.options)
                {
                    if (removingDataIndex > -1 && removingDataIndex < o.whenCanCheckingTransition.Count)
                    {
                        o.whenCanCheckingTransition.RemoveAt(removingDataIndex);
                    }
                    else if (o.whenCanCheckingTransition.Count < this.motionDataGroups[0].animationData.Count)
                    {
                        while (o.whenCanCheckingTransition.Count != this.motionDataGroups[0].animationData.Count)
                        {
                            o.whenCanCheckingTransition.Add(new float2(0f, this.motionDataGroups[0].animationData[o.whenCanCheckingTransition.Count].animationLength));
                        }
                    }
                    else if (o.whenCanCheckingTransition.Count > this.motionDataGroups[0].animationData.Count)
                    {
                        while (o.whenCanCheckingTransition.Count != this.motionDataGroups[0].animationData.Count)
                        {
                            o.whenCanCheckingTransition.RemoveAt(o.whenCanCheckingTransition.Count - 1);
                        }
                    }
                }
            }
        }

        public void UpdateTransitions(MotionMatchingState fromState, int removedDataIndex = -1)
        {
            if (stateType == MotionMatchingStateType.MotionMatching)
            {
                return;
            }
            foreach (Transition t in fromState.transitions)
            {
                if (t.nextStateIndex == GetIndex())
                {
                    foreach (TransitionOptions o in t.options)
                    {
                        if (removedDataIndex > -1 && removedDataIndex < o.whereCanFindingBestPose.Count)
                        {
                            o.whereCanFindingBestPose.RemoveAt(removedDataIndex);
                        }
                        else if (o.whereCanFindingBestPose.Count < this.motionDataGroups[0].animationData.Count)
                        {
                            while (o.whereCanFindingBestPose.Count != this.motionDataGroups[0].animationData.Count)
                            {
                                o.whereCanFindingBestPose.Add(new float2(0f, this.motionDataGroups[0].animationData[o.whereCanFindingBestPose.Count].animationLength));
                            }
                        }
                        else if (o.whereCanFindingBestPose.Count > this.motionDataGroups[0].animationData.Count)
                        {
                            while (o.whereCanFindingBestPose.Count != this.motionDataGroups[0].animationData.Count)
                            {
                                o.whereCanFindingBestPose.RemoveAt(o.whereCanFindingBestPose.Count - 1);
                            }
                        }
                    }
                }
            }
        }


        // DATA GROUPS

        public void AddDataGroup(string groupName)
        {
            motionDataGroups.Add(new MotionDataGroup(groupName));
        }

        public void RemoveDataGroup(int groupIndex)
        {
            motionDataGroups.RemoveAt(groupIndex);
        }

#endif
    }

}