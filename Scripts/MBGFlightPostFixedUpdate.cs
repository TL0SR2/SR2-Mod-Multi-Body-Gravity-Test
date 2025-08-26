using UnityEngine;
using HarmonyLib;
using System;
using ModApi.GameLoop.Interfaces;
using ModApi.GameLoop;
using Assets.Scripts.GameLoop;
using ModApi.Flight;
using ModApi;





namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MBGFlightPostFixedUpdate : IFlightPostFixedUpdate
    {

        public bool StartMethodCalled { get; set; }

        public Vector3d LastPosition = Vector3d.zero;

        public Vector3d LastVelocity = Vector3d.zero;

        public void FlightPostFixedUpdate(in FlightFrameData frame)
        {
            var referenceFrame = frame.FlightScene.CraftNode.CraftScript.ReferenceFrame;
            var craftNode = frame.FlightScene.CraftNode;
            var time = Time.fixedTime - FlightState_set_Time_Patch.LastUpdateFixedTime + FlightState_set_Time_Patch.LastUpdateTime;
            var fixedDeltaTime = Time.fixedDeltaTime;
            var position = referenceFrame.FrameToPlanetPosition(craftNode.CraftScript.FramePosition) + craftNode.Parent.GetSolarPositionAtTime(time);
            var velocity = referenceFrame.FrameToPlanetVelocity(craftNode.CraftScript.FrameVelocity) + craftNode.Parent.GetSolarVelocityAtTime(time);

            if (LastPosition == Vector3d.zero)
            {
                LastPosition = position;
                LastVelocity = velocity;
                Debug.Log($"Initial LastPosition: {{ {LastPosition.x:E3}, {LastPosition.y:E3}, {LastPosition.z:E3} }}");
            }

            Debug.Log($"FrameVelocity: {{ {craftNode.CraftScript.FrameVelocity.x:E3}, {craftNode.CraftScript.FrameVelocity.y:E3}, {craftNode.CraftScript.FrameVelocity.z:E3} }}");
            Debug.Log($"FramePosition: {{ {craftNode.CraftScript.FramePosition.x:E3}, {craftNode.CraftScript.FramePosition.y:E3}, {craftNode.CraftScript.FramePosition.z:E3} }}");

            int CaculateStepCount = 20;
            P_V_Pair pVPair_Rk = new P_V_Pair(position, velocity);
            double deltaTime = fixedDeltaTime / CaculateStepCount;
            double stepTime = time;
            for (int i = 0; i < CaculateStepCount; i++)
            {
                var h = deltaTime;
                var x_n = stepTime;
                var y_n = pVPair_Rk;
                Func<double, P_V_Pair, P_V_Pair> func = MBGMath.RKFunc;

                var k1 = h * func(x_n, y_n);
                var k2 = h * func(x_n + h / 2, y_n + k1 / 2);
                var k3 = h * func(x_n + h / 4, y_n + (3 * k1 + k2) / 16);
                var k4 = h * func(x_n + h / 2, y_n + k3 / 2);
                var k5 = h * func(x_n + 3.0 / 4.0 * h, y_n + (-3 * k2 + 6 * k3 + 9 * k4) / 16);
                var k6 = h * func(x_n + h, y_n + (k1 + 4 * k2 + 6 * k3 - 12 * k4 + 8 * k5) / 7);
                pVPair_Rk = y_n + (7 * k1 + 32 * k3 + 12 * k4 + 32 * k5 + 7 * k6) / 90;
                stepTime += deltaTime;
            }

            var velocity_eulerimp = referenceFrame.PlanetToFrameVelocity(pVPair_Rk.Velocity - craftNode.Parent.GetSolarVelocityAtTime(time + fixedDeltaTime));
            var position_eulerimp = referenceFrame.PlanetToFramePosition(pVPair_Rk.Position - craftNode.Parent.GetSolarPositionAtTime(time + fixedDeltaTime))-fixedDeltaTime*referenceFrame.Velocity;
            var velocity_eulerimp_rev = velocity_eulerimp - fixedDeltaTime * craftNode.CraftScript.FlightData.GravityFrame;
            var position_eulerimp_rev = position_eulerimp - fixedDeltaTime * velocity_eulerimp;

            var deltaPosition = position_eulerimp_rev - craftNode.CraftScript.FramePosition;
            var deltaVelocity = velocity_eulerimp_rev - craftNode.CraftScript.FrameVelocity;

            // Debug.Log($"referenceFrameVelocity: {{ {referenceFrame.Velocity.x:E3}, {referenceFrame.Velocity.y:E3}, {referenceFrame.Velocity.z:E3} }}");

            // Debug.Log($"deltaPosition: {{ {deltaPosition.x:E3}, {deltaPosition.y:E3}, {deltaPosition.z:E3} }}");
            // Debug.Log($"deltaVelocity: {{ {deltaVelocity.x:E3}, {deltaVelocity.y:E3}, {deltaVelocity.z:E3} }}");

            // Debug.Log($"LastdeltaPosition: {{ {(LastPosition - position).x:E3}, {(LastPosition - position).y:E3}, {(LastPosition - position).z:E3} }}");
            // Debug.Log($"LastdeltaVelocity: {{ {(LastVelocity - velocity).x:E3}, {(LastVelocity - velocity).y:E3}, {(LastVelocity - velocity).z:E3} }}");

            // deltaPosition = referenceFrame.PlanetToFrameVector(LastPosition - position);
            // deltaVelocity = referenceFrame.PlanetToFrameVector(LastVelocity - velocity);

            LastPosition = pVPair_Rk.Position;
            LastVelocity = pVPair_Rk.Velocity;

            // var deltaPosition = referenceFrame.PlanetToFrameVector(pVPair_Rk.Position - position_eulerimp);
            // var deltaVelocity = referenceFrame.PlanetToFrameVector(pVPair_Rk.Velocity - velocity_eulerimp);

            // var deltaPosition = referenceFrame.PlanetToFrameVector(craftNode.Parent.GetSolarPositionAtTime(time) + new Vector3d(1000E3, 0, 0) - position);
            // var deltaVelocity = referenceFrame.PlanetToFrameVector(craftNode.Parent.GetSolarVelocityAtTime(time) + new Vector3d(100, 0, 0) - velocity);

            // var deltaPosition = craftNode.Parent.GetSolarPositionAtTime(time) + new Vector3d(10000E3, 0, 0) - position;
            // var deltaVelocity = craftNode.Parent.GetSolarVelocityAtTime(time) + new Vector3d(100, 0, 0) - velocity;





            // Debug.Log("craft FramePosition: " + craftNode.CraftScript.FramePosition);
            // Debug.Log("craft planetposition: " + craftNode.CraftScript.ReferenceFrame.FrameToPlanetPosition(craftNode.CraftScript.FramePosition));
            // Debug.Log("craft SolarPosition1: " + craftNode.SolarPosition);
            // Debug.Log("craft SolarPosition2: " + (craftNode.CraftScript.ReferenceFrame.FrameToPlanetPosition(craftNode.CraftScript.FramePosition) + craftNode.Parent.GetSolarPositionAtTime(Time.fixedTime + FlightState_set_Time_Patch.LastUpdateTime)));
            // Debug.Log("craft d SolarPosition: " + ((craftNode.CraftScript.ReferenceFrame.FrameToPlanetPosition(craftNode.CraftScript.FramePosition) + craftNode.Parent.GetSolarPositionAtTime(Time.fixedTime + FlightState_set_Time_Patch.LastUpdateTime)) - craftNode.SolarPosition));


            foreach (var body in frame.FlightScene.CraftNode.CraftScript.Data.Assembly.Bodies)
            {
                // body.BodyScript.RigidBody.velocity = ToVector3(body.BodyScript.RigidBody.velocity);
                // Debug.Log("bodyvelocity" + body.BodyScript.RigidBody.velocity.ToString() + " position: " + body.BodyScript.RigidBody.position.ToString());
                body.BodyScript.RigidBody.velocity += ToVector3(deltaVelocity);
                body.BodyScript.RigidBody.position += ToVector3(deltaPosition);
            }
            // Debug.Log("MBGFlightPostFixedUpdate coroutine called with fixedTime: " + Time.fixedTime + " time: " + (frame.FlightScene.FlightState.Time - (Time.fixedTime - StartTime.Item2) - StartTime.Item1) + " craft: " + frame.FlightScene.CraftNode.Name);
        }



        public int GetInstanceID()
        {
            return GetHashCode();
        }

        public static UnityEngine.Vector3 ToVector3(Vector3d v)
        {
            return new UnityEngine.Vector3((float)v.x, (float)v.y, (float)v.z);
        }

        public static Vector3d ToVector3d(UnityEngine.Vector3 v)
        {
            return new Vector3d((double)v.x, (double)v.y, (double)v.z);
        }
    }








    // [HarmonyPatch(typeof(FlightState), MethodType.Constructor)]
    // [HarmonyPatch(new Type[] { typeof(FlightStateData), typeof(FlightStateLoadContext) })]
    // public static class FlightState_Patch
    // {
    //     static void Postfix(FlightState __instance)
    //     {
    //         MBGFlightPostFixedUpdate.StartTime = __instance.Time;


    //     }

    // }

    // Harmony postfix patch for the FlightState.Time getter


    // [HarmonyPatch(typeof(FlightSceneScript), "OnUpdate")]
    // [HarmonyPatch(new Type[] { typeof(FlightFrameData) })]
    // public static class FlightSceneScript_OnUpdate_Patch
    // {
    //     static void Postfix(FlightFrameData __instance)
    //     {
    //         MBGFlightPostFixedUpdate.StartTime = __instance.FlightScene.FlightState.Time;

    //     }

    // }

    // public class MBGFlightPostFixedUpdate2 : MonoBehaviour
    // {
    //     private int LastTimeHash;
    //     private Coroutine _updateCoroutine;

    //     private IFlightScene _flightScene;

    //     public void Start()
    //     {
    //         _flightScene = Game.Instance.FlightScene;
    //         if (_flightScene != null)
    //         {
    //             _updateCoroutine = StartCoroutine(PostFixedUpdateCoroutine());
    //         }
    //     }

    //     private void OnDestroy()
    //     {
    //         if (_updateCoroutine != null)
    //         {
    //             StopCoroutine(_updateCoroutine);
    //             _updateCoroutine = null;
    //         }
    //     }

    //     private IEnumerator PostFixedUpdateCoroutine()
    //     {
    //         while (true)
    //         {
    //             // Wait for the end of the fixed update
    //             yield return new WaitForFixedUpdate();

    //             if (_flightScene?.FlightState == null)
    //             {
    //                 yield return null;
    //                 continue;
    //             }

    //             // var frame = _flightScene.FlightState;
    //             // var timeHash = frame.Time.GetHashCode();
    //             // if (timeHash == LastTimeHash)
    //             // {
    //             //     continue; // 如果时间没有变化，则不执行后续逻辑
    //             // }

    //             // LastTimeHash = timeHash;

    //             var deltaTime = _flightScene.TimeManager.DeltaTime;
    //             var position = _flightScene.CraftNode.SolarPosition;
    //             var velocity = _flightScene.CraftNode.SolarVelocity;
    //             velocity = new Vector3d(0, 0, 0);
    //             var craftScript = _flightScene.CraftNode.CraftScript as CraftScript;

    //             foreach (var body in craftScript.Data.Assembly.Bodies)
    //             {
    //                 // body.BodyScript.RigidBody.velocity = ToVector3(body.BodyScript.RigidBody.velocity);
    //                 Debug.Log("bodyvelocity1" + body.BodyScript.RigidBody.velocity + " position: " + body.BodyScript.RigidBody.position);
    //                 // body.BodyScript.RigidBody.velocity = new Vector3(0, 0, 0);
    //                 // body.BodyScript.RigidBody.position += ToVector3(2 * deltaTime * velocity);
    //             }
    //             Debug.Log("MBGFlightPostFixedUpdate2 coroutine called with fixedTime: " + Time.fixedTime + " time: " + _flightScene.FlightState.Time + " craft: " + _flightScene.CraftNode.Name);
    //         }
    //     }

    //     public static Vector3 ToVector3(Vector3d v)
    //     {
    //         return new Vector3((float)v.x, (float)v.y, (float)v.z);
    //     }

    //     public static Vector3d ToVector3d(Vector3 v)
    //     {
    //         return new Vector3d((double)v.x, (double)v.y, (double)v.z);
    //     }
    // }

    // [HarmonyPatch(typeof(FlightGameLoop), "Awake")]
    // public static class FlightGameLoop_Awake_Patch2
    // {
    //     static void Postfix(FlightGameLoop __instance)
    //     {
    //         var go = new GameObject("MBGFlightPostFixedUpdate_Host");
    //         go.AddComponent<MBGFlightPostFixedUpdate2>();
    //         GameObject.DontDestroyOnLoad(go); // Keep it alive between scene changes

    //         Debug.Log("MBGFlightPostFixedUpdate: Custom MonoBehaviour script registered.");
    //     }
    // }



























}