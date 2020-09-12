using DW_Gameplay;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DW_Editor
{
    public class LayersAndParametersSpace
    {
        public MM_AnimatorController animator;
        Vector2 scroll = Vector2.zero;
        int toolbarOption = 0;
        string[] toolBarStrings =
        {
            "Layers",
            "Parameters"
        };

        ReorderableList layerList = null;
        ReorderableList floats = null;
        ReorderableList ints = null;
        ReorderableList bools = null;

        public int selectedLayerIndex = -1;

        private bool foldSpace;
        private float foldingSpeed = 0.2f;

        int currentAnimatorID = int.MaxValue;

        public LayersAndParametersSpace(MM_AnimatorController animator)
        {
            this.animator = animator;
        }

        public void SetAnimator(MM_AnimatorController animator)
        {
            this.animator = animator;
            if (this.animator != null)
            {
                if (currentAnimatorID != this.animator.GetInstanceID())
                {
                    currentAnimatorID = this.animator.GetInstanceID();

                    layerList = new ReorderableList(animator.layers, typeof(MotionMatchingLayer));
                    floats = new ReorderableList(animator.floatNames, typeof(string));
                    ints = new ReorderableList(animator.intNames, typeof(string));
                    bools = new ReorderableList(animator.boolNames, typeof(string));
                }
            }
        }

        public void Draw(Rect rect, ref float resizeFactor, EditorWindow window)
        {
            GUI.DrawTexture(rect, GUIResources.GetMediumTexture_1());
            GUILayout.BeginArea(rect);

            if (animator != null)
            {
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("<", GUILayout.Width(20)))
                {
                    foldSpace = true;
                    foldingSpeed = resizeFactor / 2f;
                }

                GUILayout.Space(5);

                toolbarOption = GUILayout.Toolbar(toolbarOption, toolBarStrings);

                if (foldSpace)
                {
                    resizeFactor -= (foldingSpeed * Time.deltaTime);
                    resizeFactor = Mathf.Clamp(resizeFactor, 0f, float.MaxValue);
                    window.Repaint();
                    if (resizeFactor == 0)
                    {
                        foldSpace = false;
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(5);

                scroll = EditorGUILayout.BeginScrollView(scroll);
                switch (toolbarOption)
                {
                    case 0:
                        DrawLayers(rect, ref window);
                        break;
                    case 1:
                        DrawValues();
                        break;
                }

                EditorGUILayout.EndScrollView();
            }

            GUILayout.EndArea();
        }

        public void Draw_1(Rect rect, ref float resizeFactor, EditorWindow window)
        {
            //GUI.DrawTexture(rect, GUIResources.GetMediumTexture_1());
            //GUILayout.BeginArea(rect);

            if (animator != null)
            {
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("<", GUILayout.Width(20)))
                {
                    foldSpace = true;
                    foldingSpeed = resizeFactor / 1.5f;
                }

                GUILayout.Space(5);

                toolbarOption = GUILayout.Toolbar(toolbarOption, toolBarStrings);
                if (foldSpace)
                {
                    resizeFactor -= (foldingSpeed * Time.deltaTime);
                    resizeFactor = Mathf.Clamp(resizeFactor, 0f, float.MaxValue);
                    window.Repaint();
                    if (resizeFactor == 0)
                    {
                        foldSpace = false;
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(5);

                scroll = EditorGUILayout.BeginScrollView(scroll);
                switch (toolbarOption)
                {
                    case 0:
                        DrawLayers(rect, ref window);
                        break;
                    case 1:
                        DrawValues();
                        break;
                }

                EditorGUILayout.EndScrollView();
            }

            //GUILayout.EndArea();
        }

        private void DrawLayers(Rect rect, ref EditorWindow window)
        {
            if (layerList == null)
            {
                layerList = new ReorderableList(animator.layers, typeof(MotionMatchingLayer));
            }
            HandleLayerReordableList(layerList, "Layers");
            layerList.DoLayoutList();
            GetingSelectedLayer();
        }

        private void GetingSelectedLayer()
        {
            if (animator.layers.Count > 0 && !(layerList.index >= 0 && layerList.index < layerList.count && layerList.count > 0))
            {
                layerList.index = 0;
            }
            if (layerList.index >= 0 && layerList.index < layerList.count && layerList.count > 0)
            {
                selectedLayerIndex = layerList.index;
            }
            else
            {
                selectedLayerIndex = -1;
            }
        }

        private void HandleLayerReordableList(ReorderableList list, string header)
        {
            //list.elementHeight = 40f;

            list.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, header);
            };

            list.onAddCallback = (ReorderableList reorderableList) =>
            {
                animator.AddLayer("New Layer", null);
            };

            list.onReorderCallback = (ReorderableList reorderableList) =>
            {
                for (int i = 0; i < animator.layers.Count; i++)
                {
                    animator.layers[i].index = i;
                }
            };

            list.onRemoveCallback = (ReorderableList reorderableList) =>
            {
                animator.layers.RemoveAt(reorderableList.index);
                for (int i = 0; i < animator.layers.Count; i++)
                {
                    animator.layers[i].index = i;
                }
            };

            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                float lineHeight = 20f;
                float space = 5f;
                Rect toogleRect = new Rect(rect.x, rect.y, 25, lineHeight);
                animator.layers[index].fold = EditorGUI.Toggle(toogleRect, animator.layers[index].fold);
                Rect nameRect = new Rect(rect.x + toogleRect.width, rect.y, rect.width - toogleRect.width, lineHeight);
                EditorGUI.LabelField(nameRect, animator.layers[index].name);

                if (animator.layers[index].fold)
                {
                    Rect changeNameRect = new Rect(
                        rect.x,
                        nameRect.y + nameRect.height + space,
                        rect.width,
                        lineHeight
                        );
                    Rect avatarMaskRect = new Rect(
                        rect.x,
                        changeNameRect.y + changeNameRect.height + space,
                        rect.width,
                        lineHeight
                        );
                    Rect IKPassRect = new Rect(
                        rect.x,
                        avatarMaskRect.y + avatarMaskRect.height + space,
                        rect.width,
                        lineHeight
                        );
                    Rect FootIKPassRect = new Rect(
                        rect.x,
                        IKPassRect.y + IKPassRect.height + space,
                        rect.width,
                        lineHeight
                        );
                    Rect IsAdditiveSRect = new Rect(
                        rect.x,
                        FootIKPassRect.y + FootIKPassRect.height + space,
                        rect.width,
                        lineHeight
                        );


                    animator.layers[index].name = EditorGUI.TextField(changeNameRect, animator.layers[index].name);

                    animator.layers[index].avatarMask = (AvatarMask)EditorGUI.ObjectField(
                        avatarMaskRect,
                        animator.layers[index].avatarMask,
                        typeof(AvatarMask),
                        true
                        );
                    animator.layers[index].passIK = EditorGUI.Toggle(IKPassRect, new GUIContent("Pass IK"), animator.layers[index].passIK);
                    animator.layers[index].footPassIK = EditorGUI.Toggle(FootIKPassRect, new GUIContent("Foot IK"), animator.layers[index].footPassIK);
                    animator.layers[index].isAdditive = EditorGUI.Toggle(IsAdditiveSRect, new GUIContent("Additive layer"), animator.layers[index].isAdditive);

                }
            };


            list.elementHeightCallback = (int index) =>
            {
                if (animator.layers[index].fold)
                {
                    return 6 * 25;
                }
                else
                {
                    return 25f;
                }
            };
        }

        private void CreateGenericMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Add Layer"), false, GenericMenuCallback, LeftRectGMOptions.AddLayer);
            if (layerList.index >= 0 && layerList.index < layerList.count)
            {
                menu.AddItem(new GUIContent("Edit Layer"), false, GenericMenuCallback, LeftRectGMOptions.EditLayer);
                menu.AddItem(new GUIContent("Remove Layer"), false, GenericMenuCallback, LeftRectGMOptions.RemoveLayer);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Edit Layer"));
                menu.AddDisabledItem(new GUIContent("Remove Layer"));
            }
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Collapse Inactive Layers"), false, GenericMenuCallback, LeftRectGMOptions.CollapseInactiveLayers);


            menu.ShowAsContext();
        }

        private void GenericMenuCallback(object action)
        {
            switch (action)
            {
                case LeftRectGMOptions.AddLayer:
                    animator.AddLayer("New Layer", null);
                    break;
                case LeftRectGMOptions.RemoveLayer:
                    try
                    {
                        animator.RemoveLayerAt(layerList.index);
                    }
                    catch (Exception)
                    {

                    }
                    break;
                case LeftRectGMOptions.EditLayer:
                    if (selectedLayerIndex < animator.layers.Count && selectedLayerIndex >= 0)
                    {
                        animator.layers[selectedLayerIndex].fold = true;
                    }
                    break;
                case LeftRectGMOptions.CollapseInactiveLayers:
                    for (int i = 0; i < layerList.count; i++)
                    {
                        if (i != layerList.index)
                        {
                            animator.layers[i].fold = false;
                        }
                    }
                    break;
            }
        }

        private void DrawValues()
        {
            GUILayout.BeginVertical();

            HandleStringList(bools, "bools", 0);
            bools.DoLayoutList();

            if (GUILayout.Button("Clear bools"))
            {
                animator.boolNames.Clear();
            }

            GUILayout.Space(5);

            HandleStringList(ints, "Ints", 1);
            ints.DoLayoutList();

            if (GUILayout.Button("Clear ints"))
            {
                animator.intNames.Clear();
            }

            GUILayout.Space(5);

            HandleStringList(floats, "Floats", 2);
            floats.DoLayoutList();

            if (GUILayout.Button("Clear floats"))
            {
                animator.floatNames.Clear();
            }

            GUILayout.EndVertical();
        }

        private void HandleStringList(ReorderableList list, string name, int type)
        {
            list.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, name);
            };

            list.onAddCallback = (ReorderableList rlist) =>
            {
                switch (type)
                {
                    case 0:
                        animator.AddBool("New bool");
                        break;
                    case 1:
                        animator.AddInt("New int");
                        break;
                    case 2:
                        animator.AddFloat("New float");
                        break;
                }
            };

            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                string currentName;
                string newName;
                int counter = 0;
                Rect drawRect = new Rect(rect.x, rect.y, rect.width - 50, 0.8f * rect.height);
                switch (type)
                {
                    case 0:
                        if (!isActive)
                        {
                            EditorGUI.LabelField(drawRect, animator.boolNames[index]);
                        }
                        else
                        {
                            animator.boolNames[index] = EditorGUI.TextField(drawRect, animator.boolNames[index]);

                            currentName = animator.boolNames[index];
                            newName = currentName;
                            counter = 0;
                            for (int i = 0; i < animator.boolNames.Count; i++)
                            {
                                if (animator.boolNames[i] == newName && i != index)
                                {
                                    counter++;
                                    newName = currentName + counter.ToString();
                                    i = 0;
                                }
                            }
                            animator.boolNames[index] = newName;
                        }
                        break;
                    case 1:
                        if (!isActive)
                        {
                            EditorGUI.LabelField(drawRect, animator.intNames[index]);
                        }
                        else
                        {
                            animator.intNames[index] = EditorGUI.TextField(drawRect, animator.intNames[index]);

                            currentName = animator.intNames[index];
                            newName = currentName;
                            counter = 0;
                            for (int i = 0; i < animator.intNames.Count; i++)
                            {
                                if (animator.intNames[i] == newName && i != index)
                                {
                                    counter++;
                                    newName = currentName + counter.ToString();
                                    i = 0;
                                }
                            }
                            animator.intNames[index] = newName;
                        }
                        break;
                    case 2:
                        if (!isActive)
                        {
                            EditorGUI.LabelField(drawRect, animator.floatNames[index]);
                        }
                        else
                        {
                            animator.floatNames[index] = EditorGUI.TextField(drawRect, animator.floatNames[index]);

                            currentName = animator.floatNames[index];
                            newName = currentName;
                            counter = 0;
                            for (int i = 0; i < animator.floatNames.Count; i++)
                            {
                                if (animator.floatNames[i] == newName && i != index)
                                {
                                    counter++;
                                    newName = currentName + counter.ToString();
                                    i = 0;
                                }
                            }
                            animator.floatNames[index] = newName;
                        }
                        break;
                }
            };
        }

    }

    public enum LayerView
    {
        LayersList,
        AddLayerWindow
    }

    public enum LeftRectGMOptions
    {
        AddLayer,
        RemoveLayer,
        EditLayer,
        CollapseInactiveLayers
    }
}