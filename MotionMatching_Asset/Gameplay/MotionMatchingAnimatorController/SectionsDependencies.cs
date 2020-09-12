using System.Collections.Generic;
using UnityEngine;

namespace DW_Gameplay
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "SectionsDependencies", menuName = "MotionMatching/Sections dependencies")]
    public class SectionsDependencies : ScriptableObject
    {
        [SerializeField]
        public List<SectionSettings> sectionSettings;

        public int SectionsCount { 
            get
            {
                return sectionSettings.Count;
            }
            private set { }
        }

        public SectionsDependencies()
        {
            sectionSettings = new List<SectionSettings>();
            sectionSettings.Add(new SectionSettings("Always"));
        }

        public string GetSectionName(int index)
        {
            return sectionSettings[index].name;
        }

        public void SetSectionName(string newName, int index)
        {
            int occurrenceCounter = 0;

            string currentName = newName;
            for (int i = 0; i < sectionSettings.Count; i++)
            {
                if (currentName == sectionSettings[i].name && i != index)
                {
                    occurrenceCounter++;
                    i = 0;
                    currentName = newName + "_" + occurrenceCounter.ToString();
                }
            }

            sectionSettings[index].name = currentName;
        }

        public bool AddSection()
        {
            if (sectionSettings.Count + 1 > MotionMatchingData.maxSectionsCounts)
            {
                Debug.LogWarning(string.Format("Max number of section is {0}", MotionMatchingData.maxSectionsCounts));
                return false;
            }

            sectionSettings.Add(new SectionSettings("Section"));

            SetSectionName("Section", sectionSettings.Count - 1);

            return true;
        }

        public void RemoveSection(int index)
        {
            sectionSettings.RemoveAt(index);
        }

        public void UpdateSectionDependecesInMMData(MotionMatchingData data)
        {
            int curretSectionsCount = data.sections.Count;

            for (int i = 1; i < this.sectionSettings.Count; i++)
            {
                if (curretSectionsCount == MotionMatchingData.maxSectionsCounts && data.sections[i].sectionName != this.sectionSettings[i].name)
                {
                    data.sections[i].sectionName = this.sectionSettings[i].name;
                }
                else if (i >= data.sections.Count)
                {
                    data.sections.Add(new MM_DataSection(sectionSettings[i].name));   
                }
                else if (data.sections[i].sectionName != this.sectionSettings[i].name)
                {
                    data.sections[i].sectionName = this.sectionSettings[i].name;
                }
            }

        }
    }



    [System.Serializable]
    public class SectionSettings
    {
        [SerializeField]
        public string name;
        [SerializeField]
        public List<SectionInfo> sectionInfos;

#if UNITY_EDITOR
        [SerializeField]
        public bool fold;
#endif


        public SectionSettings(string name)
        {
            this.name = name;
            sectionInfos = new List<SectionInfo>();
        }

        public bool AddNewSectionInfo(int maxInfos)
        {
            if ((sectionInfos.Count + 1) >= maxInfos)
            {
                return false;
            }

            int sectionIndex = 0;

            //for (int i = 0; i < sectionInfos.Count; i++)
            //{
            //    if (sectionIndex == sectionInfos[i].GetIndex())
            //    {
            //        sectionIndex++;
            //    }
            //}

            sectionInfos.Add(new SectionInfo(sectionIndex, 1.0f));

            return true;
        }

        public bool SetSectionIndex(int infoIndex, int newIndex)
        {
            for (int i = 0; i < sectionInfos.Count; i++)
            {
                if (infoIndex != i && newIndex == sectionInfos[i].GetIndex())
                {
                    SectionInfo changedInfo = sectionInfos[i];
                    changedInfo.SetIndex(-1);

                    sectionInfos[i] = changedInfo;
                }
            }

            SectionInfo buffor = sectionInfos[infoIndex];
            buffor.SetIndex(newIndex);

            sectionInfos[infoIndex] = buffor;

            return true;
        }

        public void SetSectionWeight(int infoIndex, float weight)
        {
            SectionInfo buffor = sectionInfos[infoIndex];
            buffor.SetWeight(weight);

            sectionInfos[infoIndex] = buffor;
        }

    }

    [System.Serializable]
    public struct SectionInfo
    {
        [SerializeField]
        public int sectionIndex;
        [SerializeField]
        public float sectionWeight;

        public SectionInfo(int sectionIndex, float sectionWeight)
        {
            this.sectionIndex = sectionIndex;
            this.sectionWeight = sectionWeight;
        }

        public void Set(int sectionIndex, float sectionWeight)
        {
            this.sectionIndex = sectionIndex;
            this.sectionWeight = sectionWeight;
        }

        public void SetIndex(int sectionIndex)
        {
            this.sectionIndex = sectionIndex;
        }

        public void SetWeight(float sectionWeight)
        {
            this.sectionWeight = sectionWeight;
        }

        public int GetIndex()
        {
            return sectionIndex;
        }

        public float GetWeight()
        {
            return sectionWeight;
        }

    }
}


