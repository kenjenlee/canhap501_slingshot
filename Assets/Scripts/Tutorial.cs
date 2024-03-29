using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Haply.hAPI;
using Haply.hAPI.Samples;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using TimeSpan = System.TimeSpan;
using Stopwatch = System.Diagnostics.Stopwatch;
using TMPro;

public class Tutorial : MonoBehaviour
{
    public enum WallOrientation
    {
        Horizontal,
        Vertical
    }

    private enum TutorialStage
    {
        Start,
        TurnOnHapticFeedback,
        TurnOnTetheredMode,
        TurnOffTetheredMode,
        AboutToEnterSlingshot,
        AboutToEnterSlingshot2,
        JustEnteredSlingshot,
        JustReleased,
        //TimeSlowed,
        Released
        //Slingshot,
        //Released,
        //GameWon,
        //GameLostBoundsExceeded,
        //GameLostFuelDrained
    }

    private TutorialStage m_TutorialStage;
    private bool m_JustWonLost = false;

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
    private bool m_DecoupleEndEffectorFromAvatar = false;
    private bool m_FiringThrusters = false;
    private float m_anchorPointX = 0f;
    private float m_anchorPointY = 0f;
    private float m_thrusterStiffness = 1f;

    ////////  Planet stuff  ////////
    public const float G = 6.67e-11f;
    public const float mass_earth = 333000.0f;
    public const float mass_moon = 1f;
    public const float mass_ship = 50f;
    GameObject[] celestials;

    [SerializeField]
    private GameObject m_Earth;

    private Vector2 earthPos;
    private Vector2 moonPos;
    private Vector2 effectorPos;

    [SerializeField]
    private GameObject m_Moon;

    [SerializeField]
    private GameObject m_Destination;

    [SerializeField]
    private bool m_IsTethered = false;
    [SerializeField]
    private bool m_HapticFeedbackEnabled = false;
    private float m_EarthDistance = 0.0f;
    private Vector2 m_EarthForce = new Vector2(0f, 0f);
    private Vector2 m_EffectorPosition = new Vector2(0f, 0f);

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
    private float fuelBurnRate = 2.5f;

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
    [SerializeField] private TextMeshProUGUI tutorialPrompt;

    ////////  Scene reloading ////////
    private bool m_Reloading = false;

    ////////  Slingshot stuff ////////
    private float xDiff, yDiff;
    private readonly System.Random m_rand = new System.Random();

    #region Setup
    private void Awake()
    {
        m_ConcurrentDataLock = new object();
        m_InitialArrowScale = m_EndEffectorArrowAvatar.transform.localScale;
        GameManager.OnGameStateChanged += OnGameStateChanged;

        currentFuel = fuel;
    }

    private void Start()
    {
        Debug.Log($"Screen.width: {Screen.width}");

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

        ////////  planet stuff  ////////
        SetInitialVelocity();
        earthPos = m_Earth.transform.position;
        moonPos = m_Moon.transform.position;

        // Initial texts
        hapticFeedbackStatus.text = m_HapticFeedbackEnabled ? "Haptic Feedback (Enabled)" : "Haptic Feedback (Disabled)";
        tetheredModeStatus.text = m_IsTethered ? "Tethered Mode (Enabled)" : "Tethered Mode (Disabled)";

        tutorialPrompt.text = "Let's get started with Slingshot! Right now, you're in 'Unrestricted Free Movement' " +
            "- that means you can move the end-effector around through space without any forces. " +
            "To turn on gravity and haptic feedback, press H.";

        m_TutorialStage = TutorialStage.Start;

    }
    #endregion
    #region UpdateFns
    private void FixedUpdate()
    {

        effectorPos = new Vector2(m_CurrentEndEffectorAvatar.transform.position[0], m_CurrentEndEffectorAvatar.transform.position[1]);
        if (m_FiringThrusters)
        {
            currentFuel -= fuelBurnRate * Time.deltaTime;
            if(currentFuel < 0f)
            {
                GameManager.UpdateGameState(GameState.GameLostFuelDrained);
            }
        }
        // Gravity forces added here
        float rMoon = Vector2.Distance(m_Earth.transform.position, m_Moon.transform.position);
        m_EarthForce =
            (Vector2) (m_Earth.transform.position - m_Moon.transform.position).normalized
            * (G * (mass_earth * mass_moon) / (rMoon * rMoon));
        //Debug.Log("Force on moon = " + m_EarthForce[0] * 1e6 + ", " + m_EarthForce[1] * 1e6);
        m_Moon.GetComponent<Rigidbody2D>().AddForce(m_EarthForce);

        // If the ship has just been released, a small force is applied towards the right
        if (m_TutorialStage == TutorialStage.JustReleased)
        {
            m_TutorialStage = TutorialStage.Released;
            //Debug.Log(xDiff + " " + yDiff);
            m_CurrentEndEffectorAvatar.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            m_CurrentEndEffectorAvatar.GetComponent<Rigidbody2D>().angularVelocity = 0f;
            m_CurrentEndEffectorAvatar.GetComponent<Rigidbody2D>().AddForce(new Vector2(300f * xDiff, 300f * yDiff));
        }

        if(m_JustWonLost)
        {
            m_JustWonLost = false;
            m_CurrentEndEffectorAvatar.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            m_CurrentEndEffectorAvatar.GetComponent<Rigidbody2D>().angularVelocity = 0f;
        }
    }

