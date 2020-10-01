using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Playables;

namespace DW_Gameplay
{
    // Single animation Motion machiing state logic

    public class LogicSingleAnimationState : LogicState
    {
        private MotionMatchingSingleAnimationJob[] saJobs; // single animation job

        public LogicSingleAnimationState(
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
            saJobs = new MotionMatchingSingleAnimationJob[dataState.maxJobsCount];
            for (int i = 0; i < dataState.maxJobsCount; i++)
            {
                saJobs[i] = new MotionMatchingSingleAnimationJob();
                saJobs[i].SetBasicOptions(
                    this.dataState.motionDataGroups[currentMotionDataGroupIndex].framesInfoPerJob[i],
                    this.dataState.motionDataGroups[currentMotionDataGroupIndex].trajectoryPointsPerJob[i],
                    this.dataState.motionDataGroups[currentMotionDataGroupIndex].bonesPerJob[i],
                    bestInfosFromJob[i]
                    );
            }
        }

        protected override void Start()
        {
            currentDataIndex = dataState.startClipIndex;
            currentClipLocalTime = dataState.startClipTime;

            playableGraph.CreateBlendMotionMatchingAnimation(
                        dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex],
                        currentDataIndex,
                        stateMixer,
                        currentClipLocalTime,
                        dataState.saFeatures.blendTime,
                        blendingSpeeds,
                        currentWeights,
                        animationsSequences,
                        this.logicLayer.GetPassIK(),
                        this.logicLayer.GetFootPassIK(),
                        1f
                        );

            currentPlayedClipsIndexes.Add(currentDataIndex);

        }

        protected override void Destroy()
        {

        }

        protected override void Enter(
            PoseData currentPose,
            Trajectory previouStateGoal,
            List<float2> whereCanFindingBestPose
            )
        {
            NativeArray<float2> findingIntervals = new NativeArray<float2>(whereCanFindingBestPose.ToArray(), Allocator.TempJob);

            for (int i = 0; i < currentPose.Count; i++)
            {
                logicLayer.nativePose[i] = currentPose.GetBoneData(i);
            }

            for (int i = 0; i < previouStateGoal.Length; i++)
            {
                logicLayer.nativeTrajectory[i] = previouStateGoal.GetPoint(i);
            }

            SingleAnimationFinding(
                logicLayer.nativePose,
                logicLayer.nativeTrajectory,
                findingIntervals
                );

            findingIntervals.Dispose();

            currentDataIndex = logicLayer.bestPoseInfo.clipIndex;
            currentClipLocalTime = (float)logicLayer.bestPoseInfo.localTime;

            playableGraph.CreateBlendMotionMatchingAnimation(
                        dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex],
                        currentDataIndex,
                        stateMixer,
                        currentClipLocalTime,
                        dataState.saFeatures.blendTime,
                        blendingSpeeds,
                        currentWeights,
                        animationsSequences,
                        this.logicLayer.GetPassIK(),
                        this.logicLayer.GetFootPassIK(),
                        1f
                        );

            currentPlayedClipsIndexes.Add(currentDataIndex);
        }

        protected override void Update()
        {
            BeforeUpdateExit();

            switch (dataState.saFeatures.updateType)
            {
                case SingleAnimationUpdateType.PlaySelected:
                    if (!dataState.saFeatures.loop)
                    {
                        if (currentClipGlobalTime > dataState.saFeatures.loopCountBeforeStop * dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex].animationLength)
                        {
                            this.stateMixer.GetInput(0).Pause();
                            currentClipLocalTime = dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex].animationLength;
                        }
                    }
                    break;
                case SingleAnimationUpdateType.PlayInSequence:
                    AnimationSequenceUpdate();
                    break;
                case SingleAnimationUpdateType.PlayRandom:
                    RandomAnimationUpdate();
                    break;
            }
        }

        private void AnimationSequenceUpdate()
        {
            if (currentClipLocalTime >= (dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex].animationLength - dataState.saFeatures.blendTime))
            {
                int nextClipIndex = (currentDataIndex + 1) % dataState.motionDataGroups[currentMotionDataGroupIndex].animationData.Count;

                currentDataIndex = nextClipIndex;

                playableGraph.CreateBlendMotionMatchingAnimation(
                        dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex],
                        currentDataIndex,
                        stateMixer,
                        0f,
                        dataState.saFeatures.blendTime,
                        blendingSpeeds,
                        currentWeights,
                        animationsSequences,
                        this.logicLayer.GetPassIK(),
                        this.logicLayer.GetFootPassIK(),
                        0.000001f
                        );

                currentPlayedClipsIndexes.Add(currentDataIndex);
            }
        }

        private void RandomAnimationUpdate()
        {
            if (currentClipLocalTime >= (GetCurrentMMData().animationLength - dataState.saFeatures.blendTime))
            {
                int nextClipIndex = Mathf.FloorToInt(UnityEngine.Random.Range(0, dataState.motionDataGroups[currentMotionDataGroupIndex].animationData.Count - 0.5f));

                if (!dataState.saFeatures.blendToTheSameAnimation && currentDataIndex == nextClipIndex)
                {
                    nextClipIndex = (nextClipIndex + 1) % dataState.motionDataGroups[currentMotionDataGroupIndex].animationData.Count;
                }

                currentDataIndex = nextClipIndex;

                playableGraph.CreateBlendMotionMatchingAnimation(
                        dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex],
                        currentDataIndex,
                        stateMixer,
                        0f,
                        dataState.saFeatures.blendTime,
                        blendingSpeeds,
                        currentWeights,
                        animationsSequences,
                        this.logicLayer.GetPassIK(),
                        this.logicLayer.GetFootPassIK(),
                        0.000001f
                        );

                currentPlayedClipsIndexes.Add(currentDataIndex);
            }
        }

        public override void BeforeUpdateExit()
        {
            playableGraph.BlendPlayablesInStateMixer(
                ref stateMixer,
                blendingSpeeds,
                currentWeights
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

        private void SingleAnimationFinding(
            NativeArray<BoneData> currentPose,
            NativeArray<TrajectoryPoint> newTrajectory,
            NativeArray<float2> whereWeCanFindingPosition
            )
        {
            for (int jobIndex = 0; jobIndex < jobsHandle.Length; jobIndex++)
            {
                saJobs[jobIndex].SetChangingOptions(
                    dataState.trajectoryCostType,
                    dataState.poseCostType,
                    currentPose,
                    newTrajectory,
                    whereWeCanFindingPosition,
                    this.dataState.trajectoryCostWeight,
                    this.dataState.poseCostWeight
                    );
                jobsHandle[jobIndex] = saJobs[jobIndex].Schedule();
            }
            JobHandle.ScheduleBatchedJobs();
            JobHandle.CompleteAll(jobsHandle);

            JoinJobsOutput();
        }

        protected override void LateUpdate()
        {
        }

        protected override void Exit()
        {
        }
    }
}
