using DW_Gameplay;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditorInternal;
using UnityEngine;

namespace DW_Editor
{
    public class MotionMatchingDataEditor : EditorWindow
    {
        Rect leftSpace;
        Rect rightSpace;

        Rect leftSpaceLayout;
        Rect rightSpaceLayout;

        bool resizing = false;

        float resizeFactor;

        float margin = 10f;

        PreparingDataPlayableGraph playableGraph;

        bool isDataSwitched = false;
        MotionMatchingData editedData;
        MotionMatchingData dataToCopyOptions;
        GameObject gameObject;

        bool _bDrawTrajectory = true;
        bool _bDrawPose = true;




        [MenuItem("MM Data Editor", menuItem = "MotionMatching/MM Data Editor")]
        private static void ShowWindow()
        {
            MotionMatchingDataEditor editor = EditorWindow.GetWindow<MotionMatchingDataEditor>();
            editor.titleContent = new GUIContent("MM Data Editor");
            editor.position = new Rect(100, 100, 1000, 300);

        }

        [OnOpenAssetAttribute(3)]
        public static bool step1(int instanceID, int line)
        {
            MotionMatchingData asset;
            try
            {
                asset = (MotionMatchingData)EditorUtility.InstanceIDToObject(instanceID);
            }
            catch (System.Exception e)
            {
                return false;
            }

            if (EditorWindow.HasOpenInstances<MotionMatchingDataEditor>())
            {
                EditorWindow.GetWindow<MotionMatchingDataEditor>().SetAsset(asset);
                EditorWindow.GetWindow<MotionMatchingDataEditor>().Repaint();
                return true;
            }

            MotionMatchingDataEditor.ShowWindow();
            EditorWindow.GetWindow<MotionMatchingDataEditor>().SetAsset(asset);
            EditorWindow.GetWindow<MotionMatchingDataEditor>().Repaint();

            return true;
        }

        private void SetAsset(MotionMatchingData asset)
        {
            this.editedData = asset;
        }
        private void OnEnable()
        {
            InitRect();

            playableGraph = new PreparingDataPlayableGraph();

            SceneView.duringSceneGui += OnSceneGUI;

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDestroy()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            playableGraph.Destroy();

        }

        private void OnGUI()
        {
            Event e = Event.current;

            GUI.DrawTexture(leftSpace, GUIResources.GetMediumTexture_1());
            GUI.DrawTexture(rightSpace, GUIResources.GetMediumTexture_2());

            FitRects();
            ResizeRects(e);

            resizeFactor = leftSpace.width / this.position.width;

            DoLayoutLeftMenu(e);
            DoLayoutRightMenu(e);

            AnimationPlaying();
            OnCurrentAnimationTimeChange();


            if (editedData != null)
            {
                EditorUtility.SetDirty(editedData);
                Undo.RecordObject(editedData, "MM_Data editor Change");
            }
        }

        private void Update()
        {
            if (playableGraph == null)
            {
                playableGraph = new PreparingDataPlayableGraph();
            }


            if (gameObject != null || editedData != null)
            {
                Undo.RecordObject(this, "Some Random text");
                EditorUtility.SetDirty(this);
            }
        }

        private void OnSceneGUI(SceneView obj)
        {
            DrawSceneGUI(obj);
        }

        private static void OnUndoRedoPerformed()
        {
            MotionMatchingDataEditor editor = EditorWindow.GetWindow<MotionMatchingDataEditor>();
            if (editor == null)
            {
                Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            }
            else
            {
                editor.Repaint();
            }
        }

        private void InitRect()
        {
            resizeFactor = 0.3f;
            resizing = false;
            leftSpaceLayout = new Rect();
            rightSpaceLayout = new Rect();

            leftSpace = new Rect(0, 0, this.position.width * resizeFactor, this.position.height);
            rightSpace = new Rect(leftSpace.x + leftSpace.width, 0, this.position.width * (1f - resizeFactor), this.position.height);
        }

        private void FitRects()
        {
            leftSpace.height = this.position.height;
            rightSpace.height = this.position.height;

            rightSpace.x = leftSpace.x + leftSpace.width;

            leftSpace.width = resizeFactor * this.position.width;
            rightSpace.width = this.position.width - leftSpace.width;
        }

        private void ResizeRects(Event e)
        {
            GUILayoutElements.ResizingRectsHorizontal(
                this,
                ref leftSpace,
                ref rightSpace,
                e,
                ref resizing,
                7,
                7
                );
        }

        #region LEFT MENU

        // Properties
        string[] optionsNames = { "Sections", "Contacts", "Event Markers" };
        int selectedOption = 0;

        Vector2 leftMenuScroll = Vector2.zero;

