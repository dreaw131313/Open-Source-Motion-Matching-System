using System.Collections.Generic;
using UnityEngine;

namespace DW_Gameplay
{
    public enum ConditionType : int
    {
        EQUAL,
        LESS,
        GREATER,
        GREATER_EQUAL,
        LESS_EQUAL,
        DIFFRENT,
    }

    public enum BoolConditionType : int
    {
        IS_TRUE,
        IS_FALSE
    }

    [System.Serializable]
    public class Condition
    {
#if UNITY_EDITOR
        [SerializeField]
        public bool fold = false;
#endif

        [SerializeField]
        public List<ConditionBool> boolConditions = new List<ConditionBool>();
        [SerializeField]
        public List<ConditionInt> intConditions = new List<ConditionInt>();
        [SerializeField]
        public List<ConditionFloat> floatConditions = new List<ConditionFloat>();

        public Condition()
        {
            floatConditions = new List<ConditionFloat>();
            intConditions = new List<ConditionInt>();
            boolConditions = new List<ConditionBool>();
        }

        public bool Result(MotionMatching motionMatchingComponent)
        {
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

    }


    [System.Serializable]
    public class ConditionFloat
    {
        [SerializeField]
        public string checkingValueName;
        [SerializeField]
        public float conditionValue;
        [SerializeField]
        public ConditionType type;

        public ConditionFloat(string checkingValueName, ConditionType type)
        {
            this.checkingValueName = checkingValueName;
            this.type = type;
        }

        public bool CalculateCondition(MotionMatching motionMatchingComponent)
        {
            bool condition = false;

            switch (type)
            {
                case ConditionType.EQUAL:
                    condition = motionMatchingComponent.GetFloat(checkingValueName) == conditionValue;
                    break;
                case ConditionType.LESS:
                    condition = motionMatchingComponent.GetFloat(checkingValueName) < conditionValue;
                    break;
                case ConditionType.GREATER:
                    condition = motionMatchingComponent.GetFloat(checkingValueName) > conditionValue;
                    break;
                case ConditionType.GREATER_EQUAL:
                    condition = motionMatchingComponent.GetFloat(checkingValueName) >= conditionValue;
                    break;
                case ConditionType.LESS_EQUAL:
                    condition = motionMatchingComponent.GetFloat(checkingValueName) <= conditionValue;
                    break;
                case ConditionType.DIFFRENT:
                    condition = motionMatchingComponent.GetFloat(checkingValueName) != conditionValue;
                    break;
            }

            return condition;
        }
    }

    [System.Serializable]
    public class ConditionInt
    {
        [SerializeField]
        public string checkingValueName;
        [SerializeField]
        public int conditionValue;
        [SerializeField]
        public ConditionType type;

        public ConditionInt(string name, ConditionType type)
        {
            this.type = type;
        }

        public bool CalculateCondition(MotionMatching motionMatchingComponent)
        {
            bool condition = false;

            switch (type)
            {
                case ConditionType.EQUAL:
                    condition = motionMatchingComponent.GetInt(checkingValueName) == conditionValue;
                    break;
                case ConditionType.LESS:
                    condition = motionMatchingComponent.GetInt(checkingValueName) < conditionValue;
                    break;
                case ConditionType.GREATER:
                    condition = motionMatchingComponent.GetInt(checkingValueName) > conditionValue;
                    break;
                case ConditionType.GREATER_EQUAL:
                    condition = motionMatchingComponent.GetInt(checkingValueName) >= conditionValue;
                    break;
                case ConditionType.LESS_EQUAL:
                    condition = motionMatchingComponent.GetInt(checkingValueName) <= conditionValue;
                    break;
                case ConditionType.DIFFRENT:
                    condition = motionMatchingComponent.GetInt(checkingValueName) != conditionValue;
                    break;
            }

            return condition;
        }
    }

    [System.Serializable]
    public class ConditionBool
    {
        [SerializeField]
        public string checkingValueName;
        [SerializeField]
        public BoolConditionType type;

        public ConditionBool(string name, BoolConditionType type)
        {
            this.type = type;
        }

        public bool CalculateCondition(MotionMatching motionMatchingComponent)
        {
            bool condition = false;

            switch (type)
            {
                case BoolConditionType.IS_FALSE:
                    condition = !motionMatchingComponent.GetBool(checkingValueName);
                    break;
                case BoolConditionType.IS_TRUE:
                    condition = motionMatchingComponent.GetBool(checkingValueName);
                    break;
            }

            return condition;
        }
    }

}
