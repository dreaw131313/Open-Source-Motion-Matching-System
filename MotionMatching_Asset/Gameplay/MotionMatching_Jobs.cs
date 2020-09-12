using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace DW_Gameplay
{
    [BurstCompile]
    public struct BasicMotionMatchingJob : IJob
    {
        // Input
        public TrajectoryCostType trajectoryCostType;
        public PoseCostType poseCostType;
        public float trajectoryWeight;
        public float poseWeight;

        [ReadOnly]
        public NativeArray<TrajectoryPoint> currentTrajectory;
        [ReadOnly]
        public NativeArray<BoneData> currentPose;
        [ReadOnly]
        public NativeArray<FrameDataInfo> framesInfo;
        [ReadOnly]
        public NativeArray<TrajectoryPoint> trajectoryPoints;
        [ReadOnly]
        public NativeArray<BoneData> bonesData;
        [ReadOnly]
        public NativeList<CurrentPlayedClipInfo> currentPlayingClips;
        [ReadOnly]
        public NativeArray<int> sectionsIndexes;
        [ReadOnly]
        public NativeArray<SectionInfo> sectionDependecies;
        [ReadOnly]
        int currentGroupIndex;

        // Output
        [WriteOnly]
        public NativeArray<NewClipInfoToPlay> infos;

        public void Execute()
        {
            NewClipInfoToPlay output = new NewClipInfoToPlay(-1, currentGroupIndex, -1, float.MaxValue);

            int trajectoryStep = currentTrajectory.Length;
            int poseStep = currentPose.Length;
            int frameCount = framesInfo.Length;
            float currentCost;
            float trajectoryCost;
            float poseCost;
            float costWeight;

            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                bool notSkipCheckingFrame = true;
                for (int i = 0; i < currentPlayingClips.Length; i++)
                {
                    if (currentPlayingClips[i].groupIndex == currentGroupIndex && currentPlayingClips[i].clipIndex == framesInfo[frameIndex].clipIndex &&
                        currentPlayingClips[i].notFindInYourself)
                    {
                        notSkipCheckingFrame = false;
                        break;
                    }
                }


                if (notSkipCheckingFrame &&
                    framesInfo[frameIndex].sections.GetSection(sectionsIndexes[framesInfo[frameIndex].clipIndex]))
                {
                    trajectoryCost = 0;
                    poseCost = 0;
                    costWeight = 1f;

                    for (int i = 0; i < sectionDependecies.Length; i++)
                    {
                        if (framesInfo[frameIndex].sections.GetSection(sectionDependecies[i].sectionIndex))
                        {
                            costWeight *= sectionDependecies[i].sectionWeight;
                        }
                    }

                    int tpIndex = frameIndex * trajectoryStep;
                    for (int i = 0; i < trajectoryStep; i++)
                    {
                        trajectoryCost += currentTrajectory[i].CalculateCost(trajectoryPoints[tpIndex], trajectoryCostType);
                        tpIndex++;
                    }
                    int boneIndex = frameIndex * poseStep;
                    for (int i = 0; i < poseStep; i++)
                    {
                        poseCost += currentPose[i].CalculateCost(bonesData[boneIndex], poseCostType);
                        boneIndex++;
                    }

                    currentCost = costWeight * (poseWeight * poseCost + trajectoryWeight * trajectoryCost);

                    if (currentCost < output.bestCost)
                    {
                        output.Set(
                                framesInfo[frameIndex].clipIndex,
                                framesInfo[frameIndex].localTime,
                                currentCost
                                );
                    }

                }
            }


            infos[0] = output;
        }

        public void SetBasicOptions(
            NativeArray<FrameDataInfo> framesInfo,
            NativeArray<TrajectoryPoint> trajectoryPoints,
            NativeArray<BoneData> bonesData,
            NativeArray<NewClipInfoToPlay> infos
            )
        {
            this.framesInfo = framesInfo;
            this.trajectoryPoints = trajectoryPoints;
            this.bonesData = bonesData;
            this.infos = infos;
        }

        public void SetChangingOptions(
            TrajectoryCostType trajectoryCostType,
            PoseCostType poseCostType,
            NativeArray<BoneData> currentPose,
            NativeArray<TrajectoryPoint> trajectory,
            NativeList<CurrentPlayedClipInfo> currentClips,
            NativeArray<int> sectionsIndex,
            NativeArray<SectionInfo> sectionDependecies,
            float trajectoryWeight,
            float poseWeight,
            int currentMotionDataGroupIndex
            )
        {
            this.trajectoryCostType = trajectoryCostType;
            this.poseCostType = poseCostType;

            this.currentPose = currentPose;
            this.currentTrajectory = trajectory;
            this.currentPlayingClips = currentClips;
            this.sectionsIndexes = sectionsIndex;
            this.sectionDependecies = sectionDependecies;

            this.trajectoryWeight = trajectoryWeight;
            this.poseWeight = poseWeight;
            this.currentGroupIndex = currentMotionDataGroupIndex;
        }
    }

    [BurstCompile]
    public struct MotionMatchingSingleAnimationJob : IJob
    {
        // Input
        public TrajectoryCostType trajectoryCostType;
        public PoseCostType poseCostType;
        float trajectoryWeight;
        float poseWeight;

        [ReadOnly]
        public NativeArray<TrajectoryPoint> currentTrajectory;
        [ReadOnly]
        public NativeArray<BoneData> currentPose;
        [ReadOnly]
        public NativeArray<FrameDataInfo> framesInfo;
        [ReadOnly]
        public NativeArray<TrajectoryPoint> trajectoryPoints;
        [ReadOnly]
        public NativeArray<BoneData> bonesData;
        [ReadOnly]
        public NativeArray<float2> whereWeCanFindingPos;

        // Output
        [WriteOnly]
        public NativeArray<NewClipInfoToPlay> infos;

        public void Execute()
        {
            NewClipInfoToPlay output = new NewClipInfoToPlay(-1, 0, -1, float.MaxValue);

            int trajectoryStep = currentTrajectory.Length;
            int poseStep = currentPose.Length;
            int frameCount = framesInfo.Length;
            int currentClipIndex;

            float currentCost;
            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                currentClipIndex = framesInfo[frameIndex].clipIndex;
                if (
                    framesInfo[frameIndex].localTime < whereWeCanFindingPos[currentClipIndex].x ||
                    framesInfo[frameIndex].localTime > whereWeCanFindingPos[currentClipIndex].y
                    )
                {
                    continue;
                }

                currentCost = 0;

                float trajectoryCost = 0f;
                int tpIndex = frameIndex * trajectoryStep;
                for (int i = 0; i < trajectoryStep; i++)
                {
                    trajectoryCost += currentTrajectory[i].CalculateCost(trajectoryPoints[tpIndex], trajectoryCostType);
                    tpIndex++;
                }

                trajectoryCost *= trajectoryWeight;

                float poseCost = 0;
                int boneIndex = frameIndex * poseStep;
                for (int i = 0; i < poseStep; i++)
                {
                    poseCost += currentPose[i].CalculateCost(bonesData[boneIndex], poseCostType);
                    boneIndex++;
                }
                poseCost *= poseWeight;

                currentCost = trajectoryCost + poseCost;

                if (currentCost < output.bestCost)
                {
                    output.Set(
                            framesInfo[frameIndex].clipIndex,
                            framesInfo[frameIndex].localTime,
                            currentCost
                            );
                }
            }

            infos[0] = output;
        }

        public void SetBasicOptions(
            NativeArray<FrameDataInfo> framesInfo,
            NativeArray<TrajectoryPoint> trajectoryPoints,
            NativeArray<BoneData> bonesData,
            NativeArray<NewClipInfoToPlay> infos
            )
        {
            this.framesInfo = framesInfo;
            this.trajectoryPoints = trajectoryPoints;
            this.bonesData = bonesData;
            this.infos = infos;
        }

        public void SetChangingOptions(
            TrajectoryCostType trajectoryCostType,
            PoseCostType poseCostType,
            NativeArray<BoneData> currentPose,
            NativeArray<TrajectoryPoint> trajectory,
            NativeArray<float2> whereWeCanFindingPos,
            float trajectoryWeight,
            float poseWeight
            )
        {
            this.trajectoryCostType = trajectoryCostType;
            this.poseCostType = poseCostType;
            this.currentPose = currentPose;
            this.currentTrajectory = trajectory;
            this.whereWeCanFindingPos = whereWeCanFindingPos;
            this.trajectoryWeight = trajectoryWeight;
            this.poseWeight = poseWeight;
        }
    }

    [BurstCompile]
    public struct MotionMatchingContactEnterJob : IJob
    {
        // Input
        public PoseCostType poseCostType;
        public TrajectoryCostType trajectoryCostType;
        public ContactStateMovemetType cntactMovmentType;
        public ContactPointCostType contactCostType;
        public int middleContactsCount;

        [ReadOnly]
        public NativeArray<FrameDataInfo> framesInfo;
        [ReadOnly]
        public NativeArray<BoneData> bonesData;
        [ReadOnly]
        public NativeArray<TrajectoryPoint> trajectoryPoints;
        [ReadOnly]
        public NativeArray<FrameContact> contactPoints;

        // Changing input
        [ReadOnly]
        public NativeArray<FrameContact> currentContactPoints; // Local space
        [ReadOnly]
        public NativeArray<BoneData> currentPose;
        [ReadOnly]
        public NativeArray<TrajectoryPoint> currentTrajectory;
        [ReadOnly]
        public NativeArray<float2> whereWeCanFindingPos;
        [ReadOnly]
        float trajectoryWeight;
        [ReadOnly]
        float poseWeight;
        [ReadOnly]
        float contactWeight;

        // Output
        [WriteOnly]
        public NativeArray<NewClipInfoToPlay> infos;

        public void Execute()
        {

            NewClipInfoToPlay output = new NewClipInfoToPlay(-1, 0, -1, float.MaxValue);

            int poseStep = currentPose.Length;
            int frameCount = framesInfo.Length;
            int currentClipIndex;
            int contactPointIndex;
            int trajectoryStep = currentTrajectory.Length;

            float currentCost;

            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                currentClipIndex = framesInfo[frameIndex].clipIndex;
                if (
                    framesInfo[frameIndex].localTime < whereWeCanFindingPos[currentClipIndex].x ||
                    framesInfo[frameIndex].localTime > whereWeCanFindingPos[currentClipIndex].y
                    )
                {
                    continue;
                }

                currentCost = 0;

                float contactCost = 0f;

                int lastContactsIndex;

                switch (cntactMovmentType)
                {
                    case ContactStateMovemetType.StartContact:
                        contactPointIndex = frameIndex * (middleContactsCount + 1);
                        for (int i = 0; i <= middleContactsCount; i++)
                        {
                            contactCost += contactPoints[contactPointIndex].CalculateCost(currentContactPoints[i], contactCostType);
                            contactPointIndex++;
                        }
                        break;
                    case ContactStateMovemetType.ContactLand:
                        contactPointIndex = frameIndex * (middleContactsCount + 1);
                        for (int i = 1; i < middleContactsCount; i++)
                        {
                            contactCost += contactPoints[contactPointIndex].CalculateCost(currentContactPoints[i], contactCostType);
                            contactPointIndex++;
                        }
                        lastContactsIndex = currentContactPoints.Length - 1;
                        contactCost += contactPoints[contactPointIndex].CalculateCost(currentContactPoints[lastContactsIndex], contactCostType);
                        break;
                    case ContactStateMovemetType.StartContactLand:
                        contactPointIndex = frameIndex * (middleContactsCount + 2);
                        for (int i = 0; i <= middleContactsCount; i++)
                        {
                            contactCost += contactPoints[contactPointIndex].CalculateCost(currentContactPoints[i], contactCostType);
                            contactPointIndex++;
                        }
                        lastContactsIndex = currentContactPoints.Length - 1;
                        contactCost += contactPoints[contactPointIndex].CalculateCost(currentContactPoints[lastContactsIndex], contactCostType);
                        break;
                    case ContactStateMovemetType.Contact:
                        contactPointIndex = frameIndex * middleContactsCount;
                        for (int i = 1; i < middleContactsCount; i++)
                        {
                            contactCost += contactPoints[contactPointIndex].CalculateCost(currentContactPoints[i], contactCostType);
                            contactPointIndex++;
                        }
                        break;
                    case ContactStateMovemetType.StartLand:
                        contactPointIndex = frameIndex * 2;
                        contactCost += contactPoints[contactPointIndex].CalculateCost(currentContactPoints[0], contactCostType);
                        contactPointIndex++;
                        lastContactsIndex = currentContactPoints.Length - 1;
                        contactCost += contactPoints[contactPointIndex].CalculateCost(currentContactPoints[lastContactsIndex], contactCostType);
                        break;
                }

                int tpIndex = frameIndex * trajectoryStep;
                float trajectoryCost = 0f;
                for (int i = 0; i < trajectoryStep; i++)
                {
                    trajectoryCost += currentTrajectory[i].CalculateCost(trajectoryPoints[tpIndex], trajectoryCostType);
                    tpIndex++;
                }

                int boneIndex = frameIndex * poseStep;
                float poseCost = 0f;
                for (int i = 0; i < poseStep; i++)
                {
                    poseCost += currentPose[i].CalculateCost(bonesData[boneIndex], poseCostType);
                    boneIndex++;
                }

                currentCost = contactCost * contactWeight + trajectoryCost * trajectoryWeight + poseCost * poseWeight;

                if (currentCost < output.bestCost)
                {
                    output.Set(
                            framesInfo[frameIndex].clipIndex,
                            framesInfo[frameIndex].localTime,
                            currentCost
                            );
                }
            }

            infos[0] = output;
        }

        public void SetBasicOptions(
            NativeArray<FrameDataInfo> framesInfo,
            NativeArray<BoneData> bonesData,
            NativeArray<TrajectoryPoint> trajectoryPoints,
            NativeArray<FrameContact> contactPoints,
            NativeArray<NewClipInfoToPlay> infos,
            int middleContactsCount
            )
        {
            this.contactPoints = contactPoints;
            this.framesInfo = framesInfo;
            this.bonesData = bonesData;
            this.trajectoryPoints = trajectoryPoints;
            this.infos = infos;
            this.middleContactsCount = middleContactsCount;
        }

        public void SetChangingOptions(
            PoseCostType poseCostType,
            TrajectoryCostType trajectoryCostType,
            ContactPointCostType contactCostType,
            ContactStateMovemetType movementType,
            NativeArray<BoneData> currentPose,
            NativeArray<TrajectoryPoint> currentTrajectory,
            NativeArray<FrameContact> currentContacts,
            NativeArray<float2> whereWeCanFindingPos,
            float trajectoryWeight,
            float poseWeight,
            float contactWeight
            )
        {
            this.poseCostType = poseCostType;
            this.trajectoryCostType = trajectoryCostType;
            this.contactCostType = contactCostType;
            this.cntactMovmentType = movementType;
            this.currentPose = currentPose;
            this.currentTrajectory = currentTrajectory;
            this.currentContactPoints = currentContacts;
            this.whereWeCanFindingPos = whereWeCanFindingPos;
            this.trajectoryWeight = trajectoryWeight;
            this.poseWeight = poseWeight;
            this.contactWeight = contactWeight;
        }
    }

    public struct MotionMatchingImpactJob : IJob
    {
        // Input
        public PoseCostType poseCostType;
        public ContactPointCostType contactCostType;
        [ReadOnly]
        public NativeArray<FrameDataInfo> framesInfo;
        [ReadOnly]
        public NativeArray<BoneData> bonesData;
        [ReadOnly]
        public NativeArray<FrameContact> contactPoints;

        // Changing input
        [ReadOnly]
        public NativeArray<FrameContact> currentContactPoints; // Local space
        [ReadOnly]
        public NativeArray<BoneData> currentPose;
        [ReadOnly]
        float poseWeight;
        [ReadOnly]
        float contactWeight;

        // Output
        [WriteOnly]
        public NativeArray<NewClipInfoToPlay> infos;

        public void Execute()
        {
            ImpactFinding();
        }

        private void ImpactFinding()
        {
            NewClipInfoToPlay output = new NewClipInfoToPlay(-1, 0, -1, float.MaxValue);

            int poseStep = currentPose.Length;
            int frameCount = framesInfo.Length;

            float currentCost;

            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                float contactCost = contactPoints[frameIndex].CalculateCost(currentContactPoints[0], contactCostType);

                int boneIndex = frameIndex * poseStep;
                float poseCost = 0f;
                for (int i = 0; i < poseStep; i++)
                {
                    poseCost += currentPose[i].CalculateCost(bonesData[boneIndex], poseCostType);
                    boneIndex++;
                }

                currentCost = poseCost * poseWeight + contactCost * contactWeight;
                if (currentCost < output.bestCost)
                {
                    output.Set(
                            framesInfo[frameIndex].clipIndex,
                            framesInfo[frameIndex].localTime,
                            currentCost
                            );
                }
            }

            infos[0] = output;
        }

        public void SetBasicOptions(
            NativeArray<FrameDataInfo> framesInfo,
            NativeArray<BoneData> bonesData,
            NativeArray<FrameContact> contactPoints,
            NativeArray<NewClipInfoToPlay> infos
            )
        {
            this.contactPoints = contactPoints;
            this.framesInfo = framesInfo;
            this.bonesData = bonesData;
            this.infos = infos;
        }

        public void SetChangingOptions(
            PoseCostType poseCostType,
            ContactPointCostType contactCostType,
            NativeArray<BoneData> currentPose,
            NativeArray<FrameContact> currentContacts,
            float poseWeight,
            float contactWeight
            )
        {
            this.poseCostType = poseCostType;
            this.contactCostType = contactCostType;
            this.currentPose = currentPose;
            this.currentContactPoints = currentContacts;
            this.poseWeight = poseWeight;
            this.contactWeight = contactWeight;
        }
    }

    [BurstCompile]
    public struct NewClipInfoToPlay
    {
        public int clipIndex;
        public int groupIndex;
        public double localTime;
        public float bestCost;

        public NewClipInfoToPlay(int clipIndex, int groupIndex, double localTime, float cost)
        {
            this.clipIndex = clipIndex;
            this.groupIndex = groupIndex;
            this.localTime = localTime;
            this.bestCost = cost;
        }

        public void Set(int clipIndex, double localTime, float cost)
        {
            this.clipIndex = clipIndex;
            this.localTime = localTime;
            this.bestCost = cost;
        }
    }

    public struct CurrentPlayedClipInfo
    {
        public int clipIndex;
        public int groupIndex;
        public float localTime;
        public bool notFindInYourself;

        public CurrentPlayedClipInfo(int clipIndex, int groupIndex, float currentTime, bool notFindInYourself)
        {
            this.clipIndex = clipIndex;
            this.localTime = currentTime;
            this.notFindInYourself = notFindInYourself;
            this.groupIndex = groupIndex;
        }
    }


    [BurstCompile]
    public struct Burst_MotionMatching
    {
        public static bool TheWinnerIsAtTheSameLocation(
                int currentClipIndex,
                int currentGroupIndex,
                float currentClipTime,
                float currentClipLength,
                NewClipInfoToPlay info,
                float maxClipDeltaTime,
                bool isLooping
                )
        {
            if (currentClipIndex != info.clipIndex || currentGroupIndex != info.groupIndex)
            {
                return false;
            }

            float clipDeltaLoop = currentClipTime + currentClipLength - (float)info.localTime;
            if (isLooping && clipDeltaLoop < maxClipDeltaTime)
            {
                return true;
            }

            float clipDeltaTime = math.abs((float)(currentClipTime - info.localTime));
            if (clipDeltaTime > maxClipDeltaTime)
            {
                return false;
            }

            return true;
        }
    }
}
