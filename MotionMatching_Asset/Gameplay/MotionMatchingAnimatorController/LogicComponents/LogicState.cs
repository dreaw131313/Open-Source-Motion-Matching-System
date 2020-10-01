using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace DW_Gameplay
{
    public class LogicState
    {
        public MotionMatchingState dataState;

        protected List<int> currentPlayedClipsIndexes = new List<int>();
        protected List<float> blendingSpeeds = new List<float>();
        protected List<float> currentWeights;
        protected List<LogicAnimationsSequence> animationsSequences;

        protected MotionMatching motionMatching;
        protected MotionMatchingPlayableGraph playableGraph;
        protected LogicMotionMatchingLayer logicLayer;
        protected Transform Transform;

        protected int currentDataIndex;
        protected float currentClipLocalTime;
        protected float currentClipGlobalTime;
        protected float lastFrameAnimationTime;
        protected bool isBlockedToEnter;

        protected int currentEventMarkerIndex;

        protected AnimationMixerPlayable stateMixer;

        protected List<MotionMatchingStateBehavior> stateBehaviors;

        // Jobs option

        protected NativeArray<int>[] currentClipsSection;
        protected NativeList<CurrentPlayedClipInfo> currentPlayedClipsInfo;
        protected NativeList<SectionInfo> sectionDependecies;
        protected NativeArray<JobHandle> jobsHandle;
        protected NativeArray<NewClipInfoToPlay>[] bestInfosFromJob;

        protected int currentMotionDataGroupIndex = 0;

        public float speedMultiplier = 1f;

        public float CurrentClipLocalTime { get => currentClipLocalTime; private set => currentClipLocalTime = value; }
        public float CurrentClipGlobalTime { get => currentClipGlobalTime; private set => currentClipGlobalTime = value; }

        protected LogicState(
            MotionMatchingState state,
            MotionMatching component,
            MotionMatchingPlayableGraph playableGraph,
            LogicMotionMatchingLayer logicLayer,
            Transform gameObject,
            int framesForJob
            )
        {
            this.dataState = state;
            this.playableGraph = playableGraph;
            this.logicLayer = logicLayer;
            this.motionMatching = component;
            this.Transform = gameObject;

            isBlockedToEnter = false;
            speedMultiplier = this.dataState.speedMultiplier;

            currentPlayedClipsIndexes = new List<int>();
            blendingSpeeds = new List<float>();
            animationsSequences = new List<LogicAnimationsSequence>();
            currentWeights = new List<float>();

            stateBehaviors = new List<MotionMatchingStateBehavior>();

            if (this.dataState.sectionsDependencies != null)
            {
                sectionDependecies = new NativeList<SectionInfo>(this.dataState.sectionsDependencies.sectionSettings.Count, Allocator.Persistent);
            }
            else
            {
                sectionDependecies = new NativeList<SectionInfo>(0, Allocator.Persistent);
            }

            state.CreateStructureAnimationData(framesForJob);

            jobsHandle = new NativeArray<JobHandle>(state.maxJobsCount, Allocator.Persistent);
            bestInfosFromJob = new NativeArray<NewClipInfoToPlay>[state.maxJobsCount];

            currentClipsSection = new NativeArray<int>[state.motionDataGroups.Count];

            for (int motionGroupIndex = 0; motionGroupIndex < currentClipsSection.Length; motionGroupIndex++)
            {
                currentClipsSection[motionGroupIndex] = new NativeArray<int>(state.motionDataGroups[motionGroupIndex].animationData.Count, Allocator.Persistent);
                for (int dataIndex = 0; dataIndex < state.motionDataGroups[motionGroupIndex].animationData.Count; dataIndex++)
                {
                    currentClipsSection[motionGroupIndex][dataIndex] = 0;
                    state.motionDataGroups[motionGroupIndex].animationData[dataIndex].OnLogicStateCreation();
                }
            }


            for (int i = 0; i < state.maxJobsCount; i++)
            {
                bestInfosFromJob[i] = new NativeArray<NewClipInfoToPlay>(1, Allocator.Persistent);
            }


#if UNITY_EDITOR
            // Checking some propably errors:

            // Wrong number of section in MM_Data an section dependences, should be: data.sections.Count>=sectionDependences.Count
            if (state.sectionsDependencies != null)
            {
                for (int motionGroupIndex = 0; motionGroupIndex < state.motionDataGroups.Count; motionGroupIndex++)
                {
                    for (int i = 0; i < state.motionDataGroups[motionGroupIndex].animationData.Count; i++)
                    {
                        if (state.motionDataGroups[motionGroupIndex].animationData[i].sections.Count < state.sectionsDependencies.sectionSettings.Count)
                        {
                            throw new System.Exception(string.Format("In state {0} animation data {1} have less sections than section dependeces!", GetName(), state.motionDataGroups[motionGroupIndex].animationData[i].name));
                        }
                    }
                }
            }
#endif
        }


        protected virtual void Start()
        {

        }

        protected virtual void Enter(PoseData currentPose, Trajectory previouStateGoal, List<float2> whereCanFindingBestPose)
        {

        }

        protected virtual void Update()
        {

        }

        public virtual void BeforeUpdateExit()
        {

        }

        protected virtual void LateUpdate()
        {

        }

        protected virtual void Exit()
        {

        }

        protected virtual void Destroy()
        {

        }

        public virtual void ForceAnimationFindngInternal()
        {
            JobHandle.CompleteAll(jobsHandle);
            CalculateCurrentPose();
            SetNativePose();
            SetNativeTrajectory();
        }

        public virtual bool SetMotionDataGroup(string groupName)
        {
            Debug.Log("nie powinno się to wyswietlac");
            return false;
        }

        public void OnDestroy()
        {
            JobHandle.CompleteAll(jobsHandle);

            dataState.DisposeStructureAnimationData();

            for (int i = 0; i < currentClipsSection.Length; i++)
            {
                currentClipsSection[i].Dispose();
            }

            if (currentPlayedClipsInfo.IsCreated)
            {
                currentPlayedClipsInfo.Dispose();
            }
            if (sectionDependecies.IsCreated)
            {
                sectionDependecies.Dispose();
            }

            for (int i = 0; i < bestInfosFromJob.Length; i++)
            {
                bestInfosFromJob[i].Dispose();
            }

            Destroy();

            jobsHandle.Dispose();
        }

        public void StateStart()
        {
            isBlockedToEnter = true;
            playableGraph.AddStatePlayable(this, logicLayer);

            currentClipLocalTime = 0f;

            Start();

            lastFrameAnimationTime = this.currentClipLocalTime;
            CalculateCurrentEventMarker();
            this.stateMixer.SetSpeed(this.dataState.speedMultiplier);
        }

        public bool WhichTransitionShouldStart()
        {
            for (int i = 0; i < dataState.transitions.Count; i++)
            {
                int optionIndex = dataState.transitions[i].ShouldTransitionBegin(
                    currentClipLocalTime + Time.deltaTime,
                    currentClipGlobalTime + Time.deltaTime,
                    currentDataIndex,
                    motionMatching,
                    dataState.GetStateType(),
                    dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex]
                    );
                if (optionIndex != -1)
                {
                    if (!logicLayer.logicStates[dataState.transitions[i].nextStateIndex].IsBlockedToEnter())
                    {
                        if (stateBehaviors != null)
                        {
                            for (int contactEventIndex = 0; contactEventIndex < stateBehaviors.Count; contactEventIndex++)
                            {
                                stateBehaviors[contactEventIndex].OnStartOutTransition();
                            }
                        }

                        JobHandle.CompleteAll(jobsHandle);
                        logicLayer.SwitchState(
                            GetCurrentPose(),
                            logicLayer.stateInputTrajectoryLocalSpace,
                            dataState.transitions[i].nextStateIndex,
                            dataState.transitions[i].options[optionIndex].blendTime,
                            dataState.transitions[i].options[optionIndex].whereCanFindingBestPose
                            );
                        return true;
                    }
                }
            }

            return false;
        }

        public void StateEnter(
            PoseData currentPose,
            Trajectory previouStateTrajectory,
            List<float2> whereCanFindngNextPose,
            string sectionName = null
            )
        {
            currentDataIndex = -1;
            currentClipLocalTime = -1f;
            isBlockedToEnter = true;
            playableGraph.AddStatePlayable(this, this.logicLayer);
            this.logicLayer.currentWeights.Add(0f);

            if (sectionName != null)
            {
                SetCurrentSection(sectionName);
            }
            else
            {
                this.SetDefaultSection();
            }


            if (stateBehaviors != null)
            {
                for (int i = 0; i < stateBehaviors.Count; i++)
                {
                    stateBehaviors[i].Enter();
                }
            }

            Enter(currentPose, previouStateTrajectory, whereCanFindngNextPose);
            lastFrameAnimationTime = this.currentClipLocalTime;
            CalculateCurrentEventMarker();

            this.stateMixer.SetSpeed(speedMultiplier);
#if UNITY_EDITOR
            speedMultiplier = this.dataState.speedMultiplier;
#endif
        }

        public void StateFixedUpdate()
        {

        }

        public void StateUpdate()
        {
            currentClipGlobalTime = GetCurrentPlayableTime();
            SetCurrentClipLocalTime();

            if (stateBehaviors != null)
            {
                for (int i = 0; i < stateBehaviors.Count; i++)
                {
                    stateBehaviors[i].Update();
                }
            }

            Update();

        }

        public void StateLateUpdate()
        {
            currentClipGlobalTime = GetCurrentPlayableTime();
            SetCurrentClipLocalTime();

            if (stateBehaviors != null)
            {
                for (int i = 0; i < stateBehaviors.Count; i++)
                {
                    stateBehaviors[i].LateUpdate();
                }
            }

            LateUpdate();
            CatchEventMarkers();
            lastFrameAnimationTime = this.currentClipLocalTime;

            WhichTransitionShouldStart();

#if UNITY_EDITOR
            speedMultiplier = this.dataState.speedMultiplier;
            this.stateMixer.SetSpeed(speedMultiplier);
#endif
        }

        public void StateExit()
        {
            isBlockedToEnter = false;

            Exit();


            if (stateBehaviors != null)
            {
                for (int i = 0; i < stateBehaviors.Count; i++)
                {
                    stateBehaviors[i].Exit();
                }
            }

            currentPlayedClipsIndexes.Clear();
            currentWeights.Clear();
            blendingSpeeds.Clear();
            animationsSequences.Clear();
        }

        protected void JoinJobsOutput()
        {
            logicLayer.bestPoseInfo = bestInfosFromJob[0][0];
            for (int i = 1; i < bestInfosFromJob.Length; i++)
            {
                if (logicLayer.bestPoseInfo.bestCost > bestInfosFromJob[i][0].bestCost)
                {
                    logicLayer.bestPoseInfo = bestInfosFromJob[i][0];
                }
            }

        }

        #region GETTERS
        public MotionMatchingStateType GetStateType()
        {
            return this.dataState.GetStateType();
        }

        public string GetName()
        {
            return this.dataState.GetName();
        }

        protected float GetCurrentPlayableTime()
        {
            float time = 0;

            if (stateMixer.GetInputCount() == 0)
            {
                time = 0;
            }
            else
            {
                switch (dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex].dataType)
                {
                    case AnimationDataType.SingleAnimation:
                        time = (float)stateMixer.GetInput(stateMixer.GetInputCount() - 1).GetTime();
                        break;
                    case AnimationDataType.BlendTree:
                        time = (float)stateMixer.GetInput(stateMixer.GetInputCount() - 1).GetInput(0).GetTime();
                        break;
                    case AnimationDataType.AnimationSequence:
                        time = animationsSequences[animationsSequences.Count - 1].GetTime();
                        break;
                }
            }

            return time;
        }

        public float GetCurrentClipTime()
        {
            return currentClipLocalTime;
        }

        public float GetCurrentGlobalTime()
        {
            return currentClipGlobalTime;
        }

        protected void GetCurrentClipsInfo()
        {
            currentPlayedClipsInfo.Clear();
            int seqCounter = 0;
            for (int i = 0; i < currentPlayedClipsIndexes.Count; i++)
            {
                int index = currentPlayedClipsIndexes[i];

                switch (dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[index].dataType)
                {
                    case AnimationDataType.BlendTree:
                        currentPlayedClipsInfo.Add(new CurrentPlayedClipInfo(
                            index,
                            currentMotionDataGroupIndex,
                            dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[index].GetLocalTime((float)stateMixer.GetInput(i).GetInput(0).GetTime()),
                            !dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[index].findInYourself
                            ));
                        break;
                    case AnimationDataType.SingleAnimation:
                        currentPlayedClipsInfo.Add(new CurrentPlayedClipInfo(
                            index,
                            currentMotionDataGroupIndex,
                            dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[index].GetLocalTime((float)stateMixer.GetInput(i).GetTime()),
                            !dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[index].findInYourself
                            ));
                        break;
                    case AnimationDataType.AnimationSequence:
                        currentPlayedClipsInfo.Add(new CurrentPlayedClipInfo(
                            index,
                            currentMotionDataGroupIndex,
                            animationsSequences[seqCounter].GetLocalTime(),
                            !dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[index].findInYourself
                            ));
                        seqCounter++;
                        break;
                }
            }
        }

        protected void CalculateCurrentPose()
        {
            switch (this.dataState.GetStateType())
            {
                case MotionMatchingStateType.MotionMatching:
                    GetCurrentClipsInfo();
                    float weight;

                    for (int i = 0; i < logicLayer.currentPose.Count; i++)
                    {
                        logicLayer.currentPose.SetBone(float3.zero, float3.zero, i);
                    }

                    for (int i = 0; i < currentPlayedClipsInfo.Length; i++)
                    {
                        this.dataState.motionDataGroups[currentPlayedClipsInfo[i].groupIndex].animationData[currentPlayedClipsInfo[i].clipIndex].GetPoseInTime(ref logicLayer.bufforPose, currentPlayedClipsInfo[i].localTime);
                        weight = stateMixer.GetInputWeight(i);

                        for (int j = 0; j < logicLayer.currentPose.Count; j++)
                        {
                            logicLayer.currentPose.SetBone(
                                logicLayer.currentPose.GetBoneData(j).localPosition + weight * logicLayer.bufforPose.GetBoneData(j).localPosition,
                                logicLayer.currentPose.GetBoneData(j).velocity + weight * logicLayer.bufforPose.GetBoneData(j).velocity,
                                j
                                );

                        }
                    }
                    return;
                case MotionMatchingStateType.ContactAnimationState:
                    this.dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex].GetPoseInTime(ref logicLayer.currentPose, currentClipLocalTime);
                    return;
                case MotionMatchingStateType.SingleAnimation:
                    this.dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex].GetPoseInTime(ref logicLayer.currentPose, currentClipLocalTime);
                    return;
            }

            throw new System.Exception("Wrong type of AnimationData!");
        }

        protected List<CurrentPlayedClipInfo> GetCurrentClipsInfo_EditorOnly()
        {
            List<CurrentPlayedClipInfo> clipInfos = new List<CurrentPlayedClipInfo>();
            int seqCounter = 0;
            for (int i = 0; i < currentPlayedClipsIndexes.Count; i++)
            {
                int index = currentPlayedClipsIndexes[i];

                switch (dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[index].dataType)
                {
                    case AnimationDataType.BlendTree:
                        clipInfos.Add(new CurrentPlayedClipInfo(
                            index,
                            currentMotionDataGroupIndex,
                            dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[index].GetLocalTime((float)stateMixer.GetInput(i).GetInput(0).GetTime()),
                            !dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[index].findInYourself
                            ));
                        break;
                    case AnimationDataType.SingleAnimation:
                        clipInfos.Add(new CurrentPlayedClipInfo(
                            index,
                            currentMotionDataGroupIndex,
                            dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[index].GetLocalTime((float)stateMixer.GetInput(i).GetTime()),
                            !dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[index].findInYourself
                            ));
                        break;
                    case AnimationDataType.AnimationSequence:
                        clipInfos.Add(new CurrentPlayedClipInfo(
                            index,
                            currentMotionDataGroupIndex,
                            animationsSequences[seqCounter].GetLocalTime(),
                            !dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[index].findInYourself
                            ));
                        seqCounter++;
                        break;
                }
            }
            return clipInfos;
        }

        public PoseData GetAndCalculateCurrentPose_EditorOnly()
        {
            switch (this.dataState.GetStateType())
            {
                case MotionMatchingStateType.MotionMatching:
                    List<CurrentPlayedClipInfo> clipInfos = GetCurrentClipsInfo_EditorOnly();
                    float weight;

                    for (int i = 0; i < logicLayer.currentPose.Count; i++)
                    {
                        logicLayer.currentPose.SetBone(float3.zero, float3.zero, i);
                    }

                    for (int i = 0; i < clipInfos.Count; i++)
                    {
                        this.dataState.motionDataGroups[clipInfos[i].groupIndex].animationData[clipInfos[i].clipIndex].GetPoseInTime(
                            ref logicLayer.bufforPose,
                            clipInfos[i].localTime
                            );
                        weight = stateMixer.GetInputWeight(i);

                        for (int j = 0; j < logicLayer.currentPose.Count; j++)
                        {
                            logicLayer.currentPose.SetBone(
                                logicLayer.currentPose.GetBoneData(j).localPosition + weight * logicLayer.bufforPose.GetBoneData(j).localPosition,
                                logicLayer.currentPose.GetBoneData(j).velocity + weight * logicLayer.bufforPose.GetBoneData(j).velocity,
                                j
                                );

                        }
                    }
                    return logicLayer.currentPose;
                case MotionMatchingStateType.ContactAnimationState:
                    GetCurrentMotionGroup().animationData[currentDataIndex].GetPoseInTime(ref logicLayer.currentPose, currentClipLocalTime);
                    return logicLayer.currentPose;
                case MotionMatchingStateType.SingleAnimation:
                    GetCurrentMotionGroup().animationData[currentDataIndex].GetPoseInTime(ref logicLayer.currentPose, currentClipLocalTime);
                    return logicLayer.currentPose;
            }

            throw new System.Exception("Wrong type of AnimationData!");
        }

        protected void SetNativePose()
        {
            // Geting native pose
            for (int i = 0; i < logicLayer.currentPose.Count; i++)
            {
                logicLayer.nativePose[i] = logicLayer.currentPose.GetBoneData(i);
            }
        }

        protected void SetNativeTrajectory()
        {
            //Geting natve trajectory
            for (int i = 0; i < logicLayer.stateInputTrajectoryLocalSpace.Length; i++)
            {
                logicLayer.nativeTrajectory[i] = logicLayer.stateInputTrajectoryLocalSpace.GetPoint(i);
            }
        }

        public bool IsBlockedToEnter()
        {
            return isBlockedToEnter;
        }

        public AnimationMixerPlayable GetPlayable()
        {
            return stateMixer;
        }

        public PoseData GetCurrentPose()
        {
            CalculateCurrentPose();
            return logicLayer.currentPose;
        }

        public Trajectory GetCurrentAnimationTrajectory()
        {
            //switch (this.dataState.GetStateType())
            //{
            //    case MotionMatchingStateType.MotionMatching:
            //        #region Smooth trajctory getting
            //        //GetCurrentClipsInfo();

            //        //float weight;

            //        //for (int i = 0; i < logicLayer.currentAnimationTrajectory.Length; i++)
            //        //{
            //        //    logicLayer.currentAnimationTrajectory.SetTrajectoryPoint(float3.zero, float3.zero, float3.zero, i);
            //        //}

            //        //for (int i = 0; i < currentPlayedClipsInfo.Length; i++)
            //        //{
            //        //    state.animationData[currentPlayedClipsInfo[i].clipIndex].GetTrajectoryInTime(
            //        //        ref logicLayer.bufforAnimationTrajectory,
            //        //        currentPlayedClipsInfo[i].localTime
            //        //        );
            //        //    weight = stateMixer.GetInputWeight(i);
            //        //    for (int j = 0; j < logicLayer.currentAnimationTrajectory.Length; j++)
            //        //    {
            //        //        TrajectoryPoint ctp = logicLayer.currentAnimationTrajectory.GetPoint(j);
            //        //        TrajectoryPoint btp = logicLayer.bufforAnimationTrajectory.GetPoint(j);
            //        //        logicLayer.currentAnimationTrajectory.SetTrajectoryPoint(
            //        //            ctp.position + btp.position * weight,
            //        //            ctp.velocity + btp.velocity * weight,
            //        //            ctp.orientation + btp.orientation * weight,
            //        //            btp.futureTime,
            //        //            j
            //        //            );
            //        //    }
            //        //}
            //        #endregion

            //        this.dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex].GetTrajectoryInTime(ref logicLayer.currentAnimationTrajectory, currentClipLocalTime);
            //        return logicLayer.currentAnimationTrajectory;
            //    case MotionMatchingStateType.SingleAnimation:
            //        this.dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex].GetTrajectoryInTime(ref logicLayer.currentAnimationTrajectory, currentClipLocalTime);
            //        return logicLayer.currentAnimationTrajectory;
            //    case MotionMatchingStateType.ContactAnimationState:
            //        this.dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex].GetTrajectoryInTime(ref logicLayer.currentAnimationTrajectory, currentClipLocalTime);
            //        return logicLayer.currentAnimationTrajectory;
            //}

            //throw new System.Exception("Wrong type of AnimationData!");

            this.dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex].GetTrajectoryInTime(ref logicLayer.currentAnimationTrajectory, currentClipLocalTime);
            return logicLayer.currentAnimationTrajectory;
        }

        public Trajectory GetInputTrajectoryLocalSpace()
        {
            return logicLayer.stateInputTrajectoryLocalSpace;
        }

        protected void GetCurrentSectionDependeces()
        {
            if (dataState.sectionsDependencies == null)
            {
                return;
            }

            sectionDependecies.Clear();

            for (int i = 1; i < this.dataState.sectionsDependencies.sectionSettings.Count; i++)
            {
                if (this.dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex].sections[i].Contain(currentClipLocalTime))
                {
                    for (int j = 0; j < this.dataState.sectionsDependencies.sectionSettings[i].sectionInfos.Count; j++)
                    {
                        sectionDependecies.Add(this.dataState.sectionsDependencies.sectionSettings[i].sectionInfos[j]);
                    }
                }
            }
        }

        public AnimationClip GetCurrentAnimationClip()
        {
            if(GetCurrentMMData().dataType!= AnimationDataType.SingleAnimation)
            {
                return null;
            }
            return GetCurrentMMData().clips[0];
        }

        #endregion

        #region SETTERS

        public void SetCurrentSection(string sectionName)
        {
            int bufforIndex;
            for (int i = 0; i < dataState.motionDataGroups[currentMotionDataGroupIndex].animationData.Count; i++)
            {
                if (!dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[i].sectionIndexes.TryGetValue(sectionName, out bufforIndex))
                {
                    bufforIndex = -1;
                }
                currentClipsSection[currentMotionDataGroupIndex][i] = bufforIndex;
            }

        }

        protected void SetCurrentSection(int sectionIndex)
        {
            for (int i = 0; i < dataState.motionDataGroups[currentMotionDataGroupIndex].animationData.Count; i++)
            {
                currentClipsSection[currentMotionDataGroupIndex][i] = sectionIndex;
            }

        }

        public void SetDefaultSection()
        {
            for (int i = 0; i < dataState.motionDataGroups[currentMotionDataGroupIndex].animationData.Count; i++)
            {
                currentClipsSection[currentMotionDataGroupIndex][i] = 0;
            }
        }

        public void SetTrajectory(Trajectory goal)
        {
            for (int i = 0; i < goal.Length; i++)
            {
                logicLayer.stateInputTrajectoryLocalSpace.SetPoint(goal.GetPoint(i), i);
            }
            logicLayer.stateInputTrajectoryLocalSpace.TransformToLocalSpace(this.Transform);
        }

        public void SetPlayable(AnimationMixerPlayable mixer)
        {
            this.stateMixer = mixer;
        }

        public void SetAnimationSpeed(float speed)
        {
            speedMultiplier = speed;
            this.stateMixer.SetSpeed(speedMultiplier);
        }

        protected void SetCurrentClipLocalTime()
        {
            switch (this.dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex].dataType)
            {
                case AnimationDataType.SingleAnimation:
                    currentClipLocalTime = GetCurrentMotionGroup().animationData[currentDataIndex].GetLocalTime(currentClipGlobalTime);
                    break;
                case AnimationDataType.BlendTree:
                    currentClipLocalTime = GetCurrentMotionGroup().animationData[currentDataIndex].GetLocalTime(currentClipGlobalTime);
                    break;
                case AnimationDataType.AnimationSequence:
                    currentClipLocalTime = animationsSequences[animationsSequences.Count - 1].GetLocalTime();
                    break;
            }
        }

        public void ClearBehaviors()
        {
            this.stateBehaviors.Clear();
        }

        public void AddBehavior(MotionMatchingStateBehavior stateEvent)
        {
            if (stateBehaviors == null)
            {
                stateBehaviors = new List<MotionMatchingStateBehavior>();
            }

            stateEvent.SetBasic(this, motionMatching, this.Transform);
            stateBehaviors.Add(stateEvent);
        }

        public void AddBehaviors(MotionMatchingStateBehavior[] newStateEvents)
        {
            if (stateBehaviors == null)
            {
                stateBehaviors = new List<MotionMatchingStateBehavior>();
            }


            for (int i = 0; i < newStateEvents.Length; i++)
            {
                newStateEvents[i].SetBasic(this, motionMatching, this.Transform);
            }
            this.stateBehaviors.AddRange(newStateEvents);
        }

        public void AddBehaviors(List<MotionMatchingStateBehavior> newStateEvents)
        {
            if (stateBehaviors == null)
            {
                stateBehaviors = new List<MotionMatchingStateBehavior>();
            }

            for (int i = 0; i < newStateEvents.Count; i++)
            {
                newStateEvents[i].SetBasic(this, motionMatching, Transform);
            }
            this.stateBehaviors.AddRange(stateBehaviors);
        }

        public void RemoveBehavior(MotionMatchingStateBehavior stateEvent)
        {
            this.stateBehaviors.Remove(stateEvent);
        }

        protected void CalculateCurrentEventMarker()
        {
            if (dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex].GetEventMarkersCount() == 0)
            {
                return;
            }
            currentEventMarkerIndex = 0;
            while (dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex].eventMarkers[currentEventMarkerIndex].GetTime() < currentClipLocalTime)
            {
                currentDataIndex++;
                if (currentDataIndex >= dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex].GetEventMarkersCount())
                {
                    break;
                }
            }
        }

        private void CatchEventMarkers()
        {
            if (currentEventMarkerIndex < GetCurrentEventMarkersCount())
            {
                while (GetCurrentEventMarkerTime() <= currentClipLocalTime)
                {
                    for (int i = 0; i < stateBehaviors.Count; i++)
                    {
                        stateBehaviors[i].CatchEventMarkers(GetCurrentEventMarkerName());
                    }
                    currentEventMarkerIndex++;
                    if (currentEventMarkerIndex >= GetCurrentEventMarkersCount())
                    {
                        break;
                    }
                }
            }
        }

        public int GetCurrentEventMarkersCount()
        {
            return dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex].GetEventMarkersCount();
        }

        public float GetCurrentEventMarkerTime()
        {
            return dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex].eventMarkers[currentEventMarkerIndex].GetTime();
        }

        public string GetCurrentEventMarkerName()
        {
            return dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex].eventMarkers[currentEventMarkerIndex].GetName();
        }
        #endregion

        public void WaitForCurrentJobsComplete()
        {
            JobHandle.CompleteAll(jobsHandle);
        }

        public MotionMatchingData GetCurrentMMData()
        {
            return dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex];
        }

        protected MotionDataGroup GetCurrentMotionGroup()
        {
            return this.dataState.motionDataGroups[currentMotionDataGroupIndex];
        }


    }

}