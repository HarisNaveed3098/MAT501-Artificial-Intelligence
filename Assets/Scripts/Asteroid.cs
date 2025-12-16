using UnityEngine;



/**
 * Asteroid represnts the asteroids that are spawned in the level that the Ship must avoid
 */
public class Asteroid : MonoBehaviour
{

    // movement and tracking
    private float moveSpeed = 2;     
    private Vector2 destination;
    private Vector2 startPoint;
    private float movePoint;
    private bool isMoving = false;
    private float moveTime;
    private bool destinationReached = false;
    private Vector3 velocity;
    


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isMoving)
        {
            ProgressMovement();
        }
    }


    /**
     * Init should be called before StartMovement() to set the origin and destination
     */
    public void Init(Vector2 _destination, float _speed)
    {
        moveSpeed = _speed;
        destination = _destination;
    }

    public bool IsDesinationReached()
    {
        return destinationReached;  
    }

    /**
     * StartMovement will init the movement data and trigger the movement to being
     *  - Desintation and SPeed should be set by calling Init() before calling this function
     */
    public void StartMovement()
    {

        // update the tracking data for the positon updates
        //  - Note: this is controled by postion adjustments, not physics
        startPoint = this.transform.position;
        float moveDist = Mathf.Sqrt(((startPoint.x - destination.x) * (startPoint.x - destination.x)) + ((startPoint.y - destination.y) * (startPoint.y - destination.y)));
        moveTime = moveDist / moveSpeed;
        movePoint = 0;
        isMoving = true;

        velocity = destination - startPoint;
        velocity.Normalize();

    }

    public float GetSpeed()
    {
        return moveSpeed;   
    }


    public Vector3 GetVelocity()
    {
        return velocity* moveSpeed;
    }



    /**
     * ProgressMovement will update the position of the astroid in the world space , and hsould be called each update() tick
     * 
     */
    void ProgressMovement()
    {
        Vector3 newPos = Vector3.zero;

        newPos.x = Mathf.Lerp(startPoint.x, destination.x, movePoint);
        newPos.y = Mathf.Lerp(startPoint.y, destination.y, movePoint);

        movePoint += Time.deltaTime / moveTime;

        this.transform.position = newPos;

        if(movePoint>=1)         // reached desination, stop movement and set flag for astroid to be destroyed
        {
            isMoving = false;
            destinationReached = true;
        }
    }
}
