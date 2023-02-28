using System;
using UnityEngine;

namespace Haply.hAPI
{
    public class Device : MonoBehaviour
    {
        [SerializeField]
        private byte m_DeviceID;

        [SerializeField]
        private Mechanism m_Mechanism;

        private byte m_CommunicationType;

        private int m_ActuatorsActive;
        private int m_EncodersActive;
        private int m_SensorsActive;
        private int m_PwmsActive;

        private Actuator[] m_Motors = new Actuator[0];
        private Sensor[] m_Encoders = new Sensor[0];
        private Sensor[] m_Sensors = new Sensor[0];
        private Pwm[] m_Pwms = new Pwm[0];

        private byte[] m_ActuatorPositions = { 0, 0, 0, 0 };
        private byte[] m_EncoderPositions = { 0, 0, 0, 0 };

        [SerializeField]
        private Board m_BoardLink;

		#region device setup functions

		// device setup functions
		/**
		 * add new actuator to platform
		 *
		 * @param    actuator index of actuator (and index of 1-4)
		 * @param    roatation positive direction of actuator rotation
		 * @param    port specified motor port to be used (motor ports 1-4 on the Haply board) 
		 */
		public void AddActuator ( int actuator, int rotation, int port )
		{

			bool error = false;

			if ( port < 1 || port > 4 )
			{
				Debug.LogError( " encoder port index out of bounds" );
				error = true;
			}

			if ( actuator < 1 || actuator > 4 )
			{
				Debug.LogError( " encoder index out of bound!" );
				error = true;
			}

			int j = 0;
			for ( int i = 0; i < m_ActuatorsActive; i++ )
			{
				if ( m_Motors[i].actuator < actuator )
				{
					j++;
				}

				if ( m_Motors[i].actuator == actuator )
				{
					Debug.LogError( " actuator " + actuator + " has already been set" );
					error = true;
				}
			}

			if ( !error )
			{
				Actuator[] temp = new Actuator[m_ActuatorsActive + 1];

				Array.Copy( m_Motors, 0, temp, 0, m_Motors.Length );

				if ( j < m_ActuatorsActive )
				{
					Array.Copy( m_Motors, j, temp, j + 1, m_Motors.Length - j );
				}

				temp[j] = new Actuator( actuator, rotation, port );
				ActuatorAssignment( actuator, port );

				m_Motors = temp;
				m_ActuatorsActive++;
			}
		}


		/**
		 * Add a new encoder to the platform
		 *
		 * @param    actuator index of actuator (an index of 1-4)
		 * @param    positive direction of rotation detection
		 * @param    offset encoder offset in degrees
		 * @param    resolution encoder resolution
		 * @param    port specified motor port to be used (motor ports 1-4 on the Haply board) 
		 */
		public void AddEncoder ( int encoder, int rotation, float offset, float resolution, int port )
		{
			bool error = false;

			if ( port < 1 || port > 4 )
			{
				Debug.LogError( " encoder port index out of bounds" );
				error = true;
			}

			if ( encoder < 1 || encoder > 4 )
			{
				Debug.LogError( " encoder index out of bound!" );
				error = true;
			}

			// determine index for copying
			int j = 0;
			for ( int i = 0; i < m_EncodersActive; i++ )
			{
				if ( m_Encoders[i].encoder < encoder )
				{
					j++;
				}

				if ( m_Encoders[i].encoder == encoder )
				{
					Debug.LogError( " encoder " + encoder + " has already been set" );
					error = true;
				}
			}

			if ( !error )
			{
				Sensor[] temp = new Sensor[m_EncodersActive + 1];

				Array.Copy( m_Encoders, 0, temp, 0, m_Encoders.Length );

				if ( j < m_EncodersActive )
				{
					Array.Copy( m_Encoders, j, temp, j + 1, m_Encoders.Length - j );
				}

				temp[j] = new Sensor( encoder, rotation, offset, resolution, port );
				EncoderAssignment( encoder, port );

				m_Encoders = temp;
				m_EncodersActive++;
			}
		}


		/**
		 * Add an analog sensor to platform
		 *
		 * @param    pin the analog pin on haply board to be used for sensor input (Ex: A0)
		 */
		public void AddAnalogSensor ( String pin )
		{
			// set sensor to be size zero
			bool error = false;

			char port = pin[0];
			String number = pin.Substring( 1 );

			int value = int.Parse( number );
			value = value + 54;

			for ( int i = 0; i < m_SensorsActive; i++ )
			{
				if ( value == m_Sensors[i].port )
				{
					Debug.LogError( " Analog pin: A" + (value - 54) + " has already been set" );
					error = true;
				}
			}

			if ( port != 'A' || value < 54 || value > 65 )
			{
				Debug.LogError( " outside analog pin range" );
				error = true;
			}

			if ( !error )
			{
				Sensor[] temp = new Sensor[m_Sensors.Length + 1]; 
				Array.Copy( m_Sensors, temp, m_Sensors.Length );
				temp[m_SensorsActive] = new Sensor();
				temp[m_SensorsActive].port = ( value );
				m_Sensors = temp;
				m_SensorsActive++;
			}
		}


