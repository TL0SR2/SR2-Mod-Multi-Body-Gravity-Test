using UnityEngine;
using System;
using ModApi.GameLoop.Interfaces;
using ModApi.GameLoop;
using ModApi.Ui;
namespace Assets.Scripts.Flight.Sim.MBG
{
    public class MBGFlightPostFixedUpdate : IFlightPostFixedUpdate
    {

        public bool StartMethodCalled { get; set; }


        public void FlightPostFixedUpdate(in FlightFrameData frame)
        {
            var referenceFrame = frame.FlightScene.CraftNode.CraftScript.ReferenceFrame;
            var craftNode = frame.FlightScene.CraftNode;
            var flightData = craftNode.CraftScript.FlightData;
            if (((flightData.Acceleration - flightData.Gravity).magnitude / flightData.GravityMagnitude) < 0.1)
            {
                Debug.Log("Acceleration: " + flightData.Acceleration);
                Debug.Log("Gravity: " + flightData.Gravity);
                var time = Time.fixedTime - FlightState_set_Time_Patch.LastUpdateFixedTime + FlightState_set_Time_Patch.LastUpdateTime;
                var fixedDeltaTime = Time.fixedDeltaTime;
                var position = referenceFrame.FrameToPlanetPosition(craftNode.CraftScript.FramePosition) + craftNode.Parent.GetSolarPositionAtTime(time);
                var velocity = referenceFrame.FrameToPlanetVelocity(craftNode.CraftScript.FrameVelocity) + craftNode.Parent.GetSolarVelocityAtTime(time);

                P_V_Pair pVPair_Rk = new P_V_Pair(position, velocity);

                pVPair_Rk = MBGMath_CaculationMethod.YoshidaMethod(pVPair_Rk, time, fixedDeltaTime, (time, Position) => MBGOrbit.CalculateGravityAtTime(Position, time));

                var velocity_eulerimp = referenceFrame.PlanetToFrameVelocity(pVPair_Rk.Velocity - craftNode.Parent.GetSolarVelocityAtTime(time + fixedDeltaTime));
                var position_eulerimp = referenceFrame.PlanetToFramePosition(pVPair_Rk.Position - craftNode.Parent.GetSolarPositionAtTime(time + fixedDeltaTime)) - fixedDeltaTime * referenceFrame.Velocity;
                var velocity_eulerimp_rev = velocity_eulerimp - fixedDeltaTime * craftNode.CraftScript.FlightData.GravityFrame;
                var position_eulerimp_rev = position_eulerimp - fixedDeltaTime * velocity_eulerimp;

                var deltaPosition = position_eulerimp_rev - craftNode.CraftScript.FramePosition;
                var deltaVelocity = velocity_eulerimp_rev - craftNode.CraftScript.FrameVelocity;

                // Debug.Log($"deltaPosition: {{ {deltaPosition.x:E3}, {deltaPosition.y:E3}, {deltaPosition.z:E3} }}");
                // Debug.Log($"deltaVelocity: {{ {deltaVelocity.x:E3}, {deltaVelocity.y:E3}, {deltaVelocity.z:E3} }}");

                foreach (var body in frame.FlightScene.CraftNode.CraftScript.Data.Assembly.Bodies)
                {
                    // body.BodyScript.RigidBody.velocity = ToVector3(body.BodyScript.RigidBody.velocity);
                    // Debug.Log("bodyvelocity" + body.BodyScript.RigidBody.velocity.ToString() + " position: " + body.BodyScript.RigidBody.position.ToString());
                    body.BodyScript.RigidBody.velocity += deltaVelocity;
                    body.BodyScript.RigidBody.position += deltaPosition.ToVector3();
                }

            }


        }



        public int GetInstanceID()
        {
            return GetHashCode();
        }

        public static Vector3d ToVector3d(UnityEngine.Vector3 v)
        {
            return new Vector3d((double)v.x, (double)v.y, (double)v.z);
        }
    }




}