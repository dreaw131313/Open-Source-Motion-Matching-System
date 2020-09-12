using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DW_Editor
{
    public static class AnimationSequenceOptions
    {
        public static void DrawSequencesList(DataCreator creator, EditorWindow editor)
        {
            GUILayoutElements.DrawHeader("Sequences", GUIResources.GetDarkHeaderStyle_MD());

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add"))
            {
                creator.sequences.Add(new AnimationsSequence("new Sequence"));
            }
            //if (GUILayout.Button("Clear"))
            //{
            //    creator.sequences.Clear();
            //}
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Find", GUILayout.Width(50));
            creator.findingSequence = EditorGUILayout.TextField(creator.findingSequence);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            for (int i = 0; i < creator.sequences.Count; i++)
            {
                if (creator.findingSequence != "" && !creator.sequences[i].name.ToLower().Contains(creator.findingSequence.ToLower()))
                {
                    continue;
                }
                GUILayout.BeginHorizontal();
                GUILayout.Space(5);
                GUILayout.Label(
                    creator.sequences[i].name,
                     i == creator.selectedSequence ? GUIResources.GetDarkHeaderStyle_SM() : GUIResources.GetLightHeaderStyle_SM()
                    );


                Event e = Event.current;
                Rect r = GUILayoutUtility.GetLastRect();

                if (r.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
                {
                    if (creator.selectedSequence == i)
                    {
                        creator.selectedSequence = -1;
                    }
                    else
                    {
                        creator.selectedSequence = i;
                    }
                    e.Use();
                    editor.Repaint();
                }

                if (GUILayout.Button("Copy", GUILayout.Width(40)))
                {
                    AnimationsSequence info = new AnimationsSequence(creator.sequences[i].name + "_New");

                    for (int j = 0; j < creator.sequences[i].clips.Count; j++)
                    {
                        info.clips.Add(creator.sequences[i].clips[j]);
                        info.neededInfo.Add(creator.sequences[i].neededInfo[j]);
                        info.findPoseInClip.Add(creator.sequences[i].findPoseInClip[j]);
                    }

                    creator.sequences.Insert(i + 1, info);
                }
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    creator.sequences.RemoveAt(i);
                    i--;
                }
                GUILayout.Space(10);
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }
            GUILayout.Space(5);
        }

        public static void DrawSelectedSequence(DataCreator creator, EditorWindow editor, float rectWidth)
        {
            if (creator.selectedSequence == -1 || creator.sequences.Count == 0 || creator.selectedSequence >= creator.sequences.Count)
            {
                creator.selectedSequence = -1;
                GUILayout.Label("No aniamations sequence item is selected");
                return;
            }

            DrawSequence(
                creator,
                creator.sequences[creator.selectedSequence],
                creator.selectedSequence,
                rectWidth
                );
        }

        private static void DrawSequence(DataCreator creator, AnimationsSequence seq, int index, float rectWidth)
        {
            if (seq.findPoseInClip.Count != seq.clips.Count)
            {
                for (int i = 0; i < seq.clips.Count; i++)
                {
                    seq.findPoseInClip.Add(true);
                }
            }
            GUILayoutElements.DrawHeader(
                seq.name,
                GUIResources.GetLightHeaderStyle_MD()
                );

            GUILayout.Space(5);

            seq.name = EditorGUILayout.TextField(
                new GUIContent("Animation sequence name"),
                seq.name
                );

            //seq.loop = EditorGUILayout.Toggle(
            //    new GUIContent("Loop"),
            //    seq.loop
            //    );

            seq.findInYourself = EditorGUILayout.Toggle(new GUIContent("Find in yourself"), seq.findInYourself);
            seq.blendToYourself = EditorGUILayout.Toggle(new GUIContent("Blend to yourself"), seq.blendToYourself);

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add clip"))
            {
                seq.AddClip(null);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            float floatWidth = 60f;
            float buttonWidth = 25f;
            float findPose = 60f;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Animation");
            GUILayout.Label("Find pose", GUILayout.Width(findPose));
            GUILayout.Label("Start", GUILayout.Width(floatWidth));
            GUILayout.Label("End", GUILayout.Width(floatWidth));
            GUILayout.Label("Blend", GUILayout.Width(floatWidth));
            GUILayout.Space(buttonWidth);
            GUILayout.EndHorizontal();
            for (int i = 0; i < seq.clips.Count; i++)
            {
                GUILayout.BeginHorizontal();
                //GUILayout.Label(string.Format("{0}.", i + 1));
                seq.clips[i] = (AnimationClip)EditorGUILayout.ObjectField(
                    seq.clips[i],
                    typeof(AnimationClip),
                    true
                    );

                seq.findPoseInClip[i] = EditorGUILayout.Toggle(seq.findPoseInClip[i], GUILayout.Width(findPose));

                float x = seq.neededInfo[i].x;
                float y = seq.neededInfo[i].y;
                float z = seq.neededInfo[i].z;

                //GUILayout.Label("Start time");
                x = EditorGUILayout.FloatField(x, GUILayout.Width(floatWidth));
                //GUILayout.Label("Blend start time");
                y = EditorGUILayout.FloatField(y, GUILayout.Width(floatWidth));
                //GUILayout.Label("Blend time");
                z = EditorGUILayout.FloatField(z, GUILayout.Width(floatWidth));

                seq.neededInfo[i] = new Vector3(x, y, z);

                if (GUILayout.Button("X", GUILayout.Width(buttonWidth)))
                {
                    seq.RemoveAnimationsAt(i);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            //deltaTimeCaculation = Time.realtimeSinceStartup;

            seq.CalculateLength();
            GUILayout.Label(string.Format("Sequence length: \t {0}", seq.length));


            GUILayout.Space(10);
        }
    }
}
