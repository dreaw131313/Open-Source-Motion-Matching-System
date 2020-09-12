using System.Collections.Generic;
using UnityEngine;
namespace DW_Gameplay
{
    [CreateAssetMenu(fileName = "TrajectoryProfile", menuName = "MotionMatching/Joystick/TrajectoryProfile")]
    public class TrajectoryProfile : ScriptableObject
    {
        [SerializeField]
        public List<TrajectoryCreationSettings> trajectorySettings;
    }

    [System.Serializable]
    public struct TrajectoryCreationSettings
    {
        [SerializeField]
        public string Name;
        [SerializeField]
        [Range(0f, 20f)]
        public float bias;
        [SerializeField]
        [Range(0f,1f)]
        public float stiffness;
        [SerializeField]
        [Range(0f, 5f)]
        public float MaxTimeToCalculateFactor;
        [SerializeField]
        [Range(0f, 1f)]
        public float sharpTurnFactor;
        [SerializeField]
        public float maxSpeed;
        [SerializeField]
        public float acceleration;
        [SerializeField]
        public float deceleration;
    }
}
