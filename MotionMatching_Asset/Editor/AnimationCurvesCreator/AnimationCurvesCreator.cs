using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DW_Editor
{
    public class AnimationCurvesCreator : ScriptableObject
    {

    }

    [System.Serializable]
    public class CreationCurveOptions
    {
        [SerializeField]
        public List<AnimationClip> animations;
        [SerializeField]
        public List<Transform> transforms;
        [SerializeField]
        public List<CurveCreationCondition> curvesCondition;
        [SerializeField]
        public float sampleTime = 0.0167f;

        public CreationCurveOptions()
        {
            animations = new List<AnimationClip>();
            transforms = new List<Transform>();
        }

        public void CreateCurve(AnimationClip animation, string curveName)
        {
            AnimationCurve curve = new AnimationCurve();

            // creating graph


            int steps = Mathf.CeilToInt(animation.length / sampleTime);
            float deltaTime = animation.length / (float)steps;
            float currentAnimationTime = 0f;

            for (int i = 0; i < steps; i++)
            {
                currentAnimationTime += deltaTime;


            }

        }
    }
}