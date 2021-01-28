//Attach this script to gameobject with AR session origin (comes along with ARFoundation)
//
//Discription
//state of previous interaction is stored in currentState variable
//quick tap to spawn object
//tap on object to select and deselect object
//tap on selected object and drag to move it
//drag anywere on screen to rotate
//pinch     to scale



using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARRaycastManager))]
public class SimpleInteractions : MonoBehaviour
{

    public enum SimpleState
    {
        //initial state
        init,
        //allow object to be selected
        Select,
        //allow object to be spawned
        Spawn,
        //allow object to be manipulated
        Manipulate
    }


    [Tooltip("Attach this to a text UI object (for debugging)")]
    public Text Debugger;
    [Tooltip("Layer where objects can spawn(Use a layer defined by user)")]
    public LayerMask layerMask;
    [Tooltip("Object to be placed")]
    public GameObject placement;

    public Text tmr;

    public float timeout = 0.5f;

    public float RotateFactor = 1;

    private Camera AR_camera;

    private ARRaycastManager aRRaycastManager;

    private List<ARRaycastHit> hitResults;

    private SimpleState currentState;

    private GameObject InitialSelect,FinalSelect,selected;

    private float timer=0.5f;

    private int touchcount;

    private Vector2 finalTouchPose;

    private float scaleSize;

    private Vector3 currentScale;

    private bool starttimer;

    //this is ued to prevent spawning
    private bool manipulated;

    public SimpleState getCurrentState()
    {
        return currentState;
    }

    void debugprint()
    {
        if(Debugger!=null)
        {
            Debugger.text = currentState.ToString();
        }
    }

    void Start()
    {
        starttimer = false;
        selected = null;
        InitialSelect = null;
        FinalSelect = null;
        hitResults = new List<ARRaycastHit>();
        if (AR_camera == null)
        {
            AR_camera = Camera.main;
        }

        if (aRRaycastManager == null)
        {
            aRRaycastManager = GetComponent<ARRaycastManager>();
        }
        currentState = SimpleState.init;
        if(timeout<=0)
        {
            timeout = timer;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.touchCount > 0)
        {
            if(touchcount< Input.touchCount)
                touchcount = Input.touchCount;
            Touch touch=Input.GetTouch(0);
            if((Input.touchCount >= 2 || touchcount >= 2))
            {
                //this prevents spawning/selection and enables update during
                //as selection and spawning is updated after touch
                manipulated = true;
                currentState = SimpleState.Manipulate;
                //scaling conditions
                if (Input.touchCount >= 2 && selected)
                {
                    if(Input.GetTouch(1).phase==TouchPhase.Began)
                    {
                        currentScale = selected.transform.localScale;
                        scaleSize =Mathf.Abs( Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position));
                    }
                    else if(Input.GetTouch(1).phase == TouchPhase.Moved|| Input.GetTouch(0).phase == TouchPhase.Moved)
                    {
                        float factor = Mathf.Abs(Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position)) /scaleSize;
                        selected.transform.localScale = currentScale * factor;
                    }
                }
            }
            //Update Variables for operation
            else if(touchcount==1 && !manipulated)
            {
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        {
                            timer = timeout;
                            starttimer = true;
                            Ray ray = AR_camera.ScreenPointToRay(touch.position);
                            RaycastHit hit;
                            if (Physics.Raycast(ray, out hit, layerMask))
                            {
                                InitialSelect = hit.transform.gameObject;
                            }
                            else
                            {
                                InitialSelect = null;
                            }
                        }
                        break;
                    case TouchPhase.Moved:
                        {
                            manipulated = true;
                        }
                        break;
                    case TouchPhase.Ended:
                        {
                            finalTouchPose = touch.position;
                            Ray ray = AR_camera.ScreenPointToRay(finalTouchPose);
                            RaycastHit hit;
                            if (Physics.Raycast(ray, out hit, layerMask))
                            {
                                FinalSelect = hit.transform.gameObject;
                            }
                            else
                            {
                                FinalSelect = null;
                            }
                        }
                        break;
                }

            }
            //real time updates if touchcount>1 or manipulated
            //as rotation transilation should be visible inscerrn during interaction
            else if(touchcount==1 && manipulated)
            {
                //rotate or transilate if selected not null
                if(selected!=null)
                {
                    //rotate
                    if (selected!=InitialSelect)
                    {
                        if (Input.GetTouch(0).phase == TouchPhase.Moved)
                        {
                            currentState = SimpleState.Manipulate;
                            selected.transform.Rotate((new Vector3(0, 1, 0)) * RotateFactor * Input.GetTouch(0).deltaPosition.x);
                        }
                    }
                    //transilate
                    else if (selected==InitialSelect)
                    {
                        if (aRRaycastManager.Raycast(Input.GetTouch(0).position, hitResults, TrackableType.PlaneWithinPolygon))
                        {
                            currentState = SimpleState.Manipulate;
                            var pose = hitResults[0].pose;
                            selected.transform.position=pose.position;
                        }
                    }
                }
                // if no selected object go to init
                else if(selected==null)
                {
                    currentState = SimpleState.init;
                }
            }

        }
        else if(Input.touchCount==0 && touchcount!=0)
        {
            if(!manipulated)
            {
                //spawnSelectCycle
                if(InitialSelect==FinalSelect && InitialSelect!=null && selected==null)
                {
                    //selected
                    selected = InitialSelect;
                    currentState = SimpleState.Select;
                }
                else if(InitialSelect == FinalSelect && InitialSelect != null && selected != null && InitialSelect==FinalSelect)
                {
                    //deselected
                    selected = null;
                    currentState = SimpleState.init;
                }
                else if(InitialSelect==null && FinalSelect==null)
                {
                    //spawned
                    if (aRRaycastManager.Raycast(finalTouchPose, hitResults, TrackableType.PlaneWithinPolygon))
                    {
                        selected = null;
                        var pose = hitResults[0].pose;
                        var tmp = Instantiate(placement, pose.position, pose.rotation);
                        tmp.layer = layerMask;
                        currentState = SimpleState.Spawn;
                    }
                }
                

            }
            debugprint();
            starttimer = false;
            touchcount = 0;
            manipulated = false;
            InitialSelect = null;
            FinalSelect = null;

        }
        if(timer>0 && starttimer)
        {
            timer = timer - Time.deltaTime;
            if(tmr!=null)
                tmr.text = timer.ToString();
        }
        else if(!manipulated && starttimer)
        {
            currentState = SimpleState.Manipulate;
            manipulated = true;
            if (tmr != null)
                tmr.text = timer.ToString();
            debugprint();
        }
    }

}
