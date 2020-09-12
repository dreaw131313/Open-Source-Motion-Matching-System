using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DW_Gameplay
{
    public enum TrajectoryCostType
    {
        //Position,
        //Velocity,
        //Orientation,
        //PositionVelocity,
        //PositionOrientation,
        //VelocityOrientation,
        PositionVelocityOrientation,
        None
    }

    public enum PoseCostType
    {
        Position,
        Velocity,
        PositionVelocity,
        None
    }

    public enum AnimationDataType
    {
        SingleAnimation,
        BlendTree,
        AnimationSequence
    }

    // Trajectory enums
    public enum TrajectoryCorrectionType
    {
        Constant, // based on first trajectory point with future time
        //ReachTarget,
        Progresive, // based on first trajectory point with future time
        MatchOrientationConstant, // based on first trajectory point with future time
        MatchOrientationProgresive, // based on first trajectory point with future time
        None
    }

    public enum PastTrajectoryType
    {
        Recorded,
        CopyFromCurrentData,
        None
    }


    // Contact state enums 
    public enum ContactPointPositionCorrectionType
    {
        LerpPosition,
        MovePosition
    }

    public enum ContactStateType
    {
        NormalContacts,
        Impacts
    }

    public enum ContactPointCostType
    {
        Postion,
        Normal_OR_Direction,
        PositionNormal_OR_Direction,
        None
    }

    public enum ContactPointType
    {
        Start,
        Contact,
        End,
        Adapted,
    }


    // Single animation enums
    public enum SingleAnimationUpdateType
    {
        PlaySelected,
        PlayInSequence,
        PlayRandom
    }


    public enum ContactStateMovemetType
    {
        StartContact,
        Contact,
        ContactLand,
        StartLand,
        StartContactLand,
        None
        //OwnMethod
    }


}
