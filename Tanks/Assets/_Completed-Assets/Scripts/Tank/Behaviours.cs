using System;
using UnityEngine;
using NPBehave;
using System.Collections.Generic;
using System.ComponentModel;
//using System.Dynamic;
using System.Linq.Expressions;
using System.Runtime.Remoting.Messaging;
using BehaviourMachine;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine.AI;
using Action = NPBehave.Action;
using Selector = NPBehave.Selector;
using Sequence = NPBehave.Sequence;
using Wait = NPBehave.Wait;


namespace Complete
{
    /*
    Example behaviour trees for the Tank AI.  This is partial definition:
    the core AI code is defined in TankAI.cs.

    Use this file to specifiy your new behaviour tree.
     */
    public partial class TankAI : MonoBehaviour
    {
        private Root CreateBehaviourTree()
        {
            switch (m_Behaviour)
            {
                case 1: //0
                    return SpinBehaviour(-0.05f, 1f);
                case 2: //1
                    return TrackBehaviour();
                case 3: //2
                    return FunAi();
//                
                //case 4:    //3
                //  return CowardlyAI();
                //case 5:    //4
                //  return unpredictableAI();

                default:
                    return new Root(new Action(
                        () => Turn(0.1f)));
            }
        }

        /* Actions */

        private Node StopTurning()
        {
            return new Action(() => Turn(0));
        }

        private Node RandomFire()
        {
            return new Action(() => Fire(UnityEngine.Random.Range(0.0f, 1.0f)));
        }

        //PREVENTS SELF HARM, allows an action if enough of a gap
        private Root SafetyFirst(Node guns)
        {
            return new Root
            (
                new BlackboardCondition
                (
                    "targetDistance",
                    Operator.IS_GREATER,
                    2.0f,
                    Stops.IMMEDIATE_RESTART,
                    guns
                )
            );
        }


        /* Example behaviour trees */
        // Constantly spin and fire on the spot 
        private Root SpinBehaviour(float turn, float shoot)
        {
            return new Root(new Sequence(
                new Action(() => Turn(turn)),
                new Action(() => Fire(shoot))
            ));
        }

        // Turn to face your opponent and fire
        private Root FastTrack(Root fight)
        {
            float Quickness = 0.01f;
            float inaccurate = UnityEngine.Random.Range(-Quickness, Quickness);
            return new Root(
                new Service(Quickness, UpdatePerception,
                    new Selector(
                        new BlackboardCondition("targetOffCentre",
                            Operator.IS_SMALLER_OR_EQUAL, true,
                            Stops.IMMEDIATE_RESTART,
                            // Stop turning and fire
                            new Sequence(StopTurning(),
//                                new Wait(Quickness),
                                new BlackboardCondition("targetOnRight",
                                    Operator.IS_EQUAL, true,
                                    Stops.IMMEDIATE_RESTART,
                                    // Turn right toward target
                                    new Action(() => Turn(Quickness))),
                                // Turn left toward target
                                new Action(() => Turn(Quickness))
                            )
                        ),
                        fight
                    )
                )
            );
        }

        private Root TrackBehaviour()
        {
            return new Root(
                new Service(0.2f, UpdatePerception,
                    new Selector(
                        new BlackboardCondition("targetOffCentre",
                            Operator.IS_SMALLER_OR_EQUAL, 0.1f,
                            Stops.IMMEDIATE_RESTART,
                            // Stop turning and fire
                            new Sequence(StopTurning(),
                                new Wait(2f),
                                RandomFire())),
                        new BlackboardCondition("targetOnRight",
                            Operator.IS_EQUAL, true,
                            Stops.IMMEDIATE_RESTART,
                            // Turn right toward target
                            new Action(() => Turn(0.2f))),
                        // Turn left toward target
                        new Action(() => Turn(-0.2f))
                    )
                )
            );
        }


        private Root FunAi()
        {
            return new Root
            (
                new Service
                (
                    0.3f, UpdatePerception,
                    new Selector
                    (
                        new BlackboardCondition
                        (
                            "collisionDetect",
                            Operator.IS_EQUAL, true,
                            Stops.IMMEDIATE_RESTART,
                            new Selector(
                                new BlackboardCondition
                                (
                                    "targetInFront",
                                    Operator.IS_EQUAL, true,
                                    Stops.IMMEDIATE_RESTART,
                                    new Sequence(
                                        new Action(() => Turn(-1f)
                                        ),
                                        new Action(() => Move(-0.5f)
                                        )
                                    )
                                ),
                                new BlackboardCondition
                                (
                                    "targetOffCentre",
                                    Operator.IS_EQUAL, true,
                                    Stops.IMMEDIATE_RESTART,
                                    new Action(() => Turn(1f)
                                    )
                                ),
                                new Action(() => Turn(-1f)
                                )
                            )
                        ),
                        new BlackboardCondition
                        (
                            "targetInFront",
                            Operator.IS_EQUAL, true,
                            Stops.IMMEDIATE_RESTART,
                            new Selector(
                                new BlackboardCondition
                                (
                                    "targetDistance",
                                    Operator.IS_SMALLER, 10f,
                                    Stops.IMMEDIATE_RESTART,
                                    new Selector(
                                        new Action(() => Move(-1f)),
                                        new Action(() => Fire(0.01f))
                                    )
                                ),
                                new Selector(
                                    new Action(() => Fire(0.4f)),
                                    new Action(() => Move(1f))
                                )
                            )
                        ),
                        new BlackboardCondition
                        (
                            "targetOnRight",
                            Operator.IS_EQUAL, true,
                            Stops.IMMEDIATE_RESTART,
                            new Action(() => Turn(1f)))
                        ,
                        new Action(() => Turn(-1f))
                    )
                )
            );
        }


        private Root FunEE()
            //Basic AI template for other variants
        {
            return new Root
                (
                    new Service
                    (
                        0.2f, UpdatePerception,
                        new Selector
                        (
                            new BlackboardCondition
                            (
                                //too close, retreat
                                "targetDistance",
                                Operator.IS_SMALLER,
                                10.0f,
                                Stops.IMMEDIATE_RESTART,
                                new Action(() => Move(-1.0f)
                                )
                            ),
                            new BlackboardCondition
                            (
                                //too far, close in
                                "targetDistance",
                                Operator.IS_GREATER_OR_EQUAL,
                                10.0f,
                                Stops.IMMEDIATE_RESTART,
                                new Action(() => Move(0.5f)
                                )
                            ),
                            new BlackboardCondition
                            (
                                //too close, retreat
                                "targetDistance",
                                Operator.IS_SMALLER,
                                10.0f,
                                Stops.IMMEDIATE_RESTART,
                                new Action(() => Fire(0.2f))
                            )
                            ,
                            new BlackboardCondition
                            (
                                "targetOffCentre",
                                Operator.IS_SMALLER_OR_EQUAL, 0.3f,
                                Stops.IMMEDIATE_RESTART,
                                new Action(() => Move(1.0f)
                                    // Stop turning and fire
                                )
                            )
                            ,
                            new BlackboardCondition
                            (
                                "targetOnRight",
                                Operator.IS_EQUAL, true,
                                Stops.IMMEDIATE_RESTART,
                                // Turn right toward target
                                new Action(() => Turn(0.3f)
                                )
                            )
                            ,
                            // Turn left toward target
                            new BlackboardCondition
                            (
                                "targetOnRight",
                                Operator.IS_EQUAL, false,
                                Stops.IMMEDIATE_RESTART,
                                // Turn right toward target
                                new Action(() => Turn(-0.3f)
                                )
                            )
//                        ,
//                        new BlackboardCondition
//                        (
//                            //too close, retreat
//                            "targetDistance",
//                            Operator.IS_SMALLER,
//                            10.0f,
//                            Stops.IMMEDIATE_RESTART,
//                            new Action(() => Move(-1.0f)
//                            )
//                        ),
//                        new BlackboardCondition
//                        (
//                            //too far, close in
//                            "targetDistance",
//                            Operator.IS_GREATER_OR_EQUAL,
//                            10.0f,
//                            Stops.IMMEDIATE_RESTART,
//                            new Action(() => Move(0.5f)
//                            )
//                        ),
//                        new BlackboardCondition
//                        (
//                            //too close, retreat
//                            "targetDistance",
//                            Operator.IS_SMALLER,
//                            10.0f,
//                            Stops.IMMEDIATE_RESTART,
//                            new Action(() => Fire(0.2f))
                        )
                    )
                )
                ;
        }


        private void UpdatePerception()
        {
            Vector3 targetPos = TargetTransform().position;
            // Takes position of target
            Vector3 localPos = this.transform.InverseTransformPoint(targetPos);
            // based on the coordinates between this tank and target tank
            Vector3 heading = localPos.normalized;
            //truncates floating point value to one decimal eg "1.0"
//            Vector3 aboveTank = TargetTransform().position(0,1,0);
            Vector3 collide = transform.TransformDirection(0, 1.5f, 1);
            //detects object in right, up, forward
            bool collisionDetect = Physics.Raycast(transform.position, collide, 10);

            blackboard["targetDistance"] = localPos.magnitude;
            blackboard["targetInFront"] = heading.z > 0;
            //determines if target is 90 degrees or less from tank face
            blackboard["targetOnRight"] = heading.x > 0;
            // if target is directly in front or behind, they will be 0
            blackboard["targetOffCentre"] = Mathf.Abs(heading.x);
            //
            blackboard["CollisionDetect"] = collisionDetect;
        }
    }
}
/*
private void UpdatePerception() {
Vector3 targetPos = TargetTransform().position;
Vector3 localPos = this.transform.InverseTransformPoint(targetPos);
Vector3 heading = localPos.normalized;
Vector3 inFront = transform.TransformDirection (Vector3.forward);
bool objectBlocking = Physics.Raycast (transform.position, inFront, 5);
            
blackboard["targetDistance"] = localPos.magnitude;
blackboard["targetInFront"] = heading.z > 0;
blackboard["targetOnRight"] = heading.x > 0;
blackboard["targetOffCentre"] = Mathf.Abs(heading.x);
blackboard ["objectBlock"] = objectBlocking;
}
*/