using UnityEngine;

namespace DW_Gameplay
{
    [System.Serializable]
    public struct FrameSections
    {
        //[SerializeField]
        //private bool sec0;
        //[SerializeField]
        //private bool sec1;
        //[SerializeField]
        //private bool sec2;
        //[SerializeField]
        //private bool sec3;
        //[SerializeField]
        //private bool sec4;
        //[SerializeField]
        //private bool sec5;
        //[SerializeField]
        //private bool sec6;
        //[SerializeField]
        //private bool sec7;
        //[SerializeField]
        //private bool sec8;
        //[SerializeField]
        //private bool sec9;
        //[SerializeField]
        //private bool sec10;
        //[SerializeField]
        //private bool sec11;
        //[SerializeField]
        //private bool sec12;
        //[SerializeField]
        //private bool sec13;
        //[SerializeField]
        //private bool sec14;
        //[SerializeField]
        //private bool sec15;
        //[SerializeField]
        //private bool sec16;
        //[SerializeField]
        //private bool sec17;
        //[SerializeField]
        //private bool sec18;
        //[SerializeField]
        //private bool sec19;

        [SerializeField]
        private uint sections;

        public FrameSections(bool alwaysCheck)
        {
            //sec0 = alwaysCheck;
            //sec1 = false;
            //sec2 = false;
            //sec3 = false;
            //sec4 = false;
            //sec5 = false;
            //sec6 = false;
            //sec7 = false;
            //sec8 = false;
            //sec9 = false;
            //sec10 = false;
            //sec11 = false;
            //sec12 = false;
            //sec13 = false;
            //sec14 = false;
            //sec15 = false;
            //sec16 = false;
            //sec17 = false;
            //sec18 = false;
            //sec19 = false;
            sections = 1;
        }

        //public void SetSection_1(int index, bool value)
        //{
        //    switch (index)
        //    {
        //        case 0:
        //            sec0 = value;
        //            break;
        //        case 1:
        //            sec1 = value;
        //            break;
        //        case 2:
        //            sec2 = value;
        //            break;
        //        case 3:
        //            sec3 = value;
        //            break;
        //        case 4:
        //            sec4 = value;
        //            break;
        //        case 5:
        //            sec5 = value;
        //            break;
        //        case 6:
        //            sec6 = value;
        //            break;
        //        case 7:
        //            sec7 = value;
        //            break;
        //        case 8:
        //            sec8 = value;
        //            break;
        //        case 9:
        //            sec9 = value;
        //            break;
        //        case 10:
        //            sec10 = value;
        //            break;
        //        case 11:
        //            sec11 = value;
        //            break;
        //        case 12:
        //            sec12 = value;
        //            break;
        //        case 13:
        //            sec13 = value;
        //            break;
        //        case 14:
        //            sec14 = value;
        //            break;
        //        case 15:
        //            sec15 = value;
        //            break;
        //        case 16:
        //            sec16 = value;
        //            break;
        //        case 17:
        //            sec17 = value;
        //            break;
        //        case 18:
        //            sec18 = value;
        //            break;
        //        case 19:
        //            sec19 = value;
        //            break;
        //    }
        //}

        //public bool GetSection_1(int index)
        //{
        //    switch (index)
        //    {
        //        case -1:
        //            return false;
        //        case 0:
        //            return sec0;
        //        case 1:
        //            return sec1;
        //        case 2:
        //            return sec2;
        //        case 3:
        //            return sec3;
        //        case 4:
        //            return sec4;
        //        case 5:
        //            return sec5;
        //        case 6:
        //            return sec6;
        //        case 7:
        //            return sec7;
        //        case 8:
        //            return sec8;
        //        case 9:
        //            return sec9;
        //        case 10:
        //            return sec10;
        //        case 11:
        //            return sec11;
        //        case 12:
        //            return sec12;
        //        case 13:
        //            return sec13;
        //        case 14:
        //            return sec14;
        //        case 15:
        //            return sec15;
        //        case 16:
        //            return sec16;
        //        case 17:
        //            return sec17;
        //        case 18:
        //            return sec18;
        //        case 19:
        //            return sec19;
        //        default:
        //            return false;
        //    }
        //}

        public void SetSection(int index, bool value)
        {
            if (value)
            {
                uint s = 1;
                s = s << index;
                sections = s | sections;
            }
            else
            {
                uint s = 1;
                s = s << index;
                sections = ~s & sections;
                
            }
        }

        public bool GetSection(int index)
        {
            uint s = sections;
            s = s >> index;
            s = s << 31;
            return s != 0;
        }

    }
}
