using DW_Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace DW_Editor
{
    public enum GraphGenericMenuActions
    {
        AddMotionMatchingState,
        AddSingleAnimationState,
        AddContactState,
        AddPortalToState,
        AddPortalToSequence,
        RemoveState,
        RemoveTransition,
        SetAsStartState,
        ShowNodes,
        ShowOriginalNode
    }

    public class GraphSpaceNEW
    {
        MM_AnimatorController animator;
        int currentAnimatorID = int.MaxValue;

        Vector2 oldMousePos;

        float minZoom = 0.2f;
        float maxZoom = 1.5f;
        //if higher zoom speed slower zooming
        float zoomSpeed = 50f;

        // Selected things
        MotionMatchingLayer selectedLayer;

        List<int> selectedNodesID = new List<int>();
        public MotionMatchingNode selectedNode = null;
        public Transition selectedTransition = null;

        int? selectedInputNode = null;
        int? selectedOutputNode = null;

        bool drawTransitionToMousePos = false;

        //Moving node or nodes
        bool moveNodes = false;
        bool moveView = false;

        // selecting multipleNodes 
        bool multipleSelection = false;
        Rect selectionArea = new Rect();
        Vector2 startSelection = Vector2.zero;
        Vector2 endSelection = Vector2.zero;

        Vector2 mousePosition = Vector2.zero;
        Vector2 startMouseDownPosition = Vector2.zero;
        Vector2 rectCentre;

        IDCreator nodeIDCreator = new IDCreator();

        public GraphSpaceNEW()
        {

        }

        public void SetAnimatorAndLayerIndex(MM_AnimatorController animator, int selectedLayerIndex)
        {
            this.animator = animator;
            if (this.animator != null)
            {
                if (selectedLayerIndex >= 0 && selectedLayerIndex < animator.layers.Count)
                {
                    selectedLayer = this.animator.layers[selectedLayerIndex];
                }
                else
                {
                    selectedLayer = null;
                }

                if (currentAnimatorID != this.animator.GetInstanceID())
                {
                    currentAnimatorID = this.animator.GetInstanceID();
                    OnAnimatorChange();
                }
            }
            else
            {
                selectedLayer = null;
            }
        }

        private void OnAnimatorChange()
        {
        }

        public void Draw(Rect rect)
        {
            GUILayoutElements.DrawGrid(
                rect,
                20f,
                20f,
                0.5f,
                new Color(0.3f, 0.3f, 0.3f),
                selectedLayer != null ? selectedLayer.zoom : 1.0f
                );
            if (selectedLayer != null)
            {
                EditorZoomArea.Begin(selectedLayer.zoom, rect);

                DrawConnections();
                DrawStates();
                DrawTransitionToMousePos(mousePosition);

                if (multipleSelection)
                {
                    GUI.Box(selectionArea, "", GUIResources.GetSelectionArea());
                }

                EditorZoomArea.End();
            }
        }

        private void DrawStates()
        {
            foreach (MotionMatchingNode node in selectedLayer.nodes)
            {
                node.Draw(
                    selectedLayer.startStateIndex == node.stateIndex && node.nodeType != MotionMatchingNodeType.Portal,
                    IsNodeSelected(node),
                    node.stateIndex >= 0 && node.stateIndex < selectedLayer.states.Count ? selectedLayer.states[node.stateIndex].GetName() : "",
                    GUIResources.SelectedNodeStyle(),
                    GUIResources.StartNodeStyle(),
                    GUIResources.NormalNodeStyle(),
                    GUIResources.PortalNodeStyle(),
                    GUIResources.ContactNodeStyle(),
                    GUIResources.InputPointStyle(),
                    GUIResources.OutputPointStyle(),
                    GUIResources.NodeTextStyle()
                    );

            }
        }

        private bool IsNodeSelected(MotionMatchingNode node)
        {
            if (selectedNode != null)
            {
                if (selectedNode.ID == node.ID)
                {
                    return true;
                }
            }
            if (selectedNodesID.Contains(node.ID))
            {
                return true;
            }

            return false;
        }

        private void DrawTransitionToMousePos(Vector2 mousePos)
        {
            if (drawTransitionToMousePos)
            {
                float lineLength = 100f;
                if (selectedOutputNode != null)
                {
                    Handles.DrawBezier(
                        selectedLayer.nodes[(int)selectedOutputNode].output.center,
                        mousePos,
                        selectedLayer.nodes[(int)selectedOutputNode].output.center + Vector2.right * lineLength,
                        mousePos + Vector2.left * lineLength,
                        Color.red,
                        null,
                        3f
                        );
                }

                if (selectedInputNode != null)
                {
                    Handles.DrawBezier(
                        selectedLayer.nodes[(int)selectedInputNode].input.center,
                        mousePos,
                        selectedLayer.nodes[(int)selectedInputNode].input.center + Vector2.left * lineLength,
                        mousePos + Vector2.right * lineLength,
                        Color.red,
                        null,
                        3f
                        );
                }
            }
        }

        private void DrawConnections()
        {
            //float lineLength = 100f;
            foreach (MotionMatchingNode n in selectedLayer.nodes)
            {
                if (n.nodeType != MotionMatchingNodeType.Portal)
                {
                    foreach (Transition t in selectedLayer.states[n.stateIndex].transitions)
                    {
                        int toNodeIndex = 0;
                        for (int i = 0; i < selectedLayer.nodes.Count; i++)
                        {
                            if (selectedLayer.nodes[i].ID == t.nodeID)
                            {
                                toNodeIndex = i;
                                break;
                            }
                        }


                        t.transitionRect.position = (n.output.center + selectedLayer.nodes[toNodeIndex].input.center) / 2f - t.transitionRect.size / 2f;

                        if (t == selectedTransition)
                        {
                            DrawTransition(
                                t.transitionRect,
                                n.output.center,
                                selectedLayer.nodes[toNodeIndex].input.center,
                                Color.yellow,
                                GUIResources.GetSelectedConnectionTexture()
                                );
                        }
                        else if (selectedLayer.nodes[toNodeIndex].nodeType == MotionMatchingNodeType.Portal)
                        {
                            DrawTransition(
                                t.transitionRect,
                                n.output.center,
                                selectedLayer.nodes[toNodeIndex].input.center,
                                Color.green,
                                GUIResources.GetPortalConnectionTexture()
                                );
                        }
                        else
                        {
                            DrawTransition(
                                t.transitionRect,
                                n.output.center,
                                selectedLayer.nodes[toNodeIndex].input.center,
                                Color.white,
                                GUIResources.GetNormalConnectionTexture()
                                );
                        }

                    }
                }
            }
        }

        private void DrawTransition(Rect rect, Vector2 from, Vector2 to, Color lineColor, Texture2D texture)
        {
            Handles.DrawBezier(
                            from,
                            to,
                            from + Vector2.right * 100,
                            to + Vector2.left * 100,
                            lineColor,
                            null,
                            3f
                            );
            GUI.DrawTexture(rect, texture);
        }

        public void UserInput(Event e, Rect rect, EditorWindow window)
        {
            if (selectedLayer == null)
            {
                return;
            }

            mousePosition = (e.mousePosition - rect.position) / selectedLayer.zoom;
            rectCentre = new Vector2(rect.width / 2f, rect.height / 2f) / selectedLayer.zoom;
            bool inRect = true;
            if (!rect.Contains(e.mousePosition))
            {
                inRect = false;
            }
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (!inRect)
                    {
                        break;
                    }
                    OnMouseDown(e, mousePosition, window);
                    window.Repaint();
                    break;
                case EventType.MouseUp:
                    OnMouseUp(e, mousePosition);
                    window.Repaint();
                    break;
                case EventType.MouseDrag:
                    OnMouseDrag(e, window);
                    break;
                case EventType.ScrollWheel:
                    if (!inRect)
                    {
                        break;
                    }
                    float oldZoom = selectedLayer.zoom;
                    selectedLayer.zoom += -e.delta.y / zoomSpeed;
                    selectedLayer.zoom = Mathf.Clamp(selectedLayer.zoom, minZoom, maxZoom);
                    oldMousePos = new Vector2(mousePosition.x, mousePosition.y);
                    mousePosition = (e.mousePosition - rect.position) / selectedLayer.zoom;
                    Vector2 mouseDelta = oldMousePos - mousePosition;
                    foreach (MotionMatchingNode node in selectedLayer.nodes)
                    {
                        node.Move(-mouseDelta);
                    }

                    e.Use();
                    break;
            }

        }

        private void OnMouseDown(Event e, Vector2 mousePos, EditorWindow window)
        {
            // left button
            if (e.button == 0)
            {
                if (!SelectingElements(mousePos))
                {
                    ClicOnEmptyField(mousePos);
                }
            }

            // right button
            if (e.button == 1)
            {
                CreateGenericMenu(e, window);
            }

            // middle button
            if (e.button == 2)
            {
                moveView = true;
            }
        }

        private void OnMouseUp(Event e, Vector2 mousePos)
        {
            EndSelectingMultipleNodes();
            moveNodes = false;
            moveView = false;

            MakingTransition(mousePos);
            drawTransitionToMousePos = false;

            selectedInputNode = null;
            selectedOutputNode = null;
        }

        private void OnMouseDrag(Event e, EditorWindow window)
        {
            if (multipleSelection)
            {
                SelectingMultipleNodes(mousePosition);
                window.Repaint();
            }
            else if (moveNodes)
            {
                if (selectedNodesID.Count != 0)
                {
                    foreach (MotionMatchingNode n in selectedLayer.nodes)
                    {
                        if (selectedNodesID.Contains(n.ID))
                        {
                            n.Move(e.delta / selectedLayer.zoom);
                        }
                    }
                }
                else if (selectedNode != null)
                {
                    selectedNode.Move(e.delta / selectedLayer.zoom);
                }
                window.Repaint();
            }
            else if (moveView)
            {
                foreach (MotionMatchingNode n in selectedLayer.nodes)
                {
                    n.Move(e.delta / selectedLayer.zoom);
                }
                window.Repaint();
            }
            else if (drawTransitionToMousePos)
            {
                window.Repaint();
            }
        }

        private int GetNewStateID()
        {
            if (nodeIDCreator == null)
            {
                nodeIDCreator = new IDCreator(selectedLayer.GetMaxNodeID());
            }
            nodeIDCreator.IDStart(selectedLayer.GetMaxNodeID());
            return nodeIDCreator.nextID();
        }

        #region Generic menu
        private void CreateGenericMenu(Event e, EditorWindow window)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddSeparator("");
            menu.AddItem(
                new GUIContent("ShowNodes"),
                false,
                GenericMenuCallback,
                GraphGenericMenuActions.ShowNodes
                );
            menu.AddSeparator("");

            if (selectedNode != null && selectedNode.nodeType == MotionMatchingNodeType.State)
            {
                menu.AddItem(
                    new GUIContent("Set as Start State"),
                    false,
                    GenericMenuCallback,
                    GraphGenericMenuActions.SetAsStartState
                    );
            }
            else
            {
                menu.AddDisabledItem(
                    new GUIContent("Set as Start State"));
            }

            if (selectedNode != null && selectedNode.nodeType == MotionMatchingNodeType.Portal)
            {
                menu.AddItem(
                    new GUIContent("Show original"),
                    false,
                    GenericMenuCallback,
                    GraphGenericMenuActions.ShowOriginalNode
                    );
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Show original"));
            }
            menu.AddSeparator("");
            if (selectedLayer != null)
            {
                menu.AddSeparator("Add State/");
                menu.AddItem(
                    new GUIContent("Add State/ Motion Matching State"),
                    false,
                    GenericMenuCallback,
                    GraphGenericMenuActions.AddMotionMatchingState
                    );
                menu.AddItem(
                    new GUIContent("Add State/ Single Animation State"),
                    false,
                    GenericMenuCallback,
                    GraphGenericMenuActions.AddSingleAnimationState
                    );
                //menu.AddDisabledItem(new GUIContent("Add State/ Contact State"));
                menu.AddItem(
                    new GUIContent("Add State/ Contact State"),
                    false,
                    GenericMenuCallback,
                    GraphGenericMenuActions.AddContactState
                    );
                menu.AddSeparator("Add State/");
                menu.AddItem(
                    new GUIContent("Add State/ Portal to State"),
                    false,
                    GenericMenuCallback,
                    GraphGenericMenuActions.AddPortalToState
                    );
                if (selectedNode != null || selectedNodesID.Count > 0)
                {
                    menu.AddItem(
                                    new GUIContent("Remove State"),
                                    false,
                                    GenericMenuCallback,
                                    GraphGenericMenuActions.RemoveState
                                    );
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Remove State"));
                }

            }
            else
            {
                menu.AddSeparator("Add State/");
                menu.AddDisabledItem(new GUIContent("Add State/ Motion Matching State"));
                menu.AddDisabledItem(new GUIContent("Add State/ Single Animation State"));
                menu.AddSeparator("Add State/");
                menu.AddDisabledItem(new GUIContent("Add State/ Portal to State"));
                menu.AddDisabledItem(new GUIContent("Remove State"));

            }

            menu.AddSeparator("");

            if (selectedTransition != null)
            {
                menu.AddItem(
                    new GUIContent("Remove Transition"),
                    false,
                    GenericMenuCallback,
                    GraphGenericMenuActions.RemoveTransition
                    );
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Remove Transition"));
            }


            e.Use();
            window.Repaint();
            menu.ShowAsContext();
        }

        private void GenericMenuCallback(object action)
        {
            switch (action)
            {
                case GraphGenericMenuActions.AddMotionMatchingState:
                    selectedLayer.AddState(
                        "New State",
                        MotionMatchingStateType.MotionMatching,
                        GetNewStateID(),
                        mousePosition
                        );
                    break;
                case GraphGenericMenuActions.AddSingleAnimationState:
                    selectedLayer.AddState(
                        "New State",
                        MotionMatchingStateType.SingleAnimation,
                        GetNewStateID(),
                        mousePosition
                        );
                    break;
                case GraphGenericMenuActions.AddContactState:
                    selectedLayer.AddState(
                        "New State",
                        MotionMatchingStateType.ContactAnimationState,
                        GetNewStateID(),
                        mousePosition
                        );
                    break;
                case GraphGenericMenuActions.RemoveState:
                    if (selectedNode != null)
                    {
                        RemoveNode(selectedNode);
                        selectedNode = null;
                    }
                    if (selectedNodesID.Count > 0)
                    {
                        for (int i = 0; i < selectedLayer.nodes.Count; i++)
                        {
                            if (selectedNodesID.Contains(selectedLayer.nodes[i].ID))
                            {
                                selectedNodesID.Remove(selectedLayer.nodes[i].ID);
                                RemoveNode(selectedLayer.nodes[i]);
                                i--;
                            }
                        }

                        selectedNodesID.Clear();
                    }

                    break;
                case GraphGenericMenuActions.RemoveTransition:
                    foreach (MotionMatchingState s in selectedLayer.states)
                    {
                        for (int i = 0; i < s.transitions.Count; i++)
                        {
                            if (s.transitions[i].transitionRect == selectedTransition.transitionRect)
                            {
                                s.RemoveTransition(selectedTransition.nextStateIndex);
                                selectedTransition = null;
                                return;
                            }
                        }
                    }
                    break;
                case GraphGenericMenuActions.SetAsStartState:
                    selectedLayer.SetStartState(selectedNode.stateIndex);
                    break;
                case GraphGenericMenuActions.ShowNodes:
                    Vector2 delta = Vector3.zero;
                    foreach (MotionMatchingNode n in selectedLayer.nodes)
                    {
                        delta += n.rect.center;
                    }

                    delta /= selectedLayer.nodes.Count;
                    delta = rectCentre - delta;
                    foreach (MotionMatchingNode n in selectedLayer.nodes)
                    {
                        n.Move(delta);
                    }

                    break;
                case GraphGenericMenuActions.AddPortalToState:
                    selectedLayer.AddPortal(mousePosition, GetNewStateID());
                    break;
                case GraphGenericMenuActions.ShowOriginalNode:
                    Vector2 delta_2 = Vector2.zero;
                    foreach (MotionMatchingNode n in selectedLayer.nodes)
                    {
                        if (n.nodeType == MotionMatchingNodeType.State && n.stateIndex == selectedNode.stateIndex)
                        {
                            delta_2 = rectCentre - n.rect.center;
                        }
                    }

                    foreach (MotionMatchingNode n in selectedLayer.nodes)
                    {
                        n.Move(delta_2);
                    }
                    break;
            }
        }

        private void RemoveNode(MotionMatchingNode node)
        {
            switch (node.nodeType)
            {
                case MotionMatchingNodeType.State:
                    selectedLayer.RemoveState(node.stateIndex);
                    break;
                case MotionMatchingNodeType.Portal:
                    selectedLayer.RemovePortal(node.ID);
                    break;
                case MotionMatchingNodeType.Contact:
                    selectedLayer.RemoveState(node.stateIndex);
                    break;
            }
        }
        #endregion

        #region selecting objects

        private bool SelectingElements(Vector2 mousePos)
        {
            for (int i = 0; i < selectedLayer.nodes.Count; i++)
            {
                if (selectedLayer.nodes[i].input.Contains(mousePos) && selectedLayer.nodes[i].nodeType != MotionMatchingNodeType.Contact)
                {
                    selectedNodesID.Clear();
                    //selectedNode = null;
                    //selectedTransition = null;
                    selectedOutputNode = null;

                    drawTransitionToMousePos = true;
                    selectedInputNode = i;
                    return true;
                }
                if (selectedLayer.nodes[i].output.Contains(mousePos) && selectedLayer.nodes[i].nodeType != MotionMatchingNodeType.Portal)
                {
                    selectedNodesID.Clear();
                    //selectedNode = null;
                    //selectedTransition = null;
                    selectedInputNode = null;

                    drawTransitionToMousePos = true;
                    selectedOutputNode = i;
                    return true;
                }
                if (selectedLayer.nodes[i].nodeType != MotionMatchingNodeType.Portal)
                {
                    foreach (Transition t in selectedLayer.states[selectedLayer.nodes[i].stateIndex].transitions)
                    {
                        if (t.transitionRect.Contains(mousePos))
                        {
                            selectedNodesID.Clear();
                            selectedNode = null;
                            selectedOutputNode = null;
                            selectedInputNode = null;
                            if (selectedTransition == t)
                            {
                                selectedTransition = null;
                            }
                            else
                            {
                                selectedTransition = t;
                            }
                            return true;
                        }
                    }
                }

                int nodeIndex = selectedLayer.nodes.Count - 1 - i;
                if (selectedLayer.nodes[nodeIndex].rect.Contains(mousePos))
                {
                    if (!selectedNodesID.Contains(selectedLayer.nodes[nodeIndex].ID))
                    {
                        selectedNodesID.Clear();
                    }
                    selectedTransition = null;
                    selectedOutputNode = null;
                    selectedInputNode = null;
                    moveNodes = true;
                    startMouseDownPosition = mousePos;
                    selectedNode = selectedLayer.nodes[nodeIndex];
                    selectedLayer.MoveNodeOnTop(nodeIndex);
                    return true;
                }
            }
            return false;
        }

        private bool SelectInput(Vector2 mousePos)
        {
            for (int i = 0; i < selectedLayer.nodes.Count; i++)
            {
                if (selectedLayer.nodes[i].input.Contains(mousePos))
                {
                    selectedNodesID.Clear();
                    selectedNode = null;
                    selectedOutputNode = null;
                    selectedTransition = null;

                    drawTransitionToMousePos = true;
                    selectedInputNode = i;
                    return true;
                }
            }
            return false;
        }

        private bool SelectOutput(Vector2 mousePos)
        {
            for (int i = 0; i < selectedLayer.nodes.Count; i++)
            {
                if (selectedLayer.nodes[i].output.Contains(mousePos))
                {
                    selectedNodesID.Clear();
                    selectedNode = null;
                    selectedInputNode = null;
                    selectedTransition = null;

                    drawTransitionToMousePos = true;
                    selectedOutputNode = i;
                    return true;
                }
            }
            return false;
        }

        private bool SelectTransition(Vector2 mousePos)
        {
            foreach (MotionMatchingState s in selectedLayer.states)
            {
                foreach (Transition t in s.transitions)
                {
                    if (t.transitionRect.Contains(mousePos))
                    {
                        selectedNodesID.Clear();
                        selectedNode = null;
                        selectedOutputNode = null;
                        selectedInputNode = null;
                        selectedTransition = t;
                        return true;
                    }
                }
            }
            return false;
        }

        private bool SelectNode(Vector2 mousePos)
        {
            for (int i = 0; i < selectedLayer.nodes.Count; i++)
            {
                if (selectedLayer.nodes[i].rect.Contains(mousePos))
                {
                    if (!selectedNodesID.Contains(selectedLayer.nodes[i].ID))
                    {
                        selectedNodesID.Clear();
                    }
                    selectedTransition = null;
                    selectedOutputNode = null;
                    selectedInputNode = null;
                    moveNodes = true;
                    startMouseDownPosition = mousePos;
                    selectedNode = selectedLayer.nodes[i];
                    selectedLayer.MoveNodeOnTop(i);
                    return true;
                }
            }

            return false;
        }

        private void ClicOnEmptyField(Vector2 mousePos)
        {
            selectedTransition = null;
            selectedNode = null;
            selectedNodesID.Clear();

            StartSelectingMultipleNodes(mousePos);
        }

        private void StartSelectingMultipleNodes(Vector2 mousePos)
        {
            multipleSelection = true;
            startSelection = mousePos;
            endSelection = mousePos;
            selectionArea.Set(startSelection.x, startSelection.y, 0f, 0f);
        }

        private void EndSelectingMultipleNodes()
        {
            multipleSelection = false;
        }

        private void SelectingMultipleNodes(Vector2 mousePos)
        {
            endSelection = mousePos;
            float selX = startSelection.x < endSelection.x ? startSelection.x : endSelection.x;
            float selY = startSelection.y < endSelection.y ? startSelection.y : endSelection.y;
            float selWidth = Mathf.Abs(endSelection.x - startSelection.x);
            float selHeight = Mathf.Abs(endSelection.y - startSelection.y);
            selectionArea.Set(selX, selY, selWidth, selHeight);

            foreach (MotionMatchingNode n in selectedLayer.nodes)
            {
                if (selectionArea.Contains(n.rect.center) && !selectedNodesID.Contains(n.ID))
                {
                    selectedNodesID.Add(n.ID);
                }
                else if (!selectionArea.Contains(n.rect.center) && selectedNodesID.Contains(n.ID))
                {
                    selectedNodesID.Remove(n.ID);
                }
            }
        }

        private void MakingTransition(Vector2 mousePos)
        {
            if (drawTransitionToMousePos)
            {
                for (int i = 0; i < selectedLayer.nodes.Count; i++)
                {
                    MotionMatchingNode n = selectedLayer.nodes[i];
                    if (n.nodeType == MotionMatchingNodeType.State)
                    {
                        if (n.rect.Contains(mousePos) || n.input.Contains(mousePos) || n.output.Contains(mousePos))
                        {
                            if (selectedInputNode != null)
                            {
                                selectedLayer.MakeTransition(
                                    n.stateIndex,
                                    selectedLayer.nodes[(int)selectedInputNode].stateIndex,
                                    selectedLayer.nodes[(int)selectedInputNode].ID,
                                    selectedLayer.nodes[(int)selectedInputNode].nodeType == MotionMatchingNodeType.Portal
                                    );
                                break;
                            }
                            else if (selectedOutputNode != null)
                            {
                                selectedLayer.MakeTransition(
                                    selectedLayer.nodes[(int)selectedOutputNode].stateIndex,
                                    n.stateIndex,
                                    n.ID,
                                    n.nodeType == MotionMatchingNodeType.Portal
                                    );
                                break;
                            }
                        }
                    }
                    else if (n.nodeType == MotionMatchingNodeType.Portal)
                    {
                        if (n.rect.Contains(mousePos) || n.input.Contains(mousePos))
                        {
                            if (selectedOutputNode != null)
                            {
                                selectedLayer.MakeTransition(
                                    selectedLayer.nodes[(int)selectedOutputNode].stateIndex,
                                    n.stateIndex,
                                    n.ID,
                                    n.nodeType == MotionMatchingNodeType.Portal
                                    );
                                break;
                            }
                        }
                    }
                    else if (n.nodeType == MotionMatchingNodeType.Contact)
                    {
                        if (n.rect.Contains(mousePos) || n.output.Contains(mousePos))
                        {
                            if (selectedInputNode != null)
                            {
                                selectedLayer.MakeTransition(
                                    n.stateIndex,
                                    selectedLayer.nodes[(int)selectedInputNode].stateIndex,
                                    selectedLayer.nodes[(int)selectedInputNode].ID,
                                    selectedLayer.nodes[(int)selectedInputNode].nodeType == MotionMatchingNodeType.Portal
                                    );
                                break;
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
}
