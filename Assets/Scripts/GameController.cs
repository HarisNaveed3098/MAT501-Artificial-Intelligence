using System;
using Assets.Scripts;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/**
 * GameController with diagnostic logging for generation tracking
 */
public class GameController : MonoBehaviour
{
    public enum RelativeDirection { Ahead, Right, Behind, Left };
    public enum ThreatLevel { Critical, High, Medium, Low };
    public enum WallProximity { None, Near, VeryNear, Critical };

    private string[] dirCatStr = { "Ahead", "Right", "Behind", "Left" };
    private string[] threatCatStr = { "Critical", "High", "Medium", "Low" };
    private string[] wallCatStr = { "None", "Near", "VeryNear", "Critical" };

    public float xBound = 9;
    public float yBound = 5;
    public float xAsteroidBound = 13;
    public float yAsteroidBound = 8;

    private float mutationRate = 0.02f;
    private int populationSize = 100;
    private int maxGenerations = 30;
    private float simTimeScale = 3f;

    private int asteroidWaveTotal = 40;
    private int asteroidPerWave = 1;

    private int asteroidWaveCounter = 0;
    Asteroid currentAsteroid;
    WaveData waveData = new WaveData();

    public GameObject asteroidPrefab;
    public GameObject shipPrefab;
    public GameObject bulletPrefab;

    bool gameRunning = false;

    float averageCollisionCount;
    public TMP_Text impactAngleText;
    public TMP_Text impactSpeedText;
    public TMP_Text impactDistanceText;
    public TMP_Text impactAngleCatText;
    public TMP_Text impactSpeedCatText;
    public TMP_Text impactDistanceCatText;
    public TMP_Text averageCollisionText;
    public TMP_Text currentAsteroidText;
    public TMP_Text currentGenerationText;
    public TMP_Text ammoText;
    public TMP_Text shotsFiredText;

    private int shipDataView = 0;

    private float impactAngleData;
    private float impactSpeedData;
    private float impactDistanceData;
    private float wallDistanceData;

    private RelativeDirection relativeDirectionData;
    private ThreatLevel threatLevelData;
    private WallProximity wallProximityData;

    private GeneticAglorithm<int> ga;
    private int curGeneration = 0;
    private List<Ship> allShips = new List<Ship>();
    private int numStates;
    private int dnaLength;
    private Boolean simulationInProgress;

    System.Random random;

    private DataLogger dataLogger;
    private float simulationStartTime;

    void Start()
    {
        int v1 = Enum.GetNames(typeof(RelativeDirection)).Length;
        int v2 = Enum.GetNames(typeof(ThreatLevel)).Length;
        int v3 = Enum.GetNames(typeof(WallProximity)).Length;

        numStates = v1 * v2 * v3;
        dnaLength = numStates;

        Debug.Log($"[INIT] State Space: {numStates} states, DNA Length: {dnaLength}");

        random = new System.Random();
        ga = new GeneticAglorithm<int>(populationSize, dnaLength, random, GetRandomGene, FitnessFunction, mutationRate: mutationRate);

        Time.timeScale = simTimeScale;

        dataLogger = new DataLogger("GA_Asteroid_Shooting_Log.txt");

        if (bulletPrefab == null)
            Debug.LogError("[INIT] CRITICAL: Bullet prefab is not assigned!");
        if (asteroidPrefab == null)
            Debug.LogError("[INIT] CRITICAL: Asteroid prefab is not assigned!");
        if (shipPrefab == null)
            Debug.LogError("[INIT] CRITICAL: Ship prefab is not assigned!");
    }

