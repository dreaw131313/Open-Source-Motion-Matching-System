using DW_Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DW_Editor
{
    [CustomEditor(typeof(SectionsDependencies))]
    public class SectionsDependenciesEditor : Editor
    {
        private SectionsDependencies data;

        List<string> sectionsNames;

        bool drawRawOption = false;
        private void OnEnable()
        {
            data = (SectionsDependencies)this.target;
        }

        public override void OnInspectorGUI()
        {

            if (sectionsNames == null)
            {
                sectionsNames = new List<string>();
            }
            sectionsNames.Clear();

            for (int i = 1; i < data.sectionSettings.Count; i++)
            {
                sectionsNames.Add(data.sectionSettings[i].name);
            }

            GUILayoutElements.DrawHeader(data.name, GUIResources.GetMediumHeaderStyle_LG());

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Section", GUIResources.Button_MD()))
            {
                data.AddSection();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            for (int i = 0; i < data.sectionSettings.Count; i++)
            {
                if (i == 0)
                {
                    GUILayoutElements.DrawHeader(
                               string.Format("{0}. {1}", i,data.sectionSettings[i].name),
                               GUIResources.GetMediumHeaderStyle_MD()
                               );
                    GUILayout.Space(5);
                    continue;
                }
                GUILayout.BeginHorizontal();
                GUILayoutElements.DrawHeader(
                           data.sectionSettings[i].name,
                           GUIResources.GetMediumHeaderStyle_MD(),
                           GUIResources.GetLightHeaderStyle_MD(),
                           ref data.sectionSettings[i].fold
                           );

                if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(25)))
                {
                    data.sectionSettings.RemoveAt(i);
                    i--;
                    continue;
                }
                GUILayout.EndHorizontal();

                if (data.sectionSettings[i].fold)
                {
                    GUILayout.Space(5);
                    DrawSectionSettings(data.sectionSettings[i], i);
                }
                GUILayout.Space(5);
            }

            GUILayout.Space(10);
            drawRawOption = EditorGUILayout.Toggle("Draw raw options", drawRawOption);

            if (drawRawOption)
            {
                base.OnInspectorGUI();
            }

            if (data != null)
            {
                EditorUtility.SetDirty(data);
            }
        }

        private void DrawSectionSettings(SectionSettings settings, int index)
        {

            data.SetSectionName(EditorGUILayout.TextField("Section name", settings.name), index);

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Cost to section");
            GUILayout.Space(15);
            GUILayout.Label("Cost weight");
            GUILayout.Space(40);
            GUILayout.EndHorizontal();

            for (int i = 0; i < settings.sectionInfos.Count; i++)
            {
                GUILayout.BeginHorizontal();

                settings.SetSectionIndex(
                    i,
                    EditorGUILayout.Popup(settings.sectionInfos[i].GetIndex()-1, sectionsNames.ToArray())+1
                    );


                GUILayout.Space(15);

                settings.SetSectionWeight(
                    i,
                    EditorGUILayout.FloatField(settings.sectionInfos[i].GetWeight())
                    );

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    settings.sectionInfos.RemoveAt(i);
                    i--;
                }
                GUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add section info", GUILayout.MaxWidth(200)))
            {
                settings.AddNewSectionInfo(data.sectionSettings.Count);
            }
        }
    }
}