    private void Update()
    {
        if (GameManager.GetState() == GameState.GameWon)
        {
            if (Input.GetMouseButtonDown(0))
            {
                SceneManager.LoadScene("Slingshot_solar");
            }
            return;
        } else if (GameManager.GameLost())
        {
            if (Input.GetMouseButtonDown(0) && !m_Reloading)
            {
                m_Reloading = true;
                tutorialPrompt.text = "Reloading scene... Please don't move or provide any input.";
                Debug.Log("Calling reload");
                StartCoroutine(Reload());
            }
            return;
        }

        fuelSlider.value = currentFuel / fuel;
        earthPos = new Vector2(m_Earth.transform.position[0], m_Earth.transform.position[1]);
        Gravity();


        if (Input.GetKeyDown(KeyCode.H))
        {
            if(m_TutorialStage == TutorialStage.Start)
            {
                tutorialPrompt.text = "Awesome! Now you can feel the pull from planets in the level. In Free Movement mode, " +
                                    "try to move around and develop a sense for the level before launching your spaceship. " +
                                    "Next, try pressing T to turn on tethered mode.";
                                    
                m_TutorialStage = TutorialStage.TurnOnHapticFeedback;
            }
            
            m_HapticFeedbackEnabled = !m_HapticFeedbackEnabled;
            hapticFeedbackStatus.text = m_HapticFeedbackEnabled ? "Haptic Feedback (Enabled)" : "Haptic Feedback (Disabled)";
        }
        else if (Input.GetKeyDown(KeyCode.T))
        {
            if(m_TutorialStage == TutorialStage.TurnOnHapticFeedback)
            {
                tutorialPrompt.text = "Now, you will feel the gravitational forces felt by the Moon that is revolving around Earth." +
                    "Next, press T again to turn off tethered mode.";
                m_TutorialStage = TutorialStage.TurnOnTetheredMode;
            }
            else if(m_TutorialStage == TutorialStage.TurnOnTetheredMode)
            {
                tutorialPrompt.text = "Tethered mode is now turned off. " +
                    "When you're ready, move to the left and hover over the ship to start launching. The " +
                                    "launch timer gives you 5 seconds to adjust the ship's position before releasing!";
                m_TutorialStage = TutorialStage.TurnOffTetheredMode;
            }
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
        else if (Input.GetKeyDown(KeyCode.F) && (GameManager.GetState() == GameState.Released))
        {
            tutorialPrompt.text = "The orange bar shows the amount of fuel you have remaining. Failing to get to the destination " +
                                    "before the bar runs out will result in GAME OVER. Good luck!";
            //if (Time.timeScale == 0)
            //{
            //    Time.timeScale = 1f;
            //}
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
        else if(Input.GetMouseButtonDown(0))
        {
            if(m_TutorialStage == TutorialStage.AboutToEnterSlingshot)
            {
                //GameManager.UpdateGameState(GameState.Slingshot);
                tutorialPrompt.text = "Please move the end effector to where the spaceship is. " +
                    "After being released, there will be an orange bar showing the amount of fuel you have remaining. " +
                    "To control the spaceship after being released, move the end effector to a position near the center to recalibrate. " +
                    "Then, press F to activate thrusters mode, and move the end effector to fire the thrusters. " +
                    "Next, press S to exit thrusters mode and recalibrate the end effector.";
                m_TutorialStage = TutorialStage.AboutToEnterSlingshot2;
            }
        }
        else if(Input.GetKeyDown(KeyCode.R) && !m_Reloading)
        {
            // reset scene
            m_Reloading = true;
            StartCoroutine(Reload());
        }
    }

    private void LateUpdate()
    {
        //if(!GameManager.GameEnded())    UpdateEndEffector();
        if (!GameManager.GameEnded()) UpdateEndEffector();
        if (m_IsCameraDynamic)
        {
            Camera.main.transform.position = new Vector3(m_CurrentEndEffectorAvatar.transform.position.x
                , m_CurrentEndEffectorAvatar.transform.position.y
                , -10f);
        }
        m_Frames++;
    }

    #endregion

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
    //#endregion

    private void OnGameStateChanged(GameState s)
    {
        if (s == GameState.Freemovement)
        {
            m_CurrentEndEffectorAvatar = m_EndEffectorStartAvatar;
            m_EndEffectorStartAvatar.enabled = true;
            m_EndEffectorAvatar.enabled = true;
            EngineFire_Left.SetActive(false);
            EngineFire_Right.SetActive(false);
            EngineFire_Up.SetActive(false);
            EngineFire_Down.SetActive(false);
            m_EndEffectorArrowAvatar.enabled = false;

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
        else if (s == GameState.Slingshot)
        {
            tutorialPrompt.text = "Great! Wait for the release, and your ship should move to the right.";
            
        }
        else if (s == GameState.Released)
        {
            //m_DeviceToGraphicsFactor = 1f; // Reduce the amount the end effector moves to provide more convincing thruster physics
            //tutorialPrompt.text = "Your ship has been released! Now, move the end effector to a position near the center to recalibrate." +
            //                        "For the tutorial, the game has paused to allow this, but otherwise you'll need to do this on the fly!" +
            //                        "In this released mode, you can press F to start firing thrusters, and S to stop firing thrusters so that " +
            //                        "you can recalibrate your end effector. The goal is to navigate to where your compass (the yellow arrow) is pointing!";
                                    
            //Time.timeScale = 0.1f;
            //m_EndEffectorAvatar.transform.position = m_EndEffectorStartAvatar.transform.position;
            //m_CurrentEndEffectorAvatar = m_EndEffectorAvatar;
            //m_EndEffectorStartAvatar.enabled = false;
            //m_EndEffectorAvatar.enabled = true;
            //m_DecoupleEndEffectorFromAvatar = true;
            m_EndEffectorArrowAvatar.enabled = false;
            m_TutorialStage = TutorialStage.JustReleased;
        }
        else if (s == GameState.GameWon)
        {
            tutorialPrompt.text = "Great job! Now you've learned the basics of Slingshot, and are ready to play more challenging levels! " +
                "Click anywhere to progress to the next level.";
            m_JustWonLost = true;
        }
        else if (s == GameState.GameLostBoundsExceeded)
        {
            Debug.Log($"Game Over!");
            tutorialPrompt.text = "You lost due to exceeding the bounds of the level :( Please reset the end effector and click to restart the tutorial!";
            m_JustWonLost = true;
        }
        else if (s == GameState.GameLostFuelDrained)
        {
            Debug.Log($"Game Over!");
            tutorialPrompt.text = "You lost due to having no fuel left :( Please reset the end effector and click to restart the tutorial!";
            m_JustWonLost = true;
        }

        if(GameManager.GameEnded())
        {
            RemoveHapticFeedback();
        }

    }

    #region Drawing

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
        if (GameManager.GameEnded() ||
            m_TutorialStage == TutorialStage.AboutToEnterSlingshot
            ) return;

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
                    float r = Vector2.Distance(earthPos, effectorPos);
                    // m_WallPenetration = new Vector2( 0f, m_WallPosition.y - (m_EndEffectorPosition[1] + m_EndEffectorRadius) );
                    Vector2 gravity_force = new Vector2(0.0f, 0.0f);
                    //Debug.Log("Gravity direction: " + (earthPos - effectorPos).normalized);

                    if (!m_IsTethered)
                    {
                        gravity_force = (earthPos - effectorPos).normalized * (G * (mass_earth * mass_ship) / (r * r));
                        m_EndEffectorForce[0] = -400 * gravity_force[0];
                        m_EndEffectorForce[1] = -400 * gravity_force[1];
                        //Debug.Log(m_EndEffectorForce[0] + " " + m_EndEffectorForce[1]);
                    }
                    else
                    {
                        float rMoon = Vector2.Distance(earthPos, moonPos);
                        gravity_force = (earthPos - moonPos).normalized * (G * (mass_earth * mass_moon) / (rMoon * rMoon));
                        m_EndEffectorForce[0] = -1200 * gravity_force[0];
                        m_EndEffectorForce[1] = -1200 * gravity_force[1];
                        //Debug.Log("Earthforce: " + m_EarthForce[0] + " " + m_EarthForce[1]);
                        //m_EndEffectorForce[0] = -500 * m_EarthForce[0];
                        //m_EndEffectorForce[1] = -500 * m_EarthForce[1];
                    }

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
                    else
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
                    //Debug.Log(timePassed);
                    //Debug.Log($"m_EndEffectorForce.x: {m_EndEffectorForce[0]}, m_EndEffectorForce.y: {m_EndEffectorForce[1]}" );
                }
                else if (GameManager.GetState() == GameState.Released)
                {
                    if (m_FiringThrusters)
                    {

                        // When thrusters are fired, only render the force caused by them
                        m_EndEffectorForce[0] = 20 * m_EndEffectorHorizontalThrustForce;
                        m_EndEffectorForce[1] = 20 * m_EndEffectorVerticalThrustForce;

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
                    else
                    {
                        m_EndEffectorForce[0] = 0f;
                        m_EndEffectorForce[1] = 0f;
                    }

                    ////Debug.Log("End effector (x, y): (" + m_EndEffectorPosition[0] + ", " + m_EndEffectorPosition[1] + ")");
                }
                else
                {
                    m_EndEffectorForce[0] = 0f;
                    m_EndEffectorForce[1] = 0f;
                }

                m_EndEffectorPosition = DeviceToGraphics(m_EndEffectorPosition);
            }

            if (m_HapticFeedbackEnabled)
            {
                m_WidgetOne.SetDeviceTorques(m_EndEffectorForce, m_Torques);
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

    #region Planet
    private void SetInitialVelocity()
    {
        Debug.Log("Setting Initial Velocity...\n");
        float r = Vector2.Distance(m_Earth.transform.position, m_Moon.transform.position);
        m_Moon.GetComponent<Rigidbody2D>().velocity +=
            (Vector2)m_Moon.transform.right * Mathf.Sqrt((G * mass_earth) / r);
    }
    

    private void Gravity()
    {
        //Debug.Log("Computing Gravity...\n");
        

        //m_Moon.GetComponent<Rigidbody2D>().AddForce(m_EarthForce);
        if (GameManager.GetState() == GameState.Released)
        {

            // We want the spaceship to move under the influence of gravity
            float r = Vector2.Distance(m_Earth.transform.position, m_CurrentEndEffectorAvatar.transform.position);
            Vector2 m_EarthShipForce = (m_Earth.transform.position - m_CurrentEndEffectorAvatar.transform.position).normalized * (G * (mass_earth * mass_ship) / (r * r));
            m_CurrentEndEffectorAvatar.GetComponent<Rigidbody2D>().AddForce(m_EarthShipForce);

            // The moon also has gravitational influence on the ship (NOT IN THE TUTORIAL)
            r = Vector2.Distance(m_Moon.transform.position, m_CurrentEndEffectorAvatar.transform.position);
            Vector2 m_MoonShipForce = (m_Moon.transform.position - m_CurrentEndEffectorAvatar.transform.position).normalized * (G * (mass_moon * mass_ship) / (r * r));
            m_CurrentEndEffectorAvatar.GetComponent<Rigidbody2D>().AddForce(m_MoonShipForce);

        }
        // Debug.Log(r);
        // Debug.Log((m_Earth.transform.position - m_Moon.transform.position).normalized);
        // Debug.Log((G * (mass_earth * mass_moon) / (r * r)));
        // Debug.Log(m_EarthForce);
    }
    #endregion

    #region Utilities
    private void UpdateEndEffector()
    {
        if (GameManager.GameEnded() ||
            m_TutorialStage == TutorialStage.AboutToEnterSlingshot
            ) return;
        //var position = m_EndEffectorAvatar.transform.position;
        var position = new Vector3();

        lock (m_ConcurrentDataLock)
        {
            position.x = m_EndEffectorPosition[0]; // Don't need worldPixelWidth/2, because Unity coordinate space is zero'd with display center
            position.y = m_EndEffectorPosition[1]; // Offset is arbitrary to keep end effector avatar inside of workspace
        }

        //position *= m_WorldSize.x / 0.24f;
        if (GameManager.GetState() != GameState.Released)
            m_CurrentEndEffectorAvatar.transform.position = position;
        else if (GameManager.GetState() == GameState.Released && m_FiringThrusters)
        {
            // when released, we want the avatar to move by an amount proportional to the change in position of the end effector
            float deltaX = position.x - LastPos_x;
            float deltaY = position.y - LastPos_y;
            m_CurrentEndEffectorAvatar.GetComponent<Rigidbody2D>().AddForce(20f * new Vector2(deltaX, 0f));
            m_CurrentEndEffectorAvatar.GetComponent<Rigidbody2D>().AddForce(20f * new Vector2(0f, deltaY));
        }


        //Debug.Log(m_CurrentEndEffectorAvatar.transform.position.y);

        if (!GameManager.GameEnded() &&
            (Mathf.Abs(m_CurrentEndEffectorAvatar.transform.position.x) > m_MapAbsBound.x ||
            Mathf.Abs(m_CurrentEndEffectorAvatar.transform.position.y) > m_MapAbsBound.y))
        {
            Debug.Log(GameManager.GameEnded());
            GameManager.UpdateGameState(GameState.GameLostBoundsExceeded);
        }
        

        if (GameManager.GetState() == GameState.Freemovement)
        {
            if (position.x <= m_WallPosition[0] &&
                m_TutorialStage == TutorialStage.TurnOffTetheredMode)
            {
                m_TutorialStage = TutorialStage.AboutToEnterSlingshot;
                tutorialPrompt.text = "You are about to enter the slingshot. The game has been paused to introduce to you everything you should know. " +
                "You are now docked into the spaceship, and your goal is to align the spaceship so that you can get to the final goal. " +
                "The yellow arrow is your compass, showing you where the final destination is. " +
                "The red arrow shows where the slingshot will sling you, and the size of the arrow indicates the amount " +
                "of force applied. You have 5 seconds to choose a position before getting slung! " +
                "Next, click anywhere and then move the end effector to the spaceship.";
                m_EndEffectorAvatar.enabled = true;
                fuelSlider.gameObject.SetActive(true);
                m_EndEffectorArrowAvatar.enabled = true;
            }
            else if (position.x <= m_InitialSpaceshipXPosition &&
                m_TutorialStage == TutorialStage.AboutToEnterSlingshot2)
            {
                m_TutorialStage = TutorialStage.JustEnteredSlingshot;
                //tutorialPrompt.text = "Five seconds till getting releaased, align yourself!";
                m_CurrentEndEffectorAvatar = m_EndEffectorAvatar;
                m_EndEffectorStartAvatar.enabled = false;
                GameManager.UpdateGameState(GameState.Slingshot);
            }
        }
        else if(GameManager.GetState() == GameState.Slingshot)
        {
            xDiff = m_WallPosition[0] - m_CurrentEndEffectorAvatar.transform.position.x;
            yDiff = (-m_WorldSize.y / 2f - m_EndEffectorRadius) -
                m_CurrentEndEffectorAvatar.transform.position.y;
            var angle = Mathf.Atan2(yDiff, xDiff) - Mathf.PI / 4f;
            m_EndEffectorArrowAvatar.transform.localRotation =
                Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.forward);
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

            //if (Vector2.Distance(m_CurrentEndEffectorAvatar.transform.position, m_Destination.transform.position) < 0.005)
            if (Vector2.Distance(m_CurrentEndEffectorAvatar.transform.position, m_Destination.transform.position) < 0.02)
                {
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

