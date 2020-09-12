using System;
using System.Collections.Generic;
using UnityEngine;

namespace DW_Gameplay
{
    [CreateAssetMenu(fileName = "MM_AnimatorController", menuName = "MotionMatching/MM_AniamtorController")]
    public class MM_AnimatorController : ScriptableObject
    {
        [SerializeField]
        public List<MotionMatchingLayer> layers = new List<MotionMatchingLayer>();
        [SerializeField]
        public List<string> boolNames = new List<string>();
        [SerializeField]
        public List<string> intNames = new List<string>();
        [SerializeField]
        public List<string> floatNames = new List<string>();


        #region Editor fields and methods
#if UNITY_EDITOR

        public MM_AnimatorController()
        {

        }

        public void AddLayer(string name, AvatarMask mask)
        {
            string newName = name;
            int counter = 0;
            if (name == "")
            {
                newName = "New Layer";
                name = newName;
            }
            for (int i = 0; i < this.layers.Count; i++)
            {
                if (layers[i].name == newName)
                {
                    counter++;
                    newName = name + counter.ToString();
                    i = 0;
                }
            }
            int newIndex = this.layers.Count;
            layers.Add(new MotionMatchingLayer(newName, newIndex));
            this.layers[newIndex].avatarMask = mask;
        }

        public void RemoveLayerAt(int index)
        {
            if (index >= layers.Count || index < 0)
            {
                Debug.Log("Can not remove layer");
            }
            if (index == (layers.Count - 1))
            {
                layers.RemoveAt(index);
                return;
            }
            else
            {
                layers.RemoveAt(index);
                for (int i = index; i < layers.Count; i++)
                {
                    layers[i].index = i;
                }
            }
        }

        public bool RemoveLayer(string layerName)
        {
            int index = -1;
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i].name == layerName)
                {
                    index = i;
                    break;
                }
            }
            if (index == -1)
            {
                return false;
            }
            else
            {
                RemoveLayerAt(index);
                return true;
            }
        }

        public void RenameLayer(int layerIndex, string newLayerName)
        {
            string newName = newLayerName;
            int counter = 0;
            for(int i = 0; i < layers.Count; i++)
            {
                if(newLayerName == layers[i].name && i!= layerIndex)
                {
                    counter++;
                    newName = newLayerName + counter.ToString();
                    i = 0;
                }
            }
            layers[layerIndex].name = newName;
        }

        public bool AddBool(string name)
        {
            string currentName = name;
            string newName = currentName;
            int counter = 0;
            for (int i = 0; i < boolNames.Count; i++)
            {
                if (boolNames[i] == newName)
                {
                    counter++;
                    newName = currentName + counter.ToString();
                    i = 0;
                }
            }
            boolNames.Add(newName);
            return true;
        }

        public void AddInt(string name)
        {
            string currentName = name;
            string newName = currentName;
            int counter = 0;
            for (int i = 0; i < intNames.Count; i++)
            {
                if (intNames[i] == newName)
                {
                    counter++;
                    newName = currentName + counter.ToString();
                    i = 0;
                }
            }
            intNames.Add(newName);
        }

        public void AddFloat(string name)
        {
            string currentName = name;
            string newName = currentName;
            int counter = 0;
            for (int i = 0; i < floatNames.Count; i++)
            {
                if (floatNames[i] == newName)
                {
                    counter++;
                    newName = currentName + counter.ToString();
                    i = 0;
                }
            }
            floatNames.Add(newName);
        }
#endif
        #endregion
    }
}
