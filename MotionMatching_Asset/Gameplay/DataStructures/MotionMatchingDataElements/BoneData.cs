using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst;

namespace DW_Gameplay
{
    [System.Serializable]
    public struct BoneData
    {
        [SerializeField]
        public float3 localPosition;
        [SerializeField]
        public float3 velocity;

        public BoneData(float3 localPosition, float3 velocity)
        {
            this.localPosition = localPosition;
            this.velocity = velocity;
        }

        public static BoneData Lerp(BoneData bone1, BoneData bone2, float factor)
        {
            return new BoneData(
                math.lerp(bone1.localPosition, bone2.localPosition, factor),
                math.lerp(bone1.velocity, bone2.velocity, factor)
                );
        }

        public void Set(BoneData bone)
        {
            this.localPosition = bone.localPosition;
            this.velocity = bone.velocity;
        }

        public void Set(float3 pos, float3 vel)
        {
            this.localPosition = pos;
            this.velocity = vel;
        }

        public static float3 CalculateVelocity(float3 firstPos, float3 nextPos, float frameTime)
        {
            float3 vel = float3.zero;
            float3 deltaPosition = nextPos - firstPos;
            vel.x = deltaPosition.x / frameTime;
            vel.y = deltaPosition.y / frameTime;
            vel.z = deltaPosition.z / frameTime;
            return vel;
        }


        #region Cost calculation
        [BurstCompile]
        public float CalculateCost(BoneData toBone, PoseCostType type)
        {
            float cost = 0;
            //switch (type)
            //{
            //    case PoseCostType.Position:
            //        cost += CalculatePositionCost(toBone);
            //        break;
            //    case PoseCostType.Velocity:
            //        cost += CalculateVelocityCost(toBone);
            //        break;
            //    case PoseCostType.PositionVelocity:
                    cost += CalculatePositionCost(toBone);
                    cost += CalculateVelocityCost(toBone);
                    //break;
            //    case PoseCostType.None:
            //        break;
            //}
            return cost;
        }

        [BurstCompile]
        public float CalculatePositionCost(BoneData bone)
        {
            float cost = math.lengthsq(bone.localPosition - localPosition);
            return cost;
        }

        [BurstCompile]
        public float CalculateVelocityCost(BoneData bone)
        {
            float cost = math.lengthsq(bone.velocity - velocity);
            return cost;
        }
        #endregion
    }
}