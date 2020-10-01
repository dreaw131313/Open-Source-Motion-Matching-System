using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Playables;

namespace DW_Gameplay
{
    // Motion machiing state logic
    public class LogicMotionMatchingState : LogicState
    {
        private BasicMotionMatchingJob[] mmJobs; // motion matching job

        public LogicMotionMatchingState(
            MotionMatchingState state,
            MotionMatching component,
            MotionMatchingPlayableGraph playableGraph,
            LogicMotionMatchingLayer logicLayer,
            Transform gameObject,
            int framesForJob
            ) :
            base(state, component, playableGraph, logicLayer, gameObject, framesForJob)
        {
            OnCreate();
        }

        bool isInLastFrameFindingBegin = false; // if true waiting for jobs complete their execute

        int motionGroupSwitchIndex = -1;

        private void OnCreate()
        {

            currentMotionDataGroupIndex = dataState.motionDataGroupsIndexes[dataState.startMotionDataGroup];

            currentPlayedClipsInfo = new NativeList<CurrentPlayedClipInfo>(dataState.mmFeatures.maxBlendedClipCount, Allocator.Persistent);
            mmJobs = new BasicMotionMatchingJob[dataState.maxJobsCount];

            for (int i = 0; i < GetCurrentMotionGroup().jobsCount; i++)
            {
                mmJobs[i] = new BasicMotionMatchingJob();
                mmJobs[i].SetBasicOptions(
                    this.dataState.motionDataGroups[currentMotionDataGroupIndex].framesInfoPerJob[i],
                    this.dataState.motionDataGroups[currentMotionDataGroupIndex].trajectoryPointsPerJob[i],
                    this.dataState.motionDataGroups[currentMotionDataGroupIndex].bonesPerJob[i],
                    bestInfosFromJob[i]
                    );
            }
        }

        protected override void Destroy()
        {
        }

        protected override void Start()
        {
            currentPlayedClipsIndexes = new List<int>();
            blendingSpeeds = new List<float>();
            dataState.startClipIndex = Mathf.Clamp(dataState.startClipIndex, 0, dataState.motionDataGroups[currentMotionDataGroupIndex].animationData.Count);
            dataState.mmFeatures.timer = 0f;
            currentDataIndex = dataState.startClipIndex;
            currentClipLocalTime = dataState.startClipTime;

            currentPlayedClipsIndexes.Add(currentDataIndex);

            playableGraph.CreateBlendMotionMatchingAnimation(
                        dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex],
                        currentDataIndex,
                        stateMixer,
                        currentClipLocalTime,
                        dataState.mmFeatures.blendTime,
                        blendingSpeeds,
                        currentWeights,
                        animationsSequences,
                        this.logicLayer.GetPassIK(),
                        this.logicLayer.GetFootPassIK(),
                        0.000001f
                        );
        }

        protected override void Enter(PoseData currentPose, Trajectory previouStateGoal, List<float2> whereCanFindingBestPose)
        {
            for (int i = 0; i < currentPose.Count; i++)
            {
                logicLayer.nativePose[i] = currentPose.GetBoneData(i);
            }

            for (int i = 0; i < previouStateGoal.Length; i++)
            {
                logicLayer.nativeTrajectory[i] = previouStateGoal.GetPoint(i);
            }

            if (this.dataState.sectionsDependencies != null)
            {
                SetCurrentSection(this.dataState.startSection);
            }

            SwitchMotionDataGroup();
            GetCurrentClipsInfo();
            MotionMatchingFinding(
                logicLayer.nativePose,
                logicLayer.nativeTrajectory
                );
            JobHandle.ScheduleBatchedJobs();
            JobHandle.CompleteAll(jobsHandle);
            JoinJobsOutput();

            currentDataIndex = logicLayer.bestPoseInfo.clipIndex;
            currentPlayedClipsIndexes.Add(currentDataIndex);
            playableGraph.CreateBlendMotionMatchingAnimation(
                dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex],
                logicLayer.bestPoseInfo.clipIndex,
                stateMixer,
                logicLayer.bestPoseInfo.localTime,
                this.dataState.mmFeatures.blendTime,
                blendingSpeeds,
                currentWeights,
                animationsSequences,
                this.logicLayer.GetPassIK(),
                this.logicLayer.GetFootPassIK(),
                1.0f,
                this.dataState.mmFeatures.minWeightToAchive
                );