		/**
		 * Add a PWM output pin to the platform
		 *
		 * @param		pin the pin on the haply board to use as the PWM output pin 
		 */
		public void AddPwmPin ( int pin )
		{

			bool error = false;

			for ( int i = 0; i < m_PwmsActive; i++ )
			{
				if ( pin == m_Pwms[i].pin )
				{
					Debug.LogError( " pwm pin: " + pin + " has already been set" );
					error = true;
				}
			}

			if ( pin < 0 || pin > 13 )
			{
				Debug.LogError( " outside pwn pin range" );
				error = true;
			}

			if ( pin == 0 || pin == 1 )
			{
				Debug.LogWarning( "0 and 1 are not pwm pins on Haply M3 or Haply original" );
			}


			if ( !error )
			{
				Pwm[] temp = new Pwm[m_Pwms.Length + 1];
				Array.Copy( m_Pwms, temp, m_Pwms.Length );
				temp[m_PwmsActive] = new Pwm();
				temp[m_PwmsActive].pin = ( pin );
				m_Pwms = temp;
				m_PwmsActive++;
			}


		}


		/**
		 * Set the device mechanism that is to be used
		 *
		 * @param    mechanisms new Mechanisms for use
		 */
		public void set_mechanism ( Mechanism mechanism )
		{
			m_Mechanism = mechanism;
		}



		/**
		 * Gathers all encoder, sensor, pwm setup inforamation of all encoders, sensors, and pwm pins that are 
		 * initialized and sequentialy formats the data based on specified sensor index positions to send over 
		 * serial port interface for hardware device initialization
		 */
		public void DeviceSetParameters ()
		{
			m_CommunicationType = 1;

			int control;

			float[] encoderParameters;

			byte[] encoderParams;
			byte[] motorParams;
			byte[] sensorParams;
			byte[] pwmParams;

			if ( m_EncodersActive > 0 )
			{
				encoderParams = new byte[m_EncodersActive + 1];
				control = 0;

				for ( int i = 0; i < m_Encoders.Length; i++ )
				{
					if ( m_Encoders[i].encoder != (i + 1) )
					{
						Debug.LogWarning( "improper encoder indexing" );
						m_Encoders[i].encoder = ( i + 1 );
						m_EncoderPositions[m_Encoders[i].port - 1] = (byte) m_Encoders[i].encoder;
					}
				}

				for ( int i = 0; i < m_EncoderPositions.Length; i++ )
				{
					control = control >> 1;

					if ( m_EncoderPositions[i] > 0 )
					{
						control = control | 0x0008;
					}
				}

				encoderParams[0] = (byte) control;

				encoderParameters = new float[2 * m_EncodersActive];

				int j = 0;
				for ( int i = 0; i < m_EncoderPositions.Length; i++ )
				{
					if ( m_EncoderPositions[i] > 0 )
					{
						encoderParameters[2 * j] = m_Encoders[m_EncoderPositions[i] - 1].encoderOffset;
						encoderParameters[2 * j + 1] = m_Encoders[m_EncoderPositions[i] - 1].encoderResolution;
						j++;
						encoderParams[j] = (byte) m_Encoders[m_EncoderPositions[i] - 1].direction;
					}
				}
			}
			else
			{
				encoderParams = new byte[1];
				encoderParams[0] = 0;
				encoderParameters = new float[0];
			}


			if ( m_ActuatorsActive > 0 )
			{
				motorParams = new byte[m_ActuatorsActive + 1];
				control = 0;

				for ( int i = 0; i < m_Motors.Length; i++ )
				{
					if ( m_Motors[i].actuator != (i + 1) )
					{
						Debug.LogWarning( "improper actuator indexing" );
						m_Motors[i].actuator = ( i + 1 );
						m_ActuatorPositions[m_Motors[i].port - 1] = (byte) m_Motors[i].actuator;
					}
				}

				for ( int i = 0; i < m_ActuatorPositions.Length; i++ )
				{
					control = control >> 1;

					if ( m_ActuatorPositions[i] > 0 )
					{
						control = control | 0x0008;
					}
				}

				motorParams[0] = (byte) control;

				int j = 1;
				for ( int i = 0; i < m_ActuatorPositions.Length; i++ )
				{
					if ( m_ActuatorPositions[i] > 0 )
					{
						motorParams[j] = (byte) m_Motors[m_ActuatorPositions[i] - 1].direction;
						j++;
					}
				}
			}
			else
			{
				motorParams = new byte[1];
				motorParams[0] = 0;
			}


			if ( m_SensorsActive > 0 )
			{
				sensorParams = new byte[m_SensorsActive + 1];
				sensorParams[0] = (byte) m_SensorsActive;

				for ( int i = 0; i < m_SensorsActive; i++ )
				{
					sensorParams[i + 1] = (byte) m_Sensors[i].port;
				}

				Array.Sort( sensorParams );

				for ( int i = 0; i < m_SensorsActive; i++ )
				{
					m_Sensors[i].port = ( sensorParams[i + 1] );
				}

			}
			else
			{
				sensorParams = new byte[1];
				sensorParams[0] = 0;
			}


			if ( m_PwmsActive > 0 )
			{
				byte[] temp = new byte[m_PwmsActive];

				pwmParams = new byte[m_PwmsActive + 1];
				pwmParams[0] = (byte) m_PwmsActive;


				for ( int i = 0; i < m_PwmsActive; i++ )
				{
					temp[i] = (byte) m_Pwms[i].pin;
				}

				Array.Sort( temp );

				for ( int i = 0; i < m_PwmsActive; i++ )
				{
					m_Pwms[i].pin = ( temp[i] );
					pwmParams[i + 1] = (byte) m_Pwms[i].pin;
				}

			}
			else
			{
				pwmParams = new byte[1];
				pwmParams[0] = 0;
			}


			byte[] encMtrSenPwm = new byte[motorParams.Length + encoderParams.Length + sensorParams.Length + pwmParams.Length];
			Array.Copy( motorParams, 0, encMtrSenPwm, 0, motorParams.Length );
			Array.Copy( encoderParams, 0, encMtrSenPwm, motorParams.Length, encoderParams.Length );
			Array.Copy( sensorParams, 0, encMtrSenPwm, motorParams.Length + encoderParams.Length, sensorParams.Length );
			Array.Copy( pwmParams, 0, encMtrSenPwm, motorParams.Length + encoderParams.Length + sensorParams.Length, pwmParams.Length );

			m_BoardLink.Transmit( m_CommunicationType, m_DeviceID, encMtrSenPwm, encoderParameters );
		}


