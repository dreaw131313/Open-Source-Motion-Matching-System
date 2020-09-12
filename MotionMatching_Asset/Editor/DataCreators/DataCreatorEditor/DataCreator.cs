using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DW_Editor
{
    [CreateAssetMenu(fileName = "DataCreator", menuName = "MotionMatching/DataCreator")]
    public class DataCreator : ScriptableObject
    {
        [SerializeField]
        public List<AnimationClip> clips = new List<AnimationClip>();
        [SerializeField]
        public List<Vector2> bonesWeights = new List<Vector2>();
        [SerializeField]
        public List<string> bonesNames = new List<string>();
        [SerializeField]
        public AvatarMask avatarMask;
        [SerializeField]
        public Transform gameObjectTransform;
        [SerializeField]
        public int posesPerSecond;
        [SerializeField]
        public List<float> trajectoryStepTimes = null;
        [SerializeField]
        public bool maskFold = false;
        [SerializeField]
        public bool trajectoryFold = false;
        [SerializeField]
        public bool animFold = false;
        [SerializeField]
        public bool basicOptionFold = false;
        [SerializeField]
        public bool findInYourself = true;
        [SerializeField]
        public bool blendToYourself = true;
        [SerializeField]
        public List<AnimationsSequence> sequences = new List<AnimationsSequence>();
        [SerializeField]
        public int selectedSequence = -1;
        [SerializeField]
        public List<BlendTreeInfo> blendTrees = new List<BlendTreeInfo>();
        [SerializeField]
        public int selectedBlendTree = -1;
        [SerializeField]
        public string saveDataPath = "";
        [SerializeField]
        public string blendTreesSavePath = "";
        [SerializeField]
        public string animationSequenceSavePath = "";
        [SerializeField]
        public float cutTimeFromStart = 0f;
        [SerializeField]
        public float cutTimeToEnd = 0f;


        public string findingSequence = "";
        public string findingBlendTree = "";
    }
}
