using DW_Editor;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

namespace DW_Gameplay
{
    [RequireComponent(typeof(Animator))]
    public class MotionMatching : MonoBehaviour
    {
        // Components
        private Animator animatorComponent;

        [SerializeField]
        private MM_AnimatorController animatorController;
        [Space]
        [SerializeField]
        [Tooltip("How many frames will be calculated by one job.")]
        private int framesForJob = 600;

        private AnimatorUpdateMode updateMode;

        #region Trajectory
        [Space]
        [Header("TRAJECTORY CORRECTION")]
        [SerializeField]
        private TrajectoryCorrectionType trajectoryCorrectionType = TrajectoryCorrectionType.Progresive;
        [SerializeField]
        [Tooltip("If it is true, correction is always applied, even in state where trajectory correction is disabled, but not in contact state.")]
        private bool forceTrajectoryCorrection = false;
        public bool ForcingTrajectoryCorrection
        {
            get
            {
                return forceTrajectoryCorrection;
            }
            set
            {
                forceTrajectoryCorrection = value;
            }
        }

        [SerializeField]
        [Tooltip("Minimum angle at which the trajectory is corrected.")]
        [Range(0, 180f)]
        private float minAngle = 3f;
        [SerializeField]
        [Tooltip("Maximum angle at which the trajectory is corrected.")]
        [Range(0, 180f)]
        private float maxAngle = 45f;
        [SerializeField]
        [Tooltip("Speed in minimum angle.")]
        private float minAngleSpeed = 10f;
        [SerializeField]
        [Tooltip("Speed in maximum angle or constant speed in constant correction type.")]
        private float maxSpeed = 90f;
        [SerializeField]
        private bool strafe = false;
        public bool Strafe
        {
            get { return strafe; }
            set { strafe = value; }
        }

        //public TrajectoryCostType TrajectoryCostType { get => trajectoryCostType; set => trajectoryCostType = value; }
        //public bool ForceTrajectoryCostType { get => forceTrajectoryCostType; set => forceTrajectoryCostType = value; }
        public Trajectory InputTrajectory { get => inputTrajectory; private set => inputTrajectory = value; }

        [SerializeField]
        [Range(0, 180f)]
        private float strafeMaxAngle = 30f;

        //[Header("COST CALCULATION IN MOTION MATCHING STATE")]
        //[SerializeField]
        //private TrajectoryCostType trajectoryCostType = TrajectoryCostType.PositionVelocityOrientation;
        //[SerializeField]
        //[Tooltip("When true, use component trajectory cost type in motion matching state instead of state settings.")]
        //private bool forceTrajectoryCostType = false;


        private Vector3 strafeForward = Vector3.forward;
        private int trajectoryFirstFutureIndex = 0;

        #endregion


        #region Logic Elements
        private Trajectory inputTrajectory;

        private MotionMatchingPlayableGraph playableGraph;
        private List<LogicMotionMatchingLayer> logicLayers;

        private Dictionary<string, int> layerIndexes;
        private Dictionary<string, bool> bools;
        private Dictionary<string, int> ints;
        private Dictionary<string, float> floats;

        private float[] trajectoryPointsTimes;

        public void GetTrajectoryPointsTimes(ref float[] times)
        {
            times = new float[trajectoryPointsTimes.Length];
            for (int i = 0; i < trajectoryPointsTimes.Length; i++)
            {
                times[i] = trajectoryPointsTimes[i];
            }
        }

        public void GetTrajectoryPointsTimes(ref List<float> times)
        {
            times.Clear();
            for (int i = 0; i < trajectoryPointsTimes.Length; i++)
            {
                times.Add(trajectoryPointsTimes[i]);
            }
        }
        #endregion

        private DirectorUpdateMode timeUpdateMode = DirectorUpdateMode.GameTime;

        private void Awake()
        {
            //float t = Time.realtimeSinceStartup;

            trajectoryPointsTimes = animatorController.layers[0].states[0].motionDataGroups[0].animationData[0].trajectoryPointsTimes.ToArray();

            InitializeDictionaries();
            logicLayers = new List<LogicMotionMatchingLayer>();

            animatorComponent = this.transform.GetComponent<Animator>();
            playableGraph = new MotionMatchingPlayableGraph(animatorComponent, string.Format("MM Graph: {0}", this.transform.gameObject.name));

            //InitializeLogicGraph(movingGameObject);
            InitializeLogicGraph(this.transform);

            layerIndexes = new Dictionary<string, int>();
            for (int i = 0; i < logicLayers.Count; i++)
            {
                layerIndexes.Add(logicLayers[i].GetName(), i);
            }

            playableGraph.Start();
            playableGraph.SetTimeUpdateMode(timeUpdateMode);

            StartLogicGraph();

            // Trajectory initzialization:
            Trajectory tSample = this.GetTrajectorySample(0);
            trajectoryFirstFutureIndex = this.GetFirstPointIndexWithFutureTime();
            strafeForward = Vector3.forward;


            //float exT = (Time.realtimeSinceStartup - t) * 1000f;
            //Debug.Log(string.Format("{0} time = {1} ms", this.gameObject.name, exT));
        }

