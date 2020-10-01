using DW_Gameplay;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
//using UnityEngine.Playables;

namespace DW_Editor
{
    public class MotionDataCalculator
    {
        public static MotionMatchingData CalculateNormalData(
            GameObject go,
            PreparingDataPlayableGraph graph,
            AnimationClip clip,
            List<Transform> bonesMask,
            List<Vector2> bonesWeights,
            int sampling,
            bool loop,
            Transform root,
            List<float> trajectoryStepTimes,
            bool blendToYourself,
            bool findInYourself
            )
        {
            if (!graph.IsValid())
            {
                graph.Initialize(go);
            }

            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;

            #region Need floats
            float frameTime = 1f / (float)sampling;
            int numberOfFrames = Mathf.FloorToInt(clip.length / frameTime) + 1;
            #endregion

            MotionMatchingData data = new MotionMatchingData(
                clip,
                sampling,
                clip.name,
                loop,
                clip.length,
                findInYourself,
                blendToYourself,
                AnimationDataType.SingleAnimation
                );
            FrameData frameBuffer;
            BoneData boneBuffer;
            PoseData poseBuffor;
            Trajectory trajectoryBuffor;

            NeedValueToCalculateData[] previuData = new NeedValueToCalculateData[bonesMask.Count];
            NeedValueToCalculateData[] nextData = new NeedValueToCalculateData[bonesMask.Count];

            graph.AddClipPlayable(clip);
            graph.SetMixerInputWeight(0, 1f);
            graph.SetMixerInputTimeInPlace(0, 0f);

            graph.Evaluate(frameTime);


            int frameIndex = 0;
            float currentCheckingTime = 0f;

            for (; frameIndex < numberOfFrames; frameIndex++)
            {
                for (int i = 0; i < bonesMask.Count; i++)
                {
                    previuData[i] = GetValuesFromTransform(bonesMask[i], root);
                }

                graph.Evaluate(frameTime);

                currentCheckingTime = frameIndex * frameTime;

                //Debug.Log((float)animator.GetMixerInputTime(0) - clip.length);

                for (int i = 0; i < bonesMask.Count; i++)
                {
                    nextData[i] = GetValuesFromTransform(bonesMask[i], root);
                }

                poseBuffor = new PoseData(bonesMask.Count);
                for (int i = 0; i < bonesMask.Count; i++)
                {
                    float2 boneWeight = bonesWeights[i];
                    float3 velocity = BoneData.CalculateVelocity(previuData[i].position, nextData[i].position, frameTime);
                    float3 localPosition = previuData[i].position;
                    quaternion orientation = previuData[i].rotation;
                    boneBuffer = new BoneData(localPosition, velocity);
                    poseBuffor.SetBone(boneBuffer, i);
                }

                trajectoryBuffor = new Trajectory(trajectoryStepTimes.Count);

                frameBuffer = new FrameData(
                    frameIndex,
                    currentCheckingTime,
                    trajectoryBuffor,
                    poseBuffor,
                    new FrameSections(true)
                    );
                data.AddFrame(frameBuffer);
            }



            float clipGlobalStart;
            Vector2 clipStartAndStop;
            float recordingClipTime;

            if (trajectoryStepTimes[0] < 0)
            {
                clipGlobalStart = trajectoryStepTimes[0];

                clipStartAndStop = new Vector2(-clipGlobalStart, -clipGlobalStart + clip.length);
            }
            else
            {
                clipGlobalStart = 0;
                clipStartAndStop = new Vector2(0, clip.length);
            }

            if (trajectoryStepTimes[trajectoryStepTimes.Count - 1] > 0)
            {
                recordingClipTime = clipStartAndStop.y + trajectoryStepTimes[trajectoryStepTimes.Count - 1] + 0.1f;
            }
            else
            {
                recordingClipTime = clipStartAndStop.y + 0.1f;
            }

            int samplesPerSecond = 100;
            float deltaTime = 1f / (float)samplesPerSecond;
            int dataCount = Mathf.CeilToInt(recordingClipTime / deltaTime);
            NeedValueToCalculateData[] recordData = new NeedValueToCalculateData[dataCount];

            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;
            graph.SetMixerInputTimeInPlace(0, clipGlobalStart);

            recordData[0] = new NeedValueToCalculateData(
                go.transform.position,
                go.transform.forward,
                go.transform.rotation
                );

            for (int i = 0; i < dataCount; i++)
            {
                graph.Evaluate(deltaTime);
                recordData[i] = new NeedValueToCalculateData(
                    go.transform.position,
                    go.transform.forward,
                    go.transform.rotation
                    );
            }

            //clearing graph from all animations
            graph.ClearMainMixerInput();

            MotionDataCalculator.CalculateTrajectoryPointsFromRecordData(
                data,
                recordData,
                recordingClipTime,
                deltaTime,
                clipStartAndStop,
                trajectoryStepTimes
                );


            data.usedFrameCount = data.numberOfFrames;


            data.trajectoryPointsTimes = new List<float>();

            for (int i = 0; i < trajectoryStepTimes.Count; i++)
            {
                data.trajectoryPointsTimes.Add(trajectoryStepTimes[i]);
            }


            //data.curves.Clear();
            return data;
        }

