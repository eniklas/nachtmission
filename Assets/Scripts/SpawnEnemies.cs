/* nachtmission
 * Copyright (c) 2017-2018 Erik Niklas
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnEnemies : MonoBehaviour {
    private float rightBoundary;                        // X position of right boundary (river)
    private float leftBoundary;                         // X position of left boundary
    private float ceiling;                              // Y position of top boundary
    private float ground;                               // Y position of bottom boundary
    private const float tankSpawnRate = 15.0f;          // Seconds between potential tank spawns
    private const float jetSpawnRate = 15.0f;           // Seconds between potential jet spawns
    private const float droneSpawnRate = 15.0f;         // Seconds between potential drone spawns
    private const int TANK_SPAWN_DISTANCE_MIN = 50;     // Min distance from chopper to spawn a tank
    private const int TANK_SPAWN_DISTANCE_MAX = 150;    // Max distance from chopper to spawn a tank
    private const float TANK_SPAWN_POS_Y = 6.3f;        // Y position to place tanks
    private const int TANK_SPAWN_POS_Z = -5;            // Z position to place tanks
    private const int JET_SPAWN_DISTANCE_X = 100;       // How far left/right of chopper to spawn jets
    private const int JET_SPAWN_DISTANCE_Y = 15;        // How high above chopper to spawn jets
    private const int JET_SPAWN_POS_Z = 10;             // Z position to place jets
    private const int DRONE_SPAWN_DISTANCE_X = 100;     // How far left/right of chopper to spawn drones
    private const int DRONE_SPAWN_POS_Z = 0;            // Z position to place drones
    private const int MAX_TANKS = 4;                    // Maximum number of tanks that can exist at once
    private const int MAX_DRONES = 1;                   // Maximum number of drones that can exist at once
    public  float chopperStillMaxSpeed = 10.0f;         // Max speed at which chopper is considered to be still
    private GameObject chopper;
    public  GameObject tank;
    public  GameObject jet;
    public  GameObject drone;

	void Start () {
        chopper = GameObject.Find("Chopper");
        rightBoundary = GameObject.Find("LeftRiverBoundary").transform.position.x;
        leftBoundary = GameObject.Find("LeftBoundary").transform.position.x;
        ceiling = GameObject.Find("Ceiling").transform.position.y;
        ground = GameObject.Find("Terrain").transform.position.y +
            chopper.GetComponent<ManageChopper>().minHeightAboveGround;
	}

    public void IntroduceTanks() {
        InvokeRepeating("SpawnTank", tankSpawnRate, tankSpawnRate);
    }

    public void IntroduceJets() {
        InvokeRepeating("SpawnJet", jetSpawnRate, jetSpawnRate);    // Jets spawn at a constant interval
    }

    public void IntroduceDrones() {
        InvokeRepeating("SpawnDrone", droneSpawnRate, droneSpawnRate);
    }

    void SpawnTank() {
        // FIXME: ground is 0.6 units too high for tanks. Give them weight, spawn them above ground, and let them fall
        // Spawn a new tank if the chopper is to the left of the river and there aren't enough tanks already
        if (chopper.transform.position.x < rightBoundary &&
            GameObject.FindGameObjectsWithTag("tank").Length < MAX_TANKS) {

            // Spawn to the left if the coin flip comes out that way, or if the chopper is too close to the river
            if (rightBoundary - chopper.transform.position.x < TANK_SPAWN_DISTANCE_MAX ||
                Random.Range(0, 2) == 0) {    // Spawn to the left at a random X position
                GameObject.Instantiate(tank,
                    new Vector3(chopper.transform.position.x -
                        Random.Range(TANK_SPAWN_DISTANCE_MIN, TANK_SPAWN_DISTANCE_MAX),
                        TANK_SPAWN_POS_Y,
                        TANK_SPAWN_POS_Z),
                        Quaternion.identity);
                Debug.Log("Spawning tank to the left.");
            }
            else {
                GameObject.Instantiate(tank,
                    new Vector3(chopper.transform.position.x +
                        Random.Range(TANK_SPAWN_DISTANCE_MIN, TANK_SPAWN_DISTANCE_MAX),
                        TANK_SPAWN_POS_Y,
                        TANK_SPAWN_POS_Z),
                        Quaternion.identity);
                Debug.Log("Spawning tank to the right.");
            }
        }
    }

    void SpawnJet() {
        // Only spawn a jet if the chopper is in enemy territory; spawn just off the screen to the left
        if (chopper.transform.position.x < rightBoundary) {
            if (chopper.GetComponent<Rigidbody>().velocity.x > chopperStillMaxSpeed &&
                rightBoundary - chopper.transform.position.x > JET_SPAWN_DISTANCE_X) {
                    GameObject.Instantiate(jet,
                        new Vector3(chopper.transform.position.x + JET_SPAWN_DISTANCE_X,
                                    chopper.transform.position.y + JET_SPAWN_DISTANCE_Y,
                                    JET_SPAWN_POS_Z),
                                    Quaternion.identity);
                Debug.Log("Spawning jet to the right.");
            }
            else {
                GameObject.Instantiate(jet,
                    new Vector3(chopper.transform.position.x - JET_SPAWN_DISTANCE_X,
                                chopper.transform.position.y + JET_SPAWN_DISTANCE_Y,
                                JET_SPAWN_POS_Z),
                                Quaternion.identity);
                Debug.Log("Spawning jet to the left.");
            }
        }
    }

    void SpawnDrone() {
        if (GameObject.FindGameObjectsWithTag("drone").Length < MAX_DRONES) {
            // Don't want to spawn in view; randomly choose whether to spawn to the left
            //  or right, but force left if the chopper is too close to the river
            if (Random.Range(0, 2) == 0 || rightBoundary - chopper.transform.position.x < DRONE_SPAWN_DISTANCE_X) {
                GameObject.Instantiate(drone,
                    new Vector3(Random.Range(leftBoundary - DRONE_SPAWN_DISTANCE_X,
                                    chopper.transform.position.x - DRONE_SPAWN_DISTANCE_X),
                                Random.Range(ground, ceiling),
                                DRONE_SPAWN_POS_Z),
                                Quaternion.identity);
                Debug.Log("Spawning drone to the left.");
            }

            // Spawn to the right as long as it's not past the river
            else {
                GameObject.Instantiate(drone,
                new Vector3(Random.Range(chopper.transform.position.x + DRONE_SPAWN_DISTANCE_X, rightBoundary),
                            Random.Range(ground, ceiling),
                            DRONE_SPAWN_POS_Z),
                            Quaternion.identity);
                Debug.Log("Spawning drone to the right.");
            }
        }
    }
}
