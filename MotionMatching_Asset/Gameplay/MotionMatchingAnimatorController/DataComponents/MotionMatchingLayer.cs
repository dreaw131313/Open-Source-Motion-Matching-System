using System.Collections.Generic;
using UnityEngine;

namespace DW_Gameplay
{
    [System.Serializable]
    public class MotionMatchingLayer
    {
        // Serialized values
        [SerializeField]
        public string name;
        [SerializeField]
        public int index;
        [SerializeField]
        public AvatarMask avatarMask = null;
        [SerializeField]
        public int startStateIndex = -1;
        [SerializeField]
        public bool passIK;
        [SerializeField]
        public bool footPassIK = false;
        [SerializeField]
        public bool isAdditive = false;
        [SerializeField]
        public List<MotionMatchingState> states = new List<MotionMatchingState>();

        public MotionMatchingLayer(string name, int index)
        {
            this.name = name;
            this.index = index;

#if UNITY_EDITOR
            fold = true;
            zoom = 1f;
#endif
        }

        public MotionMatchingState GetStateWithName(string name)
        {
            for (int stateIndex = 0; stateIndex < states.Count; stateIndex++)
            {
                if (states[stateIndex].GetName() == name)
                {
                    return states[stateIndex];
                }
            }
            return null;
        }

        public string GetName()
        {
            return name;
        }


#if UNITY_EDITOR
        [SerializeField]
        public bool fold;
        [SerializeField]
        public float zoom;
        [SerializeField]
        public List<MotionMatchingNode> nodes = new List<MotionMatchingNode>();

        private static float nodeH = 80f;
        private static float nodeW = 250f;

        // Adding and removing state
        public void AddState(
            string stateName,
            MotionMatchingStateType type,
            int stateID,
            Vector2 nodePosition
            )
        {
            string newName = stateName;
            int counter = 0;
            if (states == null)
            {
                states = new List<MotionMatchingState>();
            }
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].GetName() == newName)
                {
                    counter++;
                    newName = stateName + counter.ToString();
                    i = 0;
                }
            }
            int stateIndex = states.Count;
            states.Add(new MotionMatchingState(newName, type, stateIndex, stateID));
            if (nodes == null)
            {
                nodes = new List<MotionMatchingNode>();
            }
            string title = "";
            switch (type)
            {
                case MotionMatchingStateType.MotionMatching:
                    title = "Motion Matching:";
                    break;
                case MotionMatchingStateType.SingleAnimation:
                    title = "Single Animation:";
                    break;
                case MotionMatchingStateType.ContactAnimationState:
                    title = "Contact State:";
                    break;
            }
            nodes.Add(new MotionMatchingNode(
                new Rect(nodePosition, new Vector2(nodeW, nodeH)),
                type == MotionMatchingStateType.ContactAnimationState ? MotionMatchingNodeType.Contact : MotionMatchingNodeType.State,
                title,
                stateID,
                stateIndex
                ));
        }

        public bool RenameState(int stateIndex, string newName)
        {
            if (0 > stateIndex || stateIndex >= states.Count)
            {
                return false;
            }
            string name = newName;
            int counter = 0;
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].GetName() == newName && states[i].GetIndex() != stateIndex)
                {
                    counter++;
                    name += newName + counter.ToString();
                    i = 0;
                }
            }

            states[stateIndex].SetStateName(name);

            return true;
        }

        public bool RenameState(string stateName, string newName)
        {
            for (int i = 0; i < states.Count; i++)
            {
                if (stateName == states[i].GetName())
                {
                    return RenameState(i, newName);
                }
            }
            return false;
        }

        private void RemoveStateAndClear(int stateIndex)
        {
            foreach (MotionMatchingState state in states)
            {
                if (state.GetIndex() == stateIndex)
                {
                    continue;
                }
                for (int i = 0; i < state.transitions.Count; i++)
                {
                    if (state.transitions[i].nextStateIndex == stateIndex)
                    {
                        state.transitions.RemoveAt(i);
                        i--;
                    }
                    else if (state.transitions[i].nextStateIndex > stateIndex)
                    {
                        state.transitions[i].nextStateIndex -= 1;
                    }
                }
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].stateIndex == stateIndex)
                {
                    nodes.RemoveAt(i);
                    i--;
                }
            }
            states.RemoveAt(stateIndex);
        }

        private void RemakeStateIndexes(int removedState)
        {
            for (int i = 0; i < states.Count; i++)
            {
                if(states[i].GetIndex() == this.startStateIndex)
                {
                    this.startStateIndex = i;
                }
                states[i].SetIndex(i);
                for (int j = 0; j < nodes.Count; j++)
                {
                    if (nodes[j].ID == states[i].nodeID)
                    {
                        nodes[j].stateIndex = i;
                    }
                }

                foreach (Transition t in states[i].transitions)
                {
                    t.fromStateIndex = i;
                }
            }
        }

        public bool RemoveState(int stateIndex)
        {
            if (0 <= stateIndex && stateIndex < states.Count)
            {
                RemoveStateAndClear(stateIndex);
                RemakeStateIndexes(stateIndex);

                if (startStateIndex == stateIndex)
                {
                    startStateIndex = GetIndexOfFirstStateDiffrentType(MotionMatchingStateType.ContactAnimationState);
                }
                return true;
            }

            return false;
        }

        public bool RemoveState(string stateName)
        {
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].GetName() == stateName)
                {
                    return RemoveState(i);
                }
            }

            return false;
        }

        public void ClearStateAnimationData(int stateIndex, int groupIndex)
        {
            states[stateIndex].ClearMotionMatchingData(states[stateIndex].motionDataGroups[groupIndex]);
            foreach (MotionMatchingState s in states)
            {
                states[stateIndex].UpdateTransitions(s);
            }
        }

        public void AddAnimationDataToState(MotionMatchingData[] data, int stateIndex, int stateGroup)
        {
            states[stateIndex].AddMotionMatchingData(data, states[stateIndex].motionDataGroups[stateGroup]);
            foreach (MotionMatchingState s in states)
            {
                states[stateIndex].UpdateTransitions(s);
            }
        }

        public void RemoveAniamtionDataFromState(int dataIndex, int stateIndex, int groupIndex)
        {
            states[stateIndex].RemoveMotionMatchingData(dataIndex, states[stateIndex].motionDataGroups[groupIndex]);
            foreach (MotionMatchingState s in states)
            {
                states[stateIndex].UpdateTransitions(s, dataIndex);
            }
        }

        public string GetStateName(int index)
        {
            return states[index].GetName();
        }

        public MotionMatchingStateType GetStateType(int index)
        {
            return states[index].GetStateType();
        }

        // Making and removing transition between states
        public bool MakeTransition(int fromStateIndex, int toStateIndex, int nodeID, bool isPortal)
        {
            if (fromStateIndex >= 0 &&
                fromStateIndex < states.Count &&
                toStateIndex >= 0 &&
                toStateIndex < states.Count &&
                fromStateIndex != toStateIndex)
            {
                foreach (Transition t in states[fromStateIndex].transitions)
                {
                    if (t.nextStateIndex == toStateIndex)
                    {
                        //Debug.Log("cos nie tak");
                        return false;
                    }
                }
                if (!isPortal)
                {
                    foreach (Transition t in states[toStateIndex].transitions)
                    {
                        if (t.nextStateIndex == fromStateIndex && !t.toPortal)
                        {
                            //Debug.Log("cos nie tak 2");
                            return false;
                        }
                    }
                }
                states[fromStateIndex].AddTransition(states[toStateIndex], nodeID, isPortal);
                return true;
            }
            return false;
        }

        public bool MakeTransition(string fromState, string toState, int stateID, bool isPortal)
        {
            int f = -1;
            int t = -1;
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].GetName() == fromState)
                {
                    f = i;
                }
                if (states[i].GetName() == toState)
                {
                    t = i;
                }
            }

            return MakeTransition(f, t, stateID, isPortal);
        }

        public bool RemoveTransition(int fromStateIndex, int toStateIndex)
        {
            if (fromStateIndex >= 0 && fromStateIndex < states.Count && toStateIndex >= 0 && toStateIndex < states.Count)
            {
                for (int i = 0; i < states[fromStateIndex].transitions.Count; i++)
                {
                    if (states[fromStateIndex].transitions[i].nextStateIndex == toStateIndex)
                    {
                        states[fromStateIndex].transitions.RemoveAt(i);
                        i--;
                    }
                }

                return true;
            }
            return false;
        }

        public bool RemoveTransition(string fromState, string toState)
        {
            int f = -1;
            int t = -1;
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].GetName() == fromState)
                {
                    f = i;
                }
                if (states[i].GetName() == toState)
                {
                    t = i;
                }
            }

            return RemoveTransition(f, t);
        }

        public void UpdateTransitionOptions(int stateIndex)
        {
            if (states[stateIndex].GetStateType() == MotionMatchingStateType.SingleAnimation)
            {
                foreach (MotionMatchingState state in states)
                {
                    if (state.GetIndex() == stateIndex)
                    {
                        continue;
                    }

                    state.UpdateTransitions(state);
                }
            }
        }

        public void UpdateAllTransitionsOptions()
        {
            for (int i = 0; i < states.Count; i++)
            {
                UpdateTransitionOptions(i);
            }
        }

        // Portal state
        public void AddPortal(Vector2 nodePosition, int ID)
        {
            nodes.Add(new MotionMatchingNode(
                new Rect(nodePosition, new Vector2(nodeW, nodeH)),
                MotionMatchingNodeType.Portal,
                "Portal:",
                ID,
                -1
                ));
        }

        public bool SetPortalState(int portalNodeIndex, int portalStateIndex)
        {
            if (nodes[portalNodeIndex].nodeType != MotionMatchingNodeType.Portal)
            {
                return false;
            }

            nodes[portalNodeIndex].stateIndex = portalStateIndex;

            foreach (MotionMatchingState state in states)
            {
                bool updateT = false;
                foreach (Transition t in state.transitions)
                {
                    if (t.toPortal && t.nodeID == nodes[portalNodeIndex].ID)
                    {
                        t.nextStateIndex = portalStateIndex;
                        updateT = true;
                        break;
                    }
                }
                if (updateT)
                {
                    state.UpdateTransitions();
                }
            }

            return false;
        }

        public bool SetPortalState2(int portalNodeID, int portalStateIndex)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].ID == portalNodeID)
                {
                    SetPortalState(i, portalStateIndex);
                    return true;
                }
            }

            return false;
        }

        public bool RemovePortal(int portalNodeID)
        {
            int portalIndex = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (portalNodeID == nodes[i].ID)
                {
                    portalIndex = i;
                    break;
                }
            }
            if (nodes[portalIndex].nodeType != MotionMatchingNodeType.Portal)
            {
                return false;
            }
            foreach (MotionMatchingState s in states)
            {
                for (int i = 0; i < s.transitions.Count; i++)
                {
                    if (s.transitions[i].nodeID == nodes[portalIndex].ID)
                    {
                        s.transitions.RemoveAt(i);
                        i--;
                    }
                }
            }

            nodes.RemoveAt(portalIndex);
            return true;
        }

        public int GetMaxNodeID()
        {
            int maxID = 0;
            foreach (MotionMatchingNode n in nodes)
            {
                if (n.ID > maxID)
                {
                    maxID = n.ID;
                }
            }
            return maxID;
        }

        // Setting Layer options
        public bool SetStartState(int startStateIndex)
        {
            if (startStateIndex == this.startStateIndex ||
                startStateIndex < 0 ||
                startStateIndex >= states.Count)
            {
                return false;
            }

            this.startStateIndex = startStateIndex;

            return false;
        }

        public void MoveNodeOnTop(int index)
        {
            nodes.Add(nodes[index]);
            nodes.RemoveAt(index);
        }

        public int GetStateIndex(string stateName)
        {
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].GetName() == stateName)
                {
                    return i;
                }
            }
            return -1;
        }

        public int GetIndexOfFirstStateType(MotionMatchingStateType type)
        {
            for(int i = 0; i < this.states.Count; i++)
            {
                if(this.states[i].GetStateType() == type)
                {
                    return i;
                }
            }
            return -1;
        }

        public int GetIndexOfFirstStateDiffrentType(MotionMatchingStateType type)
        {
            for (int i = 0; i < this.states.Count; i++)
            {
                if (this.states[i].GetStateType() != type)
                {
                    return i;
                }
            }
            return -1;
        }

        public void SetNodeTitle(int nodeID, string title)
        {
            for(int i = 0; i < nodes.Count; i++)
            {
                if(nodes[i].ID == nodeID)
                {
                    nodes[i].title = title;
                    break;
                }
            }
        }

#endif
    }

    public enum MotionMatchingNodeType
    {
        State,
        Portal,
        Contact,

    }

    [System.Serializable]
    public class MotionMatchingNode
    {
        [SerializeField]
        public Rect rect;
        [SerializeField]
        public Rect input;
        [SerializeField]
        public Rect output;
        [SerializeField]
        public MotionMatchingNodeType nodeType;
        [SerializeField]
        public int ID;
        [SerializeField]
        public int stateIndex;
        [SerializeField]
        public string title;

        public static float pointsW = 15f;
        public static float pointsH = 25f;
        public static float pointsMoveFactor = 0.5f;

        public MotionMatchingNode(Rect rect, MotionMatchingNodeType nodeType, string title, int nodeID, int stateIndex)
        {

            this.rect = rect;
            this.nodeType = nodeType;
            this.ID = nodeID;
            this.stateIndex = stateIndex;
            input = new Rect(
                    rect.x - pointsW + 0.5f * pointsW,
                    rect.y + rect.height / 2f - pointsH / 2f,
                    pointsW,
                    pointsH
                );
            output = new Rect(
                    rect.x + rect.width - 0.5f * pointsW,
                    rect.y + rect.height / 2f - pointsH / 2f,
                    pointsW,
                    pointsH
                );
            this.title = title;
        }

        public void Move(Vector2 delta)
        {
            rect.position += delta;
            input.Set(
                    rect.x - pointsW + 0.5f * pointsW,
                    rect.y + rect.height / 2f - pointsH / 2f,
                    pointsW,
                    pointsH
                );
            output.Set(
                    rect.x + rect.width - 0.5f * pointsW,
                    rect.y + rect.height / 2f - pointsH / 2f,
                    pointsW,
                    pointsH
                );
        }

        public void Draw(
            bool startNode,
            bool isSelected,
            string name,
            GUIStyle selected,
            GUIStyle start,
            GUIStyle normal,
            GUIStyle portal,
            GUIStyle contact,
            GUIStyle inputS,
            GUIStyle outputS,
            GUIStyle textStyle
            )
        {
            if (isSelected)
            {
                float incresing = 3f;
                Rect selectionRect = new Rect(
                    this.rect.x - incresing,
                    this.rect.y - incresing,
                    this.rect.width + 2 * incresing,
                    this.rect.height + 2 * incresing
                    );
                GUI.Box(selectionRect, "", selected);
            }

            switch (nodeType)
            {
                case MotionMatchingNodeType.State:
                    GUI.Box(this.input, "", inputS);
                    GUI.Box(this.output, "", outputS);

                    GUIStyle stateStyle;
                    if (startNode)
                    {
                        stateStyle = start;
                    }
                    else
                    {
                        stateStyle = normal;
                    }
                    GUILayout.BeginArea(this.rect, stateStyle);
                    GUILayout.Space(rect.height / 6f);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(title, textStyle);
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(name, textStyle);
                    GUILayout.EndHorizontal();
                    GUILayout.EndArea();
                    break;
                case MotionMatchingNodeType.Portal:
                    GUI.Box(this.input, "", inputS);
                    GUILayout.BeginArea(this.rect, portal);
                    GUILayout.Space(rect.height / 6f);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(title, textStyle);
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(name, textStyle);
                    GUILayout.EndHorizontal();
                    GUILayout.EndArea();
                    break;
                case MotionMatchingNodeType.Contact:
                    GUI.Box(this.output, "", outputS);
                    GUILayout.BeginArea(this.rect, contact);
                    GUILayout.Space(rect.height / 6f);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(title, textStyle);
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(name, textStyle);
                    GUILayout.EndHorizontal();
                    GUILayout.EndArea();
                    break;
            }

        }
    }
}