            //CreateMMAnimation(this.dataState.mmFeatures.blendTime, 1f);
        }

        public void MotionMatching_FixedUpdate()
        {

        }

        protected override void Update()
        {
            if (isInLastFrameFindingBegin)
            {
                JobHandle.CompleteAll(jobsHandle);
                JoinJobsOutput();
                CheckWinnerPosition();
                CreateMMAnimation(this.dataState.mmFeatures.blendTime);
                isInLastFrameFindingBegin = false;
            }

            BeforeUpdateExit();

            dataState.mmFeatures.timer += Time.deltaTime;

            if (dataState.mmFeatures.timer >= dataState.mmFeatures.updateInterval)
            {
                dataState.mmFeatures.timer = 0f;
                if (this.stateMixer.GetInputCount() < dataState.mmFeatures.maxBlendedClipCount &&
                    dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex].CanLookingForNewPose(currentClipLocalTime))
                {
                    SwitchMotionDataGroup();
                    CalculateCurrentPose();
                    SetNativePose();
                    SetNativeTrajectory();
                    GetCurrentSectionDependeces();

                    MotionMatchingFinding(
                        logicLayer.nativePose,
                        logicLayer.nativeTrajectory
                        );
                    isInLastFrameFindingBegin = true;
                    //JoinJobsOutput();
                    //CheckWinnerPosition();
                    //CreateMMAnimation(this.state.mmFeatures.blendTime);
                }
            }

        }

        public override void BeforeUpdateExit()
        {
            playableGraph.BlendPlayablesInStateMixer(
                ref stateMixer,
                blendingSpeeds,
                currentWeights,
                this.dataState.mmFeatures.minWeightToAchive,
                this.dataState.mmFeatures.blendTime
                );

            playableGraph.RemoveZeroWeightsInputsAnimations(
                stateMixer,
                blendingSpeeds,
                currentWeights,
                currentPlayedClipsIndexes,
                animationsSequences
                );

            for (int i = 0; i < animationsSequences.Count; i++)
            {
                animationsSequences[i].Update(
                    this.playableGraph,
                    this.logicLayer.GetPassIK(),
                    this.logicLayer.GetFootPassIK(),
                    Time.deltaTime
                    );
            }
        }

        protected override void LateUpdate()
        {

        }

        protected override void Exit()
        {
            JobHandle.CompleteAll(jobsHandle);
            dataState.mmFeatures.timer = 0.0f;
        }

        private void MotionMatchingFinding(
            NativeArray<BoneData> pose,
            NativeArray<TrajectoryPoint> traj
            )
        {
            for (int i = 0; i < jobsHandle.Length; i++)
            {
                mmJobs[i].SetChangingOptions(
                    /*motionMatching.ForceTrajectoryCostType ? motionMatching.TrajectoryCostType :*/ dataState.trajectoryCostType,
                    dataState.poseCostType,
                    pose,
                    traj,
                    currentPlayedClipsInfo,
                    currentClipsSection[currentMotionDataGroupIndex],
                    this.sectionDependecies,
                    this.dataState.trajectoryCostWeight,
                    this.dataState.poseCostWeight,
                    currentMotionDataGroupIndex
                    );
                //jobsHandle[i] = mmJobs[i].Schedule();
            }


            for (int i = 0; i < jobsHandle.Length; i++)
            {
                jobsHandle[i] = mmJobs[i].Schedule();
            }

            //JobHandle.ScheduleBatchedJobs();

            //JobHandle.CompleteAll(jobsHandle);
        }

        private void CheckWinnerPosition()
        {
            for (int i = 0; i < currentPlayedClipsInfo.Length; i++)
            {
                if (Burst_MotionMatching.TheWinnerIsAtTheSameLocation(
                        currentPlayedClipsInfo[i].clipIndex,
                        currentPlayedClipsInfo[i].groupIndex,
                        currentPlayedClipsInfo[i].localTime,
                        dataState.motionDataGroups[currentPlayedClipsInfo[i].groupIndex].animationData[currentPlayedClipsInfo[i].clipIndex].animationLength,
                        logicLayer.bestPoseInfo,
                        dataState.mmFeatures.maxClipDeltaTime,
                        dataState.motionDataGroups[currentPlayedClipsInfo[i].groupIndex].animationData[currentPlayedClipsInfo[i].clipIndex].isLooping
                    ))
                {
                    logicLayer.bestPoseInfo.Set(-1, -1, -1);
                    break;
                }
            }
        }

        private void CreateMMAnimation(float blendTime, float newInputStartWeight = 0f)
        {
            if (logicLayer.bestPoseInfo.clipIndex != -1)
            {
                if (!(currentMotionDataGroupIndex == logicLayer.bestPoseInfo.groupIndex &&
                    currentDataIndex == logicLayer.bestPoseInfo.clipIndex &&
                    !GetCurrentMMData().blendToYourself))
                {
                    playableGraph.CreateBlendMotionMatchingAnimation(
                        dataState.motionDataGroups[logicLayer.bestPoseInfo.groupIndex].animationData[logicLayer.bestPoseInfo.clipIndex],
                        logicLayer.bestPoseInfo.clipIndex,
                        stateMixer,
                        logicLayer.bestPoseInfo.localTime,
                        blendTime,
                        blendingSpeeds,
                        currentWeights,
                        animationsSequences,
                        this.logicLayer.GetPassIK(),
                        this.logicLayer.GetFootPassIK(),
                        newInputStartWeight,
                        this.dataState.mmFeatures.minWeightToAchive
                        );
                    currentDataIndex = logicLayer.bestPoseInfo.clipIndex;
                    currentPlayedClipsIndexes.Add(currentDataIndex);
                    currentClipGlobalTime = GetCurrentPlayableTime();
                    SetCurrentClipLocalTime();
                    CalculateCurrentEventMarker();
                }
            }
        }

        public override bool SetMotionDataGroup(string groupName)
        {
            int groupIndex;
            if (dataState.motionDataGroupsIndexes.TryGetValue(groupName, out groupIndex))
            {
                motionGroupSwitchIndex = groupIndex;
                return true;
            }
            else
            {
                Debug.LogWarning(string.Format("In state {0} not exist motion data group with name {1}", dataState.GetName(), groupName));
            }

            return false;
        }

        private void MotionGroupChangeDelegateFunction()
        {
            for (int i = 0; i < GetCurrentMotionGroup().jobsCount; i++)
            {
                mmJobs[i].SetBasicOptions(
                    GetCurrentMotionGroup().framesInfoPerJob[i],
                    GetCurrentMotionGroup().trajectoryPointsPerJob[i],
                    GetCurrentMotionGroup().bonesPerJob[i],
                    bestInfosFromJob[i]
                    );
            }

        }

        private void SwitchMotionDataGroup()
        {
            if (motionGroupSwitchIndex > -1)
            {
                currentMotionDataGroupIndex = motionGroupSwitchIndex;
                MotionGroupChangeDelegateFunction();

                motionGroupSwitchIndex = -1;
            }
        }
    }
}