        public static MotionMatchingData CalculateBlendTreeData(
            string name,
            GameObject go,
            PreparingDataPlayableGraph graph,
            AnimationClip[] clips,
            List<Transform> bonesMask,
            List<Vector2> bonesWeights,
            Transform root,
            List<float> trajectoryStepTimes,
            float[] weightsForClips,
            int sampling,
            bool loop,
            bool blendToYourself,
            bool findInYourself
            )
        {
            if (!graph.IsValid())
            {
                graph.Initialize(go);
            }

            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;

            #region need floats
            float frameTime = 1f / (float)sampling;
            int numberOfFrames = Mathf.FloorToInt(clips[0].length / frameTime) + 1;
            #endregion

            float weightSum = 0f;
            for (int i = 0; i < weightsForClips.Length; i++)
            {
                weightSum += weightsForClips[i];
            }
            for (int i = 0; i < weightsForClips.Length; i++)
            {
                weightsForClips[i] = weightsForClips[i] / weightSum;
            }

            MotionMatchingData data = new MotionMatchingData(
                clips,
                weightsForClips,
                sampling,
                name,
                loop,
                clips[0].length,
                findInYourself,
                blendToYourself,
                AnimationDataType.BlendTree
                );
            FrameData frameBuffer;
            BoneData boneBuffer;
            PoseData poseBuffor;
            Trajectory trajectoryBuffor;

            NeedValueToCalculateData[] previewBoneData = new NeedValueToCalculateData[bonesMask.Count];
            NeedValueToCalculateData[] nextBoneData = new NeedValueToCalculateData[bonesMask.Count];

            for (int i = 0; i < clips.Length; i++)
            {
                graph.AddClipPlayable(clips[i]);
                graph.SetMixerInputTime(i, 0f);
                graph.SetMixerInputWeight(i, weightsForClips[i]);
            }

            graph.Evaluate(frameTime);

            int frameIndex = 0;
            float currentCheckingTime = 0f;

            // FramesCalculation
            for (; frameIndex < numberOfFrames; frameIndex++)
            {
                for (int i = 0; i < bonesMask.Count; i++)
                {
                    previewBoneData[i] = GetValuesFromTransform(bonesMask[i], root);
                }

                graph.Evaluate(frameTime);
                currentCheckingTime = frameIndex * frameTime;

                for (int i = 0; i < bonesMask.Count; i++)
                {
                    nextBoneData[i] = GetValuesFromTransform(bonesMask[i], root);
                }

                poseBuffor = new PoseData(bonesMask.Count);
                for (int i = 0; i < bonesMask.Count; i++)
                {
                    float2 boneWeight = bonesWeights[i];
                    float3 velocity = BoneData.CalculateVelocity(previewBoneData[i].position, nextBoneData[i].position, frameTime);
                    float3 localPosition = previewBoneData[i].position;
                    quaternion orientation = previewBoneData[i].rotation;
                    boneBuffer = new BoneData(localPosition, velocity);
                    poseBuffor.SetBone(boneBuffer, i);
                }

                trajectoryBuffor = new Trajectory(trajectoryStepTimes.Count);

                frameBuffer = new FrameData(
                    frameIndex,
                    currentCheckingTime,
                    trajectoryBuffor,
                    poseBuffor,
                    new FrameSections(true)
                    );
                data.AddFrame(frameBuffer);
            }

            // Trajectory calculations
            float clipGlobalStart;
            Vector2 clipStartAndStop;
            float recordingClipTime;

            if (trajectoryStepTimes[0] < 0)
            {
                clipGlobalStart = trajectoryStepTimes[0];

                clipStartAndStop = new Vector2(-clipGlobalStart, -clipGlobalStart + clips[0].length);
            }
            else
            {
                clipGlobalStart = 0;
                clipStartAndStop = new Vector2(0, clips[0].length);
            }

            if (trajectoryStepTimes[trajectoryStepTimes.Count - 1] > 0)
            {
                recordingClipTime = clipStartAndStop.y + trajectoryStepTimes[trajectoryStepTimes.Count - 1] + 0.1f;
            }
            else
            {
                recordingClipTime = clipStartAndStop.y + 0.1f;
            }

            int samplesPerSecond = 100;
            float deltaTime = 1f / (float)samplesPerSecond;
            int dataCount = Mathf.CeilToInt(recordingClipTime / deltaTime);
            NeedValueToCalculateData[] recordData = new NeedValueToCalculateData[dataCount];

            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;

            for (int i = 0; i < graph.GetMixerInputCount(); i++)
            {
                graph.SetMixerInputTimeInPlace(i, clipGlobalStart);
            }

            recordData[0] = new NeedValueToCalculateData(
                go.transform.position,
                go.transform.forward,
                go.transform.rotation
                );

            for (int i = 0; i < dataCount; i++)
            {
                graph.Evaluate(deltaTime);
                recordData[i] = new NeedValueToCalculateData(
                    go.transform.position,
                    go.transform.forward,
                    go.transform.rotation
                    );
            }

            //clearing graph from all animations
            graph.ClearMainMixerInput();

            MotionDataCalculator.CalculateTrajectoryPointsFromRecordData(
                data,
                recordData,
                recordingClipTime,
                deltaTime,
                clipStartAndStop,
                trajectoryStepTimes
                );

            data.usedFrameCount = data.numberOfFrames;

            data.trajectoryPointsTimes = new List<float>();

            for (int i = 0; i < trajectoryStepTimes.Count; i++)
            {
                data.trajectoryPointsTimes.Add(trajectoryStepTimes[i]);
            }

            return data;
        }

        public static MotionMatchingData CalculateAnimationSequenceData(
            string name,
            AnimationsSequence seq,
            GameObject go,
            PreparingDataPlayableGraph graph,
            List<Transform> bonesMask,
            List<Vector2> bonesWeights,
            int sampling,
            bool loop,
            Transform root,
            List<float> trajectoryStepTimes,
            bool blendToYourself,
            bool findInYourself
            )
        {

            if (!graph.IsValid())
            {
                graph.Initialize(go);
            }

            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;

            seq.CalculateLength();

            #region need floats
            float frameTime = 1f / (float)sampling;
            int numberOfFrames = Mathf.FloorToInt(seq.length / frameTime) + 1;
            #endregion

            MotionMatchingData data = new MotionMatchingData(
                seq.clips.ToArray(),
                seq.neededInfo.ToArray(),
                sampling,
                name,
                loop,
                seq.length,
                findInYourself,
                blendToYourself,
                AnimationDataType.AnimationSequence
                );

            FrameData frameBuffer;
            BoneData boneBuffer;
            PoseData poseBuffor;
            Trajectory trajectoryBuffor;

            NeedValueToCalculateData[] previuData = new NeedValueToCalculateData[bonesMask.Count];
            NeedValueToCalculateData[] nextData = new NeedValueToCalculateData[bonesMask.Count];

            int seqDeltaSampling = 3;
            //seq.CreatePlayableGraph(playableGraph, go);
            //seq.Update(-frameTime, playableGraph, seqDeltaSampling);

            seq.CreateAnimationsInTime(0f, graph);
            graph.Evaluate(frameTime);
            seq.Update(graph, frameTime);

            int frameIndex = 0;

            for (; frameIndex < numberOfFrames; frameIndex++)
            {
                for (int i = 0; i < bonesMask.Count; i++)
                {
                    previuData[i] = GetValuesFromTransform(bonesMask[i], root);
                }

                graph.Evaluate(frameTime);
                seq.Update(graph, frameTime);
                //Debug.Log((float)animator.GetMixerInputTime(0) - clip.length);

                for (int i = 0; i < bonesMask.Count; i++)
                {
                    nextData[i] = GetValuesFromTransform(bonesMask[i], root);
                }

                poseBuffor = new PoseData(bonesMask.Count);
                for (int i = 0; i < bonesMask.Count; i++)
                {
                    float2 boneWeight = bonesWeights[i];
                    float3 velocity = BoneData.CalculateVelocity(previuData[i].position, nextData[i].position, frameTime);
                    float3 localPosition = previuData[i].position;
                    quaternion orientation = previuData[i].rotation;
                    boneBuffer = new BoneData(localPosition, velocity);
                    poseBuffor.SetBone(boneBuffer, i);
                }

                trajectoryBuffor = new Trajectory(trajectoryStepTimes.Count);

                frameBuffer = new FrameData(
                    frameIndex,
                    frameIndex * frameTime,
                    trajectoryBuffor,
                    poseBuffor,
                    new FrameSections(true)
                    );
                data.AddFrame(frameBuffer);
            }


            float clipGlobalStart;
            Vector2 clipStartAndStop;
            float recordingClipTime;

            if (trajectoryStepTimes[0] < 0)
            {
                clipGlobalStart = trajectoryStepTimes[0];

                clipStartAndStop = new Vector2(-clipGlobalStart, -clipGlobalStart + seq.length);
            }
            else
            {
                clipGlobalStart = 0;
                clipStartAndStop = new Vector2(0, seq.length);
            }

            if (trajectoryStepTimes[trajectoryStepTimes.Count - 1] > 0)
            {
                recordingClipTime = clipStartAndStop.y + trajectoryStepTimes[trajectoryStepTimes.Count - 1] + 0.1f;
            }
            else
            {
                recordingClipTime = clipStartAndStop.y + 0.1f;
            }

            int samplesPerSecond = 100;
            float deltaTime = 1f / (float)samplesPerSecond;
            int dataCount = Mathf.CeilToInt(recordingClipTime / deltaTime);

            NeedValueToCalculateData[] recordData = new NeedValueToCalculateData[dataCount];

            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;

            //seq.Update(clipGlobalStart, playableGraph);

            graph.ClearMainMixerInput();

            seq.CreateAnimationsInTime(clipGlobalStart, graph);

            recordData[0] = new NeedValueToCalculateData(
                go.transform.position,
                go.transform.forward,
                go.transform.rotation
                );

            for (int i = 0; i < dataCount; i++)
            {
                graph.Evaluate(deltaTime);
                seq.Update(graph, deltaTime);
                recordData[i] = new NeedValueToCalculateData(
                    go.transform.position,
                    go.transform.forward,
                    go.transform.rotation
                    );
            }

            //clearing graph from all animations
            graph.ClearMainMixerInput();

            MotionDataCalculator.CalculateTrajectoryPointsFromRecordData(
                data,
                recordData,
                recordingClipTime,
                deltaTime,
                clipStartAndStop,
                trajectoryStepTimes
                );

            data.usedFrameCount = data.numberOfFrames;

            data.trajectoryPointsTimes = new List<float>();

            for (int i = 0; i < trajectoryStepTimes.Count; i++)
            {
                data.trajectoryPointsTimes.Add(trajectoryStepTimes[i]);
            }

            return data;
        }

        public static void CalculateTrajectoryPointsFromRecordData(
            MotionMatchingData clip,
            NeedValueToCalculateData[] recordData,
            float recordDataLength,
            float recordStep,
            Vector2 startAndStopOfClip,
            List<float> trajectoryStepTimes
            )
        {
            Matrix4x4 frameMatrix;
            int firstFrameIndex = Mathf.FloorToInt(startAndStopOfClip.x / recordStep);

            //Debug.Log("first frame index "+firstFrameIndex);
            for (int fIndex = 0; fIndex < clip.frames.Count; fIndex++)
            {
                int frameIndexInRecordData = firstFrameIndex + Mathf.FloorToInt(clip.frames[fIndex].localTime / recordStep);
                frameMatrix = Matrix4x4.TRS(
                    recordData[frameIndexInRecordData].position,
                    recordData[frameIndexInRecordData].rotation,
                    Vector3.one
                    );


                FrameData bufforFrame = clip.frames[fIndex];
                // Debug.Log(frameIndexInRecordData);
                for (int i = 0; i < trajectoryStepTimes.Count; i++)
                {
                    int pointIndex = Mathf.FloorToInt(trajectoryStepTimes[i] / recordStep);
                    //Debug.Log(trajectoryStepTimes[i] + " / " + recordStep + " = " + pointIndex);
                    //Debug.Log(frameIndexInRecordData);
                    //Debug.Log(pointIndex);
                    //Debug.Log(frameIndexInRecordData + pointIndex + " < " + recordData.Length);
                    int recordDataIndex = frameIndexInRecordData + pointIndex;
                    recordDataIndex = recordDataIndex < 0 ? 0 : recordDataIndex;
                    NeedValueToCalculateData n = recordData[recordDataIndex];


                    Vector3 pointPos = frameMatrix.inverse.MultiplyPoint3x4(n.position);
                    Vector3 pointVel;
                    if (trajectoryStepTimes[i] < 0)
                    {
                        pointVel = frameMatrix.inverse.MultiplyVector(
                            (recordData[frameIndexInRecordData].position - n.position) / Mathf.Abs(trajectoryStepTimes[i])
                            );
                    }
                    else
                    {
                        pointVel = frameMatrix.inverse.MultiplyVector(
                            (n.position - recordData[frameIndexInRecordData].position) / Mathf.Abs(trajectoryStepTimes[i])
                            );
                    }
                    Vector3 pointOrientation = frameMatrix.inverse.MultiplyVector(n.orientation);
                    bufforFrame.trajectory.SetPoint(
                            new TrajectoryPoint(
                                pointPos,
                                pointVel,
                                pointOrientation
                            ),
                            i
                        );
                }
                clip.frames[fIndex] = bufforFrame;
            }
        }

        public static void CalculateContactPoints(
            MotionMatchingData data,
            MotionMatchingContact[] contactPoints,
            PreparingDataPlayableGraph playableGraph,
            GameObject gameObject
            )
        {
            for (int i = 0; i < data.contactPoints.Count; i++)
            {
                MotionMatchingContact cp = data.contactPoints[i];
                cp.contactNormal = math.normalize(cp.contactNormal);
                data.contactPoints[i] = cp;
            }

            Vector3 startPos = gameObject.transform.position;
            Quaternion startRot = gameObject.transform.rotation;
            float deltaTime = data.frameTime;
            Matrix4x4 frameMatrix;

            NeedValueToCalculateData[] recordedData = new NeedValueToCalculateData[data.numberOfFrames];
            Vector3[] cpPos = new Vector3[contactPoints.Length];
            Vector3[] cpNormals = new Vector3[contactPoints.Length];
            Vector3[] cpForwards = new Vector3[contactPoints.Length];

            if (playableGraph != null)
            {
                playableGraph.Destroy();
            }

            playableGraph = new PreparingDataPlayableGraph();
            playableGraph.Initialize(gameObject);

            playableGraph.CreateAnimationDataPlayables(data);


            // RecordingData
            float currentTime = 0f;
            float currentDeltaTime = deltaTime;
            int contactPointIndex = 0;
            for (int i = 0; i < data.numberOfFrames; i++)
            {
                recordedData[i] = new NeedValueToCalculateData(
                    gameObject.transform.position,
                    gameObject.transform.forward,
                    gameObject.transform.rotation
                    );

                currentTime += deltaTime;
                if (contactPointIndex < contactPoints.Length && currentTime >= contactPoints[contactPointIndex].startTime)
                {
                    float buforDeltaTime = currentTime - contactPoints[contactPointIndex].startTime;
                    currentDeltaTime = deltaTime - buforDeltaTime;

                    playableGraph.EvaluateMotionMatchgData(data, currentDeltaTime);

                    cpPos[contactPointIndex] = gameObject.transform.TransformPoint(contactPoints[contactPointIndex].position);
                    cpNormals[contactPointIndex] = gameObject.transform.TransformDirection(contactPoints[contactPointIndex].contactNormal);
                    cpForwards[contactPointIndex] = gameObject.transform.forward;
                    contactPointIndex++;

                    playableGraph.EvaluateMotionMatchgData(data, buforDeltaTime);

                    currentDeltaTime = deltaTime;
                }
                else
                {
                    playableGraph.EvaluateMotionMatchgData(data, currentDeltaTime);
                }

            }

            // calcualationData
            for (int i = 0; i < data.numberOfFrames; i++)
            {
                frameMatrix = Matrix4x4.TRS(
                    recordedData[i].position,
                    recordedData[i].rotation,
                    Vector3.one
                    );

                FrameData currentFrame = data.frames[i];

                currentFrame.contactPoints = new FrameContact[cpPos.Length];
                for (int j = 0; j < cpPos.Length; j++)
                {
                    Vector3 pos = frameMatrix.inverse.MultiplyPoint3x4(cpPos[j]);
                    Vector3 norDir = frameMatrix.inverse.MultiplyVector(cpNormals[j]);
                    Vector3 forw = frameMatrix.inverse.MultiplyVector(cpForwards[j]);
                    FrameContact cp = new FrameContact(
                        pos,
                        norDir
                        //forw
                        );
                    currentFrame.contactPoints[j] = cp;
                }

                data.frames[i] = currentFrame;
            }

            gameObject.transform.position = startPos;
            gameObject.transform.rotation = startRot;

            if (data.contactPoints.Count >= 2)
            {
                for (int i = 0; i < contactPoints.Length - 1; i++)
                {
                    Vector3 firstPoint = data.GetContactPointInTime(i, data.contactPoints[i].startTime).position;
                    Vector3 secondPoint = data.GetContactPointInTime(i+1, data.contactPoints[i].startTime).position;

                    Vector3 dir = secondPoint - firstPoint;
                    dir.y = 0;

                    MotionMatchingContact c = data.contactPoints[i];
                    c.rotationFromForwardToNextContactDir = Quaternion.FromToRotation(dir, Vector3.forward);
                    data.contactPoints[i] = c;
                }
            }

            if (data.contactPoints.Count >= 2)
            {
                Vector3 firstPoint = data.GetContactPointInTime(0, data.contactPoints[0].startTime).position;
                Vector3 secondPoint = data.GetContactPointInTime(1, data.contactPoints[0].startTime).position;

                Vector3 dir = secondPoint - firstPoint;
                dir.y = 0;
                data.fromFirstToSecondContactRot = Quaternion.FromToRotation(
                    Vector3.ProjectOnPlane(dir, Vector3.up), 
                    Vector3.forward
                    );
            }
            else
            {
                data.fromFirstToSecondContactRot = Quaternion.identity;
            }

            playableGraph.ClearMainMixerInput();
            playableGraph.Destroy();
        }

        public static void CalculateImpactPoints(
            MotionMatchingData data,
            MotionMatchingContact[] contactPoints,
            PreparingDataPlayableGraph playableGraph,
            GameObject gameObject
            )
        {
            // Normalizacja kierunków kontaktów
            for (int i = 0; i < data.contactPoints.Count; i++)
            {
                MotionMatchingContact cp = data.contactPoints[i];
                cp.contactNormal = math.normalize(cp.contactNormal);
                data.contactPoints[i] = cp;
            }

            // Pobrani początkowych wartości game objectu
            Vector3 startPos = gameObject.transform.position;
            Quaternion startRot = gameObject.transform.rotation;


            float deltaTime = data.frameTime;
            Matrix4x4 frameMatrix;


            NeedValueToCalculateData[] recordedData = new NeedValueToCalculateData[data.numberOfFrames];
            Vector3[] cpPos = new Vector3[contactPoints.Length];
            Vector3[] cpNormals = new Vector3[contactPoints.Length];
            Vector3[] cpForwards = new Vector3[contactPoints.Length];

            if (playableGraph != null)
            {
                playableGraph.Destroy();
            }

            playableGraph = new PreparingDataPlayableGraph();
            playableGraph.Initialize(gameObject);

            playableGraph.CreateAnimationDataPlayables(data);


            // RecordingData
            float currentTime = 0f;
            float currentDeltaTime = deltaTime;
            int contactPointIndex = 0;
            for (int i = 0; i < data.numberOfFrames; i++)
            {
                recordedData[i] = new NeedValueToCalculateData(
                    gameObject.transform.position,
                    gameObject.transform.forward,
                    gameObject.transform.rotation
                    );

                currentTime += deltaTime;
                if (contactPointIndex < contactPoints.Length && currentTime >= contactPoints[contactPointIndex].startTime)
                {
                    float buforDeltaTime = currentTime - contactPoints[contactPointIndex].startTime;
                    currentDeltaTime = deltaTime - buforDeltaTime;

                    playableGraph.EvaluateMotionMatchgData(data, currentDeltaTime);

                    cpPos[contactPointIndex] = gameObject.transform.TransformPoint(contactPoints[contactPointIndex].position);
                    cpNormals[contactPointIndex] = gameObject.transform.TransformDirection(contactPoints[contactPointIndex].contactNormal);
                    cpForwards[contactPointIndex] = gameObject.transform.forward;
                    contactPointIndex++;

                    playableGraph.EvaluateMotionMatchgData(data, buforDeltaTime);

                    currentDeltaTime = deltaTime;
                }
                else
                {
                    playableGraph.EvaluateMotionMatchgData(data, currentDeltaTime);
                }

            }

            // calcualationData
            for (int i = 0; i < data.numberOfFrames; i++)
            {
                frameMatrix = Matrix4x4.TRS(
                    recordedData[i].position,
                    recordedData[i].rotation,
                    Vector3.one
                    );

                FrameData currentFrame = data.frames[i];

                for (int impactIndex = 0; impactIndex < data.contactPoints.Count; impactIndex++)
                {
                    if (data.contactPoints[impactIndex].IsContactInTime(currentFrame.localTime))
                    {
                        currentFrame.contactPoints = new FrameContact[1];
                        Vector3 pos = frameMatrix.inverse.MultiplyPoint3x4(cpPos[impactIndex]);
                        Vector3 norDir = frameMatrix.inverse.MultiplyVector(cpNormals[impactIndex]);
                        Vector3 forw = frameMatrix.inverse.MultiplyVector(cpForwards[impactIndex]);
                        FrameContact cp = new FrameContact(
                            pos,
                            norDir
                            //forw
                            );
                        currentFrame.contactPoints[0] = cp;
                        break;
                    }
                    else
                    {
                        currentFrame.contactPoints = new FrameContact[0];
                    }
                }
                if (data.contactPoints.Count == 0)
                {
                    currentFrame.contactPoints = new FrameContact[0];
                }
                data.frames[i] = currentFrame;
            }

            gameObject.transform.position = startPos;
            gameObject.transform.rotation = startRot;

            //if (data.contactPoints.Count >= 2)
            //{
            //    Vector3 firstPoint = data.GetContactPoint(0, data.contactPoints[0].startTime).position;
            //    Vector3 secondPoint = data.GetContactPoint(1, data.contactPoints[0].startTime).position;

            //    Vector3 dir = secondPoint - firstPoint;
            //    dir.y = 0;
            //    data.fromFirstToSecondContactRot = Quaternion.FromToRotation(dir, Vector3.forward);
            //}
            //else
            //{
            //    data.fromFirstToSecondContactRot = Quaternion.identity;
            //}

            playableGraph.ClearMainMixerInput();
            playableGraph.Destroy();
        }


        public static NeedValueToCalculateData GetValuesFromTransform(Transform t, Transform root)
        {
            return new NeedValueToCalculateData(root.InverseTransformPoint(t.position), root.InverseTransformDirection(t.forward), t.localRotation);
        }
    }

    public class NeedValueToCalculateData
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 orientation;
        public Quaternion rotation;



        public NeedValueToCalculateData(Vector3 pos, Vector3 orientation, Quaternion rot)
        {
            this.position = pos;
            this.rotation = rot;
            this.orientation = orientation;
        }

        public NeedValueToCalculateData(Vector3 pos, Vector3 velocity, Vector3 orientation, Quaternion rot)
        {
            this.position = pos;
            this.velocity = velocity;
            this.orientation = orientation;
            this.rotation = rot;
        }
    }
}
