using System;
using UnityEngine;
using static GameController;


/**
 * GameState stores data for a GameState for any ship
 * UPDATED DESIGN: Simplified from 128 states to 64 states
 * - RelativeDirection: 4 categories (was 8)
 * - ThreatLevel: 4 categories (combines distance + speed)
 * - WallProximity: 4 categories (NEW - prevents wall trapping)
 */
public class GameState
{
    private RelativeDirection direction;
    private ThreatLevel threat;
    private WallProximity wallProximity;

    // the unique ID based on the combination of the 3 status variables
    public int gameStateID;


    /**
     * UpdateState will set the game state based on the provided values, and update the gameStateID value automatically
     */
    public void UpdateState(RelativeDirection _direction, ThreatLevel _threat, WallProximity _wallProximity)
    {
        direction = _direction;
        threat = _threat;
        wallProximity = _wallProximity;

        SetGameStateID();
    }


    /**
     * SetGameStateID will calculate the unique ID for the current game state based on the 3 status variables
     */
    private void SetGameStateID()
    {
        int v1 = Enum.GetNames(typeof(RelativeDirection)).Length;  // 4
        int v2 = Enum.GetNames(typeof(ThreatLevel)).Length;        // 4
        int v3 = Enum.GetNames(typeof(WallProximity)).Length;      // 4

        int s1 = (int)direction;
        int s2 = (int)threat;
        int s3 = (int)wallProximity;

        // Calculate unique state ID: 64 total states (4 * 4 * 4)
        gameStateID = (v2 * v3 * s1) + (v3 * s2) + s3;
    }

    /**
     * Get the current state components for debugging
     */
    public string GetStateDescription()
    {
        return $"Dir:{direction}, Threat:{threat}, Wall:{wallProximity}, ID:{gameStateID}";
    }
}