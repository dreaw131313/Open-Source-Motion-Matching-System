using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace DW_Gameplay
{
    public class LogicImpactState : LogicState
    {
        private MotionMatchingImpactJob[] isJobs; // impact state jobs

        public LogicImpactState(
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

        private void OnCreate()
        {
            currentMotionDataGroupIndex = 0;
            isJobs = new MotionMatchingImpactJob[dataState.maxJobsCount];
            logicLayer.adaptedContactPoints = new List<MotionMatchingContact>();

            for (int i = 0; i < dataState.maxJobsCount; i++)
            {
                isJobs[i] = new MotionMatchingImpactJob();
                isJobs[i].SetBasicOptions(
                    this.dataState.motionDataGroups[currentMotionDataGroupIndex].framesInfoPerJob[i],
                    this.dataState.motionDataGroups[currentMotionDataGroupIndex].bonesPerJob[i],
                    this.dataState.motionDataGroups[currentMotionDataGroupIndex].contactPointsPerJob[i],
                    bestInfosFromJob[i]
                    );
            }
        }

        public override void BeforeUpdateExit()
        {
        }

        protected override void Destroy()
        {
        }

        protected override void Enter(PoseData currentPose, Trajectory previouStateGoal, List<float2> whereCanFindingBestPose)
        {
            NativeArray<float2> findingIntervals = new NativeArray<float2>(whereCanFindingBestPose.ToArray(), Allocator.TempJob);
            NativeArray<FrameContact> contactPointsNative = new NativeArray<FrameContact>(logicLayer.contactPoints.Count, Allocator.TempJob);

            //making native contactsl
            for (int i = 0; i < logicLayer.contactPoints.Count; i++)
            {
                contactPointsNative[i] = logicLayer.contactPoints[i].frameContact;
            }
            // Geting native pose
            for (int i = 0; i < currentPose.Count; i++)
            {
                logicLayer.nativePose[i] = currentPose.GetBoneData(i);
            }

            //Geting natve trajectory
            for (int i = 0; i < previouStateGoal.Length; i++)
            {
                logicLayer.nativeTrajectory[i] = previouStateGoal.GetPoint(i);
            }

            ImpactAnimationFinding(
                logicLayer.nativePose,
                findingIntervals,
                contactPointsNative,
                logicLayer.nativeTrajectory
                );

            findingIntervals.Dispose();
            contactPointsNative.Dispose();

            currentDataIndex = logicLayer.bestPoseInfo.clipIndex;
            currentClipLocalTime = (float)logicLayer.bestPoseInfo.localTime;

            playableGraph.CreateBlendMotionMatchingAnimation(
                        dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex],
                        currentDataIndex,
                        stateMixer,
                        currentClipLocalTime,
                        1f,
                        blendingSpeeds,
                        currentWeights,
                        animationsSequences,
                        this.logicLayer.GetPassIK(),
                        this.logicLayer.GetFootPassIK(),
                        1f
                        );
            currentPlayedClipsIndexes.Add(currentDataIndex);
        }

        protected override void Exit()
        {
        }

        protected override void LateUpdate()
        {
        }

        protected override void Start()
        {
            throw new System.Exception("Impact state cannot be start state!");
        }

        protected override void Update()
        {
            base.Update();
        }

        private void ImpactAnimationFinding(
           NativeArray<BoneData> currentPose,
           NativeArray<float2> whereWeCanFindingPosition,
           NativeArray<FrameContact> contactPoints,
           NativeArray<TrajectoryPoint> currentTrajectory
           )
        {
            for (int jobIndex = 0; jobIndex < jobsHandle.Length; jobIndex++)
            {
                isJobs[jobIndex].SetChangingOptions(
                    this.dataState.poseCostType,
                    this.dataState.csFeatures.contactCostType,
                    currentPose,
                    contactPoints,
                    this.dataState.poseCostWeight,
                    this.dataState.csFeatures.contactPointsWeight
                    );
                jobsHandle[jobIndex] = isJobs[jobIndex].Schedule();
            }

            JobHandle.ScheduleBatchedJobs();

            JobHandle.CompleteAll(jobsHandle);

            JoinJobsOutput();
        }

        public override void ForceAnimationFindngInternal()
        {
            base.ForceAnimationFindngInternal();
            NativeArray<float2> findingIntervals = new NativeArray<float2>(0, Allocator.TempJob);
            NativeArray<FrameContact> contactPointsNative = new NativeArray<FrameContact>(logicLayer.contactPoints.Count, Allocator.TempJob);

            //making native contactsl
            for (int i = 0; i < logicLayer.contactPoints.Count; i++)
            {
                contactPointsNative[i] = logicLayer.contactPoints[i].frameContact;
            }

            ImpactAnimationFinding(
                logicLayer.nativePose,
                findingIntervals,
                contactPointsNative,
                logicLayer.nativeTrajectory
                );

            findingIntervals.Dispose();
            contactPointsNative.Dispose();

            currentDataIndex = logicLayer.bestPoseInfo.clipIndex;
            currentClipLocalTime = (float)logicLayer.bestPoseInfo.localTime;
            lastFrameAnimationTime = currentClipLocalTime;

            playableGraph.CreateBlendMotionMatchingAnimation(
                        dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex],
                        currentDataIndex,
                        stateMixer,
                        currentClipLocalTime,
                        1f,
                        blendingSpeeds,
                        currentWeights,
                        animationsSequences,
                        this.logicLayer.GetPassIK(),
                        this.logicLayer.GetFootPassIK(),
                        1f
                        );
            currentPlayedClipsIndexes.Add(currentDataIndex);
        }
    }
}
