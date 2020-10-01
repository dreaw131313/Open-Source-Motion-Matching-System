using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace DW_Gameplay
{
    [System.Serializable]
    public struct MotionMatchingContact : IComparable<MotionMatchingContact>
    {
        [SerializeField]
        public float3 position;
        [SerializeField]
        public float3 contactNormal;
        [SerializeField]
        public Quaternion rotation;
        [SerializeField]
        public Quaternion rotationFromForwardToNextContactDir;
        [SerializeField]
        public float startTime;
        [SerializeField]
        public float endTime;
        //[SerializeField]
        //public float impactRecoveryTime;

        public MotionMatchingContact(float animationTime)
        {
            this.position = Vector3.up;
            this.startTime = animationTime;
            endTime = animationTime;
            this.contactNormal = Vector3.down;
            rotation = Quaternion.identity;
            rotationFromForwardToNextContactDir = Quaternion.identity;
            //this.impactRecoveryTime = endTime;
        }
        
        public MotionMatchingContact(float3 position, float3 contactSurfaceNormal, float animationTime)
        {
            this.position = position;
            this.startTime = animationTime;
            endTime = animationTime;
            this.contactNormal = contactSurfaceNormal;
            rotation = Quaternion.identity;
            rotationFromForwardToNextContactDir = Quaternion.identity;
            //this.impactRecoveryTime = endTime;
        }

        public void SetStartTime(float time)
        {
            this.startTime = time;
        }

        public void SetEndTime(float time)
        {
            this.endTime = time;
        }

        public void SetPosition(float3 position)
        {
            this.position = position;
        }

        public void SetContactNormal(float3 surfaceNormal)
        {
            this.contactNormal = surfaceNormal;
        }

        //public void SetImpactRecoveryTime(float time)
        //{
        //    impactRecoveryTime = time;
        //}

        //public float GetImpactRecoveryTime()
        //{
        //    return impactRecoveryTime;
        //}

        public static MotionMatchingContact Lerp(MotionMatchingContact first, MotionMatchingContact second, float factor)
        {
            MotionMatchingContact cp = new MotionMatchingContact();
            cp.position = math.lerp(first.position, second.position, factor);
            cp.contactNormal = math.lerp(first.contactNormal, second.contactNormal, factor);
            cp.startTime = math.lerp(first.startTime, second.startTime, factor);
            return cp;
        }

        public float CalculateCost(MotionMatchingContact to, ContactPointCostType costType)
        {
            float cost = 0f;

            switch (costType)
            {
                case ContactPointCostType.Postion:
                    cost += CalculatePositionCost(to);
                    break;
                case ContactPointCostType.Normal_OR_Direction:
                    cost += CalculateNormalCost(to);
                    break;
                case ContactPointCostType.PositionNormal_OR_Direction:
                    cost += CalculatePositionCost(to);
                    cost += CalculateNormalCost(to);
                    break;
            }

            return cost;
        }

        private float CalculatePositionCost(MotionMatchingContact to)
        {
            return math.lengthsq(this.position - to.position);
        }

        private float CalculateNormalCost(MotionMatchingContact to)
        {
            return math.lengthsq(this.contactNormal - to.contactNormal);
        }

        public int CompareTo(MotionMatchingContact other)
        {
            if (other.startTime > this.startTime)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }

        public bool IsContactInTime(float time)
        {
            if(this.startTime<=time && time <= endTime)
            {
                return true;
            }
            return false;
        }
    }

    [System.Serializable]
    public struct FrameContact
    {
        [SerializeField]
        public float3 position;
        //[SerializeField]
        //public float3 forward;
        [SerializeField]
        public float3 normal;

        public FrameContact(float3 position, float3 contactSurfaceNormal)//, float3 forward)
        {
            this.position = position;
            this.normal = contactSurfaceNormal;
            //this.forward = forward;
        }

        public void SetPosition(float3 position)
        {
            this.position = position;
        }

        public void SetContactSurfaceReverseNormal(float3 surfaceNormal)
        {
            this.normal = surfaceNormal;
        }

        public void SetForward(float3 forward)
        {
            //this.forward = forward;
        }

        public static FrameContact Lerp(FrameContact first, FrameContact second, float factor)
        {
            FrameContact cp = new FrameContact();
            cp.position = math.lerp(first.position, second.position, factor);
            cp.normal = math.lerp(first.normal, second.normal, factor);
            //cp.forward = math.lerp(first.forward, second.forward, factor);
            return cp;
        }

        public float CalculateCost(FrameContact to, ContactPointCostType costType)
        {
            float cost = 0f;

            switch (costType)
            {
                case ContactPointCostType.Postion:
                    cost += CalculatePositionCost(to);
                    break;
                case ContactPointCostType.Normal_OR_Direction:
                    cost += CalculateReverseSurfaceNormalCost(to);
                    break;
                case ContactPointCostType.PositionNormal_OR_Direction:
                    cost += CalculatePositionCost(to);
                    cost += CalculateReverseSurfaceNormalCost(to);
                    break;
            }

            return cost;
        }

        private float CalculatePositionCost(FrameContact to)
        {
            return math.lengthsq(this.position - to.position);
        }

        private float CalculateReverseSurfaceNormalCost(FrameContact to)
        {
            return math.lengthsq(this.normal - to.normal);
        }
    }

    [System.Serializable]
    public struct SwitchStateContact
    {
        [SerializeField]
        public FrameContact frameContact;
        [SerializeField]
        public Vector3 forward;

        public SwitchStateContact(FrameContact contactPoint, Vector3 forward)
        {
            this.forward = forward;
            this.frameContact = contactPoint;
        }
    }
}
