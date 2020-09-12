using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace DW_Gameplay
{
    public static class Vector3Extensions
    {
        public static Vector3 ClampVector(this Vector3 vector, Vector3 first, Vector3 second)
        {
            for (int i = 0; i < 3; i++)
            {
                if (first[i] >= second[i])
                {
                    vector[i] = Mathf.Clamp(vector[i], second[i], first[i]);
                }
                else
                {
                    vector[i] = Mathf.Clamp(vector[i], first[i], second[i]);
                }
            }

            return vector;
        }

        public static Vector3 MoveVectorWithSpeed(this Vector3 actuall, Vector3 target, float vel, float deltaTime)
        {
            Vector3 newVector = Vector3.zero;
            Vector3 dir = (target - actuall).normalized;
            newVector = actuall + dir * vel * deltaTime;
            return newVector.ClampVector(actuall, target);
        }
    }

    public static class float3Extension
    {
        public static float3 ClampFloat3(float3 vector, float3 first, float3 second)
        {
            for (int i = 0; i < 3; i++)
            {
                if (first[i] >= second[i])
                {
                    vector[i] = math.clamp(vector[i], second[i], first[i]);
                }
                else
                {
                    vector[i] = math.clamp(vector[i], first[i], second[i]);
                }
            }

            return vector;
        }

        public static float3 MoveFloat3WithSpeed(float3 actuall, float3 target, float vel, float deltaTime)
        {
            float3 dir = math.normalize(target - actuall);
            float3 newVector = actuall + dir * vel * deltaTime;
            return ClampFloat3(newVector, actuall, target);
        }
    }
}
