using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace DW_Gameplay
{
    public class MotionMatchingPlayableGraph
    {
        private Animator animator;
        private string name;
        public PlayableGraph graph;
        private AnimationLayerMixerPlayable layerMixer;



        public MotionMatchingPlayableGraph(Animator animator, string name)
        {
            this.animator = animator;
            this.name = name;
        }

        public void Start()
        {
            graph = PlayableGraph.Create(name);

            layerMixer = AnimationLayerMixerPlayable.Create(graph, 0);

            AnimationPlayableUtilities.Play(animator, layerMixer, graph);
        }

        public void Update()
        {

        }

        public void OnDestroy()
        {
            if (graph.IsValid())
            {
                graph.Destroy();
            }
        }

        public void SetTimeUpdateMode(DirectorUpdateMode mode)
        {
            graph.SetTimeUpdateMode(mode);
        }

        public void SetLayerAvatarMask(uint layerIndex, AvatarMask mask)
        {
            layerMixer.SetLayerMaskFromAvatarMask(layerIndex, mask);
        }

        public void SetLayerWeight(int layerIndex, float weight)
        {
            layerMixer.SetInputWeight(layerIndex, weight);
        }

        public void SetLayerAdditive(uint layerIndex, bool isAdditive)
        {
            layerMixer.SetLayerAdditive(layerIndex, isAdditive);
        }


        public bool IsLayerAdditive(uint layerIndex)
        {
            return layerMixer.IsLayerAdditive(layerIndex);
        }
        // For Logic layer 
        public void AddLayerPlayable(LogicMotionMatchingLayer layer)
        {
            layer.SetPlayable(AnimationMixerPlayable.Create(this.graph, 0, true));
            layerMixer.AddInput(layer.GetPlayable(), 0, 1f);
        }

        // For logic state
        public void AddStatePlayable(LogicState state, LogicMotionMatchingLayer layer)
        {
            state.SetPlayable(AnimationMixerPlayable.Create(this.graph));
            layer.GetPlayable().AddInput(state.GetPlayable(), 0);
        }

        public static void RemoveZeroWeightsInputs(AnimationMixerPlayable mixer)
        {
            int size = mixer.GetInputCount();
            for (int i = 0; i < size; i++)
            {
                if (mixer.GetInputWeight(i) <= 0)
                {
                    mixer.GetInput(i).Destroy();
                    for (int j = i + 1; j < size; j++)
                    {
                        // double localTime = ((AnimationClipPlayable)mixer.GetInput(j)).GetTime();
                        float _weight = mixer.GetInputWeight(j);
                        Playable clip = mixer.GetInput(j);
                        // clip.SetTime(localTime);
                        mixer.DisconnectInput(j);
                        mixer.ConnectInput(j - 1, clip, 0);
                        mixer.SetInputWeight(j - 1, _weight);
                    }
                    i--;
                    size--;
                    mixer.SetInputCount(size);
                }
            }
        }

        public void AddClipPlayable(
            AnimationMixerPlayable mixer,
            AnimationClip animation,
            bool passIK,
            bool passFootIK = false,
            double localTime = 0d,
            float weight = 0f
            )
        {
            AnimationClipPlayable playable = AnimationClipPlayable.Create(this.graph, animation);
            playable.SetApplyPlayableIK(passIK);
            playable.SetApplyFootIK(passFootIK);
            mixer.AddInput(playable, 0, weight);
            mixer.GetInput(mixer.GetInputCount() - 1).SetTime(localTime);
            mixer.GetInput(mixer.GetInputCount() - 1).SetTime(localTime);
        }

        public void RemoveZeroWeightsInputsAnimations(
            AnimationMixerPlayable mixer,
            List<float> blendingWeights,
            List<float> currentWeights,
            List<int> curretClipsIndex,
            List<LogicAnimationsSequence> animationsSequences
            )
        {
            int size = mixer.GetInputCount();

            for (int i = 0; i < size; i++)
            {
                if (currentWeights[i] <= 0f)
                {
                    int sequenceMixerInputsCount = mixer.GetInput(i).GetInputCount();
                    if (sequenceMixerInputsCount > 0)
                    {
                        for (int mixerInputInputsIndex = 0; mixerInputInputsIndex < sequenceMixerInputsCount; mixerInputInputsIndex++)
                        {
                            mixer.GetInput(i).GetInput(mixerInputInputsIndex).Destroy();
                        }
                        for (int as_Index = 0; as_Index < animationsSequences.Count; as_Index++)
                        {
                            if (curretClipsIndex[i] == animationsSequences[as_Index].dataIndex)
                            {
                                animationsSequences.RemoveAt(as_Index);
                                break;
                            }
                        }
                    }
                    mixer.GetInput(i).Destroy();
                    blendingWeights.RemoveAt(i);
                    curretClipsIndex.RemoveAt(i);
                    currentWeights.RemoveAt(i);
                    for (int j = i + 1; j < size; j++)
                    {
                        // double localTime = ((AnimationClipPlayable)mixer.GetInput(j)).GetTime();
                        float _weight = mixer.GetInputWeight(j);
                        Playable clip = mixer.GetInput(j);
                        // clip.SetTime(localTime);
                        mixer.DisconnectInput(j);
                        mixer.ConnectInput(j - 1, clip, 0);
                        mixer.SetInputWeight(j - 1, _weight);
                    }
                    i--;
                    size--;
                    mixer.SetInputCount(size);
                }
            }
        }

        public void RemoveZeroWeightsInputFromLayer(
            LogicMotionMatchingLayer layer,
            List<int> currentBlendedStates,
            List<float> blendingWeights,
            List<float> currentWeights
            )
        {
            int size = layer.mixer.GetInputCount();

            for (int inputIndex_1 = 0; inputIndex_1 < size; inputIndex_1++)
            {
                float weight = layer.mixer.GetInputWeight(inputIndex_1);
                if (weight <= 0f)
                {
                    int mixerInputs = (layer.mixer.GetInput(inputIndex_1).GetInputCount());
                    if (mixerInputs == 0)
                    {
                        layer.mixer.GetInput(inputIndex_1).Destroy();
                    }
                    else
                    {
                        for (int dataMixerIndex = 0; dataMixerIndex < mixerInputs; dataMixerIndex++)
                        {
                            layer.mixer.GetInput(inputIndex_1).GetInput(dataMixerIndex).Destroy();
                        }
                        layer.mixer.GetInput(inputIndex_1).Destroy();
                    }

                    blendingWeights.RemoveAt(inputIndex_1);
                    currentWeights.RemoveAt(inputIndex_1);

                    layer.logicStates[currentBlendedStates[inputIndex_1]].StateExit();
                    currentBlendedStates.RemoveAt(inputIndex_1);

                    for (int inputIndex_2 = inputIndex_1 + 1; inputIndex_2 < size; inputIndex_2++)
                    {
                        float _weight = layer.mixer.GetInputWeight(inputIndex_2);
                        AnimationMixerPlayable switchedMixer = (AnimationMixerPlayable)layer.mixer.GetInput(inputIndex_2);
                        layer.mixer.DisconnectInput(inputIndex_2);
                        layer.mixer.ConnectInput(inputIndex_2 - 1, switchedMixer, 0);
                        layer.mixer.SetInputWeight(inputIndex_2 - 1, _weight);
                    }
                    inputIndex_1--;
                    size--;
                    layer.mixer.SetInputCount(size);
                }
            }
        }


        public void CreateBlendMotionMatchingAnimation(
            MotionMatchingData data,
            int dataIndex,
            AnimationMixerPlayable stateMixer,
            double localTime,
            float blendTime,
            List<float> blendingSpeeds,
            List<float> currentWeights,
            List<LogicAnimationsSequence> animationsSequences,
            bool passIK,
            bool passFootIK,
            float newInputStartWeight = 0f,
            float minWeightToAchive = 0f,
            float speedMulti = 1f
            )
        {
            if (stateMixer.GetInputCount() > 0)
            {
                if (currentWeights[currentWeights.Count - 1] >= minWeightToAchive)
                {
                    blendingSpeeds[blendingSpeeds.Count - 1] = -(stateMixer.GetInputWeight(stateMixer.GetInputCount() - 1) / blendTime);
                }
            }
            currentWeights.Add(newInputStartWeight);
            blendingSpeeds.Add(1f / blendTime);

            switch (data.dataType)
            {
                case AnimationDataType.SingleAnimation:
                    AnimationClipPlayable playable_SA = AnimationClipPlayable.Create(graph, data.clips[0]);
                    playable_SA.SetApplyPlayableIK(passIK);
                    playable_SA.SetApplyFootIK(passFootIK);
                    playable_SA.SetTime(localTime - Time.deltaTime);
                    playable_SA.SetTime(localTime);
                    playable_SA.SetSpeed(speedMulti);
                    stateMixer.AddInput(playable_SA, 0, newInputStartWeight);
                    break;
                case AnimationDataType.BlendTree:
                    AnimationMixerPlayable mixerPlayable = AnimationMixerPlayable.Create(this.graph);
                    stateMixer.AddInput(mixerPlayable, 0, newInputStartWeight);

                    for (int i = 0; i < data.clips.Count; i++)
                    {
                        AnimationClipPlayable playable_BT = AnimationClipPlayable.Create(this.graph, data.clips[i]);
                        playable_BT.SetApplyPlayableIK(passIK);
                        playable_BT.SetApplyFootIK(passFootIK);
                        playable_BT.SetTime(localTime - Time.deltaTime);
                        playable_BT.SetTime(localTime);
                        playable_BT.SetSpeed(speedMulti);
                        mixerPlayable.AddInput(playable_BT, 0, data.blendTreeWeights[i]);
                    }
                    break;
                case AnimationDataType.AnimationSequence:
                    animationsSequences.Add(new LogicAnimationsSequence(data, dataIndex));
                    int new_ASIndex = animationsSequences.Count - 1;
                    animationsSequences[new_ASIndex].mixer = AnimationMixerPlayable.Create(this.graph);
                    stateMixer.AddInput(animationsSequences[new_ASIndex].mixer, 0, newInputStartWeight);
                    animationsSequences[new_ASIndex].CreateAnimationsInTime((float)localTime, this, passIK, passFootIK);
                    break;
            }
        }

        public void CreateSingleAnimation(
            MotionMatchingData data,
            AnimationMixerPlayable stateMixer,
            double localTime,
            float newInputStartWeight,
            List<LogicAnimationsSequence> animationsSequences,
            bool passIK,
            bool passFootIK,
            float speedMulti = 1f
            )
        {
            switch (data.dataType)
            {
                case AnimationDataType.SingleAnimation:
                    AnimationClipPlayable playable_1 = AnimationClipPlayable.Create(graph, data.clips[0]);
                    playable_1.SetApplyPlayableIK(passIK);
                    playable_1.SetApplyFootIK(passIK);
                    playable_1.SetTime(localTime - Time.deltaTime);
                    playable_1.SetTime(localTime);
                    playable_1.SetSpeed(speedMulti);
                    stateMixer.AddInput(playable_1, 0, newInputStartWeight);
                    break;
                case AnimationDataType.BlendTree:
                    AnimationMixerPlayable mixerPlayable = AnimationMixerPlayable.Create(this.graph);
                    stateMixer.AddInput(mixerPlayable, 0, newInputStartWeight);
                    for (int i = 0; i < data.clips.Count; i++)
                    {
                        AnimationClipPlayable playable_2 = AnimationClipPlayable.Create(this.graph, data.clips[i]);
                        playable_2.SetApplyPlayableIK(passIK);
                        playable_2.SetApplyFootIK(passIK);
                        playable_2.SetTime(localTime - Time.deltaTime);
                        playable_2.SetTime(localTime);
                        playable_2.SetSpeed(speedMulti);
                        mixerPlayable.AddInput(playable_2, 0, data.blendTreeWeights[i]);
                    }
                    break;
                case AnimationDataType.AnimationSequence:
                    animationsSequences.Add(new LogicAnimationsSequence(data, -1));
                    int new_ASIndex = animationsSequences.Count - 1;
                    animationsSequences[new_ASIndex].mixer = AnimationMixerPlayable.Create(this.graph);
                    stateMixer.AddInput(animationsSequences[new_ASIndex].mixer, 0, newInputStartWeight);
                    animationsSequences[new_ASIndex].CreateAnimationsInTime((float)localTime, this, passIK, passFootIK);
                    break;
            }
        }

        public void BlendPlayablesInStateMixer(
            ref AnimationMixerPlayable stateMixer,
            List<float> blendingSpeeds,
            List<float> currentWeights,
            float minWeightToAchive = 0f,
            float blendTime = 0.5f
            )
        {
            int size = stateMixer.GetInputCount();
            float weightSum = 0;
            for (int i = 0; i < size; i++)
            {
                if (i < (size - 1) && minWeightToAchive <= currentWeights[i] && blendingSpeeds[i] > 0)
                {
                    blendingSpeeds[i] = -(currentWeights[i] / blendTime);
                }
                currentWeights[i] = Mathf.Clamp01(currentWeights[i] + (blendingSpeeds[i] * Time.deltaTime));
                weightSum += currentWeights[i];
            }

            for (int i = 0; i < size; i++)
            {
                stateMixer.SetInputWeight(i, currentWeights[i] / weightSum);
            }
        }

        public void SetPlayableInputWeight(
            Playable stateMixer,
            int inputIndex,
            float weight
            )
        {
            stateMixer.SetInputWeight(inputIndex, weight);
        }


    }
}