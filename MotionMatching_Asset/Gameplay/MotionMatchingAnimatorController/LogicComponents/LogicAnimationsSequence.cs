using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace DW_Gameplay
{
    public class LogicAnimationsSequence
    {
        private MotionMatchingData data;
        private int currentClipIndex;
        private float currentClipTime;
        public int dataIndex;
        public AnimationMixerPlayable mixer;

        public LogicAnimationsSequence(MotionMatchingData data, int dataIndex)
        {
            this.data = data;
            this.dataIndex = dataIndex;
        }

        public float GetLength()
        {
            return data.animationLength;
        }

        public float GetTime()
        {
            return 0f;
        }

        public float GetLocalTime()
        {
            int loops = Mathf.FloorToInt(currentClipTime / data.animationLength);

            return currentClipTime - data.animationLength * loops;
        }

        public void Update(MotionMatchingPlayableGraph graph, bool passIK, bool passFootIK, float deltaTime)
        {
            currentClipTime += deltaTime;

            float seqLocalTime = GetLocalTime();

            for (int i = 0; i < data.clips.Count; i++)
            {
                float currentPlayableTime = GetPlayableTimeInSequenceLocalTime(seqLocalTime, i);
                float currentWeight = mixer.GetInputWeight(i);
                float desiredWeight = GetPlayableWeightInPlayableTime(currentPlayableTime, i);

                if (currentWeight <= 0f && desiredWeight > 0f)
                {
                    mixer.GetInput(i).SetTime(currentPlayableTime - Time.deltaTime);
                    mixer.GetInput(i).SetTime(currentPlayableTime);
                }
                mixer.SetInputWeight(i, desiredWeight);
            }
            NormalizeMixerInputWeights();
        }

        public void CreateAnimationsInTime(float time, MotionMatchingPlayableGraph graph, bool passIK, bool passFootIK)
        {
            currentClipTime = time;
            //mixer = AnimationMixerPlayable.Create(graph.graph, 0);
            float seqLocalTime = GetLocalTime();

            for (int i = 0; i < data.clips.Count; i++)
            {
                AnimationClipPlayable playable = AnimationClipPlayable.Create(graph.graph, data.clips[i]);
                playable.SetApplyFootIK(passFootIK);
                playable.SetApplyPlayableIK(passIK);
                mixer.AddInput(playable, 0);

                float currentPlayableTime = GetPlayableTimeInSequenceLocalTime(seqLocalTime, i);
                float currentplayableWeight = GetPlayableWeightInPlayableTime(currentPlayableTime, i);

                mixer.SetInputWeight(i, currentplayableWeight);
                mixer.GetInput(i).SetTime(currentPlayableTime - Time.deltaTime);
                mixer.GetInput(i).SetTime(currentPlayableTime);
            }
            NormalizeMixerInputWeights();
        }

        private float GetPlayableWeightInPlayableTime(float playableTime, int clipIndex)
        {
            float weight = 0f;

            int previewClipIndex;
            if (clipIndex == 0)
            {
                previewClipIndex = data.clips.Count - 1;
            }
            else if (clipIndex == data.clips.Count - 1)
            {
                previewClipIndex = clipIndex - 1;
            }
            else
            {
                previewClipIndex = clipIndex - 1;
            }

            Vector2 interval_1 = new Vector2(
                data.animationSeqInfos[clipIndex].x,
                data.animationSeqInfos[clipIndex].x + data.animationSeqInfos[previewClipIndex].z
                );
            Vector2 interval_2 = new Vector2(
                data.animationSeqInfos[clipIndex].x + data.animationSeqInfos[previewClipIndex].z,
                data.animationSeqInfos[clipIndex].y
                );
            Vector2 interval_3 = new Vector2(
                data.animationSeqInfos[clipIndex].y,
                data.animationSeqInfos[clipIndex].y + data.animationSeqInfos[clipIndex].z
                );

            if (interval_1.x <= playableTime && playableTime < interval_1.y)
            {
                weight = (playableTime - interval_1.x) / data.animationSeqInfos[previewClipIndex].z;
            }
            else if (interval_2.x <= playableTime && playableTime < interval_2.y)
            {
                weight = 1f;
            }
            else if (interval_3.x <= playableTime && playableTime < interval_3.y)
            {
                weight = 1f - (playableTime - interval_3.x) / data.animationSeqInfos[clipIndex].z;
            }

            return weight;
        }

        private float GetPlayableTimeInSequenceLocalTime(float localTime, int clipIndex)
        {
            float time = 0f;

            if (clipIndex == 0)
            {
                time = data.animationSeqInfos[clipIndex].x + localTime;
            }
            else if (clipIndex == data.clips.Count - 1 && localTime <= data.animationSeqInfos[clipIndex].z)
            {
                time = data.animationSeqInfos[clipIndex].y + localTime;
            }
            else
            {
                float deltaTime = 0f;
                for (int i = 0; i < clipIndex; i++)
                {
                    deltaTime += (data.animationSeqInfos[i].y - data.animationSeqInfos[i].x);
                }

                time = data.animationSeqInfos[clipIndex].x + localTime - deltaTime;
            }

            return time;
        }

        private void NormalizeMixerInputWeights()
        {
            float weightSum = 0f;
            for (int i = 0; i < data.clips.Count; i++)
            {
                weightSum += mixer.GetInputWeight(i);
            }

            for (int i = 0; i < data.clips.Count; i++)
            {
                float finalWeight = mixer.GetInputWeight(i) / weightSum;
                mixer.SetInputWeight(i, finalWeight);
            }
        }

    }
}
