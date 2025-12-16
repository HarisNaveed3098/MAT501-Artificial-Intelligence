using Assets.Scripts;
using System;
using System.Collections.Generic;
using UnityEngine;
using static GameController;



/**
 * Ship represents the AI's ship in the level
 * UPDATED DESIGN: Added shooting mechanic with limited ammo
 * - 13 combined actions (added Shoot action)
 * - Ammo management system
 * - Bullet spawning capability
 * FIXES: Proper cooldown handling, bullet ownership tracking, increased spawn offset
 */
public class Ship : MonoBehaviour
{
    // UPDATED: Combined action system with Shoot action
    public enum CombinedAction
    {
        Stop = 0,           // No movement
        SlowForward,        // Continue current heading, slow
        FastForward,        // Continue current heading, fast
        SlowLeft,           // Turn left 45°, slow
        FastLeft,           // Turn left 45°, fast
        SlowRight,          // Turn right 45°, slow
        FastRight,          // Turn right 45°, fast
        SlowSharpLeft,      // Turn left 90°, slow
        FastSharpLeft,      // Turn left 90°, fast
        SlowSharpRight,     // Turn right 90°, slow
        FastSharpRight,     // Turn right 90°, fast
        Reverse,            // Backward movement (emergency)
        Shoot               // NEW: Fire a bullet in current direction
    };

    // Movement control
    private Vector3 steerVec = new Vector3(0, 1, 0); // Start facing north
    private float currentHeading = 0; // Degrees, 0 = north
    private float speed = 0;
    private CombinedAction currentAction;

    // Speed constants
    private const float SPEED_STOP = 0f;
    private const float SPEED_SLOW = 1f;
    private const float SPEED_FAST = 2f;
    private const float SPEED_REVERSE = -0.5f;

    // Turn angles
    private const float TURN_NORMAL = 45f;
    private const float TURN_SHARP = 90f;

    // NEW: Shooting system  Increased cooldown and better spawn offset
    public GameObject bulletPrefab;
    private int maxAmmo = 20;
    private int currentAmmo;
    private float shootCooldown = 1.0f; // Increased from 0.3f to 1.0f
    private float lastShotTime = -999f; // Set to -999f to prevent first-frame shooting
    private int shotsFired;
    private int asteroidsDestroyed; // Track successful hits

    // Tracking data
    private int collisions;
    private float distanceTraveled;
    private float timeAlive;
    private Vector3 lastPosition;

    // Visual options
    public Sprite stopSprite;
    public Sprite slowSprite;
    public Sprite fastSprite;

    // DNA storage - UPDATED: Single list for combined actions (now 13 actions)
    private List<int> combinedActions = new List<int>();
    private const int ACTION_COUNT = 13; // Number of CombinedAction enum values

    private int stateSize;

    // The map boundaries the ship cannot move beyond
    private float xBound;
    private float yBound;

    // Store the current game state for this ship
    private GameState currentState = new GameState();

    void Start()
    {
        currentAction = CombinedAction.SlowForward;
        speed = SPEED_SLOW;
        currentAmmo = maxAmmo;
    }

    void Update()
    {
        // Update metrics for fitness calculation
        UpdateMetrics();
    }

    /**
     * Init should be called to initialize a ship at the start of any level/simulation phase
     */
    public void Init(int _stateSize, float _xBound, float _yBound)
    {
        collisions = 0;
        distanceTraveled = 0;
        timeAlive = 0;
        shotsFired = 0;
        asteroidsDestroyed = 0;

        this.transform.position = Vector3.zero;
        lastPosition = this.transform.position;

        stateSize = _stateSize;
        xBound = _xBound;
        yBound = _yBound;

        // Reset ammo
        currentAmmo = maxAmmo;
        lastShotTime = -999f; // Reset to prevent immediate shooting

        // Start facing north
        currentHeading = 0;
        UpdateSteerVector();
    }

