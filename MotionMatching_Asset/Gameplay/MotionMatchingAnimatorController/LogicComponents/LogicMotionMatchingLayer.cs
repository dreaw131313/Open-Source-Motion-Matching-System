using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace DW_Gameplay
{
    public class LogicMotionMatchingLayer
    {
        private MotionMatchingLayer layer;
        public AnimationMixerPlayable mixer;
        private Transform transform;

        public List<LogicState> logicStates;
        private MotionMatchingPlayableGraph playableGraph;
        private List<float> stateWeightsSpeedChanging;
        private List<int> currentBlendedStates;
        public List<float> currentWeights;

        private int currentStateIndex = -1;
        private int previousStateIndex = -1;
        private bool passIK;
        private bool footIK;
        private bool isAdditive;

        private Dictionary<string, int> logicStatesIndexes;

        public Trajectory stateInputTrajectoryLocalSpace;
        public Trajectory currentAnimationTrajectory;

        public PoseData currentPose;
        public PoseData bufforPose;

        public NewClipInfoToPlay bestPoseInfo;

        public NativeArray<TrajectoryPoint> nativeTrajectory;
        public NativeArray<BoneData> nativePose;

        public List<SwitchStateContact> contactPoints;
        public List<MotionMatchingContact> adaptedContactPoints;

        // Switch State features

        public LogicMotionMatchingLayer(
            MotionMatchingPlayableGraph mmAnimator,
            MotionMatchingLayer layer,
            Transform gameObject
            )
        {
            this.transform = gameObject;
            passIK = layer.passIK;
            this.playableGraph = mmAnimator;
            currentBlendedStates = new List<int>();
            this.layer = layer;
            logicStates = new List<LogicState>();
            currentWeights = new List<float>();


            bestPoseInfo = new NewClipInfoToPlay(-1,-1, -1, -1);

        }

        public void OnDestory()
        {
            for (int i = 0; i < logicStates.Count; i++)
            {
                logicStates[i].OnDestroy();
            }
            if (logicStates.Count > 0)
            {
                nativePose.Dispose();
                nativeTrajectory.Dispose();
            }
        }

        public void Start()
        {
            // For geting current pose
                stateInputTrajectoryLocalSpace = new Trajectory(logicStates[0].dataState.motionDataGroups[0].animationData[0][0].trajectory.Length);
                currentAnimationTrajectory = new Trajectory(logicStates[0].dataState.motionDataGroups[0].animationData[0][0].trajectory.Length);
                currentPose = new PoseData(logicStates[0].dataState.motionDataGroups[0].animationData[0][0].pose.Count);
                bufforPose = new PoseData(logicStates[0].dataState.motionDataGroups[0].animationData[0][0].pose.Count);
                nativePose = new NativeArray<BoneData>(logicStates[0].dataState.motionDataGroups[0].animationData[0][0].pose.Count, Allocator.Persistent);
                nativeTrajectory = new NativeArray<TrajectoryPoint>(logicStates[0].dataState.motionDataGroups[0].animationData[0][0].trajectory.Length, Allocator.Persistent);
            
#if UNITY_EDITOR
            //else
            //{
            //    throw new System.Exception(string.Format("State {0} have no animation Data", logicStates[0].dataState.GetName()));
            //}
#endif

            logicStatesIndexes = new Dictionary<string, int>();
            for (int i = 0; i < logicStates.Count; i++)
            {
                logicStatesIndexes.Add(logicStates[i].GetName(), i);
            }

            currentStateIndex = layer.startStateIndex;
            stateWeightsSpeedChanging = new List<float>();

            playableGraph.AddLayerPlayable(this);

            currentBlendedStates.Clear();
            currentBlendedStates.Add(currentStateIndex);
            stateWeightsSpeedChanging.Add(1f);

            logicStates[currentStateIndex].StateStart();
            mixer.SetInputWeight(0, 1f);
            currentWeights.Add(1f);


            if (this.layer.avatarMask != null)
            {
                playableGraph.SetLayerAvatarMask((uint)layer.index, layer.avatarMask);
            }

            playableGraph.SetLayerAdditive((uint)layer.index, layer.isAdditive);

        }

        public void FixedUpdate()
        {
            logicStates[currentStateIndex].StateFixedUpdate();
        }

        public void Update()
        {
            BlendingStates();
            logicStates[currentStateIndex].StateUpdate();
        }

        public void LateUpdate()
        {
            logicStates[currentStateIndex].StateLateUpdate();
        }

        private void BlendingStates()
        {
            playableGraph.BlendPlayablesInStateMixer(
                ref mixer,
                stateWeightsSpeedChanging,
                currentWeights
                );
            playableGraph.RemoveZeroWeightsInputFromLayer(
                this,
                currentBlendedStates,
                stateWeightsSpeedChanging,
                currentWeights
                );

            for (int i = 0; i < (currentBlendedStates.Count - 1); i++)
            {
                logicStates[currentBlendedStates[i]].BeforeUpdateExit();
            }
        }

        #region Getters
        public bool GetPassIK()
        {
            return passIK;
        }

        public bool GetFootPassIK()
        {
            return layer.footPassIK;
        }

        public string GetName()
        {
            return this.layer.GetName();
        }

        public bool IsNameEqual(string name)
        {
            return this.layer.name.Equals(name);
        }

        public void SetPassIK(bool passIK)
        {
            this.passIK = passIK;
        }

        public int GetCurrentStateIndex()
        {
            return currentStateIndex;
        }

        public int GetStateIndexFromName(string name)
        {
#if UNITY_EDITOR
            if (!logicStatesIndexes.ContainsKey(name))
            {
                throw new System.Exception(string.Format("Not exists state with name {0}!", name));
            }
#endif

            return logicStatesIndexes[name];
        }

        public AnimationMixerPlayable GetPlayable()
        {
            return this.mixer;
        }

        public LogicState GetCurrentState()
        {
            return logicStates[currentStateIndex];
        }

        public bool IsTrajectorryCorrectionEnabledInCurrentState()
        {
            return logicStates[currentStateIndex].dataState.trajectoryCorrection;
        }

        public MotionMatchingStateType GetCurrentStateType()
        {
            return GetCurrentState().GetStateType();
        }

        #endregion

        #region Setters
        public void SetGoal(Trajectory goal)
        {
            logicStates[currentStateIndex].SetTrajectory(goal);
        }

        public void SetPlayable(AnimationMixerPlayable layerMixer)
        {
            this.mixer = layerMixer;
        }

        public void SetCurrentSection(string sectionName)
        {
            this.logicStates[currentStateIndex].SetCurrentSection(sectionName);
        }

        public void SetPastPoints(ref Trajectory trajectory)
        {
            this.logicStates[currentStateIndex].GetCurrentAnimationTrajectory();
            for (int i = 0; i < currentAnimationTrajectory.Length; i++)
            {
                if (this.logicStates[currentStateIndex].dataState.motionDataGroups[0].animationData[0].trajectoryPointsTimes[i] < 0)
                {
                    TrajectoryPoint tp = currentAnimationTrajectory.GetPoint(i);
                    tp.TransformToWorldSpace(transform);
                    trajectory.SetPoint(tp, i);
                }
                else
                {
                    return;
                }
            }
        }

        public void SetContactPoints(SwitchStateContact[] contactPoints)
        {
            if (this.contactPoints == null)
            {
                this.contactPoints = new List<SwitchStateContact>();
            }
            else
            {
                this.contactPoints.Clear();
            }

            for (int i = 0; i < contactPoints.Length; i++)
            {
                this.contactPoints.Add(contactPoints[i]);
            }
        }

        public bool SetCurrentStateMotionDataGroup(string groupName)
        {
            return logicStates[currentStateIndex].SetMotionDataGroup(groupName);
        }
        #endregion
        public bool SwitchState(
            PoseData currentPose,
            Trajectory goal,
            int nextStateIndex,
            float blendTime,
            string startSectionName = null
            )
        {
#if UNITY_EDITOR
            if (logicStates[nextStateIndex].dataState.GetStateType() == MotionMatchingStateType.ContactAnimationState)
            {
                throw new System.Exception(
                    string.Format("This method cannot switch to contact state. {0} state type is {1}",
                    logicStates[nextStateIndex].dataState.GetName(),
                    logicStates[nextStateIndex].dataState.GetStateType().ToString()
                    ));
            }
#endif
            if (logicStates[nextStateIndex].IsBlockedToEnter())
            {
                return false;
            }

            if (currentStateIndex >= 0 && currentStateIndex < logicStates.Count)
            {
                previousStateIndex = currentStateIndex;
                stateWeightsSpeedChanging[stateWeightsSpeedChanging.Count - 1] = (-(1f / blendTime));
            }

            currentStateIndex = nextStateIndex;
            stateWeightsSpeedChanging.Add((1f / blendTime));
            currentBlendedStates.Add(currentStateIndex);
            logicStates[currentStateIndex].StateEnter(
                currentPose, 
                goal, 
                logicStates[currentStateIndex].dataState.whereCanFindingNextPose, 
                startSectionName
                );
            return true;
        }

        public bool SwitchState(
            PoseData currentPose,
            Trajectory goal,
            int nextStateIndex,
            float blendTime,
            List<float2> whereCanFindingNextPose,
            string startSectionName = null
            )
        {
#if UNITY_EDITOR
            if (logicStates[nextStateIndex].dataState.GetStateType() == MotionMatchingStateType.ContactAnimationState)
            {
                throw new System.Exception(
                    string.Format("This method cannot switch to contact state. {0} state type is {1}",
                    logicStates[nextStateIndex].dataState.GetName(),
                    logicStates[nextStateIndex].dataState.GetStateType().ToString()
                    ));
            }
#endif
            if (logicStates[nextStateIndex].IsBlockedToEnter())
            {
                return false;
            }

            if (currentStateIndex >= 0 && currentStateIndex < logicStates.Count)
            {
                previousStateIndex = currentStateIndex;
                stateWeightsSpeedChanging[stateWeightsSpeedChanging.Count - 1] = (-(1f / blendTime));
            }

            currentStateIndex = nextStateIndex;
            stateWeightsSpeedChanging.Add((1f / blendTime));
            currentBlendedStates.Add(currentStateIndex);
            logicStates[currentStateIndex].StateEnter(
                currentPose,
                goal,
                whereCanFindingNextPose,
                startSectionName
                );
            return true;
        }


        public bool SwitchToContactState(
            PoseData currentPose,
            Trajectory goal,
            List<SwitchStateContact> contactPoints,
            int nextStateIndex,
            float blendTime,
            string startSectionName = null
            )
        {
#if UNITY_EDITOR
            if (logicStates[nextStateIndex].dataState.GetStateType() != MotionMatchingStateType.ContactAnimationState)
            {
                throw new System.Exception(
                    string.Format("This method can only switch to contact state. {0} state type is {1}",
                    logicStates[nextStateIndex].dataState.GetName(),
                    logicStates[nextStateIndex].dataState.GetStateType().ToString()
                    ));
            }
#endif

            if (logicStates[nextStateIndex].IsBlockedToEnter())
            {
                return false;
            }


            if (currentStateIndex >= 0 && currentStateIndex < logicStates.Count)
            {
                previousStateIndex = currentStateIndex;
                stateWeightsSpeedChanging[stateWeightsSpeedChanging.Count - 1] = (-(1f / blendTime));
            }

            currentStateIndex = nextStateIndex;
            stateWeightsSpeedChanging.Add((1f / blendTime));
            currentBlendedStates.Add(currentStateIndex);

            SetContactPoints(contactPoints.ToArray());

            logicStates[currentStateIndex].StateEnter(
                currentPose,
                goal,
                logicStates[nextStateIndex].dataState.whereCanFindingNextPose,
                startSectionName
                );

            return true;
        }


        public bool ForceAnimationFindingInCurrentState()
        {
            switch (logicStates[currentStateIndex].GetStateType())
            {
                case MotionMatchingStateType.MotionMatching:
                    return false;
                case MotionMatchingStateType.SingleAnimation:
                    return false;
                case MotionMatchingStateType.ContactAnimationState:
                    switch (logicStates[currentStateIndex].dataState.csFeatures.contactStateType)
                    {
                        case ContactStateType.NormalContacts:
                            return false;
                        case ContactStateType.Impacts:
                            logicStates[currentStateIndex].ForceAnimationFindngInternal();
                            return true;
                    }
                    break;
            }
            return false;
        }
    }
}
