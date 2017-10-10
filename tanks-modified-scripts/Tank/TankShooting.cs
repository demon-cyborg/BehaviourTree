using UnityEngine;
using UnityEngine.UI;

namespace Complete
{
    public class TankShooting : MonoBehaviour
    {
        public int m_PlayerNumber = 1;              // Used to identify the different players.
        public Rigidbody m_Shell;                   // Prefab of the shell.
        public Transform m_FireTransform;           // A child of the tank where the shells are spawned.
        public Slider m_AimSlider;                  // A child of the tank that displays the current launch force.
        public AudioSource m_ShootingAudio;         // Reference to the audio source used to play the shooting audio. NB: different to the movement audio source.
        public AudioClip m_ChargingClip;            // Audio that plays when each shot is charging up.
        public AudioClip m_FireClip;                // Audio that plays when each shot is fired.
        public float m_MinLaunchForce = 15f;        // The force given to the shell if the fire button is not held.
        public float m_MaxLaunchForce = 30f;        // The force given to the shell if the fire button is held for the max charge time.
        public float m_MaxChargeTime = 0.75f;       // How long the shell can charge for before it is fired at max force.

        public bool m_NPC;                          // Is shooting AI controlled?

        private string m_FireButton;                // The input axis that is used for launching shells.
        private float m_CurrentLaunchForce;         // The force that will be given to the shell when the fire button is released.
        private float m_ChargeSpeed;                // How fast the launch force increases, based on the max charge time.

        private int m_FiringState;  // Current state of the firing sequence
        private const int FIRING_INACTIVE = 0;
        private const int FIRING_START = 1;
        private const int FIRING_CHARGE = 2;
        private const int FIRING_RELEASE = 3;

        private bool m_AIFireRequest;         // Does the AI want to fire?
        private float m_AILaunchForce;         // The AI's intended launch force

        private void OnEnable()
        {
            // When the tank is turned on, reset the launch force and the UI
            m_CurrentLaunchForce = m_MinLaunchForce;
            m_AimSlider.value = m_MinLaunchForce;
            m_FiringState = FIRING_INACTIVE;
        }


        private void Start ()
        {
            // The fire axis is based on the player number.
            m_FireButton = "Fire" + m_PlayerNumber;

            // The rate that the launch force charges up is the range of possible forces by the max charge time.
            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
        }


        private void Update ()
        {
            // The slider should have a default value of the minimum launch force.
            m_AimSlider.value = m_MinLaunchForce;

            // Step forward the firing state machine
            switch (m_FiringState) {

                case FIRING_INACTIVE:

                    // AI control
                    if (m_NPC) {
                        // If AI has requested fire...
                        if (m_AIFireRequest) {
                            m_FiringState = FIRING_START;                            
                        }

                    // Player control
                    } else {
                        // If the fire button has just started being pressed...
                        if (Input.GetButtonDown (m_FireButton)) {
                            m_FiringState = FIRING_START;
                        }
                    }
                    break;

                case FIRING_START:
                    // Reset the launch force.
                    m_CurrentLaunchForce = m_MinLaunchForce;

                    // Change the clip to the charging clip and start it playing.
                    m_ShootingAudio.clip = m_ChargingClip;
                    m_ShootingAudio.Play ();

                    // Automatically start charging
                    m_FiringState = FIRING_CHARGE;
                    break;

                case FIRING_CHARGE:

                    // Increment the launch force and update the slider.
                    m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
                    m_AimSlider.value = m_CurrentLaunchForce;

                    // If the max force has been exceeded and the shell hasn't yet been launched...
                    if (m_CurrentLaunchForce >= m_MaxLaunchForce)
                    {
                    // ... use the max force and launch the shell.
                        m_CurrentLaunchForce = m_MaxLaunchForce;
                        m_FiringState = FIRING_RELEASE;
                    }

                    // AI control
                    if (m_NPC) {
                        if (m_CurrentLaunchForce > m_AILaunchForce) {
                            m_FiringState = FIRING_RELEASE;
                        }
    
                    // Player control
                    } else {

                        /// if the fire button is released and the shell hasn't been launched yet...
                        if (Input.GetButtonUp (m_FireButton)) {
                            m_FiringState = FIRING_RELEASE;
                        }
                    }
                    break;

                case FIRING_RELEASE:
                    m_FiringState = FIRING_INACTIVE;
                    m_AIFireRequest = false;
                    Fire();
                    break;
            
            }
        }

        public bool AIFire(float force) {
            if (!m_AIFireRequest) {
                m_AIFireRequest = true;
                force = (force < 0) ? 0 : (force > 1) ? 1 : force;
                m_AILaunchForce = m_MinLaunchForce + force * (m_MaxLaunchForce - m_MinLaunchForce);
                return true;
            } else {
                return false;
            }
        }

        private void Fire ()
        {
            // Create an instance of the shell and store a reference to it's rigidbody.
            Rigidbody shellInstance =
                Instantiate (m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;

            // Set the shell's velocity to the launch force in the fire position's forward direction.
            shellInstance.velocity = m_CurrentLaunchForce * m_FireTransform.forward; 

            // Change the clip to the firing clip and play it.
            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play ();

            // Reset the launch force.  This is a precaution in case of missing button events.
            m_CurrentLaunchForce = m_MinLaunchForce;
        }
    }
}