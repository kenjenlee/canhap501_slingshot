using UnityEngine;
using System.IO.Ports;
using System;

namespace Haply.hAPI
{
    public class Board : MonoBehaviour
    {
        [SerializeField]
        protected string m_WindowsComPort;

        [SerializeField]
        protected string m_MacComPort;

        [SerializeField]
        protected int m_BaudRate;

        [SerializeField]
        protected int m_SerialTimeout;

        public SerialPort port { get; protected set; }

        protected bool m_HasBeenInitialized;

        public int receivedPacketSize;

        public virtual void Initialize ()
        {
            if ( m_HasBeenInitialized )
            {
                Debug.Log( "Board Already Initialized" );
                return;
            }

            try
            {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                port = new SerialPort( m_WindowsComPort, m_BaudRate );
#else
		        port = new SerialPort(m_MacComPort, m_BaudRate);
#endif
                port.ReadTimeout = m_SerialTimeout;
                port.WriteTimeout = m_SerialTimeout;
                port.DtrEnable = true;
                port.RtsEnable = true;

                port.Open();

                Debug.Log( "Initialized Board" );
                Debug.Log( port.IsOpen );

                m_HasBeenInitialized = true;
            }
            catch ( Exception exception )
            {
                Debug.LogException( exception );
            }
        }

        public virtual void ClosePort ()
        {
            port.Close();

            m_HasBeenInitialized = false;
            Debug.Log( "Port closed" );
        }

        private void OnDestroy ()
        {
            if ( m_HasBeenInitialized || (port != null && port.IsOpen) )
            {
                ClosePort();
            }
        }

        /**
         * Formats and transmits data over the serial port
         * 
         * @param	 communicationType type of communication taking place
         * @param	 deviceID ID of device transmitting the information
         * @param	 bData byte inforamation to be transmitted
         * @param	 fData float information to be transmitted
         */
        public virtual void Transmit ( byte communicationType, byte deviceID, byte[] bData, float[] fData )
        {
            byte[] outData = new byte[2 + bData.Length + 4 * fData.Length];
            byte[] segments = new byte[4];

            outData[0] = communicationType;
            outData[1] = deviceID;

            Array.Copy( bData, 0, outData, 2, bData.Length );

            int j = 2 + bData.Length;
            for ( int i = 0; i < fData.Length; i++ )
            {
                segments = FloatToBytes( fData[i] );
                Array.Copy( segments, 0, outData, j, 4 );
                j = j + 4;
            }

            port.Write( outData, 0, outData.Length );
        }

        /**
         * Receives data from the serial port and formats data to return a float data array
         * 
         * @param	 type type of communication taking place
         * @param	 deviceID ID of the device receiving the information
         * @param	 expected number for floating point numbers that are expected
         * @return	formatted float data array from the received data
         */
        public virtual float[] Receive ( byte communicationType, byte deviceID, int expected )
        {
            //Set_buffer(1 + 4 * expected);

            byte[] segments = new byte[4];

            byte[] inData = new byte[1 + 4 * expected];
            float[] data = new float[expected];

            port.Read( inData, 0, inData.Length );

            if ( inData[0] != deviceID )
            {
                //Debug.LogError("Error, another device expects this data!");
            }

            int j = 1;

            for ( int i = 0; i < expected; i++ )
            {
                Array.Copy( inData, j, segments, 0, 4 );
                data[i] = BytesToFloat( segments );
                j = j + 4;
            }

            return data;
        }

        /**
         * @return   a boolean indicating if data is available from the serial port
         */
        public virtual bool DataAvailable ()
        {
            bool available = false;

            if ( port.BytesToRead > 0 )
            {
                available = true;
            }

            return available;
        }

        /**
         * Sends a reset command to perform a software reset of the Haply board
         *
         */
        private void ResetBoard ()
        {
            byte communicationType = 0;
            byte deviceID = 0;
            byte[] bData = new byte[0];
            float[] fData = new float[0];

            Transmit( communicationType, deviceID, bData, fData );
        }

        /**
         * Set serial buffer length for receiving incoming data
         *
         * @param   length number of bytes expected in read buffer
         */
        private void SetBuffer ( int length )
        {
            port.ReadBufferSize = length;
        }

        /**
         * Translates a float point number to its raw binary format and stores it across four bytes
         *
         * @param	val floating point number
         * @return   array of 4 bytes containing raw binary of floating point number
         */
        protected byte[] FloatToBytes ( float val )
        {
            return BitConverter.GetBytes( val );
        }

        /**
         * Translates a binary of a float point to actual float point
         *
         * @param	segment array containing raw binary of floating point
         * @return   translated floating point number
         */
        protected float BytesToFloat ( byte[] segment )
        {
            return BitConverter.ToSingle( segment, 0 );
        }

        public static byte[] SubArray ( byte[] data, int index, int length )
        {
            byte[] result = new byte[length];

            Array.Copy( data, index, result, 0, length );

            return result;
        }
    }
}