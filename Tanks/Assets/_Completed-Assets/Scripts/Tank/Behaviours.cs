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
using UnityEditor.SceneManagement;
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
                case 1:
                    return SpinBehaviour(-0.05f, 1f);
                case 2:
                    return TrackBehaviour();
                case 3:
                    return FunAi();
                case 4:
                    return RandomAI();
                case 5:
                    return CowardlyAi();
                case 6:
                    return DeadlyAi();


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

        private Root DeadlyAi()
        {
            return new Root
            (
                new Service(
                    1f, UpdatePerception,
                    new Selector(
                        new BlackboardCondition(
                            "targetOffCentre",
                            Operator.IS_SMALLER, 45f,
                            Stops.IMMEDIATE_RESTART,
                            new Sequence(
                                StopTurning(),
                                new Action(() => Turn(1f)),
                                new Action(() => Move(0.5f)),
                                new Action(() => Fire(0.2f)),
                                new BlackboardCondition(
                                    "targetDistance",
                                    Operator.IS_SMALLER, 10f,
                                    Stops.IMMEDIATE_RESTART,
                                    new Selector(
                                        //retreat
                                        //new Action(() => Move(-1f)),
                                        new BlackboardCondition(
                                            "rearCollision",
                                            Operator.IS_EQUAL, true, Stops.IMMEDIATE_RESTART,
                                            new Selector(
                                                new Action(() => Turn(1f)),
                                                new Action(() => Move(0.5f)),
                                                new Action(() => Fire(0.2f)),
                                                new BlackboardCondition(
                                                    "CollisionDetect",
                                                    Operator.IS_EQUAL, true, Stops.IMMEDIATE_RESTART,
                                                    new Selector(
                                                        new Action(() => Turn(-1f)),
                                                        new Action(() => Move(-0.5f)))))),
                                        new Action(() => Move(-1f))
                                    )
                                ))), //target not on right
                        new Action(() => Turn(-1f)),
                        new Action(() => Move(-0.5f)),
                        new Action(() => Fire(0.2f))
                    )));
        }

//

        private Root FunAi()
        {
            return new Root
            (
                new Service
                (
                    1f, UpdatePerception,
                    new Selector(
                        new BlackboardCondition(
                            "CollisionDetect", Operator.IS_EQUAL, false,
                            Stops.LOWER_PRIORITY_IMMEDIATE_RESTART,
                            //yes to rearcollision
                            new Selector(
                                new BlackboardCondition
                                ("rearCollision", Operator.IS_EQUAL, false,
                                    Stops.IMMEDIATE_RESTART,
                                    new Selector(
                                        new Action(() => Turn(-1f)),
                                        new Action(() => Move(1f))
                                    )
                                ),
                                new Selector(
                                    new Action(() => Move(1f)),
                                    new Action(() => Turn(-1f)))
                            )

                            //no
                            //target in front?
                        ), new BlackboardCondition("targetDistance", Operator.IS_GREATER, 0.5f,
                            Stops.IMMEDIATE_RESTART,
                            new Selector(
                                //target close?
                                new BlackboardCondition("targetInFront",
                                    Operator.IS_EQUAL, true,
                                    Stops.IMMEDIATE_RESTART,
                                    new Action(() => Move(-1f)
                                    )
                                ), //no
                                new Action(() => Move(1f)),
                                new Action(() => Fire(0.2f)
                                )
                            )
                        ),
                        new BlackboardCondition("targetOnRight", Operator.IS_EQUAL, true,
                            Stops.IMMEDIATE_RESTART,
                            new Action(() => Turn(1f)
                            )
                            //no to rearcollision
                        ), new Action(() => Turn(-1f))))
            );
        }

        private Root CowardlyAi()
        {
            return new Root
            (
                new Service
                (
                    1f, UpdatePerception,
                    new Selector(
                        new BlackboardCondition(
                            "rearCollision", Operator.IS_EQUAL, true,
                            Stops.IMMEDIATE_RESTART,
                            //yes to rearcollision
                            new Selector(
                                new BlackboardCondition
                                ("CollisionDetect", Operator.IS_EQUAL, true,
                                    Stops.IMMEDIATE_RESTART,
                                    new Action(() => Turn(UnityEngine.Random.Range(-1f, 1f)))
                                ),
                                new Action(() => Move(1f)))

                            //no
                            //target in front?
                        ), new BlackboardCondition("targetInFront", Operator.IS_EQUAL, true,
                            Stops.IMMEDIATE_RESTART,
                            new Selector(
                                //target close?
                                new BlackboardCondition("targetDistance",
                                    Operator.IS_GREATER,
                                    2.0f,
                                    Stops.IMMEDIATE_RESTART,
                                    new Action(() => Move(-1f)
                                    )
                                ), //no
                                new Action(() => Move(1f)
                                )
                            )
                        ),
                        new BlackboardCondition("targetOnRight", Operator.IS_EQUAL, true,
                            Stops.IMMEDIATE_RESTART,
                            new Action(() => Turn(1f)
                            )
                            //no to rearcollision
                        ), new Action(() => Turn(-1f))))
            );
        }


        private Root RandomAI()
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
                            Operator.IS_GREATER, true,
                            Stops.IMMEDIATE_RESTART,
                            new Selector(
                                new BlackboardCondition
                                (
                                    "targetInFront",
                                    Operator.IS_EQUAL, true,
                                    Stops.IMMEDIATE_RESTART,
                                    new Selector(
                                        new BlackboardCondition
                                        (
                                            "targetOffCentre",
                                            Operator.IS_SMALLER, 0.9f,
                                            Stops.IMMEDIATE_RESTART,
                                            new Selector()
                                        ),
                                        new Action(() => Fire(1f)),
                                        new Action(() => Turn(-1f)),
                                        new Action(() => Move(-0.5f)
                                        )
                                        , new Sequence(
                                            new Action(() => Turn(1f)),
                                            new Action(() => Move(0.5f))
                                        ),
                                        new BlackboardCondition
                                        (
                                            "targetOffCentre",
                                            Operator.IS_SMALLER, 0.1f,
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
                                            Operator.IS_SMALLER, 15f,
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
                    )));
        }

        private void UpdatePerception()
        {
            Vector3 targetPos = TargetTransform().position;
            // Takes position of target
            Vector3 localPos = this.transform.InverseTransformPoint(targetPos);
            // based on the coordinates between this tank and target tank
            Vector3 heading = localPos.normalized;
            //truncates floating point value to one decimal eg "1.0"

            Vector3 collide = transform.TransformDirection(0, 1, 1);
            Vector3 rearCollide = transform.TransformDirection(0, 1, -1);
            //detects object in right, up, forward, raycast must be raised to avoid floor collision
            bool collisionDetect = Physics.Raycast(transform.position, collide, 10);
            bool rearDetect = Physics.Raycast(transform.position, rearCollide, 10);

            blackboard["targetDistance"] = localPos.magnitude;
            blackboard["targetInFront"] = heading.z > 0;
            //determines if target is 90 degrees or less from tank face
            blackboard["targetOnRight"] = heading.x > 0;
            // if target is directly in front or behind, they will be 0
            blackboard["targetOffCentre"] = Mathf.Abs(heading.x);
            //
            blackboard["CollisionDetect"] = collisionDetect;
            blackboard["rearCollision"] = rearDetect;
        }
    }
}