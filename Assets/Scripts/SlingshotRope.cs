using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// REF: https://raw.githubusercontent.com/dci05049/SlingShotVerletIntegration/master/Assets/SkiLift.cs

public class SlingshotRope : MonoBehaviour {

    [SerializeField]
    public Transform StartPoint;
    [SerializeField]
    public Transform EndPoint;
    public float midYpos;

    private LineRenderer lineRenderer;
    private List<RopeSegment> ropeSegments = new List<RopeSegment>();
    private float ropeSegLen = 0.001f;
    private int segmentLength = 30;
    private float lineWidth = 0.01f;

    //Sling shot 
    private bool moveToMouse = false;
    private Vector3 mousePositionWorld;
    private int indexMousePos;
    [SerializeField]
    private GameObject followTarget;



    // Use this for initialization
    void Start()
    {
        SlingshotTimer.OnSlingshotTimerEnd += SlingshotTimerEnd;
        this.lineRenderer = this.GetComponent<LineRenderer>();
        Vector3 ropeStartPoint = StartPoint.position;

        for (int i = 0; i < segmentLength; i++)
        {
            this.ropeSegments.Add(new RopeSegment(ropeStartPoint));
            ropeStartPoint.x -= ropeSegLen;
        }

        if (EndPoint.position.y > StartPoint.position.y)
            midYpos = StartPoint.position.y + (EndPoint.position.y - StartPoint.position.y) / 2f;
        else
            midYpos = EndPoint.position.y + (StartPoint.position.y - EndPoint.position.y) / 2f;
    }


    // Update is called once per frame
    void Update()
    {
        // Todo: line not drawn initially
        // Todo: need material
        // Todo: need to have taret, then ask player to move into that circle before starting slingshot state
        if(this.followTarget.transform.position.x < StartPoint.position.x)
        {
            this.DrawRope();
            if (Input.GetMouseButtonDown(0))
            {
                this.moveToMouse = true;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                this.moveToMouse = false;
            }

            Vector3 screenMousePos = Input.mousePosition;
            float yStart = StartPoint.position.y;
            float yEnd = EndPoint.position.y;
            float currY = this.followTarget.transform.position.y;

            float ratio = (currY - yStart) / (yEnd - yStart);
            //Debug.Log(ratio);
            if (ratio > 0)
            {
                this.indexMousePos = (int)(this.segmentLength * ratio);
            }
        }
        
    }

    private void FixedUpdate()
    {
        this.Simulate();
    }

    private void SlingshotTimerEnd()
    {
        gameObject.SetActive(false);
    }

    private void Simulate()
    {
        // SIMULATION
        Vector2 forceGravity = new Vector2(0f, -1f);

        for (int i = 1; i < this.segmentLength; i++)
        {
            RopeSegment firstSegment = this.ropeSegments[i];
            Vector2 velocity = firstSegment.posNow - firstSegment.posOld;
            firstSegment.posOld = firstSegment.posNow;
            firstSegment.posNow += velocity;
            firstSegment.posNow += forceGravity * Time.fixedDeltaTime;
            this.ropeSegments[i] = firstSegment;
        }

        //CONSTRAINTS
        for (int i = 0; i < 50; i++)
        {
            this.ApplyConstraint();
        }
    }

    private void ApplyConstraint()
    {
        //Constrant to First Point 
        RopeSegment firstSegment = this.ropeSegments[0];
        firstSegment.posNow = this.StartPoint.position;
        this.ropeSegments[0] = firstSegment;


        //Constrant to Second Point 
        RopeSegment endSegment = this.ropeSegments[this.ropeSegments.Count - 1];
        endSegment.posNow = this.EndPoint.position;
        this.ropeSegments[this.ropeSegments.Count - 1] = endSegment;

        for (int i = 0; i < this.segmentLength - 1; i++)
        {
            RopeSegment firstSeg = this.ropeSegments[i];
            RopeSegment secondSeg = this.ropeSegments[i + 1];

            float dist = (firstSeg.posNow - secondSeg.posNow).magnitude;
            float error = Mathf.Abs(dist - this.ropeSegLen);
            Vector2 changeDir = Vector2.zero;

            if (dist > ropeSegLen)
            {
                changeDir = (firstSeg.posNow - secondSeg.posNow).normalized;
            }
            else if (dist < ropeSegLen)
            {
                changeDir = (secondSeg.posNow - firstSeg.posNow).normalized;
            }

            Vector2 changeAmount = changeDir * error;
            if (i != 0)
            {
                firstSeg.posNow -= changeAmount * 0.5f;
                this.ropeSegments[i] = firstSeg;
                secondSeg.posNow += changeAmount * 0.5f;
                this.ropeSegments[i + 1] = secondSeg;
            }
            else
            {
                secondSeg.posNow += changeAmount;
                this.ropeSegments[i + 1] = secondSeg;
            }

            if (indexMousePos > 0 && indexMousePos < this.segmentLength - 1 && i == indexMousePos)
            {
                RopeSegment segment = this.ropeSegments[i];
                RopeSegment segment2 = this.ropeSegments[i + 1];
                segment.posNow = new Vector2(this.followTarget.transform.position.x, this.followTarget.transform.position.y);
                segment2.posNow = new Vector2(this.followTarget.transform.position.x, this.followTarget.transform.position.y);
                this.ropeSegments[i] = segment;
                this.ropeSegments[i + 1] = segment2;
            }
        }
    }

    private void DrawRope()
    {
        float lineWidth = this.lineWidth;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        Vector3[] ropePositions = new Vector3[this.segmentLength];
        for (int i = 0; i < this.segmentLength; i++)
        {
            ropePositions[i] = this.ropeSegments[i].posNow;
        }

        lineRenderer.positionCount = ropePositions.Length;
        lineRenderer.SetPositions(ropePositions);
    }

    public struct RopeSegment
    {
        public Vector2 posNow;
        public Vector2 posOld;

        public RopeSegment(Vector2 pos)
        {
            this.posNow = pos;
            this.posOld = pos;
        }
    }
}