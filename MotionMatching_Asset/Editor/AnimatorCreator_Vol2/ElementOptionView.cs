using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using DW_Gameplay;

namespace DW_Editor
{
    public class ElementOptionView
    {
        MM_AnimatorController animator;
        MotionMatchingLayer selectedLayer;
        MotionMatchingNode selectedNode;
        MotionMatchingState selectedState;
        DW_Gameplay.Transition selectedTransition;

        // Portal node stuff
        string portalFindingName = "";
        int selectedPortalStateIndex = -1;
        List<string> stateNames = new List<string>();

        ReorderableList transitionList;

        float descriptionWidth = 150f;

        // Transition stuff
        ReorderableList transitionOptionsList;
        ReorderableList boolConditions;
        ReorderableList intConditions;
        ReorderableList floatConditions;
        TransitionOptions selectedTransitionOption = null;

        public void SetNeededReferences(
            MM_AnimatorController animator,
            MotionMatchingLayer layer,
            MotionMatchingNode node,
            DW_Gameplay.Transition transition
            )
        {
            this.animator = animator;
            if (layer != null)
            {
                if (selectedLayer != layer)
                {
                    selectedNode = null;
                    selectedState = null;
                    selectedTransition = null;
                }
                selectedLayer = layer;
                selectedNode = node;
                if (selectedNode != null && selectedNode.stateIndex >= 0 && selectedNode.stateIndex < selectedLayer.states.Count)
                {
                    if (selectedState == null)
                    {
                        transitionList = new ReorderableList(
                            selectedLayer.states[selectedNode.stateIndex].transitions,
                            typeof(DW_Gameplay.Transition),
                            true,
                            true,
                            false,
                            false
                            );
                    }
                    else if (selectedState.GetIndex() != selectedNode.stateIndex)
                    {
                        transitionList = new ReorderableList(
                            selectedLayer.states[selectedNode.stateIndex].transitions,
                            typeof(DW_Gameplay.Transition),
                            true,
                            true,
                            false,
                            false
                            );
                    }

                    selectedState = selectedLayer.states[selectedNode.stateIndex];
                }
                else
                {
                    selectedState = null;
                }
                if (selectedTransition == null && transition != null)
                {
                    selectedTransition = transition;
                    transitionOptionsList = new ReorderableList(selectedTransition.options, typeof(TransitionOptions));
                }
                else if (transition != selectedTransition && transition != null)
                {
                    selectedTransition = transition;
                    transitionOptionsList = new ReorderableList(selectedTransition.options, typeof(TransitionOptions));
                }
                else if (transition == null)
                {
                    selectedTransition = null;
                }
            }
            else
            {
                selectedLayer = null;
                selectedNode = null;
                selectedState = null;
                selectedTransition = null;
            }
        }

        public void Draw()
        {
            if (selectedState != null && selectedNode.nodeType != MotionMatchingNodeType.Portal)
            {
                DrawStateOptions();
            }
            else if (selectedNode != null)
            {
                GUILayout.Space(5);
                GUILayout.Label("Portal node state selection", GUIResources.GetDarkHeaderStyle_MD());
                GUILayout.Space(5);
                DrawPortal();
            }
            else if (selectedTransition != null)
            {
                DrawTransitionOption();
            }
            else
            {
                GUILayout.Space(5);
                GUILayout.Label("Nothing is selected", GUIResources.GetDarkHeaderStyle_MD());
            }
        }

        #region State drawing
        private void DrawStateOptions()
        {
            DrawState();
        }

        private void DrawState()
        {
            DrawCommonFeatures();
            switch (selectedState.GetStateType())
            {
                case MotionMatchingStateType.MotionMatching:
                    DrawMotionMatchingFeataures();
                    break;
                case MotionMatchingStateType.SingleAnimation:
                    DrawSingleAnimationFeatures();
                    break;
                case MotionMatchingStateType.ContactAnimationState:
                    DrawContactStateFeatures();
                    break;
            }



            GUILayoutElements.DrawHeader(
                        "Transition List",
                        GUIResources.GetDarkHeaderStyle_MD()
                        );

            if (transitionList == null)
            {
                transitionList = new ReorderableList(selectedState.transitions, typeof(DW_Gameplay.Transition), true, true, false, false);
            }
            if (transitionList != null)
            {
                HandleTranistionList(transitionList, selectedState.transitions);
                transitionList.DoLayoutList();
            }
        }

        private void DrawCommonFeatures()
        {
            GUILayoutElements.DrawHeader(
                selectedState.GetStateType().ToString(),
                GUIResources.GetDarkHeaderStyle_MD()
                );

            #region Common options
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Name", GUILayout.Width(descriptionWidth));
            selectedState.SetStateName(EditorGUILayout.TextField(selectedState.GetName()));
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Speed multiplayer", GUILayout.Width(descriptionWidth));
            selectedState.speedMultiplier = EditorGUILayout.FloatField(selectedState.speedMultiplier);
            GUILayout.EndHorizontal();

            string currentName = selectedState.GetName();
            string newName;
            int counter = 0;
            if (currentName == "")
            {
                newName = "New state";
                currentName = newName;
            }
            else
            {
                newName = currentName;
            }

            for (int i = 0; i < selectedLayer.states.Count; i++)
            {
                if (selectedLayer.states[i].GetName() == newName && i != selectedState.GetIndex())
                {
                    counter++;
                    i = 0;
                    newName = currentName + counter.ToString();
                }
            }
            selectedState.SetStateName(newName);
            if (selectedNode.nodeType == MotionMatchingNodeType.State)
            {
                if (selectedLayer.startStateIndex == selectedState.GetIndex())
                {
                    GUILayoutElements.DrawHeader(
                        "Start state option",
                        GUIResources.GetDarkHeaderStyle_MD()
                        );
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10);

                    int groupIndex = 0;
                    for(int i = 0; i < selectedState.motionDataGroups.Count; i++)
                    {
                        if(selectedState.startMotionDataGroup == selectedState.motionDataGroups[i].name)
                        {
                            groupIndex = i;
                            break;
                        }
                    }

                    GUILayout.Label("Start clip index", GUILayout.Width(descriptionWidth));
                    selectedState.startClipIndex = EditorGUILayout.IntSlider(
                        selectedState.startClipIndex,
                        0,
                        selectedState.motionDataGroups[groupIndex].animationData.Count != 0 ? selectedState.motionDataGroups[groupIndex].animationData.Count - 1 : 0
                        );
                    GUILayout.EndHorizontal();

                    if (selectedState.motionDataGroups[groupIndex].animationData.Count > 0 &&
                        selectedState.motionDataGroups[groupIndex].animationData[selectedState.startClipIndex] != null)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        GUILayout.Label("Start clip time", GUILayout.Width(descriptionWidth));
                        selectedState.startClipTime = EditorGUILayout.Slider(
                            selectedState.startClipTime,
                            0f,
                            selectedState.motionDataGroups[0].animationData[selectedState.startClipIndex].animationLength
                            );
                        GUILayout.EndHorizontal();
                    }
                }
            }

            DrawTrajectoryOptions();


            GUILayoutElements.DrawHeader(
                        "Cost calculation",
                        GUIResources.GetDarkHeaderStyle_MD()
                        );
            DrawCostOptions();

            GUILayoutElements.DrawHeader(
                   "Animation data",
                   GUIResources.GetLightHeaderStyle_MD(),
                   GUIResources.GetDarkHeaderStyle_MD(),
                   ref selectedState.animDataFold
                   );

            if (selectedState.animDataFold)
            {
                for (int groupIndex = 0; groupIndex < selectedState.motionDataGroups.Count; groupIndex++)
                {
                    GUILayout.Space(5);
                    GUILayout.BeginHorizontal();

                    GUILayoutElements.DrawHeader(
                           selectedState.motionDataGroups[groupIndex].name,
                           GUIResources.GetLightHeaderStyle_SM(),
                           GUIResources.GetDarkHeaderStyle_SM(),
                           ref selectedState.motionDataGroups[groupIndex].fold
                           );
                    AddingAnimationDataToState(groupIndex);
                    if (groupIndex > 0)
                    {
                        if (GUILayout.Button("X", GUILayout.Width(30)))
                        {
                            selectedState.RemoveDataGroup(groupIndex);
                            continue;
                        }
                    }
                    GUILayout.EndHorizontal();
                    DrawAnimationDataList(selectedState.motionDataGroups[groupIndex].animationData, groupIndex);


                    GUILayout.Space(10);

                }

                if (selectedState.GetStateType() == MotionMatchingStateType.MotionMatching)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Add Motion Data Group"))
                    {
                        selectedState.AddDataGroup("MotionGroup");
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.Space(10);

            #endregion
        }

        private void DrawTrajectoryOptions()
        {
            GUILayoutElements.DrawHeader(
                        "Trajectory correction",
                        GUIResources.GetDarkHeaderStyle_MD()
                        );

            if (selectedState.GetStateType() != MotionMatchingStateType.ContactAnimationState)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                selectedState.trajectoryCorrection = EditorGUILayout.Toggle(new GUIContent("Trajectory correction"), selectedState.trajectoryCorrection);
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Space(5);
            }
            /*
            if (selectedState.GetStateType() != AnimatorStateType.ContactAnimationState)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.Label("Type", GUILayout.Width(descriptionWidth));
                selectedState.trajectoryCorrection = (TrajectoryCorrectionType)EditorGUILayout.EnumPopup(selectedState.trajectoryCorrection);
                GUILayout.EndHorizontal();


                switch (selectedState.trajectoryCorrection)
                {
                    case TrajectoryCorrectionType.CONSTANT:
                        GUILayout.Space(5);
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        selectedState.maxDegreesPerSecondSpeed = EditorGUILayout.FloatField(
                                        new GUIContent("Speed (degrees\\second)"),
                                        Mathf.Clamp(selectedState.maxDegreesPerSecondSpeed, 0f, float.MaxValue)
                                        );
                        GUILayout.EndHorizontal();

                        GUILayout.Space(5);
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        selectedState.minAngleToTrajectoryCorrection = EditorGUILayout.Slider(
                                        new GUIContent("Min correction angle"),
                                        selectedState.minAngleToTrajectoryCorrection,
                                        0f,
                                        180f
                                        );
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        selectedState.maxAngleToTrajectoryCorrection = EditorGUILayout.Slider(
                                        new GUIContent("Max correction angle"),
                                        selectedState.maxAngleToTrajectoryCorrection,
                                        0f,
                                        180f
                                        );
                        GUILayout.EndHorizontal();
                        break;
                    case TrajectoryCorrectionType.REACH_TARGET:
                        GUILayout.Space(5);
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        GUILayout.Label("Not implemented yet!");
                        GUILayout.EndHorizontal();

                        break;
                    case TrajectoryCorrectionType.PROGRESSIVE:
                        GUILayout.Space(5);
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        selectedState.minDegreesPerSecondSpeed = EditorGUILayout.FloatField(
                                        new GUIContent("Min angle speed "),
                                        Mathf.Clamp(selectedState.minDegreesPerSecondSpeed, 0f, float.MaxValue)
                                        );
                        GUILayout.EndHorizontal();

                        GUILayout.Space(5);
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        selectedState.maxDegreesPerSecondSpeed = EditorGUILayout.FloatField(
                                        new GUIContent("Max angle speed "),
                                        Mathf.Clamp(selectedState.maxDegreesPerSecondSpeed, 0f, float.MaxValue)
                                        );
                        GUILayout.EndHorizontal();

                        GUILayout.Space(5);
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        selectedState.minAngleToTrajectoryCorrection = EditorGUILayout.Slider(
                                        new GUIContent("Min correction angle"),
                                        selectedState.minAngleToTrajectoryCorrection,
                                        0f,
                                        180f
                                        );
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        selectedState.maxAngleToTrajectoryCorrection = EditorGUILayout.Slider(
                                        new GUIContent("Max correction angle"),
                                        selectedState.maxAngleToTrajectoryCorrection,
                                        0f,
                                        180f
                                        );
                        GUILayout.EndHorizontal();
                        break;
                    case TrajectoryCorrectionType.None:
                        break;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                selectedState.strafeForward = EditorGUILayout.Toggle(new GUIContent("Strafe"), selectedState.strafeForward);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                selectedState.strafeForwardMaxAngle = EditorGUILayout.Slider(
                                new GUIContent("Max strafe forward angle"),
                                selectedState.strafeForwardMaxAngle,
                                0f,
                                180f
                                );
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Space(5);
            }
            */
        }

