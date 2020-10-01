using System.Collections.Generic;
using System.Security.Principal;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace DW_Gameplay
{
    [System.Serializable]
    public enum TrajectoryCreationType
    {
        Constant,
        ConstantWithCollision,
        Smooth,
        SmoothWithCollision,
    }

    [System.Serializable]
    public struct Trajectory
    {
        [SerializeField]
        private TrajectoryPoint[] points;

        public int Length
        {
            get
            {
                return points.Length;
            }
            private set
            {
            }
        }

        public Trajectory(int length)
        {
            points = new TrajectoryPoint[length];
        }

        public Trajectory(TrajectoryPoint[] points)
        {
            this.points = points;
        }

        public bool IsValid()
        {
            if (points != null)
            {
                return true;
            }
            return false;
        }

        public float CalculateCost(Trajectory toTrajectory, TrajectoryCostType type)
        {
            float cost = 0f;
            for (int i = 0; i < Length; i++)
            {
                cost += points[i].CalculateCost(toTrajectory.GetPoint(i), type);
            }
            return cost;
        }

        public void SetPoint(TrajectoryPoint point, int index)
        {
            points[index].Set(point);
        }

        public void SetPoint(float3 position, float3 velocity, float3 orientation, int index)
        {
            points[index].Set(position, velocity, orientation);
        }

        public void SetPointPos(float3 position, int index)
        {
            points[index].SetPosition(position);
        }

        public void SetPointVel(float3 velocity, int index)
        {
            points[index].SetVelocity(velocity);
        }

        public TrajectoryPoint GetPoint(int index)
        {
            return points[index];
        }

        public static void Lerp(ref Trajectory buffor, Trajectory first, Trajectory next, float factor)
        {
            for (int i = 0; i < first.Length; i++)
            {
                buffor.SetPoint(TrajectoryPoint.Lerp(first.GetPoint(i), next.GetPoint(i), factor), i);
            }
        }

        public TrajectoryPoint GetLastPoint()
        {
            return points[Length - 1];
        }

        public void TransformToLocalSpace(Transform localSpace)
        {
            for (int i = 0; i < Length; i++)
            {
                this.points[i].TransformToLocalSpace(localSpace);
            }
        }

        public void TransformToWorldSpace(Transform localSpace)
        {
            for (int i = 0; i < Length; i++)
            {
                this.points[i].TransformToWorldSpace(localSpace);
            }
        }

        private static float CalculateFinalFactor(
            float currentPointTime,
            float lastPointTime,
            float bias,
            float acceleration,
            float stepTime,
            float desSpeed,
            float percentageFactor,
            float sharpTurnMultiplier,
            float3 currentDelta,
            float3 desiredPointDeltaPosition
            )
        {
            float percentage = Mathf.Lerp(currentPointTime / lastPointTime, 1f, percentageFactor);
            //float factor1 = 1f + bias * percentage * percentage + 0.5f * percentage;
            float doublePercentage = percentage * percentage;
            float factor1 = 1f +
                            bias * (
                            doublePercentage * doublePercentage +
                            doublePercentage * percentage +
                            doublePercentage
                            );

            float deltaMag = math.length(currentDelta - desiredPointDeltaPosition);
            float factor2 = 1f;
            float pointPartSpeed = desSpeed * stepTime;

            if (deltaMag > pointPartSpeed * 1.1 && pointPartSpeed > 0)
            {
                factor2 = 1f + ((deltaMag - pointPartSpeed) / pointPartSpeed) * sharpTurnMultiplier;
            }

            float finalFactor = factor1 * factor2 * acceleration * stepTime;

            return finalFactor;
        }

        public void CreateTrajectory(
            List<float> pointsTimes,
            float3 objectPosition,
            ref float3 objectPositionBuffor,
            float3 objectForward,
            float3 strafeForward,
            float3 desiredVel,
            float acceleration,
            float bias,
            float stiffnes,
            float maxTimeToCalculateFactor,
            float sharpTurnMultiplier,
            bool strafe,
            int firstIndexWithFutureTime
            )
        {
            float3 lastPositionDelta = objectPosition - objectPositionBuffor;
            objectPositionBuffor = objectPosition;

            float desSpeed = math.length(desiredVel);
            float stepTime;
            float3 currentDelta;
            float3 pointLastdelta = float3.zero;

            for (int pointIndex = firstIndexWithFutureTime; pointIndex < this.Length; pointIndex++)
            {
                this.SetPointPos(
                    this.GetPoint(pointIndex).position + lastPositionDelta,
                    pointIndex
                    );

                if (pointIndex == (firstIndexWithFutureTime))
                {
                    stepTime = pointsTimes[pointIndex];
                    currentDelta = this.GetPoint(pointIndex).position - objectPosition;
                }
                else
                {
                    stepTime = pointsTimes[pointIndex] - pointsTimes[pointIndex - 1];
                    currentDelta = this.GetPoint(pointIndex).position - this.GetPoint(pointIndex - 1).position + pointLastdelta;
                }
                float3 desiredPointDeltaPosition = desiredVel * stepTime;

                float finalFactor = CalculateFinalFactor(
                    pointsTimes[pointIndex],
                    maxTimeToCalculateFactor,
                    bias,
                    acceleration,
                    stepTime,
                    desSpeed,
                    stiffnes,
                    sharpTurnMultiplier,
                    currentDelta,
                    desiredPointDeltaPosition
                    );

                float3 finalDelta = float3Extension.MoveFloat3WithSpeed(currentDelta, desiredPointDeltaPosition, finalFactor, Time.deltaTime);


                float3 newPosition = pointIndex != firstIndexWithFutureTime ?
                    GetPoint(pointIndex - 1).position + finalDelta
                    : objectPosition + finalDelta;
                pointLastdelta = newPosition - GetPoint(pointIndex).position;

                float3 newVelocity = pointIndex != firstIndexWithFutureTime ?
                    (GetPoint(pointIndex).position - GetPoint(pointIndex - 1).position) / stepTime
                    : (GetPoint(pointIndex).position - objectPosition) / stepTime;

                float3 newOrientation = CalculateFinalOrientation(
                        strafe,
                        strafeForward,
                        objectForward,
                        this.GetPoint(pointIndex).position,
                        pointIndex == firstIndexWithFutureTime ? objectPosition : this.GetPoint(pointIndex - 1).position
                        );

                SetPoint(newPosition, newVelocity, newOrientation, pointIndex);
            }
        }


        public void RecordPastTimeTrajectory(
            List<float> pointsTimes,
            float updateTime,
            float timeDeltaTime,
            ref float recordTimer,
            ref List<RecordedTrajectoryPoint> recordedTrajectory,
            float3 objectPosition,
            float3 objectForward
            )
        {
            if (recordedTrajectory == null)
            {
                recordedTrajectory = new List<RecordedTrajectoryPoint>();
            }
            float maxRecordedTime = pointsTimes[0] - 0.05f;
            if (maxRecordedTime >= 0)
            {
                return;
            }
            int goalPointIndex = 0;
            RecordedTrajectoryPoint buffor;
            for (int i = 0; i < recordedTrajectory.Count; i++)
            {
                if (pointsTimes[goalPointIndex] < 0)
                {
                    if (recordedTrajectory[i].futureTime >= pointsTimes[goalPointIndex])
                    {
                        if (i == 0)
                        {
                            this.SetPoint(
                                recordedTrajectory[i].position,
                                recordedTrajectory[i].velocity,
                                recordedTrajectory[i].orientation,
                                goalPointIndex
                                );
                        }
                        else
                        {
                            float factor =
                                (pointsTimes[goalPointIndex] - recordedTrajectory[i - 1].futureTime)
                                /
                                (recordedTrajectory[i].futureTime - recordedTrajectory[i - 1].futureTime);
                            RecordedTrajectoryPoint lerpedPoint = RecordedTrajectoryPoint.Lerp(recordedTrajectory[i - 1], recordedTrajectory[i], factor);
                            this.SetPoint(
                                lerpedPoint.position,
                                lerpedPoint.velocity,
                                lerpedPoint.orientation,
                                goalPointIndex
                                );
                        }


                        goalPointIndex++;
                    }
                }
                buffor = recordedTrajectory[i];
                buffor.futureTime -= timeDeltaTime;
                recordedTrajectory[i] = buffor;
                if (recordedTrajectory[i].futureTime < maxRecordedTime)
                {
                    recordedTrajectory.RemoveAt(i);
                    i--;
                    continue;
                }
            }

            recordTimer += Time.deltaTime;
            if (recordTimer < updateTime)
            {
                return;
            }

            float3 velocity;
            if (recordedTrajectory.Count > 1)
            {
                velocity = (objectPosition - recordedTrajectory[recordedTrajectory.Count - 1].position) / recordTimer;
            }
            else
            {
                velocity = new float3(0, 0, 0);
            }
            recordTimer = 0f;
            recordedTrajectory.Add(
                new RecordedTrajectoryPoint(
                    objectPosition,
                    velocity,
                    objectForward,
                    0f
                    ));
        }

        private static float3 CheckCollision(
            float3 desiredDeltaPos,
            float3 castStart,
            float capsuleHeight,
            float capsuleRadius,
            // must be greater than 0, otherwise errors will occur
            float capsuleDeltaFromObstacle,
            LayerMask mask,
            ref bool isColliding
            )
        {
            //capsuleRadius -= capsuleRadiusReduction;

            RaycastHit hit;
            if (Physics.CapsuleCast(
                        castStart + (float3)Vector3.up * capsuleRadius,
                        castStart + (float3)Vector3.up * (capsuleHeight - capsuleRadius),
                        capsuleRadius,
                        desiredDeltaPos,
                        out hit,
                        math.length(desiredDeltaPos) + 2f * capsuleDeltaFromObstacle,
                        mask
                        ))
            {
                isColliding = true;

                float3 normal = hit.normal;
                normal.y = 0f;
                normal = math.normalize(normal);

                float3 hitPoint = hit.point;
                hitPoint.y = castStart.y;

                float3 vectorToProject = (castStart + desiredDeltaPos) - hitPoint;

                return (float3)Vector3.ProjectOnPlane(vectorToProject, normal) + normal * (capsuleRadius + capsuleDeltaFromObstacle) + hitPoint - castStart;
            }
            else
            {
                isColliding = false;
                return desiredDeltaPos;
            }

        }

        private static float3 CalculateFinalOrientation(
            bool strafe,
            float3 strafeForward,
            float3 objectForward,
            float3 currentPointPosition,
            float3 previewPointPosition
            )
        {
            if (strafe)
            {
                return strafeForward;
            }
            else
            {
                float3 finalOrientation = currentPointPosition - previewPointPosition;

                if (finalOrientation.Equals(float3.zero))
                {
                    return objectForward;
                }
                else
                {
                    return math.normalize(finalOrientation);
                }
            }
        }


        public static void CreateCollisionTrajectory(
            ref Trajectory trajectory_C,
            ref Trajectory trajectory_NC,
            List<float> pointsTimes,
            float3 objectPosition,
            ref float3 objectPositionBuffor,
            // Orientation
            float3 objectForward,
            float3 strafeForward,
            // Trajectory working settings
            float3 desiredVel,
            float acceleration,
            float bias,
            float stiffnes,
            float maxTimeToCalculateFactor,
            float sharpTurnMultiplier,
            bool strafe,
            //
            int firstIndexWithFutureTime,
            //Collsions settings
            float capsuleHeight,
            float capsuleRadius,
            LayerMask mask,
            bool orientationFromCollisionTrajectory
            )
        {
            float3 lastPositionDelta = objectPosition - objectPositionBuffor;
            objectPositionBuffor = objectPosition;

            float desSpeed = math.length(desiredVel);
            float stepTime;
            float3 currentDelta_NC;
            float3 currentDelta_C;
            float3 pointLastdelta_NC = float3.zero;
            float3 pointLastdelta_C = float3.zero;
            float3 castStart = float3.zero;

            for (int pointIndex = firstIndexWithFutureTime; pointIndex < trajectory_NC.Length; pointIndex++)
            {
                trajectory_NC.SetPointPos(
                    trajectory_NC.GetPoint(pointIndex).position + lastPositionDelta,
                    pointIndex
                    );

                trajectory_C.SetPointPos(
                    trajectory_C.GetPoint(pointIndex).position + lastPositionDelta,
                    pointIndex
                    );

                if (pointIndex == firstIndexWithFutureTime)
                {
                    stepTime = pointsTimes[pointIndex];
                    currentDelta_NC = trajectory_NC.GetPoint(pointIndex).position - objectPosition;
                    currentDelta_C = trajectory_C.GetPoint(pointIndex).position - objectPosition;

                }
                else
                {
                    stepTime = pointsTimes[pointIndex] - pointsTimes[pointIndex - 1];
                    currentDelta_NC = trajectory_NC.GetPoint(pointIndex).position - trajectory_NC.GetPoint(pointIndex - 1).position + pointLastdelta_NC;
                    currentDelta_C = trajectory_C.GetPoint(pointIndex).position - trajectory_C.GetPoint(pointIndex - 1).position + pointLastdelta_C;
                }
                float3 desiredDeltaPosition_NC = desiredVel * stepTime;

                float finalFactor = CalculateFinalFactor(
                    pointsTimes[pointIndex],
                    maxTimeToCalculateFactor,
                    bias,
                    acceleration,
                    stepTime,
                    desSpeed,
                    stiffnes,
                    sharpTurnMultiplier,
                    currentDelta_NC,
                    desiredDeltaPosition_NC
                    );

                float3 finalDelta_NC = float3Extension.MoveFloat3WithSpeed(currentDelta_NC, desiredDeltaPosition_NC, finalFactor, Time.deltaTime);

                float3 newPosition_NC = pointIndex != firstIndexWithFutureTime ?
                    trajectory_NC.GetPoint(pointIndex - 1).position + finalDelta_NC
                    : objectPosition + finalDelta_NC;
                pointLastdelta_NC = newPosition_NC - trajectory_NC.GetPoint(pointIndex).position;

                #region Orientation calculation

                float3 finalOrientation;

                if (orientationFromCollisionTrajectory)
                {
                    finalOrientation = CalculateFinalOrientation(
                                            strafe,
                                            strafeForward,
                                            objectForward,
                                            trajectory_C.GetPoint(pointIndex).position,
                                            pointIndex == firstIndexWithFutureTime ? objectPosition : trajectory_C.GetPoint(pointIndex - 1).position
                                            );
                }
                else
                {
                    finalOrientation = CalculateFinalOrientation(
                        strafe,
                        strafeForward,
                        objectForward,
                        trajectory_NC.GetPoint(pointIndex).position,
                        pointIndex == firstIndexWithFutureTime ? objectPosition : trajectory_NC.GetPoint(pointIndex - 1).position
                        );
                }


                #endregion

                trajectory_NC.SetPointPos(newPosition_NC, pointIndex);

                bool isColliding = false;
                float3 colisionCheckDelta = pointIndex == firstIndexWithFutureTime ?
                       trajectory_NC.GetPoint(pointIndex).position - objectPosition :
                       trajectory_NC.GetPoint(pointIndex).position - trajectory_NC.GetPoint(pointIndex - 1).position;
                float3 colisionCheckStart = pointIndex == firstIndexWithFutureTime ? objectPosition : castStart; //trajectory_C.GetPoint(pointIndex - 1).position,

                float3 startDesiredDeltaPos_C = CheckCollision(
                    colisionCheckDelta,
                    colisionCheckStart,
                    capsuleHeight,
                    capsuleRadius - 0.05f,
                    0.05f,
                    mask,
                    ref isColliding
                    );

                float3 newPosition_C;
                if (isColliding)
                {
                    float3 finaldesiredDeltaPos_C = CheckCollision(
                        startDesiredDeltaPos_C,
                        colisionCheckStart,
                        capsuleHeight,
                        capsuleRadius - 0.05f,
                        0.05f,
                        mask,
                        ref isColliding
                        );


                    castStart = pointIndex == firstIndexWithFutureTime ?
                        objectPosition + finaldesiredDeltaPos_C :
                        castStart + finaldesiredDeltaPos_C;


                    float3 finalDelta_C = float3Extension.MoveFloat3WithSpeed(currentDelta_C, finaldesiredDeltaPos_C, 2f * finalFactor, Time.deltaTime);


                    newPosition_C = pointIndex != firstIndexWithFutureTime ?
                        trajectory_C.GetPoint(pointIndex - 1).position + finalDelta_C
                        : objectPosition + finalDelta_C;
                }
                else
                {
                    float3 finalDelta_C = float3Extension.MoveFloat3WithSpeed(currentDelta_C, startDesiredDeltaPos_C, finalFactor, Time.deltaTime);

                    newPosition_C = pointIndex != firstIndexWithFutureTime ?
                        trajectory_C.GetPoint(pointIndex - 1).position + finalDelta_C
                        : objectPosition + finalDelta_C;
                    castStart = newPosition_C;
                }

                pointLastdelta_C = newPosition_C - trajectory_C.GetPoint(pointIndex).position;

                float3 newVelocity_C = pointIndex != firstIndexWithFutureTime ?
                    (trajectory_C.GetPoint(pointIndex).position - trajectory_C.GetPoint(pointIndex - 1).position) / stepTime
                    : (trajectory_C.GetPoint(pointIndex).position - objectPosition) / stepTime;

                trajectory_C.SetPoint(newPosition_C, newVelocity_C, finalOrientation, pointIndex);

            }
        }

        public void CreateConstantTrajectory(
            float3 objectPosition,
            float3 objectForward,
            float3 strafeForward,
            float3 desiredVel,
            float maxSpeed,
            bool strafe,
            int firstIndexWithFutureTime
            )
        {
            float speedStep = maxSpeed / (this.Length - firstIndexWithFutureTime);
            float3 velDir;
            if (math.lengthsq(desiredVel) <= 0.0001f)
            {
                velDir = float3.zero;
            }
            else
            {
                velDir = math.normalize(desiredVel);
            }
            for (int pointIndex = firstIndexWithFutureTime; pointIndex < this.Length; pointIndex++)
            {
                float3 newPosition = velDir * speedStep * (float)(pointIndex - firstIndexWithFutureTime + 1) + objectPosition;
                float3 newVelocity = desiredVel;
                float3 newOrientation = CalculateFinalOrientation(
                        strafe,
                        strafeForward,
                        objectForward,
                        this.GetPoint(pointIndex).position,
                        pointIndex == firstIndexWithFutureTime ? objectPosition : this.GetPoint(pointIndex - 1).position
                        );
                this.SetPoint(
                    newPosition,
                    newVelocity,
                    newOrientation,
                    pointIndex
                    );
            }
        }

        public static void CreateConstantTrajectoryWithCollision(
            ref Trajectory trajectory_C,
            ref Trajectory trajectory_NC,
            List<float> pointsTimes,
            float3 objectPosition,
            float3 objectForward,
            float3 strafeForward,
            float3 desiredVel,
            float maxSpeed,
            bool strafe,
            int firstIndexWithFutureTime,
            float capsuleHeight,
            float capsuleRadius,
            LayerMask mask,
            bool orientationFromCollisionTrajectory
            )
        {
            float speedStep = maxSpeed / (trajectory_NC.Length - firstIndexWithFutureTime);
            float3 velDir;

            if (math.lengthsq(desiredVel) <= 0.0001f)
            {
                velDir = float3.zero;
            }
            else
            {
                velDir = math.normalize(desiredVel);
            }

            float stepTime;
            for (int pointIndex = firstIndexWithFutureTime; pointIndex < trajectory_NC.Length; pointIndex++)
            {
                if (pointIndex == firstIndexWithFutureTime)
                {
                    stepTime = pointsTimes[pointIndex];

                }
                else
                {
                    stepTime = pointsTimes[pointIndex] - pointsTimes[pointIndex - 1];
                }

                float3 newPosition = velDir * speedStep * (float)(pointIndex - firstIndexWithFutureTime + 1) + objectPosition;
                float3 newVelocity = desiredVel;
                float3 newOrientation = CalculateFinalOrientation(
                        strafe,
                        strafeForward,
                        objectForward,
                        trajectory_NC.GetPoint(pointIndex).position,
                        pointIndex == firstIndexWithFutureTime ? objectPosition : trajectory_NC.GetPoint(pointIndex - 1).position
                        );
                trajectory_NC.SetPoint(
                    newPosition,
                    newVelocity,
                    newOrientation,
                    pointIndex
                    );
                float3 finalOrientation;
                if (orientationFromCollisionTrajectory)
                {
                    finalOrientation = CalculateFinalOrientation(
                                            strafe,
                                            strafeForward,
                                            objectForward,
                                            trajectory_C.GetPoint(pointIndex).position,
                                            pointIndex == firstIndexWithFutureTime ? objectPosition : trajectory_C.GetPoint(pointIndex - 1).position
                                            );
                }
                else
                {
                    finalOrientation = CalculateFinalOrientation(
                        strafe,
                        strafeForward,
                        objectForward,
                        trajectory_NC.GetPoint(pointIndex).position,
                        pointIndex == firstIndexWithFutureTime ? objectPosition : trajectory_NC.GetPoint(pointIndex - 1).position
                        );
                }

                bool isColliding = false;
                float3 colisionCheckDelta = pointIndex == firstIndexWithFutureTime ?
                       trajectory_NC.GetPoint(pointIndex).position - objectPosition :
                       trajectory_NC.GetPoint(pointIndex).position - trajectory_NC.GetPoint(pointIndex - 1).position;
                float3 colisionCheckStart = pointIndex == firstIndexWithFutureTime ? objectPosition : trajectory_C.GetPoint(pointIndex - 1).position; //trajectory_C.GetPoint(pointIndex - 1).position,

                float3 startDesiredDeltaPos_C = CheckCollision(
                    colisionCheckDelta,
                    colisionCheckStart,
                    capsuleHeight,
                    capsuleRadius - 0.05f,
                    0.05f,
                    mask,
                    ref isColliding
                    );

                float3 newPosition_C;
                if (isColliding)
                {
                    float3 finaldesiredDeltaPos_C = CheckCollision(
                        startDesiredDeltaPos_C,
                        colisionCheckStart,
                        capsuleHeight,
                        capsuleRadius - 0.05f,
                        0.05f,
                        mask,
                        ref isColliding
                        );

                    newPosition_C = colisionCheckStart + finaldesiredDeltaPos_C;

                }
                else
                {
                    newPosition_C = colisionCheckStart + startDesiredDeltaPos_C;
                }
                float3 newVelocity_C = pointIndex != firstIndexWithFutureTime ?
                    (trajectory_C.GetPoint(pointIndex).position - trajectory_C.GetPoint(pointIndex - 1).position) / stepTime
                    : (trajectory_C.GetPoint(pointIndex).position - objectPosition) / stepTime;

                trajectory_C.SetPoint(
                    newPosition_C,
                    newVelocity_C,
                    finalOrientation,
                    pointIndex
                    );
            }
        }
    }

    [System.Serializable]
    public struct TrajectoryPoint
    {
        [SerializeField]
        public float3 position;
        [SerializeField]
        public float3 velocity;
        [SerializeField]
        public float3 orientation;

        public TrajectoryPoint(float3 position, float3 velocity, float3 orientation)
        {
            this.position = position;
            this.velocity = velocity;
            this.orientation = orientation;
        }

        public TrajectoryPoint(TrajectoryPoint point)
        {
            position = new float3(point.position.x, point.position.y, point.position.z);
            velocity = new float3(point.velocity.x, point.velocity.y, point.velocity.z);
            orientation = new float3(point.orientation.x, point.orientation.y, point.orientation.z);
        }

        [BurstCompile]
        public static TrajectoryPoint LerpPiont(TrajectoryPoint first, TrajectoryPoint next, float factor)
        {
            return new TrajectoryPoint(
                    math.lerp(first.position, next.position, factor),
                    math.lerp(first.velocity, next.velocity, factor),
                    math.lerp(first.orientation, next.orientation, factor)
                    );
        }

        [BurstCompile]
        public void Set(float3 position, float3 velocity, float3 orientation)
        {
            this.position = position;
            this.velocity = velocity;
            this.orientation = orientation;
        }
        [BurstCompile]
        public void Set(TrajectoryPoint point)
        {
            this.position = point.position;
            this.velocity = point.velocity;
            this.orientation = point.orientation;
        }

        [BurstCompile]
        public void SetPosition(float3 position)
        {
            this.position = position;
        }

        [BurstCompile]
        public void SetVelocity(float3 velocity)
        {
            this.velocity = velocity;
        }

        [BurstCompile]
        public void SetOrientation(float3 orientation)
        {
            this.orientation = orientation;
        }

        public void TransformToLocalSpace(Transform localSpace)
        {
            this.position = localSpace.InverseTransformPoint(this.position);
            this.velocity = localSpace.InverseTransformDirection(this.velocity);
            this.orientation = localSpace.InverseTransformDirection(this.orientation);
        }

        public void TransformToWorldSpace(Transform localSpace)
        {
            this.position = localSpace.TransformPoint(this.position);
            this.velocity = localSpace.TransformDirection(this.velocity);
            this.orientation = localSpace.TransformDirection(this.orientation);
        }

        #region Cost calculation
        [BurstCompile]
        public float CalculateCost(TrajectoryPoint point, TrajectoryCostType type)
        {
            float cost = 0;
            //switch (type)
            //{
                //case TrajectoryCostType.Position:
                //    cost += CalculatePositionCost(point);
                //    break;
                //case TrajectoryCostType.Velocity:
                //    cost += CalculateVelocityCost(point);
                //    break;
                //case TrajectoryCostType.Orientation:
                //    cost += CalculateOrientationCost(point);
                //    break;
                //case TrajectoryCostType.PositionVelocity:
                //    cost += CalculatePositionCost(point);
                //    cost += CalculateVelocityCost(point);
                //    break;
                //case TrajectoryCostType.PositionOrientation:
                //    cost += CalculatePositionCost(point);
                //    cost += CalculateOrientationCost(point);
                //    break;
                //case TrajectoryCostType.VelocityOrientation:
                //    cost += CalculateVelocityCost(point);
                //    cost += CalculateOrientationCost(point);
                //    break;
                //case TrajectoryCostType.PositionVelocityOrientation:
                    cost += CalculatePositionCost(point);
                    cost += CalculateVelocityCost(point);
                    cost += CalculateOrientationCost(point);
                    //break;
               // case TrajectoryCostType.None:
                    //break;
            //}
            return cost;
        }

        [BurstCompile]
        public float CalculatePositionCost(TrajectoryPoint point)
        {
            return math.lengthsq(point.position - position);
        }

        [BurstCompile]
        public float CalculateVelocityCost(TrajectoryPoint point)
        {
            return math.lengthsq(point.velocity - velocity);
        }

        [BurstCompile]
        public float CalculateOrientationCost(TrajectoryPoint point)
        {
            return math.lengthsq(point.orientation - orientation);
        }

        [BurstCompile]
        public float3 CalculateVectorCost(TrajectoryPoint point, TrajectoryCostType type)
        {
            float3 cost = float3.zero;
            switch (type)
            {
                //case TrajectoryCostType.Position:
                //    cost += CalculatePositionVectorCost(point);
                //    break;
                //case TrajectoryCostType.Velocity:
                //    cost += CalculateVelocityVectorCost(point);
                //    break;
                //case TrajectoryCostType.Orientation:
                //    cost += CalculateOrientationVectorCost(point);
                //    break;
                //case TrajectoryCostType.PositionVelocity:
                //    cost += CalculatePositionVectorCost(point);
                //    cost += CalculateVelocityVectorCost(point);
                //    break;
                //case TrajectoryCostType.PositionOrientation:
                //    cost += CalculatePositionVectorCost(point);
                //    cost += CalculateOrientationVectorCost(point);
                //    break;
                //case TrajectoryCostType.VelocityOrientation:
                //    cost += CalculateVelocityVectorCost(point);
                //    cost += CalculateOrientationVectorCost(point);
                //    break;
                case TrajectoryCostType.PositionVelocityOrientation:
                    cost += CalculatePositionVectorCost(point);
                    cost += CalculateVelocityVectorCost(point);
                    cost += CalculateOrientationVectorCost(point);
                    break;
                case TrajectoryCostType.None:
                    break;
            }
            return cost;
        }

        [BurstCompile]
        public float3 CalculatePositionVectorCost(TrajectoryPoint point)
        {
            return math.abs(point.position - position);
        }

        [BurstCompile]
        public float3 CalculateVelocityVectorCost(TrajectoryPoint point)
        {
            return math.abs(point.velocity - velocity);
        }

        [BurstCompile]
        public float3 CalculateOrientationVectorCost(TrajectoryPoint point)
        {
            return math.abs(point.orientation - orientation);
        }
        #endregion

        #region Static methods


        [BurstCompile]
        public static TrajectoryPoint Lerp(TrajectoryPoint point1, TrajectoryPoint point2, float factor)
        {
            float3 dir = math.lerp(point1.orientation, point2.orientation, factor);
            if (dir.x != 0f && dir.y != 0f && dir.z != 0f)
            {
                dir = math.normalize(dir);
            }
            else
            {
                dir = point1.orientation;
            }
            return new TrajectoryPoint(
                math.lerp(point1.position, point2.position, factor),
                math.lerp(point1.velocity, point2.velocity, factor),
                dir
            );
        }

        #endregion
    }
    public struct RecordedTrajectoryPoint
    {
        [SerializeField]
        public float3 position;
        [SerializeField]
        public float3 velocity;
        [SerializeField]
        public float3 orientation;
        [SerializeField]
        public float futureTime;

        public RecordedTrajectoryPoint(float3 position, float3 velocity, float3 orientation, float futureTime)
        {
            this.position = position;
            this.velocity = velocity;
            this.orientation = orientation;
            this.futureTime = futureTime;
        }

        public RecordedTrajectoryPoint(RecordedTrajectoryPoint point)
        {
            position = new float3(point.position.x, point.position.y, point.position.z);
            velocity = new float3(point.velocity.x, point.velocity.y, point.velocity.z);
            orientation = new float3(point.orientation.x, point.orientation.y, point.orientation.z);
            futureTime = point.futureTime;
        }

        [BurstCompile]
        public void Set(float3 position, float3 velocity, float3 orientation, float futureTime)
        {
            this.position = position;
            this.velocity = velocity;
            this.orientation = orientation;
            this.futureTime = futureTime;
        }

        [BurstCompile]
        public void Set(float3 position, float3 velocity, float3 orientation)
        {
            this.position = position;
            this.velocity = velocity;
            this.orientation = orientation;
        }

        [BurstCompile]
        public void Set(RecordedTrajectoryPoint point)
        {
            this.position = point.position;
            this.velocity = point.velocity;
            this.orientation = point.orientation;
            this.futureTime = point.futureTime;
        }

        [BurstCompile]
        public void SetPosition(float3 position)
        {
            this.position = position;
        }

        [BurstCompile]
        public void SetVelocity(float3 velocity)
        {
            this.velocity = velocity;
        }

        [BurstCompile]
        public void SetOrientation(float3 orientation)
        {
            this.orientation = orientation;
        }

        [BurstCompile]
        public void SetFutureTime(float newFutureTime)
        {
            this.futureTime = newFutureTime;
        }

        public void TransformToLocalSpace(Transform localSpace)
        {
            this.position = localSpace.InverseTransformPoint(this.position);
            this.velocity = localSpace.InverseTransformDirection(this.velocity);
            this.orientation = localSpace.InverseTransformDirection(this.orientation);
        }

        public void TransformToWorldSpace(Transform localSpace)
        {
            this.position = localSpace.TransformPoint(this.position);
            this.velocity = localSpace.TransformDirection(this.velocity);
            this.orientation = localSpace.TransformDirection(this.orientation);
        }

        #region Static methods


        [BurstCompile]
        public static RecordedTrajectoryPoint Lerp(RecordedTrajectoryPoint point1, RecordedTrajectoryPoint point2, float factor)
        {
            float3 dir = math.lerp(point1.orientation, point2.orientation, factor);
            if (dir.x != 0f && dir.y != 0f && dir.z != 0f)
            {
                dir = math.normalize(dir);
            }
            else
            {
                dir = point1.orientation;
            }
            return new RecordedTrajectoryPoint(
                math.lerp(point1.position, point2.position, factor),
                math.lerp(point1.velocity, point2.velocity, factor),
                dir,
                math.lerp(point1.futureTime, point2.futureTime, factor)
            );
        }

        #endregion

    }
}