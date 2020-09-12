using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace DW_Editor
{
    [System.Serializable]
    public class AnimationsSequence
    {
        [SerializeField]
        public string name;
        [SerializeField]
        public List<AnimationClip> clips;
        [SerializeField]
        public List<Vector3> neededInfo;
        [SerializeField]
        public List<bool> findPoseInClip;
        [SerializeField]
        public bool loop;
        [SerializeField]
        public bool fold;
        [SerializeField]
        public float length;
        [SerializeField]
        public bool findInYourself = true;
        [SerializeField]
        public bool blendToYourself = false;

        public float currentPlayingTime = 0f;


        private int currentClipIndex;
        private int previouClipIndex;
        private float currentClipTime;

        public AnimationsSequence(string name)
        {
            this.name = name;
            clips = new List<AnimationClip>();
            neededInfo = new List<Vector3>();
            findPoseInClip = new List<bool>();
            loop = false;
        }

        public void AddClip(AnimationClip animation)
        {
            float blendTime = 0.2f;
            clips.Add(animation);
            neededInfo.Add(new Vector3(0f, 0f, blendTime));
            findPoseInClip.Add(true);
        }

        public void RemoveAnimationsAt(int index)
        {
            clips.RemoveAt(index);
            neededInfo.RemoveAt(index);
            findPoseInClip.RemoveAt(index);
        }

        public void ClearAnimations()
        {
            clips.Clear();
            neededInfo.Clear();
            findPoseInClip.Clear();
        }

        public void CalculateLength()
        {
            length = 0f;
            for (int i = 0; i < clips.Count; i++)
            {
                length += (neededInfo[i].y - neededInfo[i].x);
            }
        }


        public bool IsValid()
        {
            if (clips.Count != neededInfo.Count || clips.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < clips.Count; i++)
            {
                if (clips[i] == null)
                {
                    Debug.LogWarning(string.Format("Animation on place {0} in animation sequence {1} is null", i, this.name));
                    return false;
                }
            }

            return true;
        }

        public float GetLocalTime()
        {
            int loops = Mathf.FloorToInt(currentClipTime / length);

            return currentClipTime - length * loops;
        }

        public void Update(PreparingDataPlayableGraph graph, float deltaTime)
        {
            currentClipTime += deltaTime;

            float seqLocalTime = GetLocalTime();

            for (int i = 0; i < clips.Count; i++)
            {
                float currentPlayableTime = GetPlayableTimeInSequenceLocalTime(seqLocalTime, i);
                float currentWeight = graph.mixer.GetInputWeight(i);
                float desiredWeight = GetPlayableWeightInPlayableTime(currentPlayableTime, i);

                if (currentWeight <= 0f && desiredWeight > 0f)
                {
                    graph.mixer.GetInput(i).SetTime(currentPlayableTime - Time.deltaTime);
                    graph.mixer.GetInput(i).SetTime(currentPlayableTime);
                }
                graph.mixer.SetInputWeight(i, desiredWeight);
            }
            NormalizeMixerInputWeights(graph);
        }

        public void CreateAnimationsInTime(float time, PreparingDataPlayableGraph graph)
        {
            currentClipTime = time;
            //mixer = AnimationMixerPlayable.Create(graph.graph, 0);
            float seqLocalTime = GetLocalTime();

            for (int i = 0; i < clips.Count; i++)
            {
                AnimationClipPlayable playable = AnimationClipPlayable.Create(graph.graph, clips[i]);
                graph.mixer.AddInput(playable, 0);

                float currentPlayableTime = GetPlayableTimeInSequenceLocalTime(seqLocalTime, i);
                float currentplayableWeight = GetPlayableWeightInPlayableTime(currentPlayableTime, i);

                graph.mixer.SetInputWeight(i, currentplayableWeight);
                graph.mixer.GetInput(i).SetTime(currentPlayableTime - Time.deltaTime);
                graph.mixer.GetInput(i).SetTime(currentPlayableTime);
            }
            NormalizeMixerInputWeights(graph);
        }

        private float GetPlayableWeightInPlayableTime(float playableTime, int clipIndex)
        {
            float weight = 0f;

            int previewClipIndex;
            if (clipIndex == 0)
            {
                previewClipIndex = clips.Count - 1;
            }
            else if (clipIndex == clips.Count - 1)
            {
                previewClipIndex = clipIndex - 1;
            }
            else
            {
                previewClipIndex = clipIndex - 1;
            }

            Vector2 interval_1 = new Vector2(
                neededInfo[clipIndex].x,
                neededInfo[clipIndex].x + neededInfo[previewClipIndex].z
                );
            Vector2 interval_2 = new Vector2(
                neededInfo[clipIndex].x + neededInfo[previewClipIndex].z,
                neededInfo[clipIndex].y
                );
            Vector2 interval_3 = new Vector2(
                neededInfo[clipIndex].y,
                neededInfo[clipIndex].y + neededInfo[clipIndex].z
                );

            if (interval_1.x <= playableTime && playableTime < interval_1.y)
            {
                weight = (playableTime - interval_1.x) / neededInfo[previewClipIndex].z;
            }
            else if (interval_2.x <= playableTime && playableTime < interval_2.y)
            {
                weight = 1f;
            }
            else if (interval_3.x <= playableTime && playableTime < interval_3.y)
            {
                weight = 1f - (playableTime - interval_3.x) / neededInfo[clipIndex].z;
            }

            return weight;
        }

        private float GetPlayableTimeInSequenceLocalTime(float localTime, int clipIndex)
        {
            float time = 0f;

            if (clipIndex == 0)
            {
                time = neededInfo[clipIndex].x + localTime;
            }
            else if (clipIndex == clips.Count - 1 && localTime <= neededInfo[clipIndex].z)
            {
                time = neededInfo[clipIndex].y + localTime;
            }
            else
            {
                float deltaTime = 0f;
                for (int i = 0; i < clipIndex; i++)
                {
                    deltaTime += (neededInfo[i].y - neededInfo[i].x);
                }

                time = neededInfo[clipIndex].x + localTime - deltaTime;
            }

            return time;
        }

        private void NormalizeMixerInputWeights(PreparingDataPlayableGraph graph)
        {
            float weightSum = 0f;
            for (int i = 0; i < clips.Count; i++)
            {
                weightSum += graph.mixer.GetInputWeight(i);
            }

            for (int i = 0; i < clips.Count; i++)
            {
                float finalWeight = graph.mixer.GetInputWeight(i) / weightSum;
                graph.mixer.SetInputWeight(i, finalWeight);
            }
        }
    }
}
