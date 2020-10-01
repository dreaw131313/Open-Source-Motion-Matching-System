using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
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
        public float contactPointsWeight = 1f;
        [SerializeField]
        public ContactPointPositionCorrectionType postionCorrection = ContactPointPositionCorrectionType.MovePosition;
        [SerializeField]
        public int middleContactsCount = 1;
        [SerializeField]
        public bool rotateToStart = false;
        [SerializeField]
        public bool rotateToContacts = false;
        [SerializeField]
        public bool rotateOnContacts = false;

        public ContactStateFeatures()
        {
            contactMovementType = ContactStateMovemetType.ContactLand;
            contactCostType = ContactPointCostType.PositionNormal_OR_Direction;
            adapt = false;
            contactPointsWeight = 1f;
            rotateToStart = true;
            postionCorrection = ContactPointPositionCorrectionType.MovePosition;
        }

#if UNITY_EDITOR
        public void DrawEditorGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            if (Application.isPlaying)
            {
                EditorGUILayout.EnumPopup("Contact state type", contactStateType);
            }
            else
            {
                contactStateType = (ContactStateType)EditorGUILayout.EnumPopup("Contact state type", contactStateType);
            }

            GUILayout.EndHorizontal();

            switch (contactStateType)
            {
                case ContactStateType.NormalContacts:
                    DrawNormalContactStateFeatures();
                    break;
                case ContactStateType.Impacts:
                    break;
            }

        }

        public void DrawNormalContactStateFeatures()
        {
            //GUILayout.BeginHorizontal();
            //GUILayout.Space(10);
            //features.adapt = EditorGUILayout.Toggle(new GUIContent("Adapt movemet"), features.adapt);
            //GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            rotateToStart = EditorGUILayout.Toggle(new GUIContent("Rotate to start"), rotateToStart);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            rotateToContacts = EditorGUILayout.Toggle(new GUIContent("Rotate to contacts"), rotateToContacts);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            rotateOnContacts = EditorGUILayout.Toggle(new GUIContent("Rotate on contacts"), rotateOnContacts);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            postionCorrection = (ContactPointPositionCorrectionType)EditorGUILayout.EnumPopup(new GUIContent("Position correction"), postionCorrection);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            contactMovementType = (ContactStateMovemetType)EditorGUILayout.EnumPopup(new GUIContent("Contact type"), contactMovementType);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            middleContactsCount = EditorGUILayout.IntField(new GUIContent("Contacts count"), middleContactsCount);
            GUILayout.EndHorizontal();
        }


#endif
    }
}
