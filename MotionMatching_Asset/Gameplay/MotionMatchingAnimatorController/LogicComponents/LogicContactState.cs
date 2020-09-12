using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace DW_Gameplay
{

    public class LogicContactState : LogicState
    {
        private ContactStateMovemetType contactType = ContactStateMovemetType.StartContact;
        private ContactPointCostType contactCost;

        private int targetContactPointIndex;
        private bool gettingAdaptedPoints = true;

        Quaternion startRotation;
        float degreeSpeed;

        float startTime;
        Vector3 previewPos;

        private MotionMatchingContactEnterJob[] csJobs; // contact state job

        public LogicContactState(
            MotionMatchingState state,
            MotionMatching component,
            MotionMatchingPlayableGraph playableGraph,
            LogicMotionMatchingLayer logicLayer,
            Transform gameObject,
            int framesForJob
            ) :
            base(state, component, playableGraph, logicLayer, gameObject, framesForJob)
        {
            OnCreate();
        }


        private void OnCreate()
        {
            currentMotionDataGroupIndex = 0;

            this.contactType = dataState.csFeatures.contactMovementType;
            this.contactCost = dataState.csFeatures.contactCostType;
            csJobs = new MotionMatchingContactEnterJob[dataState.maxJobsCount];
            logicLayer.adaptedContactPoints = new List<MotionMatchingContact>();
            targetContactPointIndex = 0;

            for (int i = 0; i < dataState.maxJobsCount; i++)
            {
                csJobs[i] = new MotionMatchingContactEnterJob();
                csJobs[i].SetBasicOptions(
                    this.dataState.motionDataGroups[currentMotionDataGroupIndex].framesInfoPerJob[i],
                    this.dataState.motionDataGroups[currentMotionDataGroupIndex].bonesPerJob[i],
                    this.dataState.motionDataGroups[currentMotionDataGroupIndex].trajectoryPointsPerJob[i],
                    this.dataState.motionDataGroups[currentMotionDataGroupIndex].contactPointsPerJob[i],
                    bestInfosFromJob[i],
                    this.dataState.csFeatures.middleContactsCount
                    );
            }
        }

        protected override void Destroy()
        {

        }

        protected override void Start()
        {
            throw new System.Exception("Contact state cannot be start state!");
        }

        protected override void Enter(
            PoseData currentPose,
            Trajectory previouStateGoal,
            List<float2> whereCanFindingBestPose
            )
        {
            // valus initzialization
            gettingAdaptedPoints = true;
            targetContactPointIndex = 0;

            NativeArray<float2> findingIntervals = new NativeArray<float2>(whereCanFindingBestPose.ToArray(), Allocator.TempJob);
            NativeArray<FrameContact> contactPointsNative = new NativeArray<FrameContact>(logicLayer.contactPoints.ToArray(), Allocator.TempJob);

            for (int i = 0; i < currentPose.Count; i++)
            {
                logicLayer.nativePose[i] = currentPose.GetBoneData(i);
            }


            for (int i = 0; i < previouStateGoal.Length; i++)
            {
                logicLayer.nativeTrajectory[i] = previouStateGoal.GetPoint(i);
            }

            ContactAnimationFinding(
                logicLayer.nativePose,
                findingIntervals,
                contactPointsNative,
                logicLayer.nativeTrajectory
                );

            findingIntervals.Dispose();
            contactPointsNative.Dispose();


            currentDataIndex = logicLayer.bestPoseInfo.clipIndex;
            currentClipLocalTime = (float)logicLayer.bestPoseInfo.localTime;

            playableGraph.CreateBlendMotionMatchingAnimation(
                        dataState.motionDataGroups[currentMotionDataGroupIndex].animationData[currentDataIndex],
                        currentDataIndex,
                        stateMixer,
                        currentClipLocalTime,
                        1f,
                        blendingSpeeds,
                        currentWeights,
                        animationsSequences,
                        this.logicLayer.GetPassIK(),
                        this.logicLayer.GetFootPassIK(),
                        1f
                        );

            currentPlayedClipsIndexes.Add(currentDataIndex);

            if (dataState.csFeatures.contactStateType == ContactStateType.NormalContacts)
            {
                switch (this.contactType)
                {
                    case ContactStateMovemetType.StartContact:
                        SC_Enter();
                        break;
                    case ContactStateMovemetType.ContactLand:
                        CL_Enter();
                        break;
                    case ContactStateMovemetType.StartContactLand:
                        SCL_Enter();
                        break;
                    case ContactStateMovemetType.StartLand:
                        SL_Enter();
                        break;
                    case ContactStateMovemetType.Contact:
                        CL_Enter();
                        break;
                }

                if (dataState.csFeatures.rotateToStart)
                {
                    Vector3 dir = logicLayer.contactPoints[1].position - logicLayer.contactPoints[0].position;
                    dir = Vector3.ProjectOnPlane(dir, Vector3.up);
                    dir = this.GetCurrenMMData().fromFirstToSecondContactRot * dir;

                    startRotation = Quaternion.LookRotation(dir, Vector3.up);

                    float rotationTime = (this.GetCurrenMMData().GetContactStartTime(0) - currentClipLocalTime) / dataState.speedMultiplayer;

                    if (rotationTime == 0)
                    {
                        rotationTime = 0.00001f;
                    }
                    else if (rotationTime < 0)
                    {
                        rotationTime = Mathf.Abs(rotationTime);
                    }

                    degreeSpeed = Quaternion.Angle(this.gameObject.rotation, startRotation) / rotationTime;
                }

#if UNITY_EDITOR

                startTime = currentClipLocalTime;
                previewPos = gameObject.position;

#else

            if(state.csFeatures.postionCorrection == ContactPointPositionCorrectionType.LerpPosition)
            { 
                startTime = currentClipLocalTime;
                previewPos = gameObject.position;
            }
#endif
            }
        }

        public void ContactState_FixedUpdate()
        {

        }

        protected override void Update()
        {
            BeforeUpdateExit();
            if (dataState.csFeatures.contactStateType == ContactStateType.NormalContacts)
            {
                switch (this.contactType)
                {
                    case ContactStateMovemetType.StartContact:
                        SC_Update();
                        break;
                    case ContactStateMovemetType.ContactLand:
                        CL_Update();
                        break;
                    case ContactStateMovemetType.StartContactLand:
                        SCL_Update();
                        break;
                    case ContactStateMovemetType.StartLand:
                        SL_Update();
                        break;
                    case ContactStateMovemetType.Contact:
                        C_Update();
                        break;
                }
            }
        }

        protected override void LateUpdate()
        {
            if (dataState.csFeatures.contactStateType == ContactStateType.NormalContacts)
            {
                if (targetContactPointIndex < this.GetCurrenMMData().contactPoints.Count - 1)
                {
                    if (this.currentClipLocalTime >= this.GetCurrenMMData().GetContactEndTime(targetContactPointIndex))
                    {
                        int fromContactPoint = targetContactPointIndex;
                        targetContactPointIndex++;

                        if (stateBehaviors != null)
                        {
                            for (int contactEventIndex = 0; contactEventIndex < stateBehaviors.Count; contactEventIndex++)
                            {
                                stateBehaviors[contactEventIndex].OnContactPointChange(fromContactPoint, targetContactPointIndex);
                            }
                        }
#if UNITY_EDITOR
                        //startTime = currentClipLocalTime;
                        startTime = this.GetCurrenMMData().GetContactEndTime(targetContactPointIndex - 1);
                        previewPos = this.gameObject.position;

#else

                        if(state.csFeatures.postionCorrection == ContactPointPositionCorrectionType.LerpPosition)
                        {
                            //startTime = currentClipLocalTime;
                            startTime = state.animationData[currentDataIndex].GetContactEndTime(targetContactPointIndex);
                            previewPos = this.gameObject.position;
                        }
#endif
                    }
                }

                switch (this.contactType)
                {
                    case ContactStateMovemetType.StartContact:
                        SC_LateUpdate();
                        break;
                    case ContactStateMovemetType.ContactLand:
                        CL_LateUpdate();
                        break;
                    case ContactStateMovemetType.StartContactLand:
                        SCL_LateUpdate();
                        break;
                    case ContactStateMovemetType.StartLand:
                        SL_LateUpdate();
                        break;
                    case ContactStateMovemetType.Contact:
                        C_LateUpdate();
                        break;
                }

                RotateToStartRotation();

            }
        }

        protected override void Exit()
        {
            if (dataState.csFeatures.contactStateType == ContactStateType.NormalContacts)
            {
                switch (this.contactType)
                {
                    case ContactStateMovemetType.StartContact:
                        SC_Exit();
                        break;
                    case ContactStateMovemetType.ContactLand:
                        CL_Exit();
                        break;
                    case ContactStateMovemetType.StartContactLand:
                        SCL_Exit();
                        break;
                    case ContactStateMovemetType.StartLand:
                        SL_Exit();
                        break;
                    case ContactStateMovemetType.Contact:
                        C_Exit();
                        break;
                }

                if (this.dataState.csFeatures.adapt)
                {
                    gettingAdaptedPoints = true;
                    logicLayer.adaptedContactPoints.Clear();
                }
            }

            logicLayer.contactPoints.Clear();
            targetContactPointIndex = 0;
        }

        public override void BeforeUpdateExit()
        {
            playableGraph.BlendPlayablesInStateMixer(
                ref stateMixer,
                blendingSpeeds,
                currentWeights
                );

            playableGraph.RemoveZeroWeightsInputsAnimations(
                stateMixer,
                blendingSpeeds,
                currentWeights,
                currentPlayedClipsIndexes,
                animationsSequences
                );

            for (int i = 0; i < animationsSequences.Count; i++)
            {
                animationsSequences[i].Update(
                    this.playableGraph,
                    this.logicLayer.GetPassIK(),
                    this.logicLayer.GetFootPassIK(),
                    Time.deltaTime
                    );
            }
        }

        private void ContactAnimationFinding(
            NativeArray<BoneData> currentPose,
            NativeArray<float2> whereWeCanFindingPosition,
            NativeArray<FrameContact> contactPoints,
            NativeArray<TrajectoryPoint> currentTrajectory
            )
        {
            for (int jobIndex = 0; jobIndex < jobsHandle.Length; jobIndex++)
            {
                csJobs[jobIndex].SetChangingOptions(
                    this.dataState.poseCostType,
                    this.dataState.trajectoryCostType,
                    this.contactCost,
                    this.contactType,
                    currentPose,
                    currentTrajectory,
                    contactPoints,
                    whereWeCanFindingPosition,
                    this.dataState.trajectoryCostWeight,
                    this.dataState.poseCostWeight,
                    this.dataState.csFeatures.contactPointsWeight
                    );
                jobsHandle[jobIndex] = csJobs[jobIndex].Schedule();
            }

            JobHandle.ScheduleBatchedJobs();
            JobHandle.CompleteAll(jobsHandle);
            JoinJobsOutput();
        }

        

        private void MoveToContactPoint(int contactPointIndex, float3 toWorldPosition)
        {
            if (currentClipLocalTime < this.GetCurrenMMData().GetContactStartTime(contactPointIndex))
            {
                if (contactPointIndex > 0 &&
                    currentClipLocalTime > this.GetCurrenMMData().GetContactEndTime(contactPointIndex - 1) ||
                    contactPointIndex == 0)
                {
                    float deltaTime = (this.GetCurrenMMData().GetContactStartTime(contactPointIndex) - currentClipLocalTime) / dataState.speedMultiplayer;

                    float3 currentContactPointPosition = gameObject.TransformPoint(
                        this.GetCurrenMMData().GetContactPoint(contactPointIndex, currentClipLocalTime).position
                        );

                    float3 vel = (toWorldPosition - currentContactPointPosition) / deltaTime;

                    gameObject.position += (Vector3)(vel * Time.deltaTime);
                }
            }
        }

        private void LerpToPosition(int contactPointIndex)
        {
            if (currentClipLocalTime < this.GetCurrenMMData().GetContactStartTime(contactPointIndex))
            {
                if (contactPointIndex > 0 &&
                    currentClipLocalTime > this.GetCurrenMMData().GetContactEndTime(contactPointIndex - 1) ||
                    contactPointIndex == 0)
                {
                    float targetPointStartTime = this.GetCurrenMMData().GetContactStartTime(contactPointIndex);

                    float factor = (currentClipLocalTime - startTime) / (targetPointStartTime - startTime);
                    factor /= dataState.speedMultiplayer;

                    Vector3 nextContactPointDesiredPos = logicLayer.contactPoints[contactPointIndex].position;

                    FrameContact cp = this.GetCurrenMMData().GetContactPoint(contactPointIndex, currentClipLocalTime);

                    Vector3 currentContatcPointPos = gameObject.TransformPoint(cp.position);

                    Vector3 contactPosDelta = nextContactPointDesiredPos - currentContatcPointPos;

                    Vector3 desiredObjectPos = this.gameObject.position + contactPosDelta;


                    this.gameObject.position = Vector3.Lerp(previewPos, desiredObjectPos, factor);
                }
            }
        }


        private void RotateToStartRotation()
        {
            if (targetContactPointIndex == 0 && dataState.csFeatures.rotateToStart)
            {
                this.gameObject.rotation = Quaternion.RotateTowards(
                    this.gameObject.rotation,
                    startRotation,
                    degreeSpeed * Time.deltaTime
                    );
            }
        }

        #region Adapt movement
        private void GetAdaptedContactPoints(int startIndex, int endIndex)
        {
            for (int i = startIndex; i <= endIndex; i++)
            {

            }
        }
        #endregion

        #region Start Contact
        private void SC_Enter()
        {
        }

        private void SC_Update()
        {
            //if (
            //    gettingAdaptedPoints &&
            //    this.dataState.csFeatures.adapt &&
            //    targetContactPointIndex == 2 &&
            //    dataState.animationData[currentDataIndex][0].contactPoints.Length > 2
            //    )
            //{
            //    GetAdaptedContactPoints(2, logicLayer.contactPoints.Count - 1);
            //    gettingAdaptedPoints = false;
            //}
        }

        private void SC_LateUpdate()
        {
            if (targetContactPointIndex == 0 ||
                targetContactPointIndex <= this.dataState.csFeatures.middleContactsCount)
            {
                switch (dataState.csFeatures.postionCorrection)
                {
                    case ContactPointPositionCorrectionType.LerpPosition:
                        LerpToPosition(targetContactPointIndex);
                        break;
                    case ContactPointPositionCorrectionType.MovePosition:
                        MoveToContactPoint(targetContactPointIndex, logicLayer.contactPoints[targetContactPointIndex].position);
                        break;
                }
            }
            //else if (this.dataState.csFeatures.adapt)
            //{
            //    MoveToContactPoint(targetContactPointIndex, logicLayer.adaptedContactPoints[targetContactPointIndex - 2].position);
            //}
        }

        private void SC_Exit()
        {

        }
        #endregion

        #region Contact Land
        private void CL_Enter()
        {
        }

        private void CL_Update()
        {
            //if (
            //    gettingAdaptedPoints &&
            //    this.dataState.csFeatures.adapt &&
            //    targetContactPointIndex == 2 &&
            //    dataState.animationData[currentDataIndex][0].contactPoints.Length > 3
            //    )
            //{
            //    GetAdaptedContactPoints(2, logicLayer.contactPoints.Count - 2);
            //    gettingAdaptedPoints = false;
            //}
        }

        private void CL_LateUpdate()
        {
            if ((targetContactPointIndex > 0 &&
                targetContactPointIndex <= this.dataState.csFeatures.middleContactsCount ||
                targetContactPointIndex == logicLayer.contactPoints.Count - 1) &&
                targetContactPointIndex < logicLayer.contactPoints.Count)
            {
                switch (dataState.csFeatures.postionCorrection)
                {
                    case ContactPointPositionCorrectionType.LerpPosition:
                        LerpToPosition(targetContactPointIndex);
                        break;
                    case ContactPointPositionCorrectionType.MovePosition:
                        MoveToContactPoint(targetContactPointIndex, logicLayer.contactPoints[targetContactPointIndex].position);
                        break;
                }
            }
            //else if (this.dataState.csFeatures.adapt && targetContactPointIndex != 0)
            //{
            //    MoveToContactPoint(targetContactPointIndex, logicLayer.adaptedContactPoints[targetContactPointIndex - 2].position);
            //}
        }

        private void CL_Exit()
        {

        }
        #endregion

        #region Start Contact Land
        private void SCL_Enter()
        {

        }

        private void SCL_Update()
        {
            //    if (
            //        gettingAdaptedPoints &&
            //        this.dataState.csFeatures.adapt &&
            //        targetContactPointIndex == 2 &&
            //        dataState.animationData[currentDataIndex][0].contactPoints.Length > 3
            //        )
            //    {
            //        GetAdaptedContactPoints(2, logicLayer.contactPoints.Count - 2);
            //        gettingAdaptedPoints = false;
            //    }
        }

        private void SCL_LateUpdate()
        {
            if (((targetContactPointIndex == 0 /*&& this.state.csFeatures.gotoStartContactPoint*/) ||
                targetContactPointIndex <= this.dataState.csFeatures.middleContactsCount ||
                targetContactPointIndex == logicLayer.contactPoints.Count - 1)
                )
            {
                switch (dataState.csFeatures.postionCorrection)
                {
                    case ContactPointPositionCorrectionType.LerpPosition:
                        LerpToPosition(targetContactPointIndex);
                        break;
                    case ContactPointPositionCorrectionType.MovePosition:
                        MoveToContactPoint(targetContactPointIndex, logicLayer.contactPoints[targetContactPointIndex].position);
                        break;
                }
            }
            //else if (this.dataState.csFeatures.adapt && targetContactPointIndex != 0)
            //{
            //    MoveToContactPoint(targetContactPointIndex, logicLayer.adaptedContactPoints[targetContactPointIndex - 2].position);
            //}
        }

        private void SCL_Exit()
        {

        }
        #endregion

        #region Start Land
        private void SL_Enter()
        {

        }

        private void SL_Update()
        {
            //    if (
            //        gettingAdaptedPoints &&
            //        this.dataState.csFeatures.adapt &&
            //        targetContactPointIndex == 2 &&
            //        dataState.animationData[currentDataIndex][0].contactPoints.Length > 3
            //        )
            //    {
            //        GetAdaptedContactPoints(2, logicLayer.contactPoints.Count - 2);
            //        gettingAdaptedPoints = false;
            //    }
        }

        private void SL_LateUpdate()
        {
            if (targetContactPointIndex == 0 || targetContactPointIndex == this.logicLayer.contactPoints.Count - 1)
            {
                switch (dataState.csFeatures.postionCorrection)
                {
                    case ContactPointPositionCorrectionType.LerpPosition:
                        LerpToPosition(targetContactPointIndex);
                        break;
                    case ContactPointPositionCorrectionType.MovePosition:
                        MoveToContactPoint(targetContactPointIndex, logicLayer.contactPoints[targetContactPointIndex].position);
                        break;
                }
            }
            //else if (this.dataState.csFeatures.adapt && targetContactPointIndex != 0)
            //{
            //    MoveToContactPoint(targetContactPointIndex, logicLayer.adaptedContactPoints[targetContactPointIndex - 2].position);
            //}
        }

        private void SL_Exit()
        {

        }
        #endregion

        #region Contact
        private void C_Enter()
        {
        }

        private void C_Update()
        {
            //if (
            //    gettingAdaptedPoints &&
            //    this.dataState.csFeatures.adapt &&
            //    targetContactPointIndex == 2 &&
            //    dataState.animationData[currentDataIndex][0].contactPoints.Length > 3
            //    )
            //{
            //    GetAdaptedContactPoints(2, logicLayer.contactPoints.Count - 2);
            //    gettingAdaptedPoints = false;
            //}
        }

        private void C_LateUpdate()
        {
            if (0 < targetContactPointIndex && targetContactPointIndex <= dataState.csFeatures.middleContactsCount)
            {
                switch (dataState.csFeatures.postionCorrection)
                {
                    case ContactPointPositionCorrectionType.LerpPosition:
                        LerpToPosition(targetContactPointIndex);
                        break;
                    case ContactPointPositionCorrectionType.MovePosition:
                        MoveToContactPoint(targetContactPointIndex, logicLayer.contactPoints[targetContactPointIndex].position);
                        break;
                }
            }
            //else if (this.dataState.csFeatures.adapt && targetContactPointIndex != 0)
            //{
            //    MoveToContactPoint(targetContactPointIndex, logicLayer.adaptedContactPoints[targetContactPointIndex - 2].position);
            //}
        }

        private void C_Exit()
        {

        }
        #endregion

    }
}