    void Update()
    {
        if (gameRunning)
        {
            DisplayUI();

            simulationInProgress = !IsLevelComplete();

            if (simulationInProgress)
            {
                HandleInput();
                UpdateShips();
                MoveShips();
                ProcessAsteroids();
            }
            else
            {
                // GENERATION COMPLETE
                Debug.Log($"[GEN {curGeneration}] Generation complete! Asteroid counter: {asteroidWaveCounter}/{asteroidWaveTotal}");

                float colTotal = 0;
                for (int i = 0; i < allShips.Count; i++)
                {
                    colTotal += allShips[i].GetCollisions();
                }

                averageCollisionCount = colTotal / allShips.Count;
                Debug.Log($"[GEN {curGeneration}] Avg Collisions = {averageCollisionCount:F2}");

                // Calculate shooting stats
                int totalKills = 0;
                int totalShots = 0;
                foreach (Ship ship in allShips)
                {
                    totalKills += ship.GetAsteroidsDestroyed();
                    totalShots += ship.GetShotsFired();
                }
                Debug.Log($"[GEN {curGeneration}] Total Kills: {totalKills}, Total Shots: {totalShots}");

                float elapsedTime = Time.time - simulationStartTime;

                Debug.Log($"[GEN {curGeneration}] Calling LogGeneration...");
                dataLogger.LogGeneration(curGeneration, ga.Population, averageCollisionCount, allShips);

                Debug.Log($"[GEN {curGeneration}] Calling LogDetailedGeneration...");
                dataLogger.LogDetailedGeneration(curGeneration, ga.Population, averageCollisionCount, asteroidWaveTotal, elapsedTime, allShips);

                Debug.Log($"[GEN {curGeneration}] Creating new generation...");
                ga.NewGeneration();
                curGeneration++;

                if (curGeneration >= maxGenerations)
                {
                    Debug.Log($"[COMPLETE] Max generations ({maxGenerations}) reached!");
                    StopGame();
                    ReportEndResults();
                }
                else
                {
                    Debug.Log($"[GEN {curGeneration}] Starting new generation...");
                    CreateShipGeneration();
                    asteroidWaveCounter = 0;
                    simulationInProgress = true;
                }
            }
        }
    }

    public void StartGame()
    {
        Debug.Log("[START] Game starting...");

        waveData.CreateWave(asteroidWaveTotal, xAsteroidBound, yAsteroidBound);
        CreateShips();
        asteroidWaveCounter = 0;

        NewAsteroidWave();
        gameRunning = true;
        simulationInProgress = true;

        simulationStartTime = Time.time;
        int maxAmmo = allShips.Count > 0 ? allShips[0].GetMaxAmmo() : 20;
        dataLogger.StartNewLog(populationSize, mutationRate, maxGenerations, asteroidWaveTotal, maxAmmo);
        Debug.Log("[START] Data logging started. Log file: " + dataLogger.GetLogFilePath());
    }

    void StopGame()
    {
        Debug.Log("[STOP] Game stopping...");
        gameRunning = false;
        simulationInProgress = false;
    }

    void ProcessAsteroids()
    {
        if (currentAsteroid == null)
        {
            if (asteroidWaveCounter < asteroidWaveTotal)
            {
                NewAsteroidWave();
            }
            return;
        }

        if (currentAsteroid.IsDesinationReached())
        {
            GameObject.Destroy(currentAsteroid.gameObject);
            currentAsteroid = null;
            NewAsteroidWave();
        }
    }

    void NewAsteroidWave()
    {
        for (int i = 0; i < asteroidPerWave; i++)
            SpawnAsteroid();

        asteroidWaveCounter++;

        // Log every 10 asteroids
        if (asteroidWaveCounter % 10 == 0)
        {
            Debug.Log($"[PROGRESS] Asteroid {asteroidWaveCounter}/{asteroidWaveTotal}");
        }
    }

    void ReportEndResults()
    {
        Debug.Log("[FINAL] === END RESULTS ===");
        Debug.Log($"[FINAL] Best solution has fitness: {ga.BestFitness}");

        if (allShips.Count > 0)
        {
            Ship bestShip = allShips[0];
            float bestFitness = float.MaxValue;

            foreach (Ship ship in allShips)
            {
                if (ship.GetCollisions() < bestFitness)
                {
                    bestFitness = ship.GetCollisions();
                    bestShip = ship;
                }
            }

            Debug.Log($"[FINAL] Best Ship Stats:");
            Debug.Log($"[FINAL]   Collisions: {bestShip.GetCollisions()}");
            Debug.Log($"[FINAL]   Shots Fired: {bestShip.GetShotsFired()}");
            Debug.Log($"[FINAL]   Asteroids Destroyed: {bestShip.GetAsteroidsDestroyed()}");
            float accuracy = bestShip.GetShotsFired() > 0 ? (bestShip.GetAsteroidsDestroyed() * 100f / bestShip.GetShotsFired()) : 0;
            Debug.Log($"[FINAL]   Shot Accuracy: {accuracy:F1}%");
        }

        Debug.Log("[FINAL] Writing final summary...");
        dataLogger.LogFinalSummary(curGeneration, ga.BestFitness, ga.BestGenes, allShips);
        dataLogger.Close();

        Debug.Log("[FINAL] Log file saved to: " + dataLogger.GetLogFilePath());
    }

    bool IsLevelComplete()
    {
        return asteroidWaveCounter >= asteroidWaveTotal;
    }

    void CreateShipGeneration()
    {
        RemoveOldShips();
        CreateShips();
    }

    void RemoveOldShips()
    {
        for (int i = allShips.Count - 1; i >= 0; i--)
        {
            GameObject.Destroy(allShips[i].gameObject);
        }
        allShips.Clear();
    }

    void CreateShips()
    {
        for (int i = 0; i < populationSize; i++)
        {
            GameObject go = GameObject.Instantiate(shipPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            Ship newShip = go.GetComponent<Ship>();
            newShip.Init(numStates, xBound, yBound);
            newShip.SetGene(ga.Population[i]);

            if (bulletPrefab != null)
            {
                newShip.bulletPrefab = bulletPrefab;
            }

            allShips.Add(newShip);
        }
    }

    void MoveShips()
    {
        foreach (Ship s in allShips)
        {
            s.ApplyMovement();
        }
    }

    void SpawnAsteroid()
    {
        if (asteroidWaveCounter >= waveData.spawnPoint.Count)
        {
            Debug.LogWarning($"[SPAWN] No spawn data for asteroid {asteroidWaveCounter}");
            return;
        }

        Vector2 _spawn = waveData.spawnPoint[asteroidWaveCounter];
        Vector2 _dest = waveData.destPoint[asteroidWaveCounter];
        float _speed = waveData.speed[asteroidWaveCounter];

        GameObject go = GameObject.Instantiate(asteroidPrefab, new Vector3(_spawn.x, _spawn.y, 0), Quaternion.identity);
        Asteroid a = go.GetComponent<Asteroid>();
        a.Init(new Vector2(_dest.x, _dest.y), _speed);
        a.StartMovement();

        currentAsteroid = a;
    }

    void UpdateShips()
    {
        if (currentAsteroid == null)
        {
            return;
        }

        int _shipTracker = 0;

        foreach (Ship ship in allShips)
        {
            Asteroid _a = GetClosestAsteroidToPoint(new Vector2(ship.transform.position.x, ship.transform.position.y));

            if (_a == null)
            {
                _shipTracker++;
                continue;
            }

            float _impactAngle = Vector2.SignedAngle(
                new Vector2(ship.transform.position.x, ship.transform.position.y),
                new Vector2(_a.transform.position.x, _a.transform.position.y)
            );

            Vector3 heading = ship.transform.position - _a.transform.position;
            Vector3 relVelocity = ship.GetVelocity() - _a.GetVelocity();
            Vector3 dir = heading.normalized;
            float _impactSpeed = Vector3.Dot(relVelocity, dir);

            float _impactDistance = Vector3.Distance(_a.transform.position, ship.transform.position);
            float _wallDistance = ship.GetClosestWallDistance();

            RelativeDirection _relativeDirection = ConvertAngleToRelativeDirection(_impactAngle);
            ThreatLevel _threatLevel = ConvertToThreatLevel(_impactDistance, _impactSpeed);
            WallProximity _wallProximity = ConvertToWallProximity(_wallDistance);

            ship.SetGameState(_relativeDirection, _threatLevel, _wallProximity);
            ship.SelectActionForGameState();

            if (_shipTracker == shipDataView)
            {
                impactAngleData = _impactAngle;
                impactSpeedData = _impactSpeed;
                impactDistanceData = _impactDistance;
                wallDistanceData = _wallDistance;

                relativeDirectionData = _relativeDirection;
                threatLevelData = _threatLevel;
                wallProximityData = _wallProximity;
            }

            _shipTracker++;
        }
    }

    Asteroid GetClosestAsteroidToPoint(Vector2 point)
    {
        return currentAsteroid;
    }

    private int GetRandomGene()
    {
        return UnityEngine.Random.Range(0, 1000);
    }

    private float FitnessFunction(int _index)
    {
        Ship ship = allShips[_index];

        // Primary objective: minimize collisions (most important)
        // LOWER fitness is BETTER
        float collisionPenalty = ship.GetCollisions() * 100f;

        // Secondary: Reward effective shooting
        float shootingBonus = ship.GetAsteroidsDestroyed() * 10f;

        // Penalize wasteful shooting
        float wastedAmmo = Mathf.Max(0, ship.GetShotsFired() - ship.GetAsteroidsDestroyed());
        float wastedAmmoPenalty = wastedAmmo * 1f;

        // Tertiary: Small bonuses for movement and survival
        float efficiencyBonus = ship.GetDistanceTraveled() * 0.05f;
        float survivalBonus = ship.GetTimeAlive() * 0.1f;

        // Calculate fitness (lower is better)
        float fitness = collisionPenalty + wastedAmmoPenalty - shootingBonus - efficiencyBonus - survivalBonus;

        return fitness;
    }

    void HandleInput()
    {
    }

    void DisplayUI()
    {
        if (impactAngleText != null)
            impactAngleText.text = $"Impact Angle: {impactAngleData:F1}°";
        if (impactSpeedText != null)
            impactSpeedText.text = $"Impact Speed: {impactSpeedData:F2}";
        if (impactDistanceText != null)
            impactDistanceText.text = $"Impact Distance: {impactDistanceData:F2}";

        if (impactAngleCatText != null)
            impactAngleCatText.text = $"Direction: {GetDirectionCategoryString()}";
        if (impactSpeedCatText != null)
            impactSpeedCatText.text = $"Threat: {GetThreatCategoryString()}";
        if (impactDistanceCatText != null)
            impactDistanceCatText.text = $"Wall: {GetWallCategoryString()} ({wallDistanceData:F1})";

        if (averageCollisionText != null)
            averageCollisionText.text = $"Average Collision: {averageCollisionCount:F2}";
        if (currentAsteroidText != null)
            currentAsteroidText.text = $"Current Asteroid: {asteroidWaveCounter}/{asteroidWaveTotal}";
        if (currentGenerationText != null)
            currentGenerationText.text = $"Current Generation: {curGeneration}";

        if (shipDataView >= 0 && shipDataView < allShips.Count)
        {
            Ship selectedShip = allShips[shipDataView];
            if (ammoText != null)
                ammoText.text = $"Ammo: {selectedShip.GetCurrentAmmo()}/{selectedShip.GetMaxAmmo()}";
            if (shotsFiredText != null)
                shotsFiredText.text = $"Shots: {selectedShip.GetShotsFired()} | Destroyed: {selectedShip.GetAsteroidsDestroyed()}";
        }
    }

    RelativeDirection ConvertAngleToRelativeDirection(float angle)
    {
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;

        if (angle >= -45 && angle < 45)
            return RelativeDirection.Ahead;
        else if (angle >= 45 && angle < 135)
            return RelativeDirection.Right;
        else if (angle >= 135 || angle < -135)
            return RelativeDirection.Behind;
        else
            return RelativeDirection.Left;
    }

    ThreatLevel ConvertToThreatLevel(float distance, float speed)
    {
        if (distance < 4 && speed < -3)
            return ThreatLevel.Critical;
        if (distance < 6 && speed < 0)
            return ThreatLevel.High;
        if (distance < 8 || speed < 3)
            return ThreatLevel.Medium;
        return ThreatLevel.Low;
    }

    WallProximity ConvertToWallProximity(float distance)
    {
        if (distance < 0.5f)
            return WallProximity.Critical;
        else if (distance < 1.5f)
            return WallProximity.VeryNear;
        else if (distance < 3.0f)
            return WallProximity.Near;
        else
            return WallProximity.None;
    }

    public void ShipDataChange(string value)
    {
        if (int.TryParse(value, out int _shipData))
        {
            if (_shipData < 0 || _shipData >= allShips.Count)
            {
                Debug.LogWarning($"Cannot view data for ship {value}");
            }
            else
            {
                shipDataView = _shipData;
            }
        }
    }

    string GetDirectionCategoryString()
    {
        int index = (int)relativeDirectionData;
        return (index >= 0 && index < dirCatStr.Length) ? dirCatStr[index] : $"Invalid {index}";
    }

    string GetThreatCategoryString()
    {
        int index = (int)threatLevelData;
        return (index >= 0 && index < threatCatStr.Length) ? threatCatStr[index] : $"Invalid {index}";
    }

    string GetWallCategoryString()
    {
        int index = (int)wallProximityData;
        return (index >= 0 && index < wallCatStr.Length) ? wallCatStr[index] : $"Invalid {index}";
    }

    void OnApplicationQuit()
    {
        Debug.Log("[QUIT] OnApplicationQuit - Closing data logger");
        if (dataLogger != null)
        {
            dataLogger.Close();
        }
    }
}