        // SECTIONS
        private enum SectionSelectedType
        {
            NotLookingForNewPoseSection,
            NeverLookingForNewPoseSection,
            NormalSection,
            None
        }

        SectionSelectedType selectedSectionType = SectionSelectedType.None;
        int selectedSectionIndex = -1;
        float betweenSectionsSpace = 5f;

        // CONTACTS
        float contactOptionsMargin = 30f;

        bool drawContactsPositions = true;
        bool drawContactsRSN = false;

        bool drawPositionManipulator;
        bool drawRotationManipuator;

        // Functions
        private void DoLayoutLeftMenu(Event e)
        {
            leftSpaceLayout.Set(
                leftSpace.x + margin,
                leftSpace.y + margin,
                leftSpace.width - 2 * margin,
                leftSpace.height - margin
                );
            GUILayout.BeginArea(leftSpaceLayout);
            DrawLeftMenu(e);
            GUILayout.EndArea();
        }

        private void DrawLeftMenu(Event e)
        {
            DrawNeededAssets();
            GUILayout.Space(5);
            DrawCommonOptions();
            GUILayout.Space(5);
            DrawPosibleOptions();
            GUILayout.Space(10);
            leftMenuScroll = EditorGUILayout.BeginScrollView(leftMenuScroll);
            DrawSelectedOptionLeftMenu();
            EditorGUILayout.EndScrollView();
        }

        private void DrawNeededAssets()
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label("MM Data");
            GUILayout.Label("Game object");
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            MotionMatchingData bufforData = (MotionMatchingData)EditorGUILayout.ObjectField(editedData, typeof(MotionMatchingData), true);
            if (bufforData != editedData)
            {
                isDataSwitched = true;
                OnAnimationDataSwitched();
                editedData = bufforData;
                this.Repaint();
            }
            else
            {
                isDataSwitched = false;
            }


            gameObject = (GameObject)EditorGUILayout.ObjectField(gameObject, typeof(GameObject), true);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void OnAnimationDataSwitched()
        {
            animationState = AnimationState.NotCreated;
            if (editedData != null)
            {
                contactPointsRL = new ReorderableList(editedData.contactPoints, typeof(MotionMatchingContact), true, false, true, true);
            }
        }

        private void DrawCommonOptions()
        {
            GUILayout.BeginHorizontal();
            {
                _bDrawPose = GUILayout.Toggle(_bDrawPose, "Draw Pose", new GUIStyle("Button"));
                _bDrawTrajectory = GUILayout.Toggle(_bDrawTrajectory, "Draw Trajectory", new GUIStyle("Button"));
            }
            GUILayout.EndHorizontal();
        }

        private void DrawPosibleOptions()
        {
            selectedOption = GUILayout.Toolbar(selectedOption, optionsNames);
        }

        private void DrawSelectedOptionLeftMenu()
        {
            if (editedData == null)
            {
                return;
            }
            switch (selectedOption)
            {
                case 0:
                    SectionOptionsLeftMenu();
                    break;
                case 1:
                    ContactsOptionsLeftMenu();
                    break;
            }
        }

        private void SectionOptionsLeftMenu()
        {
            bool result;
            GUILayout.BeginVertical();

            // Selecting Not Looking for new pose section
            result = selectedSectionType == SectionSelectedType.NotLookingForNewPoseSection;
            if (GUILayoutElements.DrawHeader(
                    "NotLookingForNewPose",
                    GUIResources.GetLightHeaderStyle_MD(),
                    GUIResources.GetDarkHeaderStyle_MD(),
                    result
                    ))
            {
                selectedSectionType = SectionSelectedType.NotLookingForNewPoseSection;
                selectedSectionIndex = -1;
            }
            GUILayout.Space(betweenSectionsSpace);
            // Selecting Never Looking for new pose section
            result = selectedSectionType == SectionSelectedType.NeverLookingForNewPoseSection;
            if (GUILayoutElements.DrawHeader(
                    "NeverChecking",
                    GUIResources.GetLightHeaderStyle_MD(),
                    GUIResources.GetDarkHeaderStyle_MD(),
                    result
                    ))
            {
                selectedSectionType = SectionSelectedType.NeverLookingForNewPoseSection;
                selectedSectionIndex = -1;

            }
            GUILayout.Space(betweenSectionsSpace);
            // Selecting other sections
            result = selectedSectionType == SectionSelectedType.NormalSection;

            for (int i = 0; i < editedData.sections.Count; i++)
            {
                if (GUILayoutElements.DrawHeader(
                    editedData.sections[i].sectionName,
                    GUIResources.GetLightHeaderStyle_MD(),
                    GUIResources.GetDarkHeaderStyle_MD(),
                    result && i == selectedSectionIndex
                    ))
                {
                    selectedSectionType = SectionSelectedType.NormalSection;
                    selectedSectionIndex = i;

                }
                GUILayout.Space(betweenSectionsSpace);
            }
            GUILayout.EndVertical();
        }

        private void ContactsOptionsLeftMenu()
        {
            editedData.contactsType = (ContactStateType)EditorGUILayout.EnumPopup("Contacts types: ", editedData.contactsType);
            GUILayout.Space(5);
            ContactPointsDrawingOptionsInSceneView();
            ContactsButtonOptions();
            GUILayout.Space(10);
        }

        private void ContactPointsDrawingOptionsInSceneView()
        {
            GUILayout.Label("Calculated Contacts Draw options:");

            GUILayout.BeginHorizontal();
            drawContactsPositions = GUILayout.Toggle(drawContactsPositions, "Positions", new GUIStyle("Button"));
            drawContactsRSN = GUILayout.Toggle(drawContactsRSN, "Normals", new GUIStyle("Button"));
            GUILayout.EndHorizontal();


        }

        private void ContactsButtonOptions()
        {
            GUILayout.Space(5);

            switch (editedData.contactsType)
            {
                case ContactStateType.NormalContacts:
                    if (GUILayout.Button("Sort contacts", GUIResources.Button_MD()) && editedData != null)
                    {
                        editedData.contactPoints.Sort();
                    }

                    if (GUILayout.Button("Calculate Contacts", GUIResources.Button_MD()) && editedData != null && gameObject != null)
                    {
                        if (gameObject == null)
                        {
                            Debug.LogWarning("Game object in MM Data Editor is NULL!");
                            return;
                        }
                        else
                        {
                            editedData.contactPoints.Sort();

                            MotionDataCalculator.CalculateContactPoints(
                                editedData,
                                editedData.contactPoints.ToArray(),
                                this.playableGraph,
                                this.gameObject
                                );

                            playableGraph.Initialize(gameObject);
                            playableGraph.CreateAnimationDataPlayables(editedData, currentAnimaionTime);
                        }
                    }
                    break;
                case ContactStateType.Impacts:
                    if (GUILayout.Button("Sort impacts", GUIResources.Button_MD()) && editedData != null)
                    {
                        editedData.contactPoints.Sort();
                    }

                    if (GUILayout.Button("Calculate Impacts", GUIResources.Button_MD()) && editedData != null && gameObject != null)
                    {
                        if (gameObject == null)
                        {
                            Debug.LogWarning("Game object in MM Data Editor is NULL!");
                            return;
                        }
                        else
                        {
                            editedData.contactPoints.Sort();

                            MotionDataCalculator.CalculateImpactPoints(
                                editedData,
                                editedData.contactPoints.ToArray(),
                                this.playableGraph,
                                this.gameObject
                                );

                            playableGraph.Initialize(gameObject);
                            playableGraph.CreateAnimationDataPlayables(editedData, currentAnimaionTime);
                        }
                    }
                    break;
            }

        }

        #endregion

        #region RIGHT MENU

        Vector2 rightScroll = Vector2.zero;

        string playBTNText = "▶";
        string pauseBTNText = "||";
        float previewBTNWidth = 55f;
        float animPlayingBTNWidth = 25;
        float animSliderRightMargin = 10f;

        float currentAnimaionTime = 0f;
        float bufforAnimationTime = 0f;

        private enum AnimationState
        {
            NotCreated,
            Stoped,
            Playing
            //Paused
        }

        AnimationState animationState = AnimationState.NotCreated;

        MM_DataSection selectedSection;
        ReorderableList sectionIntervalsRL;
        ReorderableList contactPointsRL;

        private void DoLayoutRightMenu(Event e)
        {
            rightSpaceLayout.Set(
                rightSpace.x + margin,
                rightSpace.y + margin,
                rightSpace.width - margin,
                rightSpace.height - margin
                );

            GUILayout.BeginArea(rightSpaceLayout);
            DrawRightMenu(e);
            GUILayout.EndArea();
        }

        private void DrawRightMenu(Event e)
        {
            DrawPlayAnimationOptions();
            DrawAboveScrollOptions();

            rightScroll = GUILayout.BeginScrollView(rightScroll);
            DrawRightMenuOptions(e);
            GUILayout.EndScrollView();
        }

        private void DrawPlayAnimationOptions()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview", GUILayout.Width(previewBTNWidth)) && editedData != null && gameObject != null)
            {
                gameObject.transform.position = Vector3.zero;
                gameObject.transform.rotation = Quaternion.identity;
                OnPreviewBTN();
            }
            if (animationState != AnimationState.Playing)
            {
                if (GUILayout.Button(playBTNText, GUILayout.Width(animPlayingBTNWidth)) && editedData != null && gameObject != null)
                {
                    OnPlayBTN();
                }
            }
            else
            {
                if (GUILayout.Button(pauseBTNText, GUILayout.Width(animPlayingBTNWidth)) && editedData != null && gameObject != null)
                {
                    OnPauseBTN();
                }
            }

            GUILayout.Space(5);

            currentAnimaionTime = EditorGUILayout.Slider(
                currentAnimaionTime,
                0f,
                editedData != null ? editedData.animationLength : 0f
                );
            GUILayout.Space(animSliderRightMargin);
            GUILayout.EndHorizontal();
        }


        float PlayingTime = 0;
        float bufforPlayingTime = 0;
        bool previewBTNClick = false;

        private void OnPreviewBTN()
        {
            previewBTNClick = true;
            animationState = AnimationState.Stoped;

            float deltaTime = -currentAnimaionTime;
            if (playableGraph != null && playableGraph.IsValid())
            {
                playableGraph.ClearMainMixerInput();
                playableGraph.Destroy();
            }
            playableGraph = new PreparingDataPlayableGraph();
            playableGraph.Initialize(gameObject);
            playableGraph.CreateAnimationDataPlayables(editedData);
            currentAnimaionTime = 0f;
        }

        private void OnPlayBTN()
        {
            animationState = AnimationState.Playing;

            bufforPlayingTime = Time.realtimeSinceStartup;
        }

        private void OnPauseBTN()
        {
            animationState = AnimationState.Stoped;
        }

        private void DrawAboveScrollOptions()
        {
            if (editedData == null)
            {
                return;
            }
            GUILayout.Space(5);
            switch (selectedOption)
            {
                case 0:
                    DrawRightSectionsOptionsAboveScroll();
                    break;
                case 1:
                    DrawRightContactsOptionsAboveScrollOptions();
                    break;
            }
            GUILayout.Space(10);
        }

        private void DrawRightSectionsOptionsAboveScroll()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Sort Intervals", GUIResources.Button_MD()))
            {
                selectedSection.timeIntervals.Sort(delegate (float2 x, float2 y)
                {
                    if (x.x < y.x)
                    {
                        return -1;
                    }
                    return 1;
                });
            }
            if (GUILayout.Button("Set Interval Start", GUIResources.Button_MD()))
            {
                if (sectionIntervalsRL != null && selectedSection != null)
                {
                    int selectedIntervalIndex = sectionIntervalsRL.index;
                    if (0 <= selectedIntervalIndex && selectedIntervalIndex < selectedSection.timeIntervals.Count)
                    {
                        float2 newTimeInterval = new float2(
                            currentAnimaionTime,
                            selectedSection.timeIntervals[selectedIntervalIndex].y
                            );

                        selectedSection.timeIntervals[selectedIntervalIndex] = newTimeInterval;
                    }
                }
            }
            if (GUILayout.Button("Set Interval End", GUIResources.Button_MD()))
            {
                if (sectionIntervalsRL != null && selectedSection != null)
                {
                    int selectedIntervalIndex = sectionIntervalsRL.index;
                    if (0 <= selectedIntervalIndex && selectedIntervalIndex < selectedSection.timeIntervals.Count)
                    {
                        float2 newTimeInterval = new float2(
                            selectedSection.timeIntervals[selectedIntervalIndex].x,
                            currentAnimaionTime
                            );

                        selectedSection.timeIntervals[selectedIntervalIndex] = newTimeInterval;
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawRightContactsOptionsAboveScrollOptions()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Set Contact Start Time", GUIResources.Button_MD()) && editedData != null)
            {
                if (contactPointsRL != null)
                {
                    if (0 <= contactPointsRL.index && contactPointsRL.index < editedData.contactPoints.Count)
                    {
                        MotionMatchingContact cp = editedData.contactPoints[contactPointsRL.index];
                        cp.SetStartTime(currentAnimaionTime);
                        editedData.contactPoints[contactPointsRL.index] = cp;
                    }
                }
            }
            if (GUILayout.Button("Set Contact End Time", GUIResources.Button_MD()) && editedData != null)
            {
                if (contactPointsRL != null)
                {
                    if (0 <= contactPointsRL.index && contactPointsRL.index < editedData.contactPoints.Count)
                    {
                        MotionMatchingContact cp = editedData.contactPoints[contactPointsRL.index];
                        cp.SetEndTime(currentAnimaionTime);
                        editedData.contactPoints[contactPointsRL.index] = cp;
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawRightMenuOptions(Event e)
        {
            switch (selectedOption)
            {
                case 0:
                    if (editedData != null)
                    {
                        DrawSelectedSectionOptions();
                    }
                    break;
                case 1:
                    if (editedData != null)
                    {
                        DrawContactsOptionsOnRightMenu();
                    }
                    break;
                case 2:
                    if (editedData != null)
                    {
                        DrawEventMarkersRightMenu();
                    }
                    break;
            }
        }

        private void DrawSelectedSectionOptions()
        {
            switch (selectedSectionType)
            {
                case SectionSelectedType.NotLookingForNewPoseSection:
                    DrawSelectedSection(editedData.notLookingForNewPose);
                    break;
                case SectionSelectedType.NeverLookingForNewPoseSection:
                    DrawSelectedSection(editedData.neverChecking);
                    break;
                case SectionSelectedType.NormalSection:
                    DrawSelectedSection(editedData.sections[selectedSectionIndex]);
                    break;
            }
        }

        private void DrawSelectedSection(MM_DataSection section)
        {
            if (selectedSection != section)
            {
                selectedSection = section;
                sectionIntervalsRL = new ReorderableList(selectedSection.timeIntervals, typeof(float2), true, false, true, true);
            }

            HandleSectionIntervals(sectionIntervalsRL, editedData);
            sectionIntervalsRL.DoLayoutList();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy Section Settings", GUIResources.Button_MD()))
            {
                if (dataToCopyOptions != null)
                {
                    switch (selectedSectionType)
                    {
                        case SectionSelectedType.NotLookingForNewPoseSection:
                            editedData.notLookingForNewPose.timeIntervals.Clear();
                            for (int i = 0; i < dataToCopyOptions.notLookingForNewPose.timeIntervals.Count; i++)
                            {
                                editedData.notLookingForNewPose.timeIntervals.Add(new float2(
                                    dataToCopyOptions.notLookingForNewPose.timeIntervals[i].x,
                                    dataToCopyOptions.notLookingForNewPose.timeIntervals[i].y
                                    ));
                            }
                            break;
                        case SectionSelectedType.NeverLookingForNewPoseSection:
                            editedData.neverChecking.timeIntervals.Clear();
                            for (int i = 0; i < dataToCopyOptions.neverChecking.timeIntervals.Count; i++)
                            {
                                editedData.neverChecking.timeIntervals.Add(new float2(
                                    dataToCopyOptions.neverChecking.timeIntervals[i].x,
                                    dataToCopyOptions.neverChecking.timeIntervals[i].y
                                    ));
                            }
                            break;
                        case SectionSelectedType.NormalSection:
                            if (0 <= selectedSectionIndex && selectedSectionIndex < dataToCopyOptions.sections.Count)
                            {
                                editedData.sections[selectedSectionIndex].timeIntervals.Clear();
                                for (int i = 0; i < dataToCopyOptions.sections[selectedSectionIndex].timeIntervals.Count; i++)
                                {
                                    editedData.AddSectionInterval(
                                        selectedSectionIndex,
                                        i,
                                        dataToCopyOptions.sections[selectedSectionIndex].timeIntervals[i]
                                        );
                                }
                            }
                            break;
                    }
                }
            }

            dataToCopyOptions = (MotionMatchingData)EditorGUILayout.ObjectField(dataToCopyOptions, typeof(MotionMatchingData), true);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
        }

        private void HandleSectionIntervals(ReorderableList list, MotionMatchingData currentData)
        {
            list.headerHeight = 2f;

            list.elementHeight = 40f;

            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                float numberWidth = 50f;
                float space = 10f;

                float VSpace = 10f;
                float elementHeight = rect.height - 2 * VSpace;
                Rect r1 = new Rect(rect.x, rect.y + VSpace, 50, elementHeight);
                Rect r2 = new Rect(r1.x + numberWidth + space, rect.y + VSpace, rect.width - 2 * (numberWidth + space), elementHeight);
                Rect r3 = new Rect(r2.x + r2.width + space, rect.y + VSpace, 50, elementHeight);

                float min = ((float2)list.list[index]).x;
                float max = ((float2)list.list[index]).y;

                min = EditorGUI.FloatField(r1, min);
                max = EditorGUI.FloatField(r3, max);
                EditorGUI.MinMaxSlider(r2, ref min, ref max, 0f, currentData.animationLength);

                if (index == list.index)
                {
                    list.list[index] = new float2(min, max);
                }
            };

            list.onAddCallback = (ReorderableList rlist) =>
            {
                list.list.Add(new float2(0.0f, currentData.animationLength));
                list.index = list.count - 1;
            };


            list.onRemoveCallback = (ReorderableList rlist) =>
            {
                if (list.index <= list.list.Count && list.index >= 0)
                {
                    list.list.RemoveAt(list.index);
                }
            };


        }

        private void DrawContactsOptionsOnRightMenu()
        {
            if (contactPointsRL == null || isDataSwitched)
            {
                contactPointsRL = new ReorderableList(editedData.contactPoints, typeof(MotionMatchingContact), true, false, true, true);
            }

            HandleContactPointsReorderbleList(contactPointsRL, editedData, 2);
            contactPointsRL.DoLayoutList();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy Contacts Settings", GUIResources.Button_MD()))
            {
                if (dataToCopyOptions != null)
                {
                    editedData.contactPoints.Clear();
                    for (int i = 0; i < dataToCopyOptions.contactPoints.Count; i++)
                    {
                        editedData.contactPoints.Add(dataToCopyOptions.contactPoints[i]);
                    }
                }
            }

            dataToCopyOptions = (MotionMatchingData)EditorGUILayout.ObjectField(dataToCopyOptions, typeof(MotionMatchingData), true);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

        }

        private void HandleContactPointsReorderbleList(
            ReorderableList rList,
            MotionMatchingData currentData,
            int elementLines
            )
        {
            rList.onSelectCallback = (ReorderableList list) =>
            {

            };

            rList.onAddCallback = (ReorderableList list) =>
            {
                currentData.contactPoints.Add(new MotionMatchingContact(0f));
            };

            rList.onRemoveCallback = (ReorderableList list) =>
            {
                currentData.contactPoints.RemoveAt(list.index);
                list.index = list.count - 1;
            };

            rList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                index = Mathf.Clamp(index, 0, rList.count - 1);
                MotionMatchingContact cp = currentData.contactPoints[index];

                float H = 20f;
                float space = 5f;
                float numberL = 50f;
                Rect startRect = new Rect(rect.x, rect.y + space, numberL, H);
                Rect sliderRect = new Rect(rect.x + startRect.width + space, rect.y + space, rect.width - 2f * (space + numberL), H);
                Rect endRect = new Rect(sliderRect.x + sliderRect.width + space, rect.y + space, numberL, H);
                Rect posRect = new Rect(rect.x, sliderRect.y + H, 0.5f * rect.width, 2 * H);
                Rect normalRect = new Rect(posRect.x + posRect.width, sliderRect.y + H, 0.5f * rect.width, 2 * H);

                cp.endTime = Mathf.Clamp(cp.endTime, cp.startTime, currentData.animationLength);


                float startTime = EditorGUI.FloatField(startRect, cp.startTime);
                float endTime = EditorGUI.FloatField(endRect, cp.endTime);
                EditorGUI.MinMaxSlider(sliderRect, ref startTime, ref endTime, 0f, currentData.animationLength);
                Vector3 position = EditorGUI.Vector3Field(posRect, new GUIContent("Position"), cp.position);
                string normalName = currentData.contactsType == ContactStateType.Impacts ? "Impact rotation" : "Contact rotation";
                Vector4 rotation = new Vector4(cp.rotation.x, cp.rotation.y, cp.rotation.z, cp.rotation.w);
                rotation = EditorGUI.Vector4Field(normalRect, new GUIContent(normalName), rotation);

                if (rList.index == index)
                {
                    cp.startTime = startTime;
                    cp.endTime = endTime;
                    cp.position = position;
                    //cp.contactNormal = normal;
                }


                currentData.contactPoints[index] = cp;
            };

            rList.elementHeightCallback = (int index) =>
            {
                return elementLines * 40f;
            };

            rList.headerHeight = 5f;

            rList.drawHeaderCallback = (Rect rect) =>
            {

            };
        }
        #endregion


        private void AnimationPlaying()
        {
            switch (animationState)
            {
                case AnimationState.NotCreated:
                    break;
                case AnimationState.Stoped:
                    StopedAnimationState();
                    break;
                case AnimationState.Playing:
                    PlayingAnimationState();
                    break;
            }
        }


        private void PlayingAnimationState()
        {
            float deltaTime = Time.realtimeSinceStartup - bufforPlayingTime;

            currentAnimaionTime += deltaTime;

            bufforPlayingTime = Time.realtimeSinceStartup;

            if (currentAnimaionTime > editedData.animationLength)
            {
                gameObject.transform.position = Vector3.zero;
                gameObject.transform.rotation = Quaternion.identity;
                OnPreviewBTN();
                OnPlayBTN();
                //currentAnimaionTime -= editedData.animationLength;
            }

        }

        private void StopedAnimationState()
        {

        }

        private void PausedAnimationState()
        {

        }

        private void OnCurrentAnimationTimeChange()
        {
            float deltaTime = currentAnimaionTime - bufforAnimationTime;

            bufforAnimationTime = currentAnimaionTime;
            if (previewBTNClick)
            {
                previewBTNClick = false;
            }
            else
            {
                if (playableGraph != null)
                {
                    if (playableGraph.IsValid() && playableGraph.IsDataValid(editedData))
                    {

                        float minDelta = 0.01667f;
                        if (deltaTime > minDelta)
                        {
                            int deltas = Mathf.Abs(Mathf.CeilToInt(deltaTime / minDelta));
                            float finalDelta = deltaTime / (float)deltas;

                            for (int i = 0; i < deltas; i++)
                            {

                                playableGraph.EvaluateMotionMatchgData(editedData, finalDelta);
                                //playableGraph.Evaluate(finalDelta);
                            }
                        }
                        else
                        {
                            playableGraph.EvaluateMotionMatchgData(editedData, deltaTime);
                            //playableGraph.Evaluate(deltaTime);
                        }

                        this.Repaint();
                    }
                }
            }
        }

        #region Event Markers
        private void DrawEventMarkersLeftMenu()
        {

        }

        private void DrawEventMarkersRightMenu()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Event Marker", GUIResources.Button_MD()))
            {
                editedData.eventMarkers.Add(new MotionMatchingEventMarker(string.Format("EventMarker{0}", editedData.eventMarkers.Count), currentAnimaionTime));

            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            for (int i = 0; i < editedData.eventMarkers.Count; i++)
            {
                GUILayout.BeginHorizontal();
                editedData.eventMarkers[i] = DrawEventMarker(editedData.eventMarkers[i]);

                if (GUILayout.Button("Set Event Marker time"))
                {
                    MotionMatchingEventMarker em = editedData.eventMarkers[i];
                    em.SetTime(currentAnimaionTime);
                    editedData.eventMarkers[i] = em;
                }
                GUILayout.Space(5);
                if (GUILayout.Button("X", GUILayout.Width(25f)))
                {
                    editedData.eventMarkers.RemoveAt(i);
                    i--;
                }

                GUILayout.EndHorizontal();
            }
            GUILayout.Space(10);
            if (GUILayout.Button("Sort Event Markers", GUIResources.Button_MD()))
            {
                editedData.eventMarkers.Sort(delegate (MotionMatchingEventMarker x, MotionMatchingEventMarker y)
                {
                    if (x.GetTime() <= y.GetTime())
                    {
                        return -1;
                    }
                    return 1;
                });
            }
        }

        private MotionMatchingEventMarker DrawEventMarker(MotionMatchingEventMarker marker)
        {
            GUILayout.Label("Name", GUILayout.Width(40f));
            marker.SetName(GUILayout.TextField(marker.GetName(), GUILayout.Width(200)));
            //marker.SetTime(GUILayout.HorizontalSlider(marker.GetTime(), 0f, editedData.length));

            GUILayout.Label("Time", GUILayout.Width(40f));
            marker.SetTime(Mathf.Clamp(EditorGUILayout.FloatField(marker.GetTime(), GUILayout.Width(50f)), 0f, editedData.animationLength));

            return marker;
        }

        #endregion


        #region Scene GUI Drawing

        private void DrawSceneGUI(SceneView sceneView)
        {
            if (gameObject != null && editedData != null)
            {
                if (selectedOption == 1)
                {
                    DrawSelectedContactPoint();
                    DrawCalculatedContactPoints();
                }

                if (_bDrawTrajectory)
                {
                    Handles.color = Color.cyan;
                    Handles.DrawWireCube(gameObject.transform.position, Vector3.one * 0.1f);
                    Trajectory t = new Trajectory(editedData.trajectoryPointsTimes.Count);
                    editedData.GetTrajectoryInTime(ref t, currentAnimaionTime);
                    Handles.color = Color.green;
                    t.TransformToWorldSpace(gameObject.transform);
                    MM_Gizmos.DrawTrajectory_Handles(
                        editedData.trajectoryPointsTimes.ToArray(),
                        gameObject.transform.position,
                        gameObject.transform.forward,
                        t,
                        0.04f,
                        0.2f
                        );
                }

                if (_bDrawPose)
                {
                    PoseData p = new PoseData(editedData[0].pose.Count);
                    editedData.GetPoseInTime(ref p, currentAnimaionTime);
                    p.TransformToWorldSpace(gameObject.transform);
                    MM_Gizmos.DrawPose(p, Color.blue, Color.yellow);
                }

                if (selectedOption == 1)
                {
                    float length = 200f;
                    float height = 25f;
                    Rect r = new Rect(
                        sceneView.position.width / 2f - length / 2f,
                        30f,
                        length,
                        height
                        );
                    DrawContactGizmosSelectRect(r);
                }
            }
        }

        float drawCubeSize = 0.05f;
        float arrowLength = 0.5f;
        float arrowArmLength = 0.2f;

        private void DrawSelectedContactPoint()
        {
            if (editedData == null || gameObject == null)
            {
                return;
            }
            if (contactPointsRL != null)
            {
                if (0 <= contactPointsRL.index && contactPointsRL.index < editedData.contactPoints.Count)
                {
                    Handles.color = Color.green;
                    MotionMatchingContact cp = editedData.contactPoints[contactPointsRL.index];
                    Vector3 drawPosition = gameObject.transform.TransformPoint(cp.position);
                    Vector3 drawDirection = gameObject.transform.TransformDirection(cp.contactNormal);

                    cp.position = gameObject.transform.TransformPoint(cp.position);
                    // Changing contactPoint position
                    if (drawPositionManipulator)
                    {
                        Vector3 cpPosBuffor = cp.position;
                        Vector3 cpSurNorBuffor = cp.contactNormal;

                        cp.position = Handles.PositionHandle(cp.position, Quaternion.identity);
                    }

                    // Changing contactPoint surface reverse normal
                    if (drawRotationManipuator)
                    {
                        Vector3 contactNormal = gameObject.transform.TransformDirection(cp.contactNormal);
                        //Quaternion rot = Quaternion.FromToRotation(Vector3.forward, dirRSN.normalized);

                        //rot = Handles.RotationHandle(rot, cp.position);
                        if (cp.rotation.x == 0f &&
                            cp.rotation.y == 0f &&
                            cp.rotation.z == 0f &&
                            cp.rotation.w == 0f)
                        {
                            cp.rotation = Quaternion.identity;
                        }
                        cp.rotation = Handles.RotationHandle(cp.rotation, cp.position);

                        contactNormal = cp.rotation * Vector3.forward;
                        cp.contactNormal = contactNormal;

                        cp.contactNormal = gameObject.transform.InverseTransformDirection(cp.contactNormal);
                    }

                    Handles.DrawWireCube(drawPosition, Vector3.one * drawCubeSize);
                    MM_Gizmos.DrawArrowHandles(drawPosition, drawDirection, arrowLength, arrowArmLength);

                    cp.position = gameObject.transform.InverseTransformPoint(cp.position);
                    editedData.contactPoints[contactPointsRL.index] = cp;
                }
            }
        }

        private void DrawCalculatedContactPoints()
        {
            switch (editedData.contactsType)
            {
                case ContactStateType.NormalContacts:
                    DrawSceneGUIContacts();
                    break;
                case ContactStateType.Impacts:
                    DrawSceneGUIImpacts();
                    break;
            }

        }

        private void DrawSceneGUIContacts()
        {
            List<FrameContact> cpList = new List<FrameContact>();
            editedData.GetContactPoints(ref cpList, currentAnimaionTime);

            Handles.color = Color.red;

            for (int i = 0; i < cpList.Count; i++)
            {
                Vector3 cpPos = gameObject.transform.TransformPoint(cpList[i].position);

                if (drawContactsPositions)
                {
                    Handles.DrawWireCube(cpPos, Vector3.one * drawCubeSize);
                }
                if (drawContactsRSN)
                {
                    Vector3 cpRSN = gameObject.transform.TransformDirection(cpList[i].normal);

                    MM_Gizmos.DrawArrowHandles(cpPos, cpRSN.normalized, arrowLength, arrowArmLength);
                }
            }
        }

        private void DrawSceneGUIImpacts()
        {
            FrameData frame = editedData.GetClossestFrame(currentAnimaionTime);

            if (frame.contactPoints.Length != 1)
            {
                return;
            }
            Vector3 cpPos = gameObject.transform.TransformPoint(frame.contactPoints[0].position);
            Vector3 cpRSN = gameObject.transform.TransformDirection(frame.contactPoints[0].normal);
            if (drawContactsPositions)
            {
                Handles.DrawWireCube(cpPos, Vector3.one * drawCubeSize);
            }
            if (drawContactsRSN)
            {
                MM_Gizmos.DrawArrowHandles(cpPos, cpRSN.normalized, arrowLength, arrowArmLength);
            }

        }

        private void DrawContactGizmosSelectRect(Rect rect)
        {
            Handles.BeginGUI();
            {
                GUILayout.BeginArea(rect);
                {
                    GUILayout.BeginHorizontal();
                    drawPositionManipulator = GUILayout.Toggle(drawPositionManipulator, "Position", new GUIStyle("Button"));
                    drawRotationManipuator = GUILayout.Toggle(drawRotationManipuator, "Normal", new GUIStyle("Button"));
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndArea();
            }
            Handles.EndGUI();
        }
        #endregion
    }
}