    /**
     * SetGene will take the GA's DNA structure and store it in the Ship's data structures
     * UPDATED: Simplified to single action list
     */
    public void SetGene(DNA<int> _geneData)
    {
        if (_geneData.Genes.Length < stateSize)
        {
            Debug.LogWarning($"WARNING: _geneData length ({_geneData.Genes.Length}) is insufficient for state size {stateSize}");
            return;
        }

        // Copy genes and map to valid action range
        combinedActions.Clear();
        for (int i = 0; i < stateSize; i++)
        {
            combinedActions.Add(_geneData.Genes[i] % ACTION_COUNT);
        }
    }

    /**
     * SetGameState will store the game state based on the provided values
     * UPDATED: Uses new simplified state variables
     */
    public void SetGameState(RelativeDirection _direction, ThreatLevel _threat, WallProximity _wallProximity)
    {
        currentState.UpdateState(_direction, _threat, _wallProximity);
    }

    /**
     * SelectActionForGameState will set the action based on the DNA data for the current Game State
     */
    public void SelectActionForGameState()
    {
        int _gameStateID = currentState.gameStateID;

        if (_gameStateID >= combinedActions.Count)
        {
            Debug.LogWarning($"WARNING: Cannot select action for game state id {_gameStateID}");
            return;
        }

        // Get the combined action for this state
        CombinedAction action = (CombinedAction)combinedActions[_gameStateID];
        SetCombinedAction(action);
    }

    /**
     * SetCombinedAction decodes a combined action into speed and heading adjustments
     * UPDATED: Added Shoot action handling
     */
    private void SetCombinedAction(CombinedAction action)
    {
        if (currentAction == action && action != CombinedAction.Shoot)
            return; // No change needed (except for shoot which can repeat)

        currentAction = action;

        switch (action)
        {
            case CombinedAction.Stop:
                speed = SPEED_STOP;
                UpdateSprite(stopSprite);
                break;

            case CombinedAction.SlowForward:
                speed = SPEED_SLOW;
                UpdateSprite(slowSprite);
                break;

            case CombinedAction.FastForward:
                speed = SPEED_FAST;
                UpdateSprite(fastSprite);
                break;

            case CombinedAction.SlowLeft:
                speed = SPEED_SLOW;
                AdjustHeading(-TURN_NORMAL);
                UpdateSprite(slowSprite);
                break;

            case CombinedAction.FastLeft:
                speed = SPEED_FAST;
                AdjustHeading(-TURN_NORMAL);
                UpdateSprite(fastSprite);
                break;

            case CombinedAction.SlowRight:
                speed = SPEED_SLOW;
                AdjustHeading(TURN_NORMAL);
                UpdateSprite(slowSprite);
                break;

            case CombinedAction.FastRight:
                speed = SPEED_FAST;
                AdjustHeading(TURN_NORMAL);
                UpdateSprite(fastSprite);
                break;

            case CombinedAction.SlowSharpLeft:
                speed = SPEED_SLOW;
                AdjustHeading(-TURN_SHARP);
                UpdateSprite(slowSprite);
                break;

            case CombinedAction.FastSharpLeft:
                speed = SPEED_FAST;
                AdjustHeading(-TURN_SHARP);
                UpdateSprite(fastSprite);
                break;

            case CombinedAction.SlowSharpRight:
                speed = SPEED_SLOW;
                AdjustHeading(TURN_SHARP);
                UpdateSprite(slowSprite);
                break;

            case CombinedAction.FastSharpRight:
                speed = SPEED_FAST;
                AdjustHeading(TURN_SHARP);
                UpdateSprite(fastSprite);
                break;

            case CombinedAction.Reverse:
                speed = SPEED_REVERSE;
                UpdateSprite(stopSprite);
                break;

            case CombinedAction.Shoot:
                // NEW: Handle shooting action
                Shoot();
                break;
        }
    }

    /**
     * NEW: Shoot method handles firing bullets
     *  Proper cooldown check and increased spawn offset
     */
    private void Shoot()
    {
        // Check if we can shoot (ammo available and cooldown expired)
        if (currentAmmo <= 0)
            return;

        //  Proper cooldown check that handles initial case
        if (Time.time - lastShotTime < shootCooldown)
            return;

        // Check if bulletPrefab is assigned
        if (bulletPrefab == null)
        {
            Debug.LogWarning("Bullet prefab not assigned to ship!");
            return;
        }

        //  Increased spawn offset from 0.5f to 1.0f to prevent immediate collisions
        Vector3 spawnOffset = steerVec * 1.0f;
        GameObject bulletObj = GameObject.Instantiate(
            bulletPrefab,
            transform.position + spawnOffset,
            Quaternion.identity
        );

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            //  Pass ship reference for tracking
            bullet.Init(steerVec, 10f, this);
        }

        // Update shooting stats
        currentAmmo--;
        shotsFired++;
        lastShotTime = Time.time;
    }

    /**
     * AdjustHeading changes the ship's heading by the specified angle
     */
    private void AdjustHeading(float angleDelta)
    {
        currentHeading += angleDelta;

        // Normalize to 0-360 range
        while (currentHeading < 0) currentHeading += 360;
        while (currentHeading >= 360) currentHeading -= 360;

        UpdateSteerVector();
    }

    /**
     * UpdateSteerVector converts the heading angle to a direction vector
     */
    private void UpdateSteerVector()
    {
        // Convert heading to radians (Unity uses degrees, but Mathf uses radians)
        float radians = currentHeading * Mathf.Deg2Rad;

        // Calculate direction vector (0° = north = positive Y)
        steerVec.x = Mathf.Sin(radians);
        steerVec.y = Mathf.Cos(radians);
        steerVec.z = 0;
        steerVec.Normalize();

        // Update visual rotation
        this.transform.localEulerAngles = new Vector3(0, 0, -currentHeading);
    }

    /**
     * UpdateSprite changes the ship's visual based on speed
     */
    private void UpdateSprite(Sprite sprite)
    {
        this.GetComponent<SpriteRenderer>().sprite = sprite;
    }

    /**
     * ApplyMovement will move the ship according to the steer vector and speed
     */
    public void ApplyMovement()
    {
        Vector3 movement = steerVec * Time.deltaTime * speed;
        this.transform.position += movement;

        // Enforce boundaries
        Vector3 _newPos = this.transform.position;

        if (this.transform.position.x >= xBound)
            _newPos.x = xBound;
        if (this.transform.position.x <= -xBound)
            _newPos.x = -xBound;
        if (this.transform.position.y >= yBound)
            _newPos.y = yBound;
        if (this.transform.position.y <= -yBound)
            _newPos.y = -yBound;

        this.transform.position = _newPos;
    }

    /**
     * UpdateMetrics tracks performance metrics for enhanced fitness calculation
     */
    private void UpdateMetrics()
    {
        // Track distance traveled
        float frameDist = Vector3.Distance(transform.position, lastPosition);
        distanceTraveled += frameDist;
        lastPosition = transform.position;

        // Track time alive
        timeAlive += Time.deltaTime;
    }

    /**
     * OnTriggerEnter2D will be called when the Ship collides with another object
     */
    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.CompareTag("Asteroid"))
        {
            collisions++;
        }
    }

    /**
     * NEW: Notify ship when it successfully destroys an asteroid
     *  Now properly called from Bullet collision
     */
    public void OnAsteroidDestroyed()
    {
        asteroidsDestroyed++;
    }

    // Getters for fitness calculation
    public int GetCollisions() => collisions;
    public float GetDistanceTraveled() => distanceTraveled;
    public float GetTimeAlive() => timeAlive;
    public int GetShotsFired() => shotsFired;
    public int GetAsteroidsDestroyed() => asteroidsDestroyed;
    public int GetCurrentAmmo() => currentAmmo;
    public int GetMaxAmmo() => maxAmmo;

    public Vector3 GetVelocity()
    {
        return steerVec * speed;
    }

    /**
     * GetWallProximity calculates the closest distance to any boundary
     */
    public float GetClosestWallDistance()
    {
        float distToRight = xBound - transform.position.x;
        float distToLeft = transform.position.x + xBound;
        float distToTop = yBound - transform.position.y;
        float distToBottom = transform.position.y + yBound;

        return Mathf.Min(distToRight, distToLeft, distToTop, distToBottom);
    }
}