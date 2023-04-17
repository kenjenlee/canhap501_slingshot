using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Jobs;
using UnityEngine;
using Haply.hAPI;
using Haply.hAPI.Samples;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


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
    private Vector2 m_MapAbsBound = new Vector2(1f, .61f);

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
    private float m_WallStiffness = 900f;

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
    private float[] m_EndEffectorForceZERO;
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
    private bool m_JustReleased = false;
    private bool m_JustWonLost = false;
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
    float alpha = .01f;
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
    private bool m_IsTethered = false;
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
    private float fuelBurnRate = .5f;

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
    [SerializeField] private TextMeshProUGUI tetheredModeStatus;
    [SerializeField] private TextMeshProUGUI tetheredModeIndex;
    [SerializeField] private TextMeshProUGUI alphaStatus;
    [SerializeField] private TextMeshProUGUI tutorialPrompt;
    private GameObject tutorialPromptParent;

    ////////  Scene reloading ////////
    private bool m_Reloading = false;

    ////////  Slingshot stuff ////////
    private float xDiff, yDiff;

    #region Setup
    private void Awake()
    {
        m_ConcurrentDataLock = new object();
        m_InitialArrowScale = m_EndEffectorArrowAvatar.transform.localScale;
        GameManager.OnGameStateChanged += OnGameStateChanged;
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
        currentFuel = fuel;
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
        m_EndEffectorForceZERO = new float[2];
        m_EndEffectorForceZERO[0] = 0f;
        m_EndEffectorForceZERO[1] = 0f;

        m_RenderingForce = false;

        m_SimulationLoopTask = new Task(SimulationLoop);

        m_SimulationLoopTask.Start();

        StartCoroutine(StepCountTimer());

        tutorialPromptParent = tutorialPrompt.gameObject.transform.parent.gameObject;
        tutorialPromptParent.SetActive(false);

        hapticFeedbackStatus.text = m_HapticsOn ? "Haptic Feedback (Enabled)" : "Haptic Feedback (Disabled)";
        tetheredModeStatus.text = m_IsTethered ? "Tethered Mode (Enabled)" : "Tethered Mode (Disabled)";
        thrustersStatus.text = "Thrusters (Disabled)";
        tetheredModeIndex.text = "Tethered Planet (" + planet_vals[cur_cel].color + ")";
    }

    private void FixedUpdate()
    {
        if (m_FiringThrusters)
        {
            currentFuel -= fuelBurnRate * Time.deltaTime;
            ship_val.fuel = currentFuel;
            if (currentFuel < 0f)
            {
                GameManager.UpdateGameState(GameState.GameLostFuelDrained);
            }
        }
        if(m_JustReleased)
        {
            m_CurrentEndEffectorAvatar.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            m_CurrentEndEffectorAvatar.GetComponent<Rigidbody2D>().angularVelocity = 0f;
            m_CurrentEndEffectorAvatar.GetComponent<Rigidbody2D>().AddForce(new Vector2(300f * xDiff, 300f * yDiff));
            m_JustReleased = false;
        }

        if (m_JustWonLost)
        {
            m_JustWonLost = false;
            m_CurrentEndEffectorAvatar.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            m_CurrentEndEffectorAvatar.GetComponent<Rigidbody2D>().angularVelocity = 0f;
        }
    }

    private void Update()
    {
        if (GameManager.GameLost())
        {
            if (Input.GetMouseButtonDown(0) && !m_Reloading)
            {
                m_Reloading = true;
                tutorialPromptParent.SetActive(true);
                tutorialPrompt.text = "Reloading scene... Please don't move or provide any input.";
                Debug.Log("Calling reload");
                StartCoroutine(Reload());
            }
            return;
        }

        fuelSlider.value = currentFuel / fuel;

        if (Input.GetKeyDown(KeyCode.H))
        {
            m_HapticsOn = !m_HapticsOn;
            hapticFeedbackStatus.text = m_HapticsOn ? "Haptic Feedback (Enabled)" : "Haptic Feedback (Disabled)";
        }
        else if (Input.GetKeyDown(KeyCode.T))
        {
            m_IsTethered = !m_IsTethered;
            tetheredModeStatus.text = m_IsTethered ? "Tethered Mode (Enabled)" : "Tethered Mode (Disabled)";
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
        else if (Input.GetKeyDown(KeyCode.F) && (GameManager.GetState() == GameState.Released))    {
            m_FiringThrusters = true;
            thrustersStatus.text = "Thrusters (Enabled)";
            m_anchorPointX = m_EndEffectorPosition[0];
            m_anchorPointY = m_EndEffectorPosition[1];
        }
        else if (Input.GetKeyDown(KeyCode.S) && (GameManager.GetState() == GameState.Released))
        {
            m_FiringThrusters = false;
            thrustersStatus.text = "Thrusters (Disabled)";
            m_anchorPointX = 0f;
            m_anchorPointY = 0f;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (--cur_cel < 0) cur_cel += celestials.Length;
            Debug.Log("Currently selected index: " + cur_cel);
            tetheredModeIndex.text = "Tethered Planet (" + planet_vals[cur_cel].color + ")";
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (++cur_cel >= celestials.Length) cur_cel -= celestials.Length;
            Debug.Log("Currently selected index: " + cur_cel);
            tetheredModeIndex.text = "Tethered Planet (" + planet_vals[cur_cel].color + ")";
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (alpha + .05f < 1f) alpha += .05f;
            alphaStatus.text = "Haptic Alpha: " + alpha.ToString("F2") + " \n0: Only thrusters \n1: Only gravity";
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (alpha - .05f > 0f) alpha -= .05f;
            alphaStatus.text = "Haptic Alpha: " + alpha.ToString("F2") + " \n0: Only thrusters \n1: Only gravity";
        }
        else if (Input.GetKeyDown(KeyCode.R) && !m_Reloading)
        {
            // reset scene
            m_Reloading = true;
            tutorialPromptParent.SetActive(true);
            tutorialPrompt.text = "Reloading scene... Please don't move or provide any input.";
            Debug.Log("Calling reload");
            StartCoroutine(Reload());
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
                m_EndEffectorRadius * 4,
                m_EndEffectorRadius * 4,
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
            m_EndEffectorArrowAvatar.enabled = true;
            fuelSlider.gameObject.SetActive(true);
        }
        else if (s == GameState.Released)
        {
            //m_DeviceToGraphicsFactor = 1f; // Reduce the amount the end effector moves to provide more convincing thruster physics
            //m_DecoupleEndEffectorFromAvatar = true;
            m_EndEffectorArrowAvatar.enabled = false;
            int LayerAsteroid = LayerMask.NameToLayer("Asteroid");
            m_EndEffectorAvatar.gameObject.layer = LayerAsteroid;
            m_JustReleased = true;
        }
        else if (s == GameState.GameWon)
        {
            tutorialPromptParent.SetActive(true);
            tutorialPrompt.text = "Great job! You won the level, congragulations!";
            m_JustWonLost = true;
        }
        else if (s == GameState.GameLostBoundsExceeded)
        {
            Debug.Log($"Game Over!");
            tutorialPromptParent.SetActive(true);
            tutorialPrompt.text = "You lost due to exceeding the bounds of the level :( Please reset the end effector and click to restart the tutorial!";
            m_JustWonLost = true;
        }
        else if (s == GameState.GameLostFuelDrained)
        {
            Debug.Log($"Game Over!");
            tutorialPromptParent.SetActive(true);
            tutorialPrompt.text = "You lost due to having no fuel left :( Please reset the end effector and click to restart the tutorial!";
            m_JustWonLost = true;
        }

        if (GameManager.GameEnded())
        {
            RemoveHapticFeedback();
        }
    }

    #region Drawing
    private void LateUpdate()
    {
        if (!GameManager.GameEnded()) UpdateEndEffector();
        if (m_IsCameraDynamic)
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
        if (GameManager.GameEnded()) return;

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
                        m_EndEffectorForce[0] = 600 * planet_vals[cur_cel].grav.x;
                        m_EndEffectorForce[1] = 600 * planet_vals[cur_cel].grav.y;
                    } else  {
                        m_EndEffectorForce[0] = (float) 1e11 * ship_val.gravitational_forces[0];
                        m_EndEffectorForce[1] = (float)1e11 * ship_val.gravitational_forces[1];
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
                            m_WallPosition.x - (m_EndEffectorPosition[0] + m_EndEffectorRadius) + .414f,
                            0f
                        );
                        //Debug.Log(m_WallPenetration.x);
                        if (m_WallPenetration.x < 0f)
                        {
                            m_WallForce += m_WallPenetration * -m_WallStiffness;
                            Debug.Log(((-m_WorldSize.y / 2f - m_EndEffectorRadius - m_EndEffectorPosition[1]) + .24f) + " " + (m_SlingshotRope.midYpos - m_EndEffectorRadius - m_EndEffectorPosition[1] + .04f));
                            m_WallForce[1] = (m_SlingshotRope.midYpos - m_EndEffectorRadius - m_EndEffectorPosition[1] + .04f) * -m_WallStiffness;
                        }
                    }

                    m_EndEffectorForce[0] = -m_WallForce[0];
                    m_EndEffectorForce[1] = -m_WallForce[1];

                    float timePassed = SlingshotTimer.secondsToRelease - SlingshotTimer.GetSlingshotTimeLeft();
                    m_EndEffectorForce[0] -= timePassed * (float)m_rand.NextDouble() * 10f;
                    m_EndEffectorForce[1] -= timePassed * (float)m_rand.NextDouble() * 10f;

                    //Debug.Log( $"m_EndEffectorForce.x: {m_EndEffectorForce[0]}, m_EndEffectorForce.y: {m_EndEffectorForce[1]}" );
                }
                else if (GameManager.GetState() == GameState.Released)
                {
                    if (m_FiringThrusters)  {
                        // When thrusters are fired, only render the force caused by them
                         m_EndEffectorForce[0] += 20 * m_EndEffectorHorizontalThrustForce * (1 - alpha);
                        m_EndEffectorForce[1] += 20 * m_EndEffectorVerticalThrustForce * (1 - alpha);

                        if (currentFuel / fuel < 0.1f)
                        {
                            m_EndEffectorHorizontalThrustForce *= 0.5f * currentFuel / fuel;
                            m_EndEffectorVerticalThrustForce *= 0.5f * currentFuel / fuel;
                        }
                        else
                        {
                            m_EndEffectorHorizontalThrustForce *= 0.99f;
                            m_EndEffectorVerticalThrustForce *= 0.99f;
                        }

                    }
                    else    {
                        m_EndEffectorForce[0] = 0f;
                        m_EndEffectorForce[1] = 0f;
                    }
                    m_EndEffectorForce[0] += (float)1e11 * ship_val.gravitational_forces.x*alpha;
                    m_EndEffectorForce[1] += (float)1e11 * ship_val.gravitational_forces.y*alpha; 
                    

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
            }
            else
            {
                m_WidgetOne.SetDeviceTorques(m_EndEffectorForceZERO, m_Torques);
            }
            m_WidgetOne.DeviceWriteTorques();

            m_RenderingForce = false;
            m_Steps++;
        }
    }

    /// <summary>
    /// This function is called when the MonoBehaviour will be destroyed.
    /// </summary>
    private void OnDestroy()
    {
        RemoveHapticFeedback();
        // Have to unsubscribe, otherwise there will be issues with reloading scene.
        // https://forum.unity.com/threads/missingreferenceexception-when-scene-is-reloaded.533658/
        GameManager.OnGameStateChanged -= OnGameStateChanged;
    }
    #endregion

    #region Utilities
    private void UpdateEndEffector()
    {
        if (GameManager.GameEnded()) return;
        //var position = m_EndEffectorAvatar.transform.position;
        var position = new Vector3();

        lock (m_ConcurrentDataLock)
        {
            position.x = m_EndEffectorPosition[0]; // Don't need worldPixelWidth/2, because Unity coordinate space is zero'd with display center
            position.y = m_EndEffectorPosition[1]; // Offset is arbitrary to keep end effector avatar inside of workspace
            //Debug.Log("updating end effector graphics " + position.x + " " + position.y);
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

        if (!GameManager.GameEnded() &&
            (Mathf.Abs(m_CurrentEndEffectorAvatar.transform.position.x) > m_MapAbsBound.x ||
            Mathf.Abs(m_CurrentEndEffectorAvatar.transform.position.y) > m_MapAbsBound.y))
        {
            Debug.Log(GameManager.GameEnded());
            GameManager.UpdateGameState(GameState.GameLostBoundsExceeded);
        }

        if (GameManager.GetState() == GameState.Freemovement)
        {
            if(position.x <= m_InitialSpaceshipXPosition)
            {
                GameManager.UpdateGameState(GameState.Slingshot);
            }
        }
        else if (GameManager.GetState() == GameState.Slingshot)
        {
            xDiff = m_WallPosition[0] - m_CurrentEndEffectorAvatar.transform.position.x;
            yDiff = (-m_WorldSize.y / 2f - m_EndEffectorRadius) -
                m_CurrentEndEffectorAvatar.transform.position.y;
            var angle = Mathf.Atan2(yDiff, xDiff) - Mathf.PI / 4f;
            m_EndEffectorArrowAvatar.transform.localRotation =
                UnityEngine.Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.forward);
            m_EndEffectorArrowAvatar.transform.localPosition = new Vector3(
                4f * Mathf.Cos(angle),
                4f * Mathf.Sin(angle),
                0f
                );

            float forceMag = Vector2.SqrMagnitude(new Vector2(xDiff, yDiff));
            //Debug.Log(forceMag);
            float m = forceMag * 15f + .8f;
            m_EndEffectorArrowAvatar.transform.localScale = Vector3.Scale(
                m_InitialArrowScale,
                new Vector3(m, m, 1)
            );
        }
        else if (GameManager.GetState() == GameState.Released)
        {
            if (m_FiringThrusters)
            {
                if (LastPos_x + 0.009 < position.x)
                {
                    EngineFire_Left.SetActive(true);
                }

                else if (LastPos_x == position.x)
                {
                    EngineFire_Left.SetActive(false);
                    EngineFire_Right.SetActive(false);
                }

                else if (LastPos_x - 0.009 > position.x)
                {
                    EngineFire_Right.SetActive(true);
                }


                if (LastPos_y - 0.009 > position.y)
                {
                    EngineFire_Up.SetActive(true);
                }

                else if (LastPos_y == position.y)
                {
                    EngineFire_Up.SetActive(false);
                    EngineFire_Down.SetActive(false);
                }

                else if (LastPos_y + 0.009 < position.y)
                {
                    EngineFire_Down.SetActive(true);
                }

                m_EndEffectorHorizontalThrustForce = m_thrusterStiffness * (position.x - m_anchorPointX);
                m_EndEffectorVerticalThrustForce = m_thrusterStiffness * (position.y - m_anchorPointY);

            }
            else
            {
                EngineFire_Down.SetActive(false);
                EngineFire_Up.SetActive(false);
                EngineFire_Left.SetActive(false);
                EngineFire_Right.SetActive(false);
            }


            if (Vector2.Distance(m_CurrentEndEffectorAvatar.transform.position, m_Destination.transform.position) < 0.02)   {
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

    private IEnumerator Reload()
    {
        //Debug.Log("Reloading scene");
        yield return new WaitForSeconds(3);
        SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        Resources.UnloadUnusedAssets();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    }

    private void RemoveHapticFeedback()
    {
        lock (m_ConcurrentDataLock)
        {
            m_WidgetOne.SetDeviceTorques(new float[2] { 0f, 0f }, m_Torques);
            m_WidgetOne.DeviceWriteTorques();
        }
    }
    #endregion
}
