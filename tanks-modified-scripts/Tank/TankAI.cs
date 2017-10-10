using UnityEngine;
using NPBehave;
using System.Collections.Generic;

namespace Complete
{
    /*
    Script for the Tank AI.  This is partial definition: behaviour trees 
    for this class are defined in Behaviours.cs
    */
    public partial class TankAI : MonoBehaviour
    {
        public int m_PlayerNumber = 1;      // Used to identify which tank belongs to which player.  This is set by this tank's manager.
        public int m_Behaviour = 0;         // Used to select an AI behaviour in the Unity Inspector

        private TankMovement m_Movement;    // Reference to tank's movement script, used by the AI to control movement.
        private TankShooting m_Shooting;    // Reference to tank's shooting script, used by the AI to fire shells.
        private List<GameObject> m_Targets; // List of enemy targets for this tank
        private Root tree;                  // The tank's behaviour tree
        private Blackboard blackboard;      // The tank's behaviour blackboard

        // Initialisation
        private void Awake()
        {
            m_Targets = new List<GameObject>();
        }

        // Start behaviour tree
        private void Start() {
            Debug.Log("Initialising AI player " + m_PlayerNumber);
            m_Movement = GetComponent<TankMovement> ();
            m_Shooting = GetComponent<TankShooting> ();

            tree = CreateBehaviourTree();
            blackboard = tree.Blackboard;
            #if UNITY_EDITOR
            Debugger debugger = (Debugger)this.gameObject.AddComponent(typeof(Debugger));
            debugger.BehaviorTree = tree;
            #endif
            
            tree.Start();
        }

        // Register an enemy target 
        public void AddTarget(GameObject target) {
            m_Targets.Add(target);
        }

        // Get the transform for the first target
        private Transform TargetTransform() {
            if (m_Targets.Count > 0) {
                return m_Targets[0].transform;
            } else {
                return null;
            }
        }

        // ACTION: move the tank with a velocity between -1 and 1.
        // -1: fast reverse
        // 0: no change
        // 1: fast forward
        private void Move(float velocity) { 
            m_Movement.AIMove(velocity);
        }
        
        // ACTION: turn the tank with angular velocity between -1 and 1.
        // -1: fast turn left
        // 0: no change
        // 1: fast turn right
        private void Turn(float velocity) { 
            m_Movement.AITurn(velocity);
        }

        // ACTION: fire a shell with a force between 0 and 1
        // 0: minimum force
        // 1: maximum force
        private void Fire(float force) { 
            m_Shooting.AIFire(force);
        }

    }
}