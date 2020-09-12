using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace DW_Gameplay
{
    [System.Serializable]
    public class MotionMatchingStateFeatures
    {
        [SerializeField]
        public float updateInterval;
        [SerializeField]
        public float blendTime;
        [SerializeField]
        [Range(0.01f, 1f)]
        public float maxClipDeltaTime;
        [SerializeField]
        public float timer = 0f;
        [SerializeField]
        public int maxBlendedClipCount = 20;
        [SerializeField]
        [Range(0.0f, 1.0f)]
        public float minWeightToAchive = 0.5f; // waga która musi zostać osiągnięta zanim jej waga znowu zacznie zmierzać do zera. Nie jest to ta sama wartość którą posiada animacja w PlayableGraph, jest ona osiągana podczas obliczania wag wszystkich animacji przez w danym MMLogicState, które są podawane dla Playable graph w postaci znormalizowanej (Same pozostają w postaci nie znormalizowanej).
        public MotionMatchingStateFeatures()
        {
            updateInterval = 0.05f;
            blendTime = 0.2f;
            maxClipDeltaTime = 0.3f;
        }
    }

    [System.Serializable]
    public class SingleAnimationStateFeatures
    {
        [SerializeField]
        public bool loop;
        [SerializeField]
        public int loopCountBeforeStop = 1;
        [SerializeField]
        public SingleAnimationUpdateType updateType;
        [SerializeField]
        public float blendTime;
        [SerializeField]
        public bool blendToTheSameAnimation;

        public SingleAnimationStateFeatures()
        {
            loop = true;
            updateType = SingleAnimationUpdateType.PlaySelected;
            blendTime = 0.35f;
            blendToTheSameAnimation = false;
        }
    }

    [System.Serializable]
    public class ContactStateFeatures
    {
        [SerializeField]
        public ContactStateMovemetType contactMovementType;
        [SerializeField]
        public ContactPointCostType contactCostType;
        [SerializeField]
        public ContactStateType contactStateType = ContactStateType.NormalContacts;
        [SerializeField]
        public bool adapt = false;
        [SerializeField]
        public bool gotoStartContactPoint = false;
        [SerializeField]
        public float contactPointsWeight = 1f;
        [SerializeField]
        public bool rotateToStart = true;
        [SerializeField]
        public ContactPointPositionCorrectionType postionCorrection = ContactPointPositionCorrectionType.MovePosition;
        [SerializeField]
        public int middleContactsCount = 1;

        public ContactStateFeatures()
        {
            contactMovementType = ContactStateMovemetType.ContactLand;
            contactCostType = ContactPointCostType.PositionNormal_OR_Direction;
            adapt = false;
            gotoStartContactPoint = false;
            contactPointsWeight = 1f;
            rotateToStart = true;
            postionCorrection = ContactPointPositionCorrectionType.MovePosition;
        }
    }
}
