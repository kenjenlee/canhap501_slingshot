using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Jobs;
using UnityEngine;
using Haply.hAPI;
using Haply.hAPI.Samples;
using UnityEngine.UI;


using TimeSpan = System.TimeSpan;
using Stopwatch = System.Diagnostics.Stopwatch;
using TMPro;
using System.Numerics;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class Slingshot : MonoBehaviour
{
    public enum WallOrientation
    {
        Horizontal,
        Vertical
    }

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

    private ship_class ship_val;

    [SerializeField]
    private SpriteRenderer m_EndEffectorStartAvatar;

    private SpriteRenderer m_CurrentEndEffectorAvatar;

    [SerializeField]
    private SpriteRenderer m_WallAvatar;

    [SerializeField]
    private SpriteRenderer m_EndEffectorArrowAvatar;

    //[Space]
    //[SerializeField]
    //private Vector2 m_WorldSize = new Vector2(0.25f, 0.2f);
    private Vector2 m_WorldSize = new Vector2(0.55f, 0.4f);
    private float m_DeviceToGraphicsFactor = 1f;

    [Space]
    [SerializeField]
    private float m_EndEffectorRadius = 0.006f;

    [SerializeField]
    private float m_WallStiffness = 45f;

    [SerializeField]
    private Vector2 m_WallAdditionalForce = new Vector2(0f, 50000f);

    //[SerializeField]
    private Vector2 m_WallPosition = new Vector2(-.35f, 0f);

    private float m_InitialSpaceshipXPosition = -.38f;

    // private Vector2 m_WallPosition = new Vector2( 0f, 0.1f );

    [SerializeField]
    private WallOrientation m_WallOrientation = WallOrientation.Vertical;

    [SerializeField]
    private SlingshotRope m_SlingshotRope;

    private Task m_SimulationLoopTask;

    private object m_ConcurrentDataLock;

    private float[] m_Angles;
    private float[] m_Torques;

    private float[] m_EndEffectorPosition;
    private float[] m_EndEffectorForce;
    private float m_EndEffectorHorizontalThrustForce = 0f;
    private float m_EndEffectorVerticalThrustForce = 0f;

    private bool m_RenderingForce;

    private int m_Steps;
    private int m_Frames;

    private int m_DrawSteps;
    private int m_DrawFrames;

    private Vector2 m_WallForce = new Vector2(0f, 0f);
    private Vector2 m_WallPenetration = new Vector2(0f, 0f);

    private Vector3 m_InitialArrowScale;

    private Vector2 m_ReleasedForce = new Vector2(0f, 0f);
    private bool m_JustReleased = true;
    private bool m_DecoupleEndEffectorFromAvatar = false;
    private bool m_FiringThrusters = false;
    private bool m_HapticsOn = true;
    private float m_anchorPointX = 0f;
    private float m_anchorPointY = 0f;
    private float m_thrusterStiffness = 1f;

    ////////  Planet stuff  ////////
    public const float G = 6.67e-11f; 
    public const float mass_earth = 333000.0f;
    public const float mass_moon = 1.0f;
    public const float mass_ship = 20f;
    GameObject[] celestials;
    planet[] planet_vals;
    int cur_cel;
    Vector2[] planet_vel;
    float alpha;
    //int fuel;

    [SerializeField]
    public const float gravitationalConstant = 1000f;
    public const float physicsTimeStep = 0.01f;

    [SerializeField]
    private GameObject m_Earth;

    [SerializeField]
    private GameObject m_Moon;

    [SerializeField]
    private GameObject m_Destination;

    [SerializeField]
    private bool m_IsTethered;
    private float m_EarthDistance = 0.0f;
    private Vector2 m_EarthForce = new Vector2(0f, 0f);
    private Vector2 m_EffectorPosition = new Vector2(0f, 0f);

    private readonly System.Random m_rand = new System.Random();

    ////////  Booster visuals stuff  ////////
    private float LastPos_x;
    private float LastPos_y;

    [SerializeField]
    private GameObject EngineFire_Left;

    [SerializeField]
    private GameObject EngineFire_Right;

    [SerializeField]
    private GameObject EngineFire_Up;

    [SerializeField]
    private GameObject EngineFire_Down;

    [SerializeField]
    private float fuel = 100f;

    [SerializeField]
    private Slider fuelSlider;

    [SerializeField]
    private float fuelBurnRate = 1.5f;

    private float currentFuel;

    ////////  Camera stuff ////////
    private bool m_IsCameraDynamic = true;

    [SerializeField]
    private float m_CameraStaticSize = .5f;

    [SerializeField]
    private float m_CameraDynamicSize = 0.15f;

    ////////  HUD  ////////
    [SerializeField] private TextMeshProUGUI thrustersStatus;
    [SerializeField] private TextMeshProUGUI hapticFeedbackStatus;

    #region Setup
    private void Awake()
    {
        m_ConcurrentDataLock = new object();
        m_InitialArrowScale = m_EndEffectorArrowAvatar.transform.localScale;
        GameManager.OnGameStateChanged += OnGameStateChanged;
        currentFuel = fuel;
        int LayerIgnoreAsteroid = LayerMask.NameToLayer("Ignore Asteroid");
        m_EndEffectorAvatar.gameObject.layer = LayerIgnoreAsteroid;
        m_EndEffectorStartAvatar.gameObject.layer = LayerIgnoreAsteroid;
    }

    private void Start()
    {
        Debug.Log($"Screen.width: {Screen.width}");
        celestials = GameObject.FindGameObjectsWithTag("Celestial");
        planet_vals = FindObjectsOfType<planet>();
        ship_val = FindObjectOfType<ship_class>();
        alpha = 0.5f;
        fuel = 1;
        Application.targetFrameRate = 60;
        

        m_HaplyBoard.Initialize();

        m_WidgetOne.AddActuator(1, CCW, 2);
        m_WidgetOne.AddActuator(2, CW, 1);
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

        m_SimulationLoopTask = new Task(SimulationLoop);

        m_SimulationLoopTask.Start();

        StartCoroutine(StepCountTimer());
    }

    private void FixedUpdate()
    {
        if (m_FiringThrusters)
        {
            currentFuel -= fuelBurnRate * Time.deltaTime;
        }
    }

    private void Update()
    {
        fuelSlider.value = currentFuel / fuel;

        Gravity();
        if (Input.GetKeyDown(KeyCode.H))
        {
            m_HapticsOn = !m_HapticsOn;
        }
        else if (Input.GetKeyDown(KeyCode.T))
        {
            m_IsTethered = !m_IsTethered;
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            m_IsCameraDynamic = !m_IsCameraDynamic;
            if (!m_IsCameraDynamic)
            {
                Camera.main.transform.position = new Vector3(0f
                    , -m_WorldSize.y / 2f
                    , -20f);
                Camera.main.orthographicSize = m_CameraStaticSize;
            }
            else
            {
                Camera.main.orthographicSize = m_CameraDynamicSize;
            }
        }
        if (Input.GetKeyDown(KeyCode.F) && (GameManager.GetState() == GameState.Released))    {
            m_FiringThrusters = true;
            thrustersStatus.text = "Thrusters (Enabled)";
            m_anchorPointX = m_EndEffectorPosition[0];
            m_anchorPointY = m_EndEffectorPosition[1];
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            //m_FiringThrusters = false;
            //thrustersStatus.text = "Thrusters (Disabled)";
            //m_anchorPointX = 0f;
            //m_anchorPointY = 0f;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (--cur_cel <= 0) cur_cel += celestials.Length;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (++cur_cel >= celestials.Length) cur_cel -= celestials.Length;
        }
    }
    
    private IEnumerator StepCountTimer()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(1f);

            lock (m_ConcurrentDataLock)
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

    private void OnGameStateChanged(GameState s)
    {
        if(s == GameState.Freemovement)
        {
            m_CurrentEndEffectorAvatar = m_EndEffectorStartAvatar;
            m_EndEffectorStartAvatar.enabled = true;
            m_EndEffectorAvatar.enabled = true;
            EngineFire_Left.SetActive(false);
            EngineFire_Right.SetActive(false);
            EngineFire_Up.SetActive(false);
            EngineFire_Down.SetActive(false);

            //m_WorldSize.x = 0.4f;
            //m_WorldSize.y = 0.4f;
            m_DeviceToGraphicsFactor = 6f;

            //Camera.main.transform.position = new Vector3(0f, -m_WorldSize.y / 2f, -10f);
            Camera.main.transform.position = new Vector3(0f
                    , -m_WorldSize.y / 2f
                    , -20f);

            Camera.main.orthographicSize = m_IsCameraDynamic ?
                m_CameraDynamicSize : m_CameraStaticSize;
            //Debug.Log(m_IsCameraDynamic);
            //Debug.Log(m_CameraDynamicSize);
            //Debug.Log(m_CameraStaticSize);
            //Debug.Log(Camera.main.orthographicSize);

            m_Background.transform.position = new Vector3(
                0f,
                -m_WorldSize.y / 2f - m_EndEffectorRadius,
                1f
            );
            m_Background.transform.localScale = new Vector3(m_WorldSize.x, m_WorldSize.y, 1f);

            m_EndEffectorAvatar.transform.localScale = new Vector3(
                m_EndEffectorRadius,
                m_EndEffectorRadius,
                1f
            );

            m_EndEffectorAvatar.transform.position = new Vector3(
                m_InitialSpaceshipXPosition
                , -m_WorldSize.y / 2f - m_EndEffectorRadius
                , 0f);

            m_EndEffectorStartAvatar.transform.localScale = new Vector3(
                m_EndEffectorRadius,
                m_EndEffectorRadius,
                1f
            );

            ////////  wall stuff  ////////
            //var wallPosition = DeviceToGraphics(new float[2] { m_WallPosition.x, m_WallPosition.y });
            //Debug.Log($"{wallPosition[0]} {wallPosition[1]}");
            //var wallPosition = new Vector2(m_WallPosition.x, m_WallPosition.y);
            //Debug.Log($"{wallPosition[0]} {wallPosition[1]}");

            if (m_WallOrientation == WallOrientation.Horizontal)
            {
                // horizontal wall
                //m_WallAvatar.transform.position = new Vector3(wallPosition[0], wallPosition[1], 0f);
                //m_WallAvatar.transform.localScale = new Vector3(m_WorldSize.x, m_EndEffectorRadius, 1f);
            }
            else if (m_WallOrientation == WallOrientation.Vertical)
            {
                // vertical wall
                m_WallAvatar.transform.localScale = new Vector3(m_EndEffectorRadius, m_WorldSize.y, 1f);
                m_WallAvatar.transform.position = new Vector3(
                    m_WallPosition[0],
                    m_WallPosition[1] - m_WorldSize.y / 2f - m_EndEffectorRadius,
                    0f
                );
                m_SlingshotRope.StartPoint.position = new Vector3(
                    //wallPosition[0],
                    //wallPosition[1] - m_WorldSize.y - m_EndEffectorRadius,
                    m_WallPosition[0],
                    m_WallPosition[1] - m_WorldSize.y - m_EndEffectorRadius,
                    0f
                );
                //m_SlingshotRope.EndPoint.position = new Vector3(wallPosition[0], wallPosition[1], 0f);
                m_SlingshotRope.EndPoint.position = new Vector3(m_WallPosition[0], m_WallPosition[1], 0f);
            }

        }
        else if(s == GameState.Slingshot)
        {
            m_CurrentEndEffectorAvatar = m_EndEffectorAvatar;
            m_EndEffectorStartAvatar.enabled = false;
            m_EndEffectorAvatar.enabled = true;
            fuelSlider.gameObject.SetActive(true);
        }
        else if (s == GameState.Released)
        {
            //m_DeviceToGraphicsFactor = 1f; // Reduce the amount the end effector moves to provide more convincing thruster physics
            m_EndEffectorAvatar.transform.position = m_EndEffectorStartAvatar.transform.position;
            m_CurrentEndEffectorAvatar = m_EndEffectorAvatar;
            m_EndEffectorStartAvatar.enabled = false;
            m_EndEffectorAvatar.enabled = true;
            m_DecoupleEndEffectorFromAvatar = true;
            int LayerAsteroid = LayerMask.NameToLayer("Asteroid");
            m_EndEffectorAvatar.gameObject.layer = LayerAsteroid;
        }
        else
            Debug.Log($"Game Over!");
    }

    #region Drawing
    private void LateUpdate()
    {
        UpdateEndEffector();
        if(m_IsCameraDynamic)
        {
            Camera.main.transform.position = new Vector3(m_CurrentEndEffectorAvatar.transform.position.x
                , m_CurrentEndEffectorAvatar.transform.position.y
                , -10f);
        }
        m_Frames++;
    }

    private void OnGUI()
    {
        GUI.color = Color.black;
        GUILayout.Label($" Simulation: {m_DrawSteps} Hz");
        GUILayout.Label($" Rendering: {m_DrawFrames} Hz");
        //GUILayout.Label( $" End Effector: {m_EndEffectorPosition[0]}" );
        //GUILayout.Label( $" Wall: {m_WallPosition.y}" );
        GUI.color = Color.white;
    }
    #endregion

    #region Simulation
    private void SimulationLoop()
    {
        var length = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 1000);
        var sw = new Stopwatch();

        while (true)
        {
            sw.Start();

            var simulationStepTask = new Task(SimulationStep);

            simulationStepTask.Start();

            simulationStepTask.Wait();

            while (sw.Elapsed < length)
                ;

            sw.Stop();
            sw.Reset();
        }
    }

    private void SimulationStep()
    {
        lock (m_ConcurrentDataLock)
        {
            m_RenderingForce = true;

            if (m_HaplyBoard.DataAvailable())
            {
                m_WidgetOne.DeviceReadData();

                m_WidgetOne.GetDeviceAngles(ref m_Angles);
                m_WidgetOne.GetDevicePosition(m_Angles, m_EndEffectorPosition);

                // Debug.Log( $"m_WallPosition.y: {m_WallPosition.y}, m_EndEffectorPosition[1] + m_EndEffectorRadius: {m_EndEffectorPosition[1] + m_EndEffectorRadius}" );
                if (GameManager.GetState() == GameState.Freemovement)
                {
                    if (m_IsTethered){
                        // Using left and right arrows one can audition the planets
                        /* TODO: Adjust Values of Gravity*/
                        m_EndEffectorForce[0] = 400 * planet_vals[cur_cel].grav.x;
                        m_EndEffectorForce[1] = 400 * planet_vals[cur_cel].grav.y;
                    }
                    //Debug.Log(m_EndEffectorForce[0] + " " + m_EndEffectorForce[1]);

                }
                else if (GameManager.GetState() == GameState.Slingshot)
                {
                    m_WallForce = Vector2.zero;

                    if (m_WallOrientation == WallOrientation.Horizontal)
                    {
                        m_WallPenetration = new Vector2(
                            0f,
                            m_WallPosition.y - (m_EndEffectorPosition[1] + m_EndEffectorRadius)
                        );
                        if (m_WallPenetration.y < 0f)
                        {
                            m_WallForce += m_WallPenetration * -m_WallStiffness;
                            // m_WallForce += m_WallAdditionalForce;
                        }
                    }
                    else // vertical wall
                    {
                        m_WallPenetration = new Vector2(
                            m_WallPosition.x - (m_EndEffectorPosition[0] + m_EndEffectorRadius),
                            10f * (m_SlingshotRope.midYpos - m_EndEffectorPosition[1] - m_EndEffectorRadius)
                        );
                        // Debug.Log(m_WallPenetration.x);
                        if (m_WallPenetration.x < 0f)
                        {
                            m_WallForce += m_WallPenetration * -m_WallStiffness;
                        }

                    }


                    m_EndEffectorForce[0] = -m_WallForce[0];
                    m_EndEffectorForce[1] = -m_WallForce[1];

                    float timePassed = SlingshotTimer.secondsToRelease - SlingshotTimer.GetSlingshotTimeLeft();
                    m_EndEffectorForce[0] -= timePassed * (float)m_rand.NextDouble() * 5f;
                    m_EndEffectorForce[1] -= timePassed * (float)m_rand.NextDouble() * 5f;

                    Debug.Log( $"m_EndEffectorForce.x: {m_EndEffectorForce[0]}, m_EndEffectorForce.y: {m_EndEffectorForce[1]}" );
                }
                else if (GameManager.GetState() == GameState.Released)
                {
                    m_EndEffectorForce[0] = 0f;
                    m_EndEffectorForce[1] = 0f;
                    //Not needed and is it really how a slingshot works?
                    //should the ship not feel the force slowly dissipase at it is being released by the sling shot?

                    //if (m_JustReleased)
                    //{
                    //    //m_JustReleased = false;
                    //    // m_ReleasedForce[0] = m_EndEffectorForce[0];
                    //    // m_ReleasedForce[1] = m_EndEffectorForce[1];
                    //    // m_EndEffectorForce[0] = m_ReleasedForce[0];
                    //    // m_EndEffectorForce[1] = m_ReleasedForce[1];
                    //    m_EndEffectorForce[0] = 0f;
                    //    m_EndEffectorForce[1] = 0f;
                    //}

                    /* TODO: Adjust Values of Gravity and thrustors*/
                    if (m_FiringThrusters)  {
                        // When thrusters are fired, only render the force caused by them
                        m_EndEffectorForce[0] = 20 * m_EndEffectorHorizontalThrustForce;
                        m_EndEffectorForce[1] = 20 * m_EndEffectorVerticalThrustForce;
                        // m_EndEffectorForce[0] += 20 * m_EndEffectorHorizontalThrustForce*(1-alpha);
                        // m_EndEffectorForce[1] += 20 * m_EndEffectorVerticalThrustForce*(1-alpha);

                        m_EndEffectorHorizontalThrustForce *= 0.99f;
                        m_EndEffectorVerticalThrustForce *= 0.99f;

                    }
                    else    {
                        m_EndEffectorForce[0] = 0f;
                        m_EndEffectorForce[1] = 0f;
                    }
                    m_EndEffectorForce[0] += ship_val.gravitational_forces.x*alpha;
                    m_EndEffectorForce[1] += ship_val.gravitational_forces.y*alpha; 
                    //else    {
                    //    m_EndEffectorForce[0] = 0f;
                    //    m_EndEffectorForce[1] = 0f;
                    //}

                    //Debug.Log("End effector (x, y): (" + m_EndEffectorPosition[0] + ", " + m_EndEffectorPosition[1] + ")"  
                }
                else
                {
                    m_EndEffectorForce[0] = 0f;
                    m_EndEffectorForce[1] = 0f;
                }

                m_EndEffectorPosition = DeviceToGraphics(m_EndEffectorPosition);
            }

            if (m_HapticsOn)
            {
                m_WidgetOne.SetDeviceTorques(m_EndEffectorForce, m_Torques);
                m_WidgetOne.DeviceWriteTorques();
            }

            m_RenderingForce = false;
            m_Steps++;
        }
    }

    /// <summary>
    /// This function is called when the MonoBehaviour will be destroyed.
    /// </summary>
    private void OnDestroy()
    {
        lock (m_ConcurrentDataLock)
        {
            m_WidgetOne.SetDeviceTorques(new float[2] { 0f, 0f }, m_Torques);
            m_WidgetOne.DeviceWriteTorques();
        }
    }
    #endregion

    #region Utilities
    private void UpdateEndEffector()
    {
        //var position = m_EndEffectorAvatar.transform.position;
        var position = new Vector3();

        lock (m_ConcurrentDataLock)
        {
            position.x = m_EndEffectorPosition[0]; // Don't need worldPixelWidth/2, because Unity coordinate space is zero'd with display center
            position.y = m_EndEffectorPosition[1]; // Offset is arbitrary to keep end effector avatar inside of workspace
            Debug.Log("updating end effector graphics " + position.x + " " + position.y);
        }

        //position *= m_WorldSize.x / 0.24f;
        if (GameManager.GetState() != GameState.Released)
            m_CurrentEndEffectorAvatar.transform.position = position;
        else if (GameManager.GetState() == GameState.Released && m_FiringThrusters) {
            // when released, we want the avatar to move by an amount proportional to the change in position of the end effector
            float deltaX = position.x - LastPos_x;
            float deltaY = position.y - LastPos_y;
            m_CurrentEndEffectorAvatar.GetComponent<Rigidbody2D>().AddForce(20f * new Vector2(deltaX, 0f));
            m_CurrentEndEffectorAvatar.GetComponent<Rigidbody2D>().AddForce(20f * new Vector2(0f, deltaY));
        }

        // float m = Mathf.Max(1.0f, CalculateMagnitude(m_EndEffectorForce));
        // m_EndEffectorArrowAvatar.transform.localScale = Vector3.Scale(
        //     m_InitialArrowScale,
        //     new Vector3(1, m, 1)
        // );

        if(GameManager.GetState() == GameState.Freemovement)
        {
            if(position.x <= m_InitialSpaceshipXPosition)
            {
                GameManager.UpdateGameState(GameState.Slingshot);
            }
        }
        else if (GameManager.GetState() == GameState.Released && m_FiringThrusters)
        {
            if (LastPos_x + 0.003 < position.x)
            {
                EngineFire_Left.SetActive(true);
            }

            else if (LastPos_x == position.x)
            {
                EngineFire_Left.SetActive(false);
                EngineFire_Right.SetActive(false);
            }

            else if (LastPos_x - 0.003 > position.x)
            {
                EngineFire_Right.SetActive(true);
            }


            if (LastPos_y - 0.003 > position.y)
            {
                EngineFire_Up.SetActive(true);
            }

            else if (LastPos_y == position.y)
            {
                EngineFire_Up.SetActive(false);
                EngineFire_Down.SetActive(false);
            }

            else if (LastPos_y + 0.003 < position.y)
            {
                EngineFire_Down.SetActive(true);
            }

            m_EndEffectorHorizontalThrustForce = m_thrusterStiffness * (position.x - m_anchorPointX);
            m_EndEffectorVerticalThrustForce = m_thrusterStiffness * (position.y - m_anchorPointY);


            if (Vector2.Distance(m_CurrentEndEffectorAvatar.transform.position, m_Destination.transform.position) < 0.005)   {
                Debug.Log("Game Won!");
                GameManager.UpdateGameState(GameState.GameWon);
            }

            
        }
        LastPos_x = position.x;
        LastPos_y = position.y;
        
    }

    private float[] DeviceToGraphics(float[] position)
    {
        return new float[] { -position[0] * m_DeviceToGraphicsFactor
            , -position[1] * m_DeviceToGraphicsFactor };
    }

    private float CalculateMagnitude(float[] num)
    {
        // REF: http://www.claysturner.com/dsp/FastMagnitude.pdf
        float sm = Mathf.Min(num[0], num[1]);
        float la = Mathf.Max(num[0], num[1]);
        return Mathf.Abs(la + .25f * sm);
    }

    private float CalculateAbsMagnitude(float[] num)
    {
        return Mathf.Abs(CalculateAbsMagnitude(num));
    }

    static float NextFloat(System.Random random)
    //static float NextFloat()
    {
        double mantissa = (random.NextDouble() * 2.0) - 1.0;
        // choose -149 instead of -126 to also generate subnormal floats (*)
        double exponent = System.Math.Pow(2.0, random.Next(-126, 128));
        return (float)(mantissa * exponent);
    }
    #endregion
}