		/**
		 * assigns actuator positions based on actuator port
		 */
		private void ActuatorAssignment ( int actuator, int port )
		{
			if ( m_ActuatorPositions[port - 1] > 0 )
			{
				Debug.LogWarning( "double check actuator port usage" );
			}

			m_ActuatorPositions[port - 1] = (byte) actuator;
		}


		/**
		 * assigns encoder positions based on actuator port
		 */
		private void EncoderAssignment ( int encoder, int port )
		{

			if ( m_EncoderPositions[port - 1] > 0 )
			{
				Debug.LogWarning( "double check encoder port usage" );
			}

			m_EncoderPositions[port - 1] = (byte) encoder;
		}



		// device communication functions
		/**
		 * Receives angle position and sensor inforamation from the serial port interface and updates each indexed encoder 
		 * sensor to their respective received angle and any analog sensor that may be setup
		 */
		public void DeviceReadData ()
		{
			m_CommunicationType = 2;
			int dataCount = 0;

			//float[] device_data = new float[sensorUse + encodersActive];
			float[] device_data = m_BoardLink.Receive( m_CommunicationType, m_DeviceID, m_SensorsActive + m_EncodersActive );

			for ( int i = 0; i < m_SensorsActive; i++ )
			{
				m_Sensors[i].value = ( device_data[dataCount] );
				dataCount++;
			}

			for ( int i = 0; i < m_EncoderPositions.Length; i++ )
			{
				if ( m_EncoderPositions[i] > 0 )
				{
					m_Encoders[m_EncoderPositions[i] - 1].value = ( device_data[dataCount] );
					dataCount++;
				}
			}
		}


		/**
		 * Requests data from the hardware based on the initialized setup. function also sends a torque output 
		 * command of zero torque for each actuator in use
		 */
		public void DeviceReadRequest ()
		{
			m_CommunicationType = 2;
			byte[] pulses = new byte[m_PwmsActive];
			float[] encoderRequest = new float[m_ActuatorsActive];

			for ( int i = 0; i < m_Pwms.Length; i++ )
			{
				pulses[i] = (byte) m_Pwms[i].value;
			}

			// think about this more encoder is detached from actuators
			int j = 0;
			for ( int i = 0; i < m_ActuatorPositions.Length; i++ )
			{
				if ( m_ActuatorPositions[i] > 0 )
				{
					encoderRequest[j] = 0;
					j++;
				}
			}

			m_BoardLink.Transmit( m_CommunicationType, m_DeviceID, pulses, encoderRequest );
		}


		/**
		 * Transmits specific torques that has been calculated and stored for each actuator over the serial
		 * port interface, also transmits specified pwm outputs on pwm pins
		 */
		public void DeviceWriteTorques ()
		{
			m_CommunicationType = 2;
			byte[] pulses = new byte[m_PwmsActive];
			float[] deviceTorques = new float[m_ActuatorsActive];

			for ( int i = 0; i < m_Pwms.Length; i++ )
			{
				pulses[i] = (byte) m_Pwms[i].value;
			}

			int j = 0;
			for ( int i = 0; i < m_ActuatorPositions.Length; i++ )
			{
				if ( m_ActuatorPositions[i] > 0 )
				{
					deviceTorques[j] = m_Motors[m_ActuatorPositions[i] - 1].Torque;
					j++;
				}
			}

			m_BoardLink.Transmit( m_CommunicationType, m_DeviceID, pulses, deviceTorques );
		}


		/**
		 * Set pulse of specified PWM pin
		 */
		public void SetPwmPulse ( int pin, float pulse )
		{

			for ( int i = 0; i < m_Pwms.Length; i++ )
			{
				if ( m_Pwms[i].pin == pin )
				{
					m_Pwms[i].SetPulse( pulse );
				}
			}
		}


		/**
		 * Gets percent PWM pulse value of specified pin
		 */
		public float GetPwmPulse ( int pin )
		{
			float pulse = 0;

			for ( int i = 0; i < m_Pwms.Length; i++ )
			{
				if ( m_Pwms[i].pin == pin )
				{
					pulse = m_Pwms[i].get_pulse();
				}
			}

			return pulse;
		}

		/**
		 * Gathers current state of angles information from encoder objects
		 *
		 * @returns    most recent angles information from encoder objects
		 */
		public void GetDeviceAngles ( ref float[] buffer )
		{
			if ( buffer == null || buffer.Length != m_EncodersActive )
			{
				buffer = new float[m_EncodersActive];
			}

			for ( int i = 0; i < m_EncodersActive; i++ )
			{
				buffer[i] = m_Encoders[i].value;
			}
		}

		/**
		 * Gathers current state of angles information from encoder objects
		 *
		 * @returns    most recent angles information from encoder objects
		 */
		public void GetDeviceAnglesUnchecked ( float[] buffer )
		{
			for ( int i = 0; i < m_EncodersActive; i++ )
			{
				buffer[i] = m_Encoders[i].value;
			}
		}

		/**
		* Gathers current state of the angular velocity information from encoder objects
		*
		* @returns	most recent angles information from encoder objects
		*/
		public void GetDeviceAngularVelocities ( ref float[] buffer )
		{
			if ( buffer == null || buffer.Length != m_EncodersActive )
			{
				buffer = new float[m_EncodersActive];
			}

			for ( int i = 0; i < m_EncodersActive; i++ )
			{
				buffer[i] = m_Encoders[i].velocity;
			}
		}

		/**
		 * Gathers current data from sensor objects
		 *
		 * @returns    most recent analog sensor information from sensor objects
		 */
		public void GetSensorData ( ref float[] buffer )
		{
			if ( buffer == null || buffer.Length != m_SensorsActive )
            {
				buffer = new float[m_SensorsActive];
			}

			for ( int i = 0; i < m_SensorsActive; i++ )
			{
				buffer[i] = m_Sensors[i].value;
			}
		}


		/**
		 * Performs physics calculations based on the given angle values
		 *
		 * @param      angles angles to be used for physics position calculation
		 * @returns    end-effector coordinate position
		 */
		public void GetDevicePosition ( float[] angles, float[] buffer )
		{
			m_Mechanism.ForwardKinematics( angles );
			m_Mechanism.GetCoordinate( buffer );
		}

		/**
		* Gathers current state of angles information from encoder objects
		*
		* @returns    most recent angles information from encoder objects
		*/
		public void GetDeviceVelocities ( float[] angularVelocities, float[] buffer )
		{
			m_Mechanism.VelocityCalculation( angularVelocities );
			m_Mechanism.GetVelocity( buffer );
		}

		/**
		 * Calculates the needed output torques based on forces input and updates each initialized 
		 * actuator respectively
		 *
		 * @param   forces forces that need to be generated
		 * @param   buffer torques that need to be outputted to the physical device
		 */
		public void SetDeviceTorques ( float[] forces, float[] buffer )
		{
			m_Mechanism.TorqueCalculation( forces );
			m_Mechanism.GetTorque( buffer );

			for ( int i = 0; i < m_ActuatorsActive; i++ )
			{
				m_Motors[i].Torque = buffer[i];
			}
		}
		#endregion
	}
}