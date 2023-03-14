using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;

using TimeSpan = System.TimeSpan;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Haply.hAPI.Samples
{
    public class DeviceSetup : MonoBehaviour
    {
        public const int CW = 0;
        public const int CCW = 1;

        [SerializeField]
        private Board m_HaplyBoard;

        [SerializeField]
        private Device m_WidgetOne;

        [SerializeField]
        private Pantograph m_Pantograph;

        [Space]
        [SerializeField]
        private SpriteRenderer m_Background;

        [SerializeField]
        private SpriteRenderer m_EndEffectorAvatar;

        [Space]
        [SerializeField]
        private Vector2 m_WorldSize = new Vector2( 10f, 6.5f );

        private Task m_SimulationLoopTask;

        private object m_ConcurrentDataLock;

        private float[] m_Angles;
        private float[] m_Torques;

        private float[] m_EndEffectorPosition;
        private float[] m_EndEffectorForce;

        private bool m_RenderingForce;

        private float m_PixelsPerMeter;
        private float m_WorldPixelWidth;

        private int m_Steps;
        private int m_Frames;

        private int m_DrawSteps;
        private int m_DrawFrames;

        #region Setup
        private void Awake ()
        {
            m_ConcurrentDataLock = new object();
        }

        private void Start ()
        {
            m_Background.transform.localScale = new Vector3( m_WorldSize.x, m_WorldSize.y, 1f );

            var camera = Camera.main;

            Debug.Log( $"Screen.width: {Screen.width}" );

            m_PixelsPerMeter = Screen.width / (camera.aspect * camera.orthographicSize * 2f);
            m_WorldPixelWidth = m_PixelsPerMeter * m_WorldSize.x;

            Debug.Log( $"{nameof( m_PixelsPerMeter )}: {m_PixelsPerMeter}" );
            Debug.Log( $"{nameof( m_WorldPixelWidth )}: {m_WorldPixelWidth}" );

            Application.targetFrameRate = 60;

            m_HaplyBoard.Initialize();

            m_WidgetOne.AddActuator( 1, CCW, 2 );
            m_WidgetOne.AddActuator( 2, CW, 1 );
            //m_WidgetOne.AddEncoder( 1, CCW, 241, 10752, 2 );
            //m_WidgetOne.AddEncoder( 2, CW, -61, 10752, 1 );

            // AS5047P @ 2048 steps per rev
            m_WidgetOne.AddEncoder(1, CCW, 241, 10752, 2);
            m_WidgetOne.AddEncoder(2, CW, -61, 10752, 1);

            m_WidgetOne.DeviceSetParameters();

            m_Angles = new float[2];
            m_Torques = new float[2];

            m_EndEffectorPosition = new float[2];
            m_EndEffectorForce = new float[2];

            m_RenderingForce = false;

            m_SimulationLoopTask = new Task( SimulationLoop );

            m_SimulationLoopTask.Start();

            StartCoroutine( StepCountTimer() );
        }

        private IEnumerator StepCountTimer ()
        {
            while ( true )
            {
                yield return new WaitForSecondsRealtime( 1f );

                lock ( m_ConcurrentDataLock )
                {
                    m_DrawSteps = m_Steps;
                    m_Steps = 0;
                }

                m_DrawFrames = m_Frames;
                m_Frames = 0;

                // Debug.Log( $"Simulation: {m_DrawSteps} Hz,\t Rendering: {m_DrawFrames} Hz" );
            }
        }
        #endregion

        #region Drawing
        private void LateUpdate ()
        {
            UpdateEndEffector();
            m_Frames++;
        }

        private void OnGUI ()
        {
            GUI.color = Color.black;
            GUILayout.Label( $" Simulation: {m_DrawSteps} Hz" );
            GUILayout.Label( $" Rendering: {m_DrawFrames} Hz" );
            GUI.color = Color.white;
        }
        #endregion

        #region Simulation
        private void SimulationLoop ()
        {
            var length = TimeSpan.FromTicks( TimeSpan.TicksPerSecond / 1000 );
            var sw = new Stopwatch();

            while ( true )
            {
                sw.Start();

                var simulationStepTask = new Task( SimulationStep );

                simulationStepTask.Start();

                simulationStepTask.Wait();

                while ( sw.Elapsed < length ) ;

                sw.Stop();
                sw.Reset();
            }
        }

        private void SimulationStep ()
        {
            lock ( m_ConcurrentDataLock )
            {
                m_RenderingForce = true;

                if ( m_HaplyBoard.DataAvailable() )
                {
                    m_WidgetOne.DeviceReadData();

                    m_WidgetOne.GetDeviceAngles( ref m_Angles );
                    m_WidgetOne.GetDevicePosition( m_Angles, m_EndEffectorPosition );
                    m_EndEffectorPosition = DeviceToGraphics( m_EndEffectorPosition );
                }

                m_WidgetOne.SetDeviceTorques( m_EndEffectorForce, m_Torques );
                m_WidgetOne.DeviceWriteTorques();

                m_RenderingForce = false;
                m_Steps++;
            }
        }
        #endregion

        #region Utilities
        private void UpdateEndEffector ()
        {
            var position = m_EndEffectorAvatar.transform.position;

            lock ( m_ConcurrentDataLock )
            {
                position.x = m_PixelsPerMeter * m_EndEffectorPosition[0];                               // Don't need worldPixelWidth/2, because Unity coordinate space is zero'd with display center
                position.y = m_PixelsPerMeter * m_EndEffectorPosition[1] + m_WorldSize.y / 2 + 0.5f;    // Offset is arbitrary to keep end effector avatar inside of workspace
            }

            //position *= m_WorldSize.x / 0.24f;

            m_EndEffectorAvatar.transform.position = position;
        }

        private float[] DeviceToGraphics ( float[] position )
        {
            return new float[] { -position[0], -position[1] };
        }
        #endregion
    }
}