        void Start()
        {
            animatorComponent.applyRootMotion = true;
        }

        void Update()
        {
#if UNITY_EDITOR
            this.updateMode = animatorComponent.updateMode;
#endif

            for (int i = 0; i < logicLayers.Count; i++)
            {
                logicLayers[i].Update();
            }
        }

        private void LateUpdate()
        {
            for (int i = 0; i < logicLayers.Count; i++)
            {
                logicLayers[i].LateUpdate();
            }

            if ((logicLayers[0].IsTrajectorryCorrectionEnabledInCurrentState() || forceTrajectoryCorrection) &&
                GetCurrentStateType(0) != MotionMatchingStateType.ContactAnimationState)
            {
                TrajectoryCorrection();
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < logicLayers.Count; i++)
            {
                logicLayers[i].OnDestory();
            }
            playableGraph.OnDestroy();
        }

        /// <summary>
        /// Force finding new best place only in Contact state with contactStateType setetd to Impact.
        /// </summary>
        /// <param name="layerName"></param>
        /// <returns>Returns true when find forcing is successful happen.</returns>
        public bool ForceAnimationFinding(string layerName)
        {
#if UNITY_EDITOR
            if (!layerIndexes.ContainsKey(layerName))
            {
                throw new System.Exception(string.Format("Layer with name {0} not exist!", layerName));
            }
#endif

            return logicLayers[layerIndexes[layerName]].ForceAnimationFindingInCurrentState();
        }

        /// <summary>
        /// Force finding new best place only in Contact state with contactStateType setetd to Impact.
        /// </summary>
        /// <param name="layerName"></param>
        /// <returns>Returns true when find forcing is successful happen.</returns>
        public bool ForceAnimationFinding(int layerIndex)
        {
            return logicLayers[layerIndex].ForceAnimationFindingInCurrentState();
        }

        /// <summary>
        /// Force finding new best place only in Contact state with contactStateType setetd to Impact.
        /// </summary>
        /// <param name="layerName"></param>
        /// <returns>Returns true when find forcing is successful happen.</returns>
        public void ForceAnimationFinding()
        {
            for (int i = 0; i < logicLayers.Count; i++)
            {
                logicLayers[i].ForceAnimationFindingInCurrentState();
            }
        }

        public bool SetMotionDataGroupInCurrentState(int layerIndex, string groupName)
        {
            return logicLayers[layerIndex].SetCurrentStateMotionDataGroup(groupName);
        }

        #region State Switching
        public bool SwitchToSingleAnimationState(
            string layerName,
            string stateName,
            float blendTime,
            string startSectionName = null
            )
        {
            int layerIndex = layerIndexes[layerName];

#if UNITY_EDITOR
            if (logicLayers[layerIndex].GetCurrentState().GetStateType() != MotionMatchingStateType.SingleAnimation)
            {
                throw new System.Exception(string.Format("State {0} type is not Single Animation!", logicLayers[layerIndex].GetCurrentState().GetName()));
            }
#endif
            if (layerIndex != -1)
            {
                if (logicLayers[layerIndex].GetCurrentState().GetStateType() == MotionMatchingStateType.MotionMatching)
                {
                    logicLayers[layerIndex].GetCurrentState().WaitForCurrentJobsComplete();
                }
                return logicLayers[layerIndex].SwitchState(
                    logicLayers[layerIndex].GetCurrentState().GetCurrentPose(),
                    logicLayers[layerIndex].GetCurrentState().GetInputTrajectoryLocalSpace(),
                    logicLayers[layerIndex].GetStateIndexFromName(stateName),
                    blendTime,
                    startSectionName
                    );
            }
            else
            {
                return false;
            }
        }

        public bool SwitchToSingleAnimationState(
            int layerIndex,
            string stateName,
            float blendTime,
            string startSectionName = null
            )
        {

#if UNITY_EDITOR
            if (logicLayers[layerIndex].GetCurrentState().GetStateType() != MotionMatchingStateType.SingleAnimation)
            {
                throw new System.Exception(string.Format("State {0} type is not Single Animation!", logicLayers[layerIndex].GetCurrentState().GetName()));
            }
#endif
            if (layerIndex != -1)
            {
                if (logicLayers[layerIndex].GetCurrentState().GetStateType() == MotionMatchingStateType.MotionMatching)
                {
                    logicLayers[layerIndex].GetCurrentState().WaitForCurrentJobsComplete();
                }
                return logicLayers[layerIndex].SwitchState(
                    logicLayers[layerIndex].GetCurrentState().GetCurrentPose(),
                    logicLayers[layerIndex].GetCurrentState().GetInputTrajectoryLocalSpace(),
                    logicLayers[layerIndex].GetStateIndexFromName(stateName),
                    blendTime,
                    startSectionName
                    );
            }
            else
            {
                return false;
            }
        }


        public bool SwitchToMotionMatchingState(
            string layerName,
            string stateName,
            float blendTime,
            string startSectionName = null)
        {
            int layerIndex = layerIndexes[layerName];

#if UNITY_EDITOR
            if (logicLayers[layerIndex].GetCurrentState().GetStateType() != MotionMatchingStateType.MotionMatching)
            {
                throw new System.Exception(string.Format("State {0} type is not Motion Matching!", logicLayers[layerIndex].GetCurrentState().GetName()));
            }
#endif

            if (layerIndex != -1)
            {
                if (logicLayers[layerIndex].GetCurrentState().GetStateType() == MotionMatchingStateType.MotionMatching)
                {
                    logicLayers[layerIndex].GetCurrentState().WaitForCurrentJobsComplete();
                }
                return logicLayers[layerIndex].SwitchState(
                    logicLayers[layerIndex].GetCurrentState().GetCurrentPose(),
                    logicLayers[layerIndex].GetCurrentState().GetInputTrajectoryLocalSpace(),
                    logicLayers[layerIndex].GetStateIndexFromName(stateName),
                    blendTime,
                    startSectionName
                    );
            }
            else
            {
                return false;
            }
        }

        public bool SwitchToMotionMatchingState(
            int layerIndex,
            string stateName,
            float blendTime,
            string startSectionName = null)
        {
#if UNITY_EDITOR
            if (logicLayers[layerIndex].GetCurrentState().GetStateType() != MotionMatchingStateType.MotionMatching)
            {
                throw new System.Exception(string.Format("State {0} type is not Motion Matching!", logicLayers[layerIndex].GetCurrentState().GetName()));
            }
#endif

            if (layerIndex != -1)
            {
                if (logicLayers[layerIndex].GetCurrentState().GetStateType() == MotionMatchingStateType.MotionMatching)
                {
                    logicLayers[layerIndex].GetCurrentState().WaitForCurrentJobsComplete();
                }
                return logicLayers[layerIndex].SwitchState(
                    logicLayers[layerIndex].GetCurrentState().GetCurrentPose(),
                    logicLayers[layerIndex].GetCurrentState().GetInputTrajectoryLocalSpace(),
                    logicLayers[layerIndex].GetStateIndexFromName(stateName),
                    blendTime,
                    startSectionName
                    );
            }
            else
            {
                return false;
            }
        }

        public bool SwitchToContactState(
            string layerName,
            string stateName,
            float blendTime,
            List<SwitchStateContact> contactPoints
            )
        {
            int layerIndex = layerIndexes[layerName];

            if (layerIndex != -1)
            {
                if (logicLayers[layerIndex].GetCurrentState().GetStateType() == MotionMatchingStateType.MotionMatching)
                {
                    logicLayers[layerIndex].GetCurrentState().WaitForCurrentJobsComplete();
                }
                return logicLayers[layerIndex].SwitchToContactState(
                    logicLayers[layerIndex].GetCurrentState().GetCurrentPose(),
                    logicLayers[layerIndex].GetCurrentState().GetInputTrajectoryLocalSpace(),
                    contactPoints,
                    logicLayers[layerIndex].GetStateIndexFromName(stateName),
                    blendTime,
                    null
                    );
            }
            else
            {
                return false;
            }
        }

        public bool SwitchToContactState(
            int layerIndex,
            string stateName,
            float blendTime,
            List<SwitchStateContact> contactPoints
            )
        {
            if (layerIndex != -1)
            {
                if (logicLayers[layerIndex].GetCurrentState().GetStateType() == MotionMatchingStateType.MotionMatching)
                {
                    logicLayers[layerIndex].GetCurrentState().WaitForCurrentJobsComplete();
                }
                return logicLayers[layerIndex].SwitchToContactState(
                    logicLayers[layerIndex].GetCurrentState().GetCurrentPose(),
                    logicLayers[layerIndex].GetCurrentState().GetInputTrajectoryLocalSpace(),
                    contactPoints,
                    logicLayers[layerIndex].GetStateIndexFromName(stateName),
                    blendTime,
                    null
                    );
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region State Behavior
        public void AddBehaviorToState(int layerIndex, string stateName, MotionMatchingStateBehavior stateEvent)
        {
            int stateIndex = logicLayers[layerIndex].GetStateIndexFromName(stateName);
            logicLayers[layerIndex].logicStates[stateIndex].AddBehavior(stateEvent);
        }

        public void AddBehaviorToState(int layerIndex, string stateName, MotionMatchingStateBehavior[] stateEvents)
        {
            int stateIndex = logicLayers[layerIndex].GetStateIndexFromName(stateName);
            logicLayers[layerIndex].logicStates[stateIndex].AddBehaviors(stateEvents);
        }

        public void AddBehaviorToState(int layerIndex, string stateName, List<MotionMatchingStateBehavior> stateEvents)
        {
            int stateIndex = logicLayers[layerIndex].GetStateIndexFromName(stateName);
            logicLayers[layerIndex].logicStates[stateIndex].AddBehaviors(stateEvents);
        }

        public void AddBehaviorToState(string layerName, string stateName, MotionMatchingStateBehavior stateEvent)
        {
            int layerIndex = layerIndexes[layerName];
            int stateIndex = logicLayers[layerIndex].GetStateIndexFromName(stateName);
            logicLayers[layerIndex].logicStates[stateIndex].AddBehavior(stateEvent);
        }

        public void AddBehaviorToState(string layerName, string stateName, MotionMatchingStateBehavior[] stateEvents)
        {
            int layerIndex = layerIndexes[layerName];
            int stateIndex = logicLayers[layerIndex].GetStateIndexFromName(stateName);
            logicLayers[layerIndex].logicStates[stateIndex].AddBehaviors(stateEvents);
        }

        public void AddBehaviorToState(string layerName, string stateName, List<MotionMatchingStateBehavior> stateEvents)
        {
            int layerIndex = layerIndexes[layerName];
            int stateIndex = logicLayers[layerIndex].GetStateIndexFromName(stateName);
            logicLayers[layerIndex].logicStates[stateIndex].AddBehaviors(stateEvents);
        }

        public void RemoveBehaviorFromState(string layerName, string stateName, MotionMatchingStateBehavior stateEvent)
        {
            int layerIndex = layerIndexes[layerName];
            int stateIndex = logicLayers[layerIndex].GetStateIndexFromName(stateName);
            logicLayers[layerIndex].logicStates[stateIndex].RemoveBehavior(stateEvent);
        }

        public void RemoveBehaviorFromState(int layerIndex, string stateName, MotionMatchingStateBehavior stateEvent)
        {
            int stateIndex = logicLayers[layerIndex].GetStateIndexFromName(stateName);
            logicLayers[layerIndex].logicStates[stateIndex].RemoveBehavior(stateEvent);
        }

        public void ClearBehaviorInState(int layerIndex, string stateName, MotionMatchingStateBehavior stateEvent)
        {
            int stateIndex = logicLayers[layerIndex].GetStateIndexFromName(stateName);
            logicLayers[layerIndex].logicStates[stateIndex].ClearBehaviors();
        }

        public void ClearBehaviorInState(string layerName, string stateName, MotionMatchingStateBehavior stateEvent)
        {
            int layerIndex = layerIndexes[layerName];
            int stateIndex = logicLayers[layerIndex].GetStateIndexFromName(stateName);
            logicLayers[layerIndex].logicStates[stateIndex].ClearBehaviors();
        }
        #endregion

        #region Geters
        public bool CanEnterToState(string stateName, int layerIndex)
        {
            int stateIndex = logicLayers[layerIndex].GetStateIndexFromName(stateName);
            return !logicLayers[layerIndex].logicStates[stateIndex].IsBlockedToEnter();
        }

        public MotionMatchingStateType GetCurrentStateType(int layerIndex)
        {
            return logicLayers[layerIndex].GetCurrentStateType();
        }

        public MotionMatchingStateType GetCurrentStateType(string layerName)
        {
            return logicLayers[layerIndexes[layerName]].GetCurrentStateType();
        }

        /// <summary>
        /// Get animation trajectory of  the most up-to-date played animation. Position, velocity and orientation are in local space of animated object transform.
        /// </summary>
        /// <param name="layerIndex"></param>
        /// <returns></returns>
        public Trajectory GetCurrentAnimationTrajectory(int layerIndex = 0)
        {
            return logicLayers[layerIndex].GetCurrentState().GetCurrentAnimationTrajectory();
        }

        /// <summary>
        /// Get animation trajectory of  the most up-to-date played animation. Position, velocity and orientation are in local space of animated object transform.
        /// </summary>
        /// <param name="layerIndex"></param>
        /// <returns></returns>
        private Trajectory GetCurrentAnimationTrajectory(string layerName)
        {
            return logicLayers[layerIndexes[layerName]].GetCurrentState().GetCurrentAnimationTrajectory();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="layerIndex"></param>
        /// <returns>Trajectory of first frame in first animation data in start state of layer with index layerIndex. Position, velocity and orientation are in local space of animated object transform.</returns>
        public Trajectory GetTrajectorySample(int layerIndex = 0)
        {
            return this.animatorController.layers[layerIndex].states[0].motionDataGroups[0].animationData[0][0].trajectory;
        }

        //public Trajectory GetTrajectorySample(string layerName)
        //{
        //    return this.animatorController.layers[layerIndexes[layerName]].states[0].animationData[0][0].trajectory;
        //}

        //public PoseData GetCurrentPoseData(int layerIndex)
        //{
        //    return logicLayers[layerIndex].GetCurrentState().GetCurrentPose();
        //}

        //public PoseData GetCurrentPoseData(string layerName)
        //{
        //    return logicLayers[layerIndexes[layerName]].GetCurrentState().GetCurrentPose();
        //}

        public bool GetBool(string name)
        {
            return this.bools[name];
        }

        public int GetInt(string name)
        {
            return this.ints[name];
        }

        public float GetFloat(string name)
        {
            return this.floats[name];
        }

        public AnimatorUpdateMode GetUpdateMode()
        {
            return updateMode;
        }

        public int GetFirstPointIndexWithFutureTime()
        {
            for (int i = 0; i < trajectoryPointsTimes.Length; i++)
            {
                if (trajectoryPointsTimes[i] >= 0f)
                {
                    return i;
                }
            }

            return -1;
        }

        public PoseData GetCurrentPose(int layerIndex)
        {
            return logicLayers[layerIndex].GetCurrentState().GetAndCalculateCurrentPose_EditorOnly();
        }

        #endregion

        #region Setters
        public void SetInputTrajectory(Trajectory goal, int layerIndex = 0)
        {
            this.inputTrajectory = goal;
            logicLayers[layerIndex].SetGoal(goal);
        }

        public void SetBool(string boolName, bool value)
        {
            this.bools[boolName] = value;
        }

        public void SetInt(string intName, int value)
        {
            this.ints[intName] = value;
        }

        public void SetFloat(string floatName, float value)
        {
            this.floats[floatName] = value;
        }

        public void SetCurrentStateSection(string sectionName, int layerIndex = 0)
        {
            logicLayers[layerIndex].SetCurrentSection(sectionName);
        }

        public void SetLayerWeight(int layerIndex, float weight)
        {
            playableGraph.SetLayerWeight(layerIndex, weight);
        }

        public void SetLayerWeight(string layerName, float weight)
        {
            playableGraph.SetLayerWeight(layerIndexes[layerName], weight);
        }

        //public bool SetContactPointPosition(string layerName, float3 position)
        //{
        //    int layerIndex = -1;

        //    for (int i = 0; i < logicLayers.Count; i++)
        //    {
        //        if (logicLayers[i].IsNameEqual(layerName))
        //        {
        //            layerIndex = i;
        //            break;
        //        }
        //    }

        //    if (layerIndex != -1)
        //    {
        //        return logicLayers[layerIndex].SetContactPointPosition(position);
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

        public void SetLayerAdditive(uint layerIndex, bool isAdditive)
        {
            this.playableGraph.SetLayerAdditive(layerIndex, isAdditive);
        }

        public void SetLayerAdditive(string layerName, bool isAdditive)
        {
            this.playableGraph.SetLayerAdditive((uint)layerIndexes[layerName], isAdditive);
        }

        /// <summary>
        /// Sets past points(which times is lesser than 0) for forwarded trajectory. Setted position, velocity and orientation are in world space.
        /// </summary>
        /// <param name="trajectory"></param>
        /// <param name="layerIndex"></param>
        public void SetPastPointsFromData(ref Trajectory trajectory, int layerIndex)
        {
            logicLayers[layerIndex].SetPastPoints(ref trajectory);
        }

        /// <summary>
        /// Sets past points(which times is lesser than 0) for trajectory. Setted position, velocity and orientation are in world space.
        /// </summary>
        /// <param name="trajectory"></param>
        /// <param name="layerName"></param>
        public void SetPastPointsFromData(ref Trajectory trajectory, string layerName)
        {
            logicLayers[layerIndexes[layerName]].SetPastPoints(ref trajectory);
        }

        public void SetLayerMask(string layerName, AvatarMask mask)
        {
            this.playableGraph.SetLayerAvatarMask((uint)layerIndexes[layerName], mask);
        }

        public void SetLayerMask(uint layerIndex, AvatarMask mask)
        {
            this.playableGraph.SetLayerAvatarMask(layerIndex, mask);
        }

        #endregion

        #region Component Initialization
        private void InitializeDictionaries()
        {
            bools = new Dictionary<string, bool>();
            ints = new Dictionary<string, int>();
            floats = new Dictionary<string, float>();
            for (int i = 0; i < animatorController.boolNames.Count; i++)
            {
                bools.Add(animatorController.boolNames[i], false);
            }
            for (int i = 0; i < animatorController.intNames.Count; i++)
            {
                ints.Add(animatorController.intNames[i], 0);
            }
            for (int i = 0; i < animatorController.floatNames.Count; i++)
            {
                floats.Add(animatorController.floatNames[i], 0f);
            }
        }

        private void InitializeLogicGraph(Transform gameObjectTransform)
        {
            for (int layerIndex = 0; layerIndex < animatorController.layers.Count; layerIndex++)
            {
                logicLayers.Add(new LogicMotionMatchingLayer(
                    playableGraph,
                    animatorController.layers[layerIndex],
                    gameObjectTransform
                    ));
                for (int stateIndex = 0; stateIndex < animatorController.layers[layerIndex].states.Count; stateIndex++)
                {
                    // Zoptymalizować na wielowątkowość
                    switch (animatorController.layers[layerIndex].states[stateIndex].GetStateType())
                    {
                        case MotionMatchingStateType.MotionMatching:
                            logicLayers[layerIndex].logicStates.Add(new LogicMotionMatchingState(
                                animatorController.layers[layerIndex].states[stateIndex],
                                this,
                                playableGraph,
                                logicLayers[layerIndex],
                                gameObjectTransform,
                                framesForJob
                                ));
                            break;
                        case MotionMatchingStateType.SingleAnimation:
                            logicLayers[layerIndex].logicStates.Add(new LogicSingleAnimationState(
                                animatorController.layers[layerIndex].states[stateIndex],
                                this,
                                playableGraph,
                                logicLayers[layerIndex],
                                gameObjectTransform,
                                framesForJob
                                ));

                            break;
                        case MotionMatchingStateType.ContactAnimationState:
                            if (animatorController.layers[layerIndex].states[stateIndex].csFeatures.contactStateType == ContactStateType.NormalContacts)
                            {
                                logicLayers[layerIndex].logicStates.Add(new LogicContactState(
                                    animatorController.layers[layerIndex].states[stateIndex],
                                    this,
                                    playableGraph,
                                    logicLayers[layerIndex],
                                    gameObjectTransform,
                                    framesForJob
                                    ));
                            }
                            else
                            {
                                logicLayers[layerIndex].logicStates.Add(new LogicImpactState(
                                       animatorController.layers[layerIndex].states[stateIndex],
                                       this,
                                       playableGraph,
                                       logicLayers[layerIndex],
                                       gameObjectTransform,
                                       framesForJob
                                       ));
                            }
                            break;
                    }
                }
            }
        }

        private void StartLogicGraph()
        {
            for (int layerIndex = 0; layerIndex < logicLayers.Count; layerIndex++)
            {
                logicLayers[layerIndex].Start();
            }
        }

        #endregion

        #region Trajectory corrections method

        private void TrajectoryCorrection()
        {
            switch (trajectoryCorrectionType)
            {
                case TrajectoryCorrectionType.Constant:
                    ConstantTrajectoryCorrection(
                        minAngle,
                        maxAngle
                        );
                    break;
                //case TrajectoryCorrectionType.REACH_TARGET:
                //    //ReachingTargetBeforeTransitionTrajectoryCorrection();
                //    break;
                case TrajectoryCorrectionType.Progresive:
                    ProgresiveTrajectoryCorrection(
                        minAngleSpeed,
                        maxSpeed,
                        minAngle,
                        maxAngle
                        );
                    break;
                case TrajectoryCorrectionType.MatchOrientationConstant:
                    OrientationConstantCorrection();
                    break;
                case TrajectoryCorrectionType.MatchOrientationProgresive:
                    OrientationProgresiveCorrection(
                        minAngleSpeed,
                        maxSpeed,
                        minAngle,
                        maxAngle
                        );
                    break;
            }
        }

        private void ConstantTrajectoryCorrection(float minAngle, float maxAngle)
        {
            if (inputTrajectory.Length <= trajectoryFirstFutureIndex)
            {
                return;
            }


            float3 wantedFuturePos = inputTrajectory.GetPoint(trajectoryFirstFutureIndex).position;
            float3 minDistanceVector = wantedFuturePos - inputTrajectory.GetPoint(trajectoryFirstFutureIndex + 1).position;

            if (math.lengthsq(minDistanceVector) <= 0.001f)
            {
                return;
            }

            logicLayers[0].logicStates[logicLayers[0].GetCurrentStateIndex()].GetCurrentAnimationTrajectory();

            Vector3 fpPos = logicLayers[0].currentAnimationTrajectory.GetPoint(trajectoryFirstFutureIndex).position;

            fpPos = this.transform.TransformPoint(fpPos);

            Vector3 currentDir;
            Vector3 wantedDir;
            float angle;
            if (strafe)
            {
                currentDir = this.transform.forward;
                wantedDir = strafeForward;
                angle = Mathf.Abs(Vector3.Angle(currentDir, wantedDir));

                if (angle < strafeMaxAngle)
                {
                    currentDir = fpPos - this.transform.position;
                    wantedDir = (Vector3)inputTrajectory.GetPoint(trajectoryFirstFutureIndex).position - this.transform.position;
                }

            }
            else
            {
                currentDir = fpPos - this.transform.position;
                wantedDir = (Vector3)inputTrajectory.GetPoint(trajectoryFirstFutureIndex).position - this.transform.position;
                angle = Mathf.Abs(Vector3.Angle(currentDir, wantedDir));
            }

            if (minAngle <= angle && angle <= maxAngle)
            {
                Quaternion deltaRot = Quaternion.FromToRotation(currentDir, wantedDir);
                Quaternion desiredRotation = this.transform.rotation * deltaRot;
                desiredRotation = Quaternion.Euler(
                    0f,
                    desiredRotation.eulerAngles.y,
                    0f);
                this.transform.rotation = Quaternion.RotateTowards(
                                                this.transform.rotation,
                                                desiredRotation,
                                                maxSpeed * Time.deltaTime
                                                );
            }
        }

        private void ReachingTargetBeforeTransitionTrajectoryCorrection()
        {

        }

        private void ProgresiveTrajectoryCorrection(
            float speedInMinAngle,
            float speedInMaxAngle,
            float minAngle,
            float maxAngle
            )
        {
            if (inputTrajectory.Length <= trajectoryFirstFutureIndex)
            {
                return;
            }

            float3 wantedFuturePos = inputTrajectory.GetPoint(trajectoryFirstFutureIndex).position;
            float3 minDistanceVector = wantedFuturePos - inputTrajectory.GetPoint(trajectoryFirstFutureIndex + 1).position;

            if (math.lengthsq(minDistanceVector) <= 0.001f)
            {
                return;
            }

            logicLayers[0].logicStates[logicLayers[0].GetCurrentStateIndex()].GetCurrentAnimationTrajectory();

            Vector3 fpPos = logicLayers[0].currentAnimationTrajectory.GetPoint(trajectoryFirstFutureIndex).position;

            fpPos = this.transform.TransformPoint(fpPos);

            Vector3 currentDir;
            Vector3 wantedDir;
            float angle;
            if (strafe)
            {
                currentDir = this.transform.forward;
                wantedDir = strafeForward;
                angle = Mathf.Abs(Vector3.Angle(currentDir, wantedDir));

                if (angle < strafeMaxAngle)
                {
                    currentDir = fpPos - this.transform.position;
                    wantedDir = (Vector3)inputTrajectory.GetPoint(trajectoryFirstFutureIndex).position - this.transform.position;
                }

            }
            else
            {
                currentDir = fpPos - this.transform.position;
                wantedDir = (Vector3)inputTrajectory.GetPoint(trajectoryFirstFutureIndex).position - this.transform.position;
                angle = Mathf.Abs(Vector3.Angle(currentDir, wantedDir));
            }

            if (minAngle < angle && angle < maxAngle)
            {
                Quaternion deltaRot = Quaternion.FromToRotation(currentDir, wantedDir);
                Quaternion desiredRotation = this.transform.rotation * deltaRot;
                desiredRotation = Quaternion.Euler(0f, desiredRotation.eulerAngles.y, 0f);

                float speedFactor = (angle - minAngle) / (maxAngle - minAngle);
                float rotationSpeed = Mathf.Lerp(speedInMinAngle, speedInMaxAngle, speedFactor);

                this.transform.rotation = Quaternion.RotateTowards(
                                                this.transform.rotation,
                                                desiredRotation,
                                                rotationSpeed * Time.deltaTime
                                                );
            }
        }

        private void OrientationConstantCorrection()
        {
            float3 wantedOrientation = inputTrajectory.GetPoint(trajectoryFirstFutureIndex).orientation;
            float3 currentOrientation = this.transform.forward;

            float angle = Mathf.Abs(Vector3.Angle(wantedOrientation, currentOrientation));

            if (minAngle <= angle && angle <= maxAngle)
            {
                Quaternion deltaRot = Quaternion.FromToRotation(currentOrientation, wantedOrientation);
                Quaternion desiredRotation = this.transform.rotation * deltaRot;
                desiredRotation = Quaternion.Euler(
                    0f,
                    desiredRotation.eulerAngles.y,
                    0f);
                this.transform.rotation = Quaternion.RotateTowards(
                                                this.transform.rotation,
                                                desiredRotation,
                                                maxSpeed * Time.deltaTime
                                                );
            }
        }

        private void OrientationProgresiveCorrection(
            float speedInMinAngle,
            float speedInMaxAngle,
            float minAngle,
            float maxAngle
            )
        {
            float3 wantedOrientation = inputTrajectory.GetPoint(trajectoryFirstFutureIndex).orientation;
            float3 currentOrientation = this.transform.forward;

            float angle = Mathf.Abs(Vector3.Angle(wantedOrientation, currentOrientation));

            if (minAngle < angle && angle < maxAngle)
            {
                Quaternion deltaRot = Quaternion.FromToRotation(currentOrientation, wantedOrientation);
                Quaternion desiredRotation = this.transform.rotation * deltaRot;
                desiredRotation = Quaternion.Euler(0f, desiredRotation.eulerAngles.y, 0f);

                float speedFactor = (angle - minAngle) / (maxAngle - minAngle);
                float rotationSpeed = Mathf.Lerp(speedInMinAngle, speedInMaxAngle, speedFactor);

                this.transform.rotation = Quaternion.RotateTowards(
                                                this.transform.rotation,
                                                desiredRotation,
                                                rotationSpeed * Time.deltaTime
                                                );
            }
        }

        public void SetStrafeForward(Vector3 strafeForward)
        {
            this.strafeForward = strafeForward;
        }

        public void SetTrajectoryCorrectionType(TrajectoryCorrectionType type)
        {
            this.trajectoryCorrectionType = type;
        }

        public void SetMinAngleSpeed(float speed)
        {
            minAngleSpeed = speed;
        }

        public void SetMaxAngleSpeed(float speed)
        {
            maxSpeed = speed;
        }

        public void SetMinAngleCorrection(float minAngle)
        {
            this.minAngle = Mathf.Clamp(minAngle, 0, maxAngle);
        }

        public void SetMaxAngleCorrection(float maxAngle)
        {
            this.maxAngle = Mathf.Clamp(maxAngle, minAngle, 180f);
        }

        public void SetConstatnCorrectionSpeed(float speed)
        {
            this.maxAngle = speed;
        }

        public void SetStrafeMaxAngle(float angle)
        {
            this.strafeMaxAngle = angle;
        }

        public TrajectoryCorrectionType GetTrajectoryCorrectionType()
        {
            return trajectoryCorrectionType;
        }

        public float GetMaxCorrectionAngle()
        {
            return maxAngle;
        }

        public float GetMinCorrectionAngle()
        {
            return minAngle;
        }

        public float GetMaxAngleSpeed()
        {
            return maxAngle;
        }

        public float GetConstantCorrectionSpeed()
        {
            return maxAngle;
        }

        public float GetMinAngleSpeed()
        {
            return minAngle;
        }

        public float GetStrafeMaxAngle()
        {
            return strafeMaxAngle;
        }
        #endregion

#if UNITY_EDITOR
        [Header("DEBUG")]
        [SerializeField]
        private bool drawAnimationTrajectory = true;
        [SerializeField]
        private bool drawCurrentPose = false;
        [SerializeField]
        private bool drawBoneVelocities = false;
        [Range(0.01f, 0.1f)]
        [SerializeField]
        private float pointRadius = 0.05f;

        #region Drawing Gizmos
        private void DrawTrajectory()
        {
            if (drawAnimationTrajectory)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(this.transform.position + Vector3.up * pointRadius, pointRadius);
            }

            if (drawAnimationTrajectory)
            {
                Gizmos.color = Color.red;
                Trajectory animT = this.GetCurrentAnimationTrajectory(0);
                animT.TransformToWorldSpace(this.transform);
                MM_Gizmos.DrawTrajectory(
                    trajectoryPointsTimes,
                    this.transform.position,
                    this.transform.forward,
                    animT,
                    false,
                    pointRadius,
                    0.3f
                    );
            }
        }

        private void DrawPose()
        {
            PoseData pose = this.GetCurrentPose(0);

            for (int i = 0; i < pose.Count; i++)
            {
                BoneData bone = pose.bones[i];
                Vector3 pos = this.transform.TransformPoint(bone.localPosition);
                Vector3 vel = this.transform.TransformDirection(bone.velocity);

                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(pos, 0.05f);
                if (drawBoneVelocities)
                {
                    Gizmos.color = Color.yellow;
                    MM_Gizmos.DrawArrow(pos, pos + vel, 0.05f);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                DrawTrajectory();

                if (drawCurrentPose)
                {
                    DrawPose();
                }
            }
        }
        #endregion
#endif
    }
}