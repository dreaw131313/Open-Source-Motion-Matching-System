using DW_Gameplay;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace DW_Editor
{
    public static class MM_Gizmos
    {
#if UNITY_EDITOR
        public static void DrawTrajectory(
            float[] pointsTime,
            Vector3 objectPosition,
            Vector3 objectForward,
            Trajectory trajectory,
            bool solidSphere,
            int spheresCount,
            float arrowLength,
            float startArrowSpherRadious,
            float endArrowSphereRadious
            )
        {
            float3 up = new float3(0, 1, 0);
            for (int i = 0; i < trajectory.Length; i++)
            {
                float3 current = up * startArrowSpherRadious + trajectory.GetPoint(i).position;
                float3 arrowDir = trajectory.GetPoint(i).orientation;
                if (solidSphere)
                {
                    Gizmos.DrawSphere(new Vector3(current.x, current.y, current.z), startArrowSpherRadious);
                }
                else
                {
                    Gizmos.DrawWireSphere(new Vector3(current.x, current.y, current.z), startArrowSpherRadious);
                }

                bool3 result = arrowDir == float3.zero;
                if (result[0] && result[1] && result[2])
                {
                    DrawArrowFromSpheres(
                        current,
                        objectForward,
                        solidSphere,
                        spheresCount,
                        arrowLength,
                        endArrowSphereRadious,
                        endArrowSphereRadious
                        );
                }
                else
                {
                    DrawArrowFromSpheres(
                        current,
                        arrowDir,
                        solidSphere,
                        spheresCount,
                        arrowLength,
                        endArrowSphereRadious,
                        endArrowSphereRadious
                        );
                }

                if (i < (trajectory.Length - 1))
                {
                    float3 next = up * startArrowSpherRadious + trajectory.GetPoint(i + 1).position;
                    if (pointsTime[i + 1] < 0 ||
                        pointsTime[i] > 0 && pointsTime[i + 1] > 0)
                    {
                        Gizmos.DrawLine(
                            current,
                            next
                            );
                    }
                    else// if(goal[i].futureTime<0 && goal[i+1].futureTime > 0)
                    {
                        Gizmos.DrawLine(
                            current,
                            Vector3.up * startArrowSpherRadious + objectPosition
                            );
                        Gizmos.DrawLine(
                            Vector3.up * startArrowSpherRadious + objectPosition,
                            up * startArrowSpherRadious + trajectory.GetPoint(i + 1).position
                            );
                    }
                }
            }
        }


        public static void DrawTrajectory(
            float[] pointsTime,
            Vector3 objectPosition,
            Vector3 objectForward,
            Trajectory trajectory,
            bool solidSphere,
            float pointSphereRadious,
            float arrowLength
            )
        {
            float arrowRiseUp = 1.5f * pointSphereRadious;
            float3 up = new float3(0, 1, 0);
            for (int i = 0; i < trajectory.Length; i++)
            {
                float3 current = up * pointSphereRadious + trajectory.GetPoint(i).position;
                float3 arrowDir = trajectory.GetPoint(i).orientation;
                if (solidSphere)
                {
                    Gizmos.DrawSphere(new Vector3(current.x, current.y, current.z), pointSphereRadious);
                }
                else
                {
                    Gizmos.DrawWireSphere(new Vector3(current.x, current.y, current.z), pointSphereRadious);
                }

                bool3 result = arrowDir == float3.zero;

                Gizmos.DrawLine(current, current + up * arrowRiseUp);

                if (result[0] && result[1] && result[2])
                {
                    DrawArrow(
                        current + up * arrowRiseUp,
                        objectForward,
                        arrowLength,
                        arrowLength * 0.33f
                        );
                }
                else
                {
                    DrawArrow(
                        current + up * arrowRiseUp,
                        arrowDir,
                        arrowLength,
                        arrowLength * 0.33f
                        );
                }

                if (i < (trajectory.Length - 1))
                {
                    float3 next = up * pointSphereRadious + trajectory.GetPoint(i + 1).position;
                    if (pointsTime[i + 1] < 0 ||
                        pointsTime[i] > 0 && pointsTime[i + 1] > 0)
                    {
                        Gizmos.DrawLine(
                            current,
                            next
                            );
                    }
                    else// if(goal[i].futureTime<0 && goal[i+1].futureTime > 0)
                    {
                        Gizmos.DrawLine(
                            current,
                            Vector3.up * pointSphereRadious + objectPosition
                            );
                        Gizmos.DrawLine(
                            Vector3.up * pointSphereRadious + objectPosition,
                            up * pointSphereRadious + trajectory.GetPoint(i + 1).position
                            );
                    }
                }
            }
        }

        public static void DrawTrajectory_Handles(
            float[] pointsTime,
            Vector3 objectPosition,
            Vector3 objectForward,
            Trajectory trajectory,
            float pointSphereRadious,
            float arrowLength
            )
        {
            float arrowRiseUp = 1.5f * pointSphereRadious;
            float3 up = new float3(0, 1, 0);
            for (int i = 0; i < trajectory.Length; i++)
            {
                float3 current = up * pointSphereRadious + trajectory.GetPoint(i).position;
                float3 arrowDir = trajectory.GetPoint(i).orientation;

                bool3 result = arrowDir == float3.zero;

                Handles.DrawLine(current, current + up * arrowRiseUp);

                if (result[0] && result[1] && result[2])
                {
                    DrawArrow_Handles(
                        current + up * arrowRiseUp,
                        objectForward,
                        arrowLength,
                        arrowLength * 0.33f
                        );
                }
                else
                {
                    DrawArrow_Handles(
                        current + up * arrowRiseUp,
                        arrowDir,
                        arrowLength,
                        arrowLength * 0.33f
                        );
                }

                if (i < (trajectory.Length - 1))
                {
                    float3 next = up * pointSphereRadious + trajectory.GetPoint(i + 1).position;
                    if (pointsTime[i + 1] < 0 ||
                        pointsTime[i] > 0 && pointsTime[i + 1] > 0)
                    {
                        Handles.DrawLine(
                            current,
                            next
                            );
                    }
                    else// if(goal[i].futureTime<0 && goal[i+1].futureTime > 0)
                    {
                        Handles.DrawLine(
                            current,
                            Vector3.up * pointSphereRadious + objectPosition
                            );
                        Handles.DrawLine(
                            Vector3.up * pointSphereRadious + objectPosition,
                            up * pointSphereRadious + trajectory.GetPoint(i + 1).position
                            );
                    }
                }
            }
        }

        public static void DrawArrowFromSpheres(
            Vector3 pos,
            Vector3 direction,
            bool solid,
            int sphersCount = 5,
            float length = 0.5f,
            float startSpherRadious = 0.1f,
            float endSpherRadious = 0.05f
            )
        {
            Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 45f, 0) * new Vector3(0, 0, 1);
            Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 45f, 0) * new Vector3(0, 0, 1);

            Vector3 arrowStartPos = pos + direction * length;
            Vector3 rightDir = right * 0.33f * length;
            Vector3 leftDir = left * 0.33f * length;


            for (int i = 0; i < sphersCount; i++)
            {
                float factor = (float)(i + 1) / (float)sphersCount;
                if (solid)
                {
                    Gizmos.DrawSphere(
                        pos + direction * factor * length,
                        Mathf.Lerp(startSpherRadious, endSpherRadious, factor)
                        );
                }
                else
                {
                    Gizmos.DrawWireSphere(
                        pos + direction * factor * length,
                        Mathf.Lerp(startSpherRadious, endSpherRadious, factor)
                        );

                }
            }
            int half = Mathf.CeilToInt((float)sphersCount / 1.5f);
            for (int i = 0; i < half; i++)
            {
                if (solid)
                {
                    Gizmos.DrawSphere(
                          arrowStartPos + (float)(i + 1) / (float)half * rightDir,
                          endSpherRadious
                      );
                }
                else
                {
                    Gizmos.DrawWireSphere(
                        arrowStartPos + (float)(i + 1) / (float)half * rightDir,
                        endSpherRadious
                    );
                }
            }

            for (int i = 0; i < half; i++)
            {
                if (solid)
                {
                    Gizmos.DrawSphere(
                        arrowStartPos + (float)(i + 1) / (float)half * leftDir,
                        endSpherRadious
                    );
                }
                else
                {
                    Gizmos.DrawWireSphere(
                        arrowStartPos + (float)(i + 1) / (float)half * leftDir,
                        endSpherRadious
                    );
                }
            }
        }

        public static void DrawArrow(
            Vector3 pos,
            Vector3 dir,
            float length,
            float armsLenght
            )
        {
            if (dir == Vector3.zero)
            {
                return;
            }
            Vector3 right = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180 + 45f, 0) * new Vector3(0, 0, 1);
            Vector3 left = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180 - 45f, 0) * new Vector3(0, 0, 1);

            Vector3 arrowTipPos = pos + dir * length;
            Vector3 rightDir = right * armsLenght;
            Vector3 leftDir = left * armsLenght;

            Gizmos.DrawLine(pos, arrowTipPos);
            Gizmos.DrawLine(arrowTipPos, arrowTipPos + rightDir);
            Gizmos.DrawLine(arrowTipPos, arrowTipPos + leftDir);
        }

        public static void DrawArrow(
            Vector3 from,
            Vector3 to,
            float armsLength
            )
        {
            Vector3 delta = from - to;
            float length = delta.magnitude;
            DrawArrow(from, delta.normalized, length, armsLength);
        }

        public static void DrawArrow_Handles(
            Vector3 pos,
            Vector3 dir,
            float length,
            float armsLenght
            )
        {
            if(dir == Vector3.zero)
            {
                return;
            }
            Vector3 right = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180 + 45f, 0) * new Vector3(0, 0, 1);
            Vector3 left = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180 - 45f, 0) * new Vector3(0, 0, 1);

            Vector3 arrowTipPos = pos + dir * length;
            Vector3 rightDir = right * armsLenght;
            Vector3 leftDir = left * armsLenght;

            Handles.DrawLine(pos, arrowTipPos);
            Handles.DrawLine(arrowTipPos, arrowTipPos + rightDir);
            Handles.DrawLine(arrowTipPos, arrowTipPos + leftDir);
        }

        public static void DrawArrow_Handles(
            Vector3 from,
            Vector3 to,
            float armsLength
            )
        {
            Vector3 delta = to- from;
            float length = delta.magnitude;
            DrawArrow_Handles(from, delta.normalized, length, armsLength);
        }


        public static void DrawArrowHandles(
            Vector3 pos,
            Vector3 dir,
            float length,
            float armsLenght
            )
        {
            Vector3 right = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180 + 45f, 0) * new Vector3(0, 0, 1);
            Vector3 left = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180 - 45f, 0) * new Vector3(0, 0, 1);

            Vector3 arrowTipPos = pos + dir * length;
            Vector3 rightDir = right * armsLenght;
            Vector3 leftDir = left * armsLenght;

            Handles.DrawLine(pos, arrowTipPos);
            Handles.DrawLine(arrowTipPos, arrowTipPos + rightDir);
            Handles.DrawLine(arrowTipPos, arrowTipPos + leftDir);
        }

        public static void DrawCapsule(Vector3 position, float radious, float height)
        {


        }

        public static void DrawWireCapsule(Vector3 _pos, Quaternion _rot, float _radius, float _height, Color _color = default(Color))
        {
            if (_color != default(Color))
                Handles.color = _color;
            Matrix4x4 angleMatrix = Matrix4x4.TRS(_pos, _rot, Handles.matrix.lossyScale);
            using (new Handles.DrawingScope(angleMatrix))
            {
                var pointOffset = (_height - (_radius * 2)) / 2;

                //draw sideways
                Handles.DrawWireArc(Vector3.up * pointOffset, Vector3.left, Vector3.back, -180, _radius);
                Handles.DrawLine(new Vector3(0, pointOffset, -_radius), new Vector3(0, -pointOffset, -_radius));
                Handles.DrawLine(new Vector3(0, pointOffset, _radius), new Vector3(0, -pointOffset, _radius));
                Handles.DrawWireArc(Vector3.down * pointOffset, Vector3.left, Vector3.back, 180, _radius);
                //draw frontways
                Handles.DrawWireArc(Vector3.up * pointOffset, Vector3.back, Vector3.left, 180, _radius);
                Handles.DrawLine(new Vector3(-_radius, pointOffset, 0), new Vector3(-_radius, -pointOffset, 0));
                Handles.DrawLine(new Vector3(_radius, pointOffset, 0), new Vector3(_radius, -pointOffset, 0));
                Handles.DrawWireArc(Vector3.down * pointOffset, Vector3.back, Vector3.left, -180, _radius);
                //draw center
                Handles.DrawWireDisc(Vector3.up * pointOffset, Vector3.up, _radius);
                Handles.DrawWireDisc(Vector3.down * pointOffset, Vector3.up, _radius);

            }
        }

        public static void DrawPose(PoseData pose, Color poseColor, Color velColor, bool drawVel = false)
        {
            for (int i = 0; i < pose.Count; i++)
            {
                Handles.color = poseColor;
                Handles.DrawWireCube(
                    pose.bones[i].localPosition,
                    Vector3.one * 0.1f
                    );

                if (drawVel)
                {
                    Handles.color = velColor;
                    DrawArrow_Handles(
                        pose.bones[i].localPosition,
                        pose.bones[i].localPosition + pose.bones[i].velocity,
                        0.2f
                        );
                }
            }
        }
#endif
    }
}
