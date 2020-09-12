using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DW_Gameplay
{
    public abstract class MotionMatchingStateBehavior
    {
        protected LogicState logicState;
        protected MotionMatching mmAnimator;
        protected Transform transform;

        public abstract void Enter();

        public abstract void Update();

        public abstract void LateUpdate();

        public abstract void OnStartOutTransition();

        public abstract void Exit();

        /// <summary>
        /// Called once after contact index change only when added to MotionMatchingContactState;
        /// </summary>
        /// <param name="fromContactIndex"></param>
        /// <param name="toContatctIndex"></param>
        public abstract void OnContactPointChange(int fromContactIndex, int toContatctIndex);

        public abstract void CatchEventMarkers(string eventName);

        public void SetBasic(LogicState logicState, MotionMatching mmAnimator, Transform transform)
        {
            this.logicState = logicState;
            this.mmAnimator = mmAnimator;
            this.transform = transform;
        }
    }
}