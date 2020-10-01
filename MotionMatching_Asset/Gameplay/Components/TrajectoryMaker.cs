
using DW_Editor;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace DW_Gameplay
{
    public class TrajectoryMaker : MonoBehaviour
    {
        [SerializeField]
        private Transform animatedObject;
        [Space]
        [Header("TRAJECTORY OPTIONS")]
        [SerializeField]
        private TrajectoryCreationType creationType = TrajectoryCreationType.Smooth;
        [SerializeField]
        [Range(0.001f, 10)]
        private float bias = 1f;
        [SerializeField]
        [Range(0f, 1f)]
        private float stiffness = 0f;
        [SerializeField]
        [Range(0.01f, 5f)]
        private float maxTimeToCalculateFactor = 1f;
        [SerializeField]
        [Range(0f, 1f)]
        private float sharpTurnFactor = 1f;
        [SerializeField]
        [Min(0.01f)]
        private float maxSpeed = 4f;
        [SerializeField]
        [Min(0.01f)]
        private float acceleration = 4f;
        [SerializeField]
        [Min(0.01f)]
        private float deceleration = 4f;
        [SerializeField]
        private PastTrajectoryType pastTrajectoryType = PastTrajectoryType.Recorded;
        [SerializeField]
        private float trajectoryRecordUpdateTime = 0.033f;
        [SerializeField]
        private bool strafe = false;
        //[SerializeField]
        //private bool trajectoryWithCollision = false;
        [SerializeField]
        private bool orientationFromCollisionTrajectory = false;
        [SerializeField]
        private float capsuleHeight = 1.7f;
        [SerializeField]
        private float capsuleRadius = 0.3f;
        [SerializeField]
        private LayerMask collisionMask;
        [SerializeField]
        private bool useAttachedMotionMatchingComponent = true;
        [SerializeField]
        public List<float> trajectoryTimes;
#if UNITY_EDITOR
        [Header("DEBUG")]
        [SerializeField]
        public bool drawDebug = true;
        [SerializeField]
        public float pointRadius = 0.04f;
#endif

        // Components
        private CapsuleCollider capsule;

        // Private
        private Trajectory noCollisionTrajectory;
        private Trajectory collisionTrajectory;
        private Vector3 lastForward;

        private MotionMatching MMC;
        private int firstIndexWithFutureTime;
        private Vector3 input;

        private float recordTimer;
        private List<RecordedTrajectoryPoint> recordedTrajectoryPoints;
        private float3 bufforPosition;
        private Vector3 strafeForward;


        public float Bias { get => bias; set => bias = value; }
        public float MaxSpeed { get => maxSpeed; set => maxSpeed = value; }
        public float Acceleration { get => acceleration; set => acceleration = value; }
        public PastTrajectoryType PastTrajectoryType
        {
            get => pastTrajectoryType;
            set
            {
                pastTrajectoryType = value;
                if (recordedTrajectoryPoints != null)
                {
                    recordedTrajectoryPoints.Clear();
                }
            }
        }
        public float TrajectoryRecordUpdateTime { get => trajectoryRecordUpdateTime; set => trajectoryRecordUpdateTime = value; }
        public bool Strafe { get => strafe; set => strafe = value; }
        public float CapsuleHeight { get => capsuleHeight; set => capsuleHeight = value; }
        public float CapsuleRadius { get => capsuleRadius; set => capsuleRadius = value; }
        public LayerMask CollisionMask { get => collisionMask; set => collisionMask = value; }
        public float Stiffness { get => stiffness; set => stiffness = value; }
        public TrajectoryCreationType CreationType { get => creationType; set => creationType = value; }
        public float Deceleration { get => deceleration; set => deceleration = value; }
        public bool OrientationFromCollisionTrajectory { get => orientationFromCollisionTrajectory; set => orientationFromCollisionTrajectory = value; }
        public bool UseAttachedMotionMatchingComponent { get => useAttachedMotionMatchingComponent; set => useAttachedMotionMatchingComponent = value; }

        // Need be seted by user
        public Vector3 Input { get => input; set => input = value; }
        public Vector3 StrafeDirection { get => strafeForward; set => strafeForward = value; }

        private void Awake()
        {
            if (animatedObject == null)
            {
                animatedObject = this.transform;
            }
            capsule = GetComponent<CapsuleCollider>();

            if (capsule != null)
            {
                capsuleHeight = capsule.height;
                capsuleRadius = capsule.radius;
            }

            recordTimer = 0f;
            recordedTrajectoryPoints = new List<RecordedTrajectoryPoint>();
            bufforPosition = animatedObject.position;
            strafeForward = animatedObject.forward;
            input = Vector3.zero;

        }

        void Start()
        {
            if (useAttachedMotionMatchingComponent)
            {
                MMC = animatedObject.GetComponent<MotionMatching>();
                if (MMC == null)
                {
                    throw new System.Exception("Cannot find the Motion Matching component!");
                }

                noCollisionTrajectory = MMC.GetTrajectorySample(0);
                collisionTrajectory = new Trajectory(noCollisionTrajectory.Length);

                for (int i = 0; i < noCollisionTrajectory.Length; i++)
                {
                    noCollisionTrajectory.SetPoint(animatedObject.position, Vector3.zero, animatedObject.forward, i);
                    collisionTrajectory.SetPoint(animatedObject.position, Vector3.zero, animatedObject.forward, i);
                }

                firstIndexWithFutureTime = MMC.GetFirstPointIndexWithFutureTime();

                //finalFactors = new float[collisionTrajectory.Length - firstIndexWithFutureTime];

                MMC.SetInputTrajectory(noCollisionTrajectory);
                MMC.GetTrajectoryPointsTimes(ref trajectoryTimes);
            }
            else
            {
                trajectoryTimes.Sort();
                noCollisionTrajectory = new Trajectory(trajectoryTimes.Count);
                collisionTrajectory = new Trajectory(trajectoryTimes.Count);

                for (int i = 0; i < noCollisionTrajectory.Length; i++)
                {
                    noCollisionTrajectory.SetPoint(animatedObject.position, Vector3.zero, animatedObject.forward, i);
                    collisionTrajectory.SetPoint(animatedObject.position, Vector3.zero, animatedObject.forward, i);
                }

                for (int i = 0; i < trajectoryTimes.Count; i++)
                {
                    if (trajectoryTimes[i] >= 0)
                    {
                        firstIndexWithFutureTime = i;
                        break;
                    }
                }
            }

        }

        private void LateUpdate()
        {
            if (Input != Vector3.zero)
            {
                lastForward = Input.normalized;
            }
            CreatePastTrajectory();
            CreateFutureTrajectory();
            if (MMC != null)
            {
                MMC.SetInputTrajectory(collisionTrajectory);
            }
        }

        private void CreatePastTrajectory()
        {
            switch (pastTrajectoryType)
            {
                case PastTrajectoryType.Recorded:
                    collisionTrajectory.RecordPastTimeTrajectory(
                        trajectoryTimes,
                        trajectoryRecordUpdateTime,
                        Time.deltaTime,
                        ref recordTimer,
                        ref recordedTrajectoryPoints,
                        animatedObject.position,
                        GetCurrentForward()
                        );
                    break;
                case PastTrajectoryType.CopyFromCurrentData:
                    if (MMC != null)
                    {
                        MMC.SetPastPointsFromData(ref collisionTrajectory, 0);
                        MMC.SetInputTrajectory(collisionTrajectory);
                    }
                    break;
            }
        }

        private void CreateFutureTrajectory()
        {
            switch (creationType)
            {
                case TrajectoryCreationType.Constant:
                    bufforPosition = animatedObject.position;
                    collisionTrajectory.CreateConstantTrajectory(
                        transform.position,
                        lastForward,
                        strafeForward,
                        input * maxSpeed,
                        maxSpeed * input.magnitude,
                        strafe,
                        firstIndexWithFutureTime
                        );
#if UNITY_EDITOR
                    for (int i = firstIndexWithFutureTime; i < noCollisionTrajectory.Length; i++)
                    {
                        noCollisionTrajectory.SetPoint(collisionTrajectory.GetPoint(i), i);
                    }
#endif
                    break;
                case TrajectoryCreationType.ConstantWithCollision:
                    bufforPosition = animatedObject.position;
                    Trajectory.CreateConstantTrajectoryWithCollision(
                                ref collisionTrajectory,
                                ref noCollisionTrajectory,
                                trajectoryTimes,
                                transform.position,
                                lastForward,
                                strafeForward,
                                input * maxSpeed,
                                maxSpeed * input.magnitude,
                                strafe,
                                firstIndexWithFutureTime,
                                capsuleHeight,
                                capsuleRadius,
                                collisionMask,
                                orientationFromCollisionTrajectory
                        );
#if UNITY_EDITOR
                    for (int i = firstIndexWithFutureTime; i < noCollisionTrajectory.Length; i++)
                    {
                        noCollisionTrajectory.SetPoint(collisionTrajectory.GetPoint(i), i);
                    }
#endif
                    break;
                case TrajectoryCreationType.Smooth:
                    collisionTrajectory.CreateTrajectory(
                        trajectoryTimes,
                        transform.position,
                        ref bufforPosition,
                        lastForward,
                        strafeForward,
                        input * maxSpeed,
                        input.sqrMagnitude > 0.01 ? acceleration : deceleration,
                        bias,
                        stiffness,
                        maxTimeToCalculateFactor,
                        sharpTurnFactor,
                        strafe,
                        firstIndexWithFutureTime
                        );

#if UNITY_EDITOR
                    for (int i = firstIndexWithFutureTime; i < noCollisionTrajectory.Length; i++)
                    {
                        noCollisionTrajectory.SetPoint(collisionTrajectory.GetPoint(i), i);
                    }
#endif
                    break;
                case TrajectoryCreationType.SmoothWithCollision:
                    Trajectory.CreateCollisionTrajectory(
                                ref collisionTrajectory,
                                ref noCollisionTrajectory,
                                trajectoryTimes,
                                transform.position,
                                ref bufforPosition,
                                lastForward,
                                strafeForward,
                                input * maxSpeed,
                                input.sqrMagnitude > 0.01 ? acceleration : deceleration,
                                bias,
                                stiffness,
                                maxTimeToCalculateFactor,
                                sharpTurnFactor,
                                strafe,
                                firstIndexWithFutureTime,
                                capsuleHeight,
                                capsuleRadius,
                                collisionMask,
                                orientationFromCollisionTrajectory
                        );
                    break;
            }
        }

        public void SetTrajectoryCreationType(TrajectoryCorrectionType type)
        {
            switch (creationType)
            {
                case TrajectoryCreationType.Constant:
                    this.creationType = TrajectoryCreationType.Constant;
                    CreateFutureTrajectory();
                    if (MMC != null)
                    {
                        MMC.SetInputTrajectory(collisionTrajectory);
                    }
                    break;
                case TrajectoryCreationType.ConstantWithCollision:
                    this.creationType = TrajectoryCreationType.ConstantWithCollision;
                    CreateFutureTrajectory();
                    if (MMC != null)
                    {
                        MMC.SetInputTrajectory(collisionTrajectory);
                    }
                    break;
                case TrajectoryCreationType.Smooth:

                    this.creationType = TrajectoryCreationType.Smooth;
                    break;
                case TrajectoryCreationType.SmoothWithCollision:
                    for (int i = firstIndexWithFutureTime; i < noCollisionTrajectory.Length; i++)
                    {
                        noCollisionTrajectory.SetPoint(collisionTrajectory.GetPoint(i), i);
                    }
                    this.creationType = TrajectoryCreationType.SmoothWithCollision;
                    break;
            }
        }

        public void SetTrajectory(Trajectory trajectory)
        {
#if UNITY_EDITOR
            if (trajectory.Length != collisionTrajectory.Length)
            {
                Debug.LogError(string.Format("Wrong number of points seted to Trajectory Creator in object {0}!. Trajectory creator create only {1} points, trajectory which is seted have {2} points", this.name, this.collisionTrajectory.Length, trajectory.Length));
                return;
            }
#endif

            for (int i = 0; i < collisionTrajectory.Length; i++)
            {
                collisionTrajectory.SetPoint(
                    trajectory.GetPoint(i),
                    i
                    );
                noCollisionTrajectory.SetPoint(
                    trajectory.GetPoint(i),
                    i
                    );
            }
            if (MMC != null)
            {
                MMC.SetInputTrajectory(collisionTrajectory);
            }

        }

        public Vector3 GetCurrentVelocity()
        {
            return collisionTrajectory.GetPoint(firstIndexWithFutureTime).velocity;
        }

        public Vector3 GetCurrentForward()
        {
            if (animatedObject != this.transform)
            {
                return animatedObject.forward;
            }
            return collisionTrajectory.GetPoint(firstIndexWithFutureTime).orientation;
        }

        public void SetMototionMatchingComponent(MotionMatching motionMatching)
        {
            this.MMC = motionMatching;
        }

        public void StartUseMotionMatchingComponent()
        {
            trajectoryTimes = new List<float>();

            noCollisionTrajectory = new Trajectory(MMC.GetTrajectorySample(0).Length);
            collisionTrajectory = new Trajectory(noCollisionTrajectory.Length);

            for (int i = 0; i < noCollisionTrajectory.Length; i++)
            {
                noCollisionTrajectory.SetPoint(animatedObject.position, Vector3.zero, animatedObject.forward, i);
                collisionTrajectory.SetPoint(animatedObject.position, Vector3.zero, animatedObject.forward, i);
            }

            firstIndexWithFutureTime = MMC.GetFirstPointIndexWithFutureTime();

            //finalFactors = new float[collisionTrajectory.Length - firstIndexWithFutureTime];

            MMC.SetInputTrajectory(noCollisionTrajectory);
            MMC.GetTrajectoryPointsTimes(ref trajectoryTimes);
        }

        public Trajectory GetCurrentTrajectory()
        {
            return collisionTrajectory;
        }

        public ref Trajectory GetCurrentTrajectoryRefernece()
        {
            return ref collisionTrajectory;
        }

        public void SetTrajectoryToMotionMatching(MotionMatching motionMatching)
        {
            motionMatching.SetInputTrajectory(collisionTrajectory);
        }

        public void SetTrajectorySettings(TrajectoryCreationSettings settings)
        {
            this.bias = settings.bias;
            this.stiffness = settings.stiffness;
            this.maxTimeToCalculateFactor = settings.MaxTimeToCalculateFactor;
            this.sharpTurnFactor = settings.sharpTurnFactor;
            this.MaxSpeed = settings.maxSpeed;
            this.acceleration = settings.acceleration;
            this.deceleration = settings.deceleration;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                if (drawDebug)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(this.transform.position + Vector3.up * pointRadius, pointRadius);

                    Gizmos.color = Color.green;
                    MM_Gizmos.DrawTrajectory(
                        trajectoryTimes.ToArray(),
                        transform.position,
                        GetCurrentForward(),
                        collisionTrajectory,
                        false,
                        pointRadius,
                        0.3f
                        );
                }
            }
        }
#endif

    }
}