        private void DrawCostOptions()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            //GUILayout.Label("In Motion Matching State trajectory cost type is set in component");//, GUILayout.Width(descriptionWidth));
            GUILayout.Label("Trajectory", GUILayout.Width(descriptionWidth));
            selectedState.trajectoryCostType = (TrajectoryCostType)EditorGUILayout.EnumPopup(selectedState.trajectoryCostType);
            GUILayout.EndHorizontal();

            if (selectedState.trajectoryCostType != TrajectoryCostType.None)
            {
                //GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.Label("Trajectory weight", GUILayout.Width(descriptionWidth));
                selectedState.trajectoryCostWeight = EditorGUILayout.Slider(
                                                                    selectedState.trajectoryCostWeight,
                                                                    0.001f,
                                                                    1f
                                                                    );
                GUILayout.EndHorizontal();
            }
            if (selectedState.GetStateType() == MotionMatchingStateType.ContactAnimationState)
            {
                //GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.Label("Contacts", GUILayout.Width(descriptionWidth));
                selectedState.csFeatures.contactCostType = (ContactPointCostType)EditorGUILayout.EnumPopup(selectedState.csFeatures.contactCostType);
                GUILayout.EndHorizontal();

                if (selectedState.csFeatures.contactCostType != ContactPointCostType.None)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    selectedState.csFeatures.contactPointsWeight = EditorGUILayout.Slider(
                        new GUIContent("Contacts factor cost"),
                        selectedState.csFeatures.contactPointsWeight,
                        0f,
                        1f
                        );
                    GUILayout.EndHorizontal();
                }
            }


            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.Label("Pose", GUILayout.Width(descriptionWidth));
            selectedState.poseCostType = (PoseCostType)EditorGUILayout.EnumPopup(selectedState.poseCostType);
            GUILayout.EndHorizontal();

            if (selectedState.poseCostType != PoseCostType.None)
            {
                //GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.Label("Pose weight", GUILayout.Width(descriptionWidth));
                selectedState.poseCostWeight = EditorGUILayout.Slider(
                                                                    selectedState.poseCostWeight,
                                                                    0.001f,
                                                                    1f
                                                                    );
                GUILayout.EndHorizontal();
            }
        }

        private void DrawAnimationDataList(List<MotionMatchingData> list, int groupIndex)
        {
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.Label("Motion Data group name", GUILayout.Width(150));
            selectedState.motionDataGroups[groupIndex].name = GUILayout.TextField(selectedState.motionDataGroups[groupIndex].name);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            if (selectedState.motionDataGroups[groupIndex].fold)
            {
                if (list.Count == 0)
                {
                    GUILayout.Label("Animation data list is empty");
                }
                for (int i = 0; i < list.Count; i++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    GUILayout.Label(string.Format("{0}.", i), GUILayout.Width(30));
                    EditorGUILayout.ObjectField(list[i], typeof(MotionMatchingData), false);
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        selectedLayer.RemoveAniamtionDataFromState(i, selectedState.GetIndex(), groupIndex);
                        i--;
                        continue;
                    }
                    GUILayout.Space(10);
                    GUILayout.EndHorizontal();
                    if (selectedState.GetStateType() == MotionMatchingStateType.ContactAnimationState ||
                        selectedState.GetStateType() == MotionMatchingStateType.SingleAnimation)
                    {
                        if (list[i] != null)
                        {
                            float x = selectedState.whereCanFindingNextPose[i].x;
                            float y = selectedState.whereCanFindingNextPose[i].y;
                            GUILayoutElements.MinMaxSlider(ref x, ref y, 0f, list[i].animationLength);
                            selectedState.whereCanFindingNextPose[i] = new float2(x, y);
                        }
                    }
                }
            }


            float checkedTime = 0f;
            int checkedPoseCount = 0;
            float totalTime = 0f;
            int totalPoseCount = 0;
            foreach (MotionMatchingData a in selectedState.motionDataGroups[groupIndex].animationData)
            {
                if (a != null)
                {
                    float checkedDataTime = a.animationLength - a.neverChecking.GetSectionTime();
                    checkedTime += checkedDataTime;
                    checkedPoseCount += Mathf.FloorToInt(checkedDataTime / a.frameTime);
                    totalTime += a.animationLength;
                    totalPoseCount += a.numberOfFrames;
                }
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            // Descriptions
            GUILayout.BeginVertical();
            GUILayout.Label("Number of clips:");
            GUILayout.Label("Frames:");
            GUILayout.Label("Animations time:");
            GUILayout.EndVertical();
            // Values
            GUILayout.BeginVertical();
            GUILayout.Label(selectedState.motionDataGroups[groupIndex].animationData.Count.ToString());
            GUILayout.Label(string.Format("{0} / {1}", checkedPoseCount, totalPoseCount));
            GUILayout.Label(string.Format(
                "{0} min {1} s / {2} min {3} s",
                Mathf.FloorToInt(checkedTime / 60),
                Math.Round(checkedTime % 60, 2),
                Mathf.FloorToInt(totalTime / 60),
                Math.Round(totalTime % 60, 2)
                ));
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();


            if (selectedState.GetStateType() == MotionMatchingStateType.ContactAnimationState)
            {
                if (GUILayout.Button("Adjust finding intervals"))
                {
                    for (int i = 0; i < selectedState.motionDataGroups[groupIndex].animationData.Count; i++)
                    {
                        if (selectedState.motionDataGroups[groupIndex].animationData[i] != null)
                        {
                            float2 findingInterval = new float2(0, selectedState.motionDataGroups[groupIndex].animationData[i].GetContactStartTime(0) * 0.5f);
                            selectedState.whereCanFindingNextPose[i] = findingInterval;
                        }
                    }
                }
                GUILayout.Space(5);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear"))
            {
                selectedLayer.ClearStateAnimationData(selectedState.GetIndex(), groupIndex);
            }

            if (GUILayout.Button("Remove nulls"))
            {
                for (int i = 0; i < selectedState.motionDataGroups[groupIndex].animationData.Count; i++)
                {
                    if (selectedState.motionDataGroups[groupIndex].animationData[i] == null)
                    {
                        selectedLayer.RemoveAniamtionDataFromState(i, selectedState.GetIndex(), groupIndex);
                        i--;
                    }
                }
            }
            GUILayout.EndHorizontal();


        }

        private void AddingAnimationDataToState(int groupIndex)
        {
            Rect dropRect = GUILayoutUtility.GetLastRect();
            //dropRect.y -= scroll.y;
            if (dropRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.DragPerform)
                {
                    bool correctData = true;
                    List<MotionMatchingData> newData = new List<MotionMatchingData>();
                    for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                    {
                        try
                        {
                            newData.Add((MotionMatchingData)DragAndDrop.objectReferences[i]);
                        }
                        catch (Exception)
                        {
                            correctData = false;
                            break;
                        }
                    }

                    if (correctData)
                    {
                        selectedLayer.AddAnimationDataToState(newData.ToArray(), selectedState.GetIndex(), groupIndex);
                    }
                    Event.current.Use();
                }
            }

        }

        private void DrawMotionMatchingFeataures()
        {
            MotionMatchingStateFeatures features = selectedState.mmFeatures;

            GUILayoutElements.DrawHeader(
                        "Motion Matching state features:",
                        GUIResources.GetDarkHeaderStyle_MD()
                        );

            GUILayout.BeginHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Default Motion Group", GUILayout.Width(descriptionWidth));

            int selectedGroupIndex = 0;

            string[] groupNames = new string[selectedState.motionDataGroups.Count];
            for (int i = 0; i < groupNames.Length; i++)
            {
                groupNames[i] = selectedState.motionDataGroups[i].name;

                if (selectedState.startMotionDataGroup == groupNames[i])
                {
                    selectedGroupIndex = i;
                }
            }
            selectedGroupIndex = EditorGUILayout.Popup(selectedGroupIndex, groupNames);
            selectedState.startMotionDataGroup = groupNames[selectedGroupIndex];



            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.Label("Update interval", GUILayout.Width(descriptionWidth));
            features.updateInterval = EditorGUILayout.FloatField(features.updateInterval);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.Label("Blend time", GUILayout.Width(descriptionWidth));
            features.blendTime = EditorGUILayout.FloatField(features.blendTime);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.Label("Max clip delta time", GUILayout.Width(descriptionWidth));
            features.maxClipDeltaTime = EditorGUILayout.FloatField(features.maxClipDeltaTime);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.Label("Min weight to achive", GUILayout.Width(descriptionWidth));
            features.minWeightToAchive = EditorGUILayout.Slider(features.minWeightToAchive, 0.0f, 1.0f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.Label("Max blended clips count", GUILayout.Width(descriptionWidth));
            features.maxBlendedClipCount = EditorGUILayout.IntSlider(features.maxBlendedClipCount, 2, 30);
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.Label("Sections dependecies", GUILayout.Width(descriptionWidth));
            selectedState.sectionsDependencies = (SectionsDependencies)EditorGUILayout.ObjectField(
                selectedState.sectionsDependencies,
                typeof(SectionsDependencies),
                true
                );
            GUILayout.EndHorizontal();
            if (selectedState.sectionsDependencies != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.Label("Start section", GUILayout.Width(descriptionWidth));
                selectedState.startSection = EditorGUILayout.IntSlider(
                    selectedState.startSection,
                    0,
                    selectedState.sectionsDependencies.SectionsCount - 1
                    );
                GUILayout.EndHorizontal();
            }

            if (selectedState.sectionsDependencies != null)
            {
                if (GUILayout.Button("Sections from dependences", GUIResources.Button_MD()))
                {
                    for (int groupIndex = 0; groupIndex < selectedState.motionDataGroups.Count; groupIndex++)
                    {
                        for (int i = 0; i < selectedState.motionDataGroups[groupIndex].animationData.Count; i++)
                        {
                            selectedState.sectionsDependencies.UpdateSectionDependecesInMMData(selectedState.motionDataGroups[groupIndex].animationData[i]);
                            EditorUtility.SetDirty(selectedState.motionDataGroups[groupIndex].animationData[i]);
                            selectedState.motionDataGroups[groupIndex].animationData[i].sectionFold = true;
                        }
                    }
                    AssetDatabase.SaveAssets();
                }
            }

            GUILayout.Space(10);
        }

        private void DrawSingleAnimationFeatures()
        {
            SingleAnimationStateFeatures features = selectedState.saFeatures;
            GUILayoutElements.DrawHeader(
                        "Single Animation state features:",
                        GUIResources.GetDarkHeaderStyle_MD()
                        );

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            //GUILayout.Label("Loop", GUILayout.Width(descriptionWidth));
            features.loop = EditorGUILayout.Toggle(new GUIContent("Loop"), features.loop);
            GUILayout.EndHorizontal();

            if (!features.loop)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                //GUILayout.Label("Loop count before stop", GUILayout.Width(descriptionWidth));
                features.loopCountBeforeStop = Mathf.Clamp(
                    EditorGUILayout.IntField(new GUIContent("Loops count before stop"), features.loopCountBeforeStop),
                    1,
                    int.MaxValue
                    );
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            //GUILayout.Label("Loop", GUILayout.Width(descriptionWidth));
            features.updateType = (SingleAnimationUpdateType)EditorGUILayout.EnumPopup(new GUIContent("Update type"), features.updateType);
            GUILayout.EndHorizontal();

            if (features.updateType != SingleAnimationUpdateType.PlaySelected)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                //GUILayout.Label("Loop", GUILayout.Width(descriptionWidth));
                features.blendTime = EditorGUILayout.FloatField(new GUIContent("Blend time"), features.blendTime);
                GUILayout.EndHorizontal();
            }
            switch (features.updateType)
            {
                case SingleAnimationUpdateType.PlaySelected:
                    break;
                case SingleAnimationUpdateType.PlayInSequence:
                    break;
                case SingleAnimationUpdateType.PlayRandom:
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    features.blendToTheSameAnimation = EditorGUILayout.Toggle(new GUIContent("Blend to current animation"), features.blendToTheSameAnimation);
                    GUILayout.EndHorizontal();
                    break;
            }
        }

        private void DrawContactStateFeatures()
        {
            GUILayoutElements.DrawHeader(
                        "Contact state features:",
                        GUIResources.GetDarkHeaderStyle_MD()
                        );

            ContactStateFeatures features = selectedState.csFeatures;
            selectedState.csFeatures.DrawEditorGUI();
            //GUILayout.BeginHorizontal();
            //GUILayout.Space(10);

            //if (Application.isPlaying)
            //{
            //    EditorGUILayout.EnumPopup("Contact state type", features.contactStateType);
            //}
            //else
            //{
            //    features.contactStateType = (ContactStateType)EditorGUILayout.EnumPopup("Contact state type", features.contactStateType);
            //    switch (features.contactStateType)
            //    {
            //        case ContactStateType.NormalContacts:
            //            selectedLayer.SetNodeTitle(selectedState.nodeID, "Contact state:");
            //            break;
            //        case ContactStateType.Impacts:
            //            selectedLayer.SetNodeTitle(selectedState.nodeID, "Impact state:");
            //            break;
            //    }
            //}
            //GUILayout.EndHorizontal();
            //switch (features.contactStateType)
            //{
            //    case ContactStateType.NormalContacts:
            //        DrawNormalContactStateFeatures(features);
            //        break;
            //    case ContactStateType.Impacts:
            //        DrawImpactStateFeatures(features);
            //        break;
            //}


        }

        private void DrawNormalContactStateFeatures(ContactStateFeatures features)
        {
            //GUILayout.BeginHorizontal();
            //GUILayout.Space(10);
            //features.adapt = EditorGUILayout.Toggle(new GUIContent("Adapt movemet"), features.adapt);
            //GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            features.rotateToStart = EditorGUILayout.Toggle(new GUIContent("Rotate to start"), features.rotateToStart);
            GUILayout.EndHorizontal();

            //GUILayout.BeginHorizontal();
            //GUILayout.Space(10);
            //features.gotoStartContactPoint = EditorGUILayout.Toggle(new GUIContent("Move to start contact"), features.gotoStartContactPoint);
            //GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            features.postionCorrection = (ContactPointPositionCorrectionType)EditorGUILayout.EnumPopup(new GUIContent("Position correction"), features.postionCorrection);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            features.contactMovementType = (ContactStateMovemetType)EditorGUILayout.EnumPopup(new GUIContent("Contact type"), features.contactMovementType);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            features.middleContactsCount = EditorGUILayout.IntField(new GUIContent("Contacts count"), features.middleContactsCount);
            GUILayout.EndHorizontal();
        }

        private void DrawImpactStateFeatures(ContactStateFeatures features)
        {

        }

        private void DrawPortal()
        {
            GUILayout.BeginHorizontal();
            portalFindingName = EditorGUILayout.TextField("State name", portalFindingName);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            if (stateNames == null)
            {
                stateNames = new List<string>();
            }
            stateNames.Clear();
            foreach (MotionMatchingState s in selectedLayer.states)
            {
                if (s.GetStateType() == MotionMatchingStateType.ContactAnimationState)
                {
                    continue;
                }
                bool goToNextState = false;
                foreach (DW_Gameplay.Transition t in s.transitions)
                {
                    if (t.nodeID == selectedNode.ID)
                    {
                        goToNextState = true;
                        break;
                    }
                }
                if (goToNextState) { continue; }

                if (portalFindingName != "")
                {
                    if (s.GetName().Contains(portalFindingName))
                    {
                        stateNames.Add(s.GetName());
                    }
                }
                else
                {
                    stateNames.Add(s.GetName());
                }
            }

            if (selectedNode.stateIndex >= 0 && selectedNode.stateIndex < selectedLayer.states.Count)
            {
                for (int i = 0; i < stateNames.Count; i++)
                {
                    if (stateNames[i] == selectedLayer.states[selectedNode.stateIndex].GetName())
                    {
                        selectedPortalStateIndex = i;
                        break;
                    }
                }
            }
            else
            {
                selectedPortalStateIndex = selectedNode.stateIndex;
            }

            selectedPortalStateIndex = GUILayout.SelectionGrid(
                selectedPortalStateIndex,
                stateNames.ToArray(),
                1
                );
            if (selectedPortalStateIndex >= 0 && selectedPortalStateIndex < stateNames.Count)
            {
                int newPortalStateIndex = selectedLayer.GetStateIndex(stateNames[selectedPortalStateIndex]);

                if (selectedNode.stateIndex != newPortalStateIndex)
                {
                    selectedLayer.SetPortalState2(selectedNode.ID, newPortalStateIndex);
                }
            }

        }

        private void HandleTranistionList(ReorderableList list, List<DW_Gameplay.Transition> tList)
        {
            list.drawHeaderCallback = (Rect rect) =>
            {
                GUI.Label(rect, "TransitionList");
            };

            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                string transitionString = selectedState.GetName() + "  ->  ";
                switch (tList[index].nextStateType)
                {
                    case MotionMatchingStateType.MotionMatching:
                        transitionString += selectedLayer.states[tList[index].nextStateIndex].GetName();
                        break;
                    case MotionMatchingStateType.SingleAnimation:
                        transitionString += selectedLayer.states[tList[index].nextStateIndex].GetName();
                        break;
                }
                Rect newRect = rect;
                newRect.height = 0.8f * rect.height;
                newRect.x += (0.05f * rect.width);
                newRect.y += (0.1f * rect.height);
                GUI.Label(newRect, transitionString);
            };
        }

        #endregion


        #region Transition Drawing
        private void DrawTransitionOption()
        {
            if (selectedTransition == null)
            {
                return;
            }
            #region Common transition options
            string transitionDest = selectedLayer.GetStateName(selectedTransition.fromStateIndex) + " -> " + selectedLayer.GetStateName(selectedTransition.nextStateIndex);

            GUILayoutElements.DrawHeader(
                        transitionDest,
                        GUIResources.GetDarkHeaderStyle_MD()
                        );
            GUILayout.Space(5);

            //if (transitionOptionsList == null)
            //{
            //    transitionOptionsList = new ReorderableList(selectedTransition.options, typeof(TransitionOptions));
            //}

            if (transitionOptionsList != null)
            {
                HandleTransitionOptionList(transitionOptionsList, selectedTransition.options);
                transitionOptionsList.DoLayoutList();
            }

            if (!(transitionOptionsList.index >= 0 && transitionOptionsList.index < selectedTransition.options.Count) && selectedTransition.options.Count > 0)
            {
                transitionOptionsList.index = 0;
            }

            #endregion

            if (transitionOptionsList.index >= 0 && transitionOptionsList.index < selectedTransition.options.Count)
            {
                TransitionOptions option = selectedTransition.options[transitionOptionsList.index];
                GUILayout.Space(5);
                GUILayoutElements.DrawHeader(
                        "Common options",
                        GUIResources.GetDarkHeaderStyle_MD()
                        );
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                option.blendTime = EditorGUILayout.FloatField("Blend time", option.blendTime);
                option.blendTime = Mathf.Clamp(option.blendTime, 0.00001f, float.MaxValue);
                GUILayout.EndHorizontal();

                // From state Option
                GUILayoutElements.DrawHeader(
                        "From state options",
                        GUIResources.GetDarkHeaderStyle_MD()
                        );
                GUILayout.Space(5);

                switch (selectedLayer.GetStateType(selectedTransition.fromStateIndex))
                {
                    case MotionMatchingStateType.MotionMatching:
                        DrawTransitionFromMMState(option);
                        break;
                    case MotionMatchingStateType.SingleAnimation:
                        DrawTransitionFromSAState(option);
                        break;
                    case MotionMatchingStateType.ContactAnimationState:
                        DrawTransitionFromSAState(option);
                        break;
                }
                // To state options
                GUILayoutElements.DrawHeader(
                        "To state options",
                        GUIResources.GetDarkHeaderStyle_MD()
                        );
                GUILayout.Space(5);

                switch (selectedLayer.GetStateType(selectedTransition.nextStateIndex))
                {
                    case MotionMatchingStateType.MotionMatching:
                        DrawTransitionToMMState(option);
                        break;
                    case MotionMatchingStateType.SingleAnimation:
                        DrawTransitionToSAState(option, selectedLayer.states[selectedTransition.nextStateIndex]);
                        break;
                }

                // Drawing Condition
                GUILayoutElements.DrawHeader(
                        "Option Conditions",
                        GUIResources.GetDarkHeaderStyle_MD()
                        );
                GUILayout.Space(5);

                DrawCondition(option);
            }
        }

        private void HandleTransitionOptionList(ReorderableList list, List<TransitionOptions> toList)
        {
            list.drawHeaderCallback = (Rect rect) =>
            {
                Rect newRect = rect;
                newRect.height = 0.8f * rect.height;
                newRect.x += (0.05f * rect.width);
                newRect.y += (0.1f * rect.height);
                GUI.Label(newRect, "Transition options");
            };
            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                Rect newRect = rect;
                newRect.height = 0.8f * rect.height;
                newRect.width = Mathf.Clamp(200, 0, rect.width * 0.9f);
                newRect.x += (0.05f * rect.width);
                newRect.y += (0.1f * rect.height);
                if (isActive)
                {
                    toList[index].SetName(EditorGUI.TextField(newRect, toList[index].GetName()));

                    string optionName = toList[index].GetName();
                    string newName = optionName;
                    int counter = 0;
                    for (int i = 0; i < toList.Count; i++)
                    {
                        if (toList[i].GetName() == newName && i != index)
                        {
                            counter++;
                            newName = optionName + counter.ToString();
                            i = 0;
                        }
                    }
                    toList[index].SetName(newName);
                }
                else
                {
                    GUI.Label(newRect, toList[index].GetName());
                }
            };

            list.onAddCallback = (ReorderableList rlist) =>
            {
                string optionName = "New option";
                string newName = optionName;
                int counter = 0;
                for (int i = 0; i < toList.Count; i++)
                {
                    if (toList[i].GetName() == newName)
                    {
                        counter++;
                        newName = optionName + counter.ToString();
                        i = 0;
                    }
                }

                toList.Add(new TransitionOptions(newName));

                MotionMatchingState fromState = selectedLayer.states[selectedTransition.fromStateIndex];
                MotionMatchingState toState = selectedLayer.states[selectedTransition.nextStateIndex];

                toList[toList.Count - 1].AddCheckingTransitionOption(fromState);
                toList[toList.Count - 1].AddFindigBestPoseOption(toState);
            };
        }

        private void DrawTransitionFromMMState(TransitionOptions option)
        {

        }

        private void DrawTransitionFromSAState(TransitionOptions option)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Check transition on max lenght");
            option.startOnExitTime = EditorGUILayout.Toggle(
                option.startOnExitTime
                );
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            for (int i = 0; i < option.whenCanCheckingTransition.Count; i++)
            {
                //GUILayoutElements.DrawHeader(
                //    selectedConection.outPoint.node.state.animationData[i].name, 
                //    GUIResources.GetLightHeaderStyle_SM()
                //    );

                int lastFrameIndex = selectedLayer.states[selectedTransition.fromStateIndex].motionDataGroups[0].animationData[i].frames.Count - 1;
                EditorGUILayout.ObjectField(selectedLayer.states[selectedTransition.fromStateIndex].motionDataGroups[0].animationData[i], typeof(AnimationClip), true);
                float x = option.whenCanCheckingTransition[i].x;
                float y = option.whenCanCheckingTransition[i].y;
                GUILayoutElements.MinMaxSlider(
                    ref x,
                    ref y,
                    0f,
                    selectedLayer.states[selectedTransition.fromStateIndex].motionDataGroups[0].animationData[i].frames[lastFrameIndex].localTime,
                    50
                    );
                option.whenCanCheckingTransition[i] = new float2(
                    math.clamp(x, 0f, y),
                    math.clamp(y, x, selectedLayer.states[selectedTransition.fromStateIndex].motionDataGroups[0].animationData[i].frames[lastFrameIndex].localTime)
                    );
            }
        }

        private void DrawTransitionToMMState(TransitionOptions option)
        {

        }

        private void DrawTransitionToSAState(TransitionOptions option, MotionMatchingState to)
        {
            for (int i = 0; i < option.whereCanFindingBestPose.Count; i++)
            {
                //GUILayoutElements.DrawHeader(
                //    selectedConection.outPoint.node.state.animationData[i].name, 
                //    GUIResources.GetLightHeaderStyle_SM()
                //    );
                int lastFrameIndex = selectedLayer.states[selectedTransition.nextStateIndex].motionDataGroups[0].animationData[i].frames.Count - 1;
                EditorGUILayout.ObjectField(selectedLayer.states[selectedTransition.nextStateIndex].motionDataGroups[0].animationData[i], typeof(AnimationClip), true);
                float x = option.whereCanFindingBestPose[i].x;
                float y = option.whereCanFindingBestPose[i].y;
                GUILayoutElements.MinMaxSlider(
                    ref x,
                    ref y,
                    0f,
                    selectedLayer.states[selectedTransition.nextStateIndex].motionDataGroups[0].animationData[i].frames[lastFrameIndex].localTime,
                    50
                    );
                option.whereCanFindingBestPose[i] = new float2(
                    math.clamp(x, 0f, y),
                    math.clamp(y, x, selectedLayer.states[selectedTransition.nextStateIndex].motionDataGroups[0].animationData[i].frames[lastFrameIndex].localTime)
                    );
            }
        }

        private void DrawCondition(TransitionOptions option)
        {
            if (selectedTransitionOption != option)
            {
                selectedTransitionOption = option;
                boolConditions = new ReorderableList(option.boolConditions, typeof(ConditionBool));
                intConditions = new ReorderableList(option.intConditions, typeof(ConditionInt));
                floatConditions = new ReorderableList(option.floatConditions, typeof(ConditionFloat));
            }

            HandleBoolConditionList(boolConditions, option);
            HandleIntConditionList(intConditions, option);
            HandleFloatConditionList(floatConditions, option);

            boolConditions.DoLayoutList();
            intConditions.DoLayoutList();
            floatConditions.DoLayoutList();
        }

        private void HandleBoolConditionList(ReorderableList list, TransitionOptions option)
        {
            list.drawHeaderCallback = (Rect rect) =>
            {
                Rect newRect = rect;
                newRect.height = 0.8f * rect.height;
                newRect.x += (0.05f * rect.width);
                newRect.y += (0.1f * rect.height);
                GUI.Label(newRect, "Bool conditions");
            };

            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                Rect r1 = new Rect(
                    rect.x + rect.width * 0.03f,
                    rect.y + 0.1f * rect.height,
                    rect.width * 0.45f,
                    rect.height * 0.8f
                    );
                Rect r2 = new Rect(
                    r1.x + r1.width + rect.width * 0.04f,
                    rect.y + 0.1f * rect.height,
                    rect.width * 0.45f,
                    rect.height * 0.8f
                    );

                if (animator.boolNames.Count > 0)
                {
                    int selectedBool = 0;
                    for (int i = 0; i < animator.boolNames.Count; i++)
                    {
                        if (animator.boolNames[i] == option.boolConditions[index].checkingValueName)
                        {
                            selectedBool = i;
                        }
                    }

                    option.boolConditions[index].checkingValueName = animator.boolNames[EditorGUI.Popup(r1, selectedBool, animator.boolNames.ToArray())];
                }
                else
                {
                    option.boolConditions[index].checkingValueName = "";
                    EditorGUI.DropdownButton(r1, new GUIContent(""), FocusType.Passive);
                }
                //condition.boolConditions[index].checkingValueName = GUI.TextField(r1, condition.boolConditions[index].checkingValueName);
                option.boolConditions[index].type = (BoolConditionType)EditorGUI.EnumPopup(r2, option.boolConditions[index].type);

            };

            list.onAddCallback = (ReorderableList rlist) =>
            {
                option.boolConditions.Add(new ConditionBool("", BoolConditionType.IS_TRUE));
            };
        }

        private void HandleIntConditionList(ReorderableList list, TransitionOptions option)
        {
            list.drawHeaderCallback = (Rect rect) =>
            {
                Rect newRect = rect;
                newRect.height = 0.8f * rect.height;
                newRect.x += (0.05f * rect.width);
                newRect.y += (0.1f * rect.height);
                GUI.Label(newRect, "Int conditions");
            };

            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                Rect r1 = new Rect(
                    rect.x + rect.width * 0.04f,
                    rect.y + 0.1f * rect.height,
                    rect.width * 0.28f,
                    rect.height * 0.8f
                    );
                Rect r2 = new Rect(
                    r1.x + r1.width + rect.width * 0.04f,
                    rect.y + 0.1f * rect.height,
                    rect.width * 0.28f,
                    rect.height * 0.8f
                    );
                Rect r3 = new Rect(
                    r2.x + r2.width + rect.width * 0.04f,
                    rect.y + 0.1f * rect.height,
                    rect.width * 0.28f,
                    rect.height * 0.8f
                    );


                if (animator.intNames.Count > 0)
                {
                    int selectedInt = 0;
                    for (int i = 0; i < animator.intNames.Count; i++)
                    {
                        if (animator.intNames[i] == option.intConditions[index].checkingValueName)
                        {
                            selectedInt = i;
                        }
                    }
                    option.intConditions[index].checkingValueName = animator.intNames[EditorGUI.Popup(r1, selectedInt, animator.intNames.ToArray())];
                }
                else
                {
                    option.intConditions[index].checkingValueName = "";
                    EditorGUI.DropdownButton(r1, new GUIContent(""), FocusType.Passive);
                }
                option.intConditions[index].type = (ConditionType)EditorGUI.EnumPopup(r2, option.intConditions[index].type);
                option.intConditions[index].conditionValue = EditorGUI.IntField(r3, option.intConditions[index].conditionValue);
            };

            list.onAddCallback = (ReorderableList rlist) =>
            {
                option.intConditions.Add(new ConditionInt("", ConditionType.EQUAL));
            };
        }

        private void HandleFloatConditionList(ReorderableList list, TransitionOptions option)
        {
            list.drawHeaderCallback = (Rect rect) =>
            {
                Rect newRect = rect;
                newRect.height = 0.8f * rect.height;
                newRect.x += (0.05f * rect.width);
                newRect.y += (0.1f * rect.height);
                GUI.Label(newRect, "Float conditions");
            };

            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                Rect r1 = new Rect(
                    rect.x + rect.width * 0.04f,
                    rect.y + 0.1f * rect.height,
                    rect.width * 0.28f,
                    rect.height * 0.8f
                    );
                Rect r2 = new Rect(
                    r1.x + r1.width + rect.width * 0.04f,
                    rect.y + 0.1f * rect.height,
                    rect.width * 0.28f,
                    rect.height * 0.8f
                    );
                Rect r3 = new Rect(
                    r2.x + r2.width + rect.width * 0.04f,
                    rect.y + 0.1f * rect.height,
                    rect.width * 0.28f,
                    rect.height * 0.8f
                    );


                if (animator.floatNames.Count > 0)
                {
                    int selectedfloat = 0;
                    for (int i = 0; i < animator.floatNames.Count; i++)
                    {
                        if (animator.floatNames[i] == option.floatConditions[index].checkingValueName)
                        {
                            selectedfloat = i;
                        }
                    }
                    option.floatConditions[index].checkingValueName = animator.floatNames[EditorGUI.Popup(r1, selectedfloat, animator.floatNames.ToArray())];
                }
                else
                {
                    option.floatConditions[index].checkingValueName = "";
                    EditorGUI.DropdownButton(r1, new GUIContent(""), FocusType.Passive);
                }

                option.floatConditions[index].type = (ConditionType)EditorGUI.EnumPopup(r2, option.floatConditions[index].type);
                option.floatConditions[index].conditionValue = EditorGUI.FloatField(r3, option.floatConditions[index].conditionValue);
            };

            list.onAddCallback = (ReorderableList rlist) =>
            {
                option.floatConditions.Add(new ConditionFloat("", ConditionType.EQUAL));
            };
        }
        #endregion

    }
}
