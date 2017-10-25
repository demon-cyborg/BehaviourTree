using System;
using UnityEngine;
using NPBehave;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEditor;
using Action = NPBehave.Action;

namespace Complete
{
    /*
    Example behaviour trees for the Tank AI.  This is partial definition:
    the core AI code is defined in TankAI.cs.

    Use this file to specifiy your new behaviour tree.
     */
    public partial class TankAI : MonoBehaviour
    {
        /* 
        AI targets
        
        0. Fun - 
            Tank moves around level, alternating between hunting and chasing
            Tank fires inaccurately at target (attack range is like a donut, avoid the center )
            
        1. Deadly - 
            if healthy, tank moves around level hunting target, moves in s curves, fires and moves
            fires 
            if unhealthy, tank avoids target, more likely to dodge
            fires frantically, tank leads its shots
            
        2. Frightened - same as Deadly.unhealthy state
            fires less at player, only takes a shot if within range, otherwise moves erratically
        
        3. Unpredictable - decides between either state 1 - 3 within randomly decided intervals between 0.5f
        
        sub states -         
        */


        private Root CreateBehaviourTree()
        {
            switch (m_Behaviour)
            {
                case 1: //0
                    return SpinBehaviour(-0.05f, 1f);
                case 2: //1
                    return TrackBehaviour();
                case 3: //2
                    return FunAI();
                case 4: //3
                    return FastTrack();
                //case 5:    //6
                //  return CowardlyAI();
                //case 6:    //7
                //  return unpredictableAI();

                default:
                    return new Root(new Action(
                        () => Turn(0.1f)));
            }
        }

        /* Actions */
        private Node ScatterShot(float spray)
        {
            float scatterShot = UnityEngine.Random.Range(-spray, spray);
            return new Action(() => Turn(scatterShot));
        }

        private Node RapidFire(float spray, float reloadSpeed)
        {
            return new Sequence
            (
                ScatterShot(spray),
                new Wait(reloadSpeed),
//                RandomFire()
                new Action(
                    () => Fire(UnityEngine.Random.Range(0.01f, 0.05f))
                )
            );
        }


        private Node StopTurning()
        {
            return new Action(() => Turn(0));
        }

        private Node RandomFire()
        {
            return new Action(() => Fire(UnityEngine.Random.Range(0.0f, 1.0f)));
        }

        private Root RangerDanger()
        {
            return new Root(
                /*if too close: 
                    retreat
                else
                    chase
                */
                new BlackboardCondition
                (
                    "targetDistance",
                    Operator.IS_EQUAL,
                    1.0f,
                    Stops.IMMEDIATE_RESTART,
                    new Action
                    (
                        () => Move(0.90f)
                    )
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
        private Root FastTrack()
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
                                RapidFire(Quickness, Quickness),
                                new BlackboardCondition("targetOnRight",
                                    Operator.IS_EQUAL, true,
                                    Stops.IMMEDIATE_RESTART,
                                    // Turn right toward target
                                    new Action(() => Turn(Quickness))),
                                // Turn left toward target
                                new Action(() => Turn(Quickness))
                            )
                        )
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

        private Root TrackandShoot()
        {
            return new Root(
                new Sequence(
                    TrackBehaviour(),
                    RapidFire(0.1f, 0.1f)
                )
            );
        }

        /**
        BlackboardQuery(string[] keys, Stops stopsOnChange, System.Func<bool> query, Node decoratee): while BlackboardCondition allows to check only one key, this one will observe multiple blackboard keys and evaluate the given query function as soon as one of the value's changes, allowing you to do arbitrary queries on the blackboard. It will stop running nodes based on the stopsOnChange stops rules.
        **/

        private Root FunAI()
//Basic AI template for other variants
        {
            return new Root
            (
                new Service(0.1f, UpdatePerception,

//                    new Selector
                    /*1, movement 
                            if too close
                                if not in front
                                    turn to face
                                    retreat
                            if too far
                                if not in front
                                    turn to retreat                                        
                                forward
                           */
                    new Sequence(
                        TrackandShoot()
                    )
                )
            );
        }
        /*
                    new BlackboardCondition(
                        "targetInFront",
                        Operator.IS_EQUAL,
                        false,
                        Stops.IMMEDIATE_RESTART,
                        new Action
                        (
                            () => Turn(0.3f)
                        )
                    )
                    ,
                    new BlackboardCondition
                    (
                        "targetOnRight",
                        Operator.IS_EQUAL, true,
                        Stops.IMMEDIATE_RESTART,
                        // Turn right toward target
                        //
                        new Action
                        (
                            () => Turn(0.3f)
                        )
                    )
                    ,

                    // Turn left toward target
                    new Action
                    (
                        () => Turn(-0.2f))
                )
        )*/

//        */

        private void UpdatePerception()
        {
            //uses raycasting?
            Vector3 targetPos = TargetTransform().position;
            // Takes position of target
            Vector3 localPos = this.transform.InverseTransformPoint(targetPos);
            // based on the coordinates between this tank and target tank
            Vector3 heading = localPos.normalized;
            //truncates floating point value to one decimal eg "1.0"

            blackboard["targetDistance"] = localPos.magnitude;
            blackboard["targetInFront"] = heading.z > 0;
            //determines if target is 90 degrees or less from tank face
            blackboard["targetOnRight"] = heading.x > 0;
            // if target is directly in front or behind, they will be 0
            blackboard["targetOffCentre"] = Mathf.Abs(heading.x);
            //
//            blackboard["targetHP"] = transform.GetComponent<TankHealth>().;
//            global::GameManager.FindObjectsOfType<object.Tank>()
        }
    }


//        */

    /* design only for single opponent
     data- target health, speedx and speedy
     RadarPulse - gives Tank all data on objects in range
     sets as 1 = obstacle, 2 = target.
     obstacles are stored in a map
     pulse activates every 500ms
          
     * attack-  fire in target's vicinity (accuracy adjustable, speed bias.
     * targetlock(float accuracy, float leadingX, float leadingY)
    // defence- alternate between charging and retreating based on a value provided by both AI and target's HP. 
    aggressionFactor(AIHP, TargetHP)
    
    //(AIHP - TargetHP) determines health advantage
    
    //targetBearing()
    get target's angle relative to AI. the closer to 0, the higher the riskfactor
    
    //
    
    */


    //private Root FunAI()
    //private Root EvasiveAI
    //private Root DeadlyAI
    //Private Root RandomAI
}