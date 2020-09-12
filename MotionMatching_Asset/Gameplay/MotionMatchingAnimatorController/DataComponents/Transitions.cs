using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace DW_Gameplay
{
    [System.Serializable]
    public class Transition
    {
#if UNITY_EDITOR
        [SerializeField]
        public bool toPortal;
        [SerializeField]
        public Rect portalRect;
        [SerializeField]
        public int nodeID;
        [SerializeField]
        public Rect transitionRect;
        [SerializeField]
        public int fromStateIndex;
#endif

        [SerializeField]
        public MotionMatchingStateType nextStateType;
        [SerializeField]
        public int nextStateIndex;
        [SerializeField]
        public List<TransitionOptions> options;


        public Transition(MotionMatchingStateType type, int toState)
        {
            this.nextStateIndex = toState;
            this.nextStateType = type;
            options = new List<TransitionOptions>();

            switch (this.nextStateType)
            {
                case MotionMatchingStateType.MotionMatching:

                    break;
                case MotionMatchingStateType.SingleAnimation:

                    break;
            }
        }

        public int ShouldTransitionBegin(
            float currentAnimLocalTime,
            float currentAnimGlobalTime,
            int animationIndex,
            MotionMatching motionMatchingComponent,
            MotionMatchingStateType currentStateType,
            MotionMatchingData data
            )
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].ShouldTranistionFromThisOption(
                    currentAnimLocalTime,
                    currentAnimGlobalTime,
                    animationIndex,
                    motionMatchingComponent,
                    currentStateType,
                    data
                    ))
                {
                    return i;
                }
            }
            return -1;
        }

#if UNITY_EDITOR
#endif
    }

    [System.Serializable]
    public class TransitionOptions
    {
        [SerializeField]
        private string name;
        [SerializeField]
        public List<float2> whenCanCheckingTransition;
        [SerializeField]
        public List<float2> whereCanFindingBestPose;
        [SerializeField]
        public float blendTime;
        [SerializeField]
        public bool startOnExitTime;


        [SerializeField]
        public List<ConditionBool> boolConditions = new List<ConditionBool>();
        [SerializeField]
        public List<ConditionInt> intConditions = new List<ConditionInt>();
        [SerializeField]
        public List<ConditionFloat> floatConditions = new List<ConditionFloat>();

        public TransitionOptions(string name)
        {
            this.startOnExitTime = false;
            this.name = name;
            blendTime = 0.3f;
            whenCanCheckingTransition = new List<float2>();
            whereCanFindingBestPose = new List<float2>();
        }

        public bool ShouldTranistionFromThisOption(
            float currentAnimLocalTime,
            float currentAnimGlobalTime,
            int animationIndex,
            MotionMatching motionMatchingComponent,
            MotionMatchingStateType currentStateType,
            MotionMatchingData data
            )
        {
            if (currentStateType != MotionMatchingStateType.MotionMatching)
            {
                if (
                    (currentAnimLocalTime < this.whenCanCheckingTransition[animationIndex].x || currentAnimLocalTime > this.whenCanCheckingTransition[animationIndex].y)
                    )
                {
                    if (
                        (startOnExitTime && currentAnimGlobalTime < (data.animationLength - (blendTime))) ||
                        !startOnExitTime
                        )
                    {
                        return false;
                    }
                }

            }

            for (int conditionIndex = 0; conditionIndex < floatConditions.Count; conditionIndex++)
            {
                if (!floatConditions[conditionIndex].CalculateCondition(motionMatchingComponent))
                {
                    return false;
                }
            }

            for (int conditionIndex = 0; conditionIndex < intConditions.Count; conditionIndex++)
            {
                if (!intConditions[conditionIndex].CalculateCondition(motionMatchingComponent))
                {
                    return false;
                }
            }

            for (int conditionIndex = 0; conditionIndex < boolConditions.Count; conditionIndex++)
            {
                if (!boolConditions[conditionIndex].CalculateCondition(motionMatchingComponent))
                {
                    return false;
                }
            }

            return true;
        }

        public string GetName()
        {
            return this.name;
        }

        public void SetName(string name)
        {
            this.name = name;
        }

        public void AddCheckingTransitionOption(MotionMatchingState fromState)
        {
            if (fromState.GetStateType() != MotionMatchingStateType.MotionMatching)
            {
                for (int i = 0; i < fromState.motionDataGroups[0].animationData.Count; i++)
                {
                    this.whenCanCheckingTransition.Add(new Vector2(0f, fromState.motionDataGroups[0].animationData[i].animationLength));
                }
            }
        }

        public void AddFindigBestPoseOption(MotionMatchingState toState)
        {
            if (toState.GetStateType() != MotionMatchingStateType.MotionMatching)
            {
                for (int i = 0; i < toState.motionDataGroups[0].animationData.Count; i++)
                {
                    whereCanFindingBestPose.Add(new Vector2(0f, toState.motionDataGroups[0].animationData[i].animationLength));
                }
            }
        }

        public void AddCheckingTransitionOption(Vector2 o)
        {
            whenCanCheckingTransition.Add(o);
        }

        public void AddFindigBestPoseOption(Vector2 o)
        {
            whereCanFindingBestPose.Add(o);
        }

        public void RemoveCheckingTransitionOption(int index)
        {
            whenCanCheckingTransition.RemoveAt(index);
        }

        public void RemoveFindigBestPoseOption(int index)
        {
            whereCanFindingBestPose.RemoveAt(index);
        }
    }
}