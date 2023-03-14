using UnityEngine;

namespace Haply.hAPI
{
    public abstract class Mechanism : MonoBehaviour
    {
        /**
         * Performs the forward kinematics physics calculation of a specific physical mechanism
         *
         * @param	angles angular inpujts of physical mechanisms (array element length based
         *			on the degree of freedom of the mechanism in question)
         */
        public abstract void ForwardKinematics ( float[] angles );


        /**
         * Performs velocity calculations at the end-effector of the device 
         *
         * @param	angularVelocities the angular velocities in (deg/s) of the active encoders 
         */
        public abstract void VelocityCalculation ( float[] angularVelocities );

        /**
         * Performs torque calculations that actuators need to output
         *
         * @param	force force values calculated from physics simulation that needs to be conteracted
         */
        public abstract void TorqueCalculation ( float[] forces );

        /**
         * Performs force calculations
         */
        public abstract void ForceCalculation ();

        /**
         * Performs calculations for position control
         */
        public abstract void PositionControl ( float[] position );

        /**
         * Performs inverse kinematics calculations
         */
        public abstract void InverseKinematics ();

        /**
         * Initializes or changes mechanisms parameters
         *
         * @param	parameters mechanism parameters 
         */
        public abstract void SetMechanismParameters ( float[] parameters );

        /**
         * Sets and updates sensor data that may be used by the mechanism
         *
         * @param	data sensor data from sensors attached to Haply board
         */
        public abstract void SetSensorData ( float[] data );

        /**
         * @return	end-effector coordinate position
         */
        public abstract void GetCoordinate ( float[] buffer );

        /**
         * @return	torque values from physics calculations
         */
        public abstract void GetTorque ( float[] buffer );

        /**
         * @return	angle values from physics calculations
         */
        public abstract void GetAngle ( float[] buffer );

        /**
         * @return	velocity values from physics calculations
         */
        public abstract void GetAngularVelocity ( float[] buffer );

        /**
         * @return	velocity values from physics calculations
         */
        public abstract void GetVelocity ( float[] buffer );
    }
}