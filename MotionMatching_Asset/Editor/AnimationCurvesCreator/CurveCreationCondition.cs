using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DW_Editor
{
    public abstract class CurveCreationCondition : ScriptableObject
    {
        public abstract void GetTransformInfoBeforeAnimationUpdate(Transform currentTransform);
        public abstract void GetTransformInfoAfterAnimationUpdate(Transform currentTransform);
        public abstract bool ConditionBasedOnTransformInfos();
        public abstract bool SetingCurveValues(float animationTime, AnimationCurve createdCurve, bool conditionResult);
    }
}