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

public class ManagePrisoner : MonoBehaviour {
    private const float MIN_DIR_CHANGE_FREQ = 1.0f; // Minimum time between prisoner direction change
    private const float MAX_DIR_CHANGE_FREQ = 5.0f; // Maximum time between prisoner direction change
    private const int FACE_LEFT = 270;              // Y rotation when facing left
    private const int FACE_RIGHT = 90;
    private const int FACE_FORWARD = 180;
    private float leftBoundary;                     // Prisoners won't go left of this point
    private float rightBoundary;                    // Prisoners won't go right of this point
    private float finalZPos = 2.5f;                 // The Z position where prisoners go in enemy territory
    private float speed = 3.0f;                     // Prisoner movement speed
    private float snapDistance = 0.1f;              // Prisoner snaps to destination when this close to it
    private float timeToDirChange;                  // Time until prisoner changes direction
    private float timeSinceDirChange = 0.0f;        // Time since last direction change
    private enum  dir { left, right, forward };     // Possible directions prisoner can move
    private dir   direction;                        // Direction prisoner is going
    public  bool  isRescued = false;                // True if prisoner has been dropped off at base
    private float xDistanceToBase;                  // Distance in X axis to base when exiting chopper
    private float zDistanceToBase;                  // Distance in Z axis to base when exiting chopper
    private float zSpeedFactor = 1.0f;              // Affects speed in Z direction when running into base
    private GameObject baseEntrance;                // Entrance of base
    private float baseEntranceX;                    // X value of base entrance
    private float baseEntranceZ;                    // Z value of base entrance
    private GameObject chopper;
    private GameObject Base;
    private GameObject score;
    private Vector3 boxSizeForward;                 // Box collider dimensions when facing forward
    private Vector3 boxSizeLeftRight;               // Box collider dimensions when facing left or right
    public Animator anim;
    public int numChopperColliders = 0;             // Number of chopper box colliders we're colliding with

    void Awake() {
        anim = GetComponent<Animator>();
        boxSizeForward = new Vector3(1, 2, 20);
        boxSizeLeftRight = new Vector3(20, 2, 1);
        timeToDirChange = Random.Range(MIN_DIR_CHANGE_FREQ, MAX_DIR_CHANGE_FREQ);
        SetDirection((dir) Random.Range(0, 3));
    }

    void Start() {
        chopper = GameObject.Find("Chopper");
        Base = GameObject.Find("Base");
        rightBoundary = GameObject.Find("LeftRiverBoundary").transform.position.x;
        leftBoundary = GameObject.Find("LeftBoundary").transform.position.x;
        baseEntrance = GameObject.FindWithTag("baseEntrance");
        baseEntranceX = baseEntrance.transform.position.x;
        baseEntranceZ = baseEntrance.transform.position.z;
        score = GameObject.Find("Canvas");
    }

	void Update () {
        if (!isRescued) {
            // If chopper has landed in enemy territory and has room, run towards it
            if (!chopper.GetComponent<ControlChopper>().isCrashing &&
                chopper.GetComponent<ControlChopper>().isOnGround &&
                chopper.transform.position.x < rightBoundary &&
                score.GetComponent<ManageUI>().prisonersOnboard <
                chopper.GetComponent<ControlChopper>().capacity) {
                    if (chopper.transform.position.x < transform.position.x && direction != dir.left)
                        SetDirection(dir.left);
                    else if (chopper.transform.position.x > transform.position.x && direction != dir.right)
                        SetDirection(dir.right);
            }

            // If chopper is airborne, full, or crashing, run around randomly
            else {
                timeSinceDirChange += Time.deltaTime;

                // Stay within boundaries
                if (transform.position.x < leftBoundary) SetDirection(dir.right);
                else if (transform.position.x > rightBoundary) SetDirection(dir.left);

                // Every so often, prisoner changes direction randomly
                else if (timeSinceDirChange >= timeToDirChange) {
                    SetDirection((dir) Random.Range(0, 3));
                    timeSinceDirChange = 0.0f;

                    // Also randomize time until next change in direction
                    timeToDirChange = Random.Range(MIN_DIR_CHANGE_FREQ, MAX_DIR_CHANGE_FREQ);
                }
            }
        }

        // Once the prisoner reaches the entrance to the base, go inside
        else if (transform.position.z == baseEntranceZ) finalZPos = Base.transform.position.z;

        transform.position = new Vector3(GetXPos(), transform.position.y, GetZPos());
        // In theory, this should only need to be called by SetDirection(), but I found that SetTrigger
        //  often don't cause a state change. Spamming SetTrigger by calling it every frame works
        UpdateAnimation();
	}

    void SetDirection(dir newDirection) {
        direction = newDirection;

        // When changing direction, we need to update the box collider so it always extends into Z. But don't
        //  do this before the prisoner reaches finalZPos or it can result in picking them up early
        if (transform.position.z == finalZPos) {
            if (newDirection == dir.forward)
                transform.GetComponent<BoxCollider>().size = boxSizeForward;
            else
                transform.GetComponent<BoxCollider>().size = boxSizeLeftRight;
        }

        if (direction == dir.left) {
            if (transform.position.z == finalZPos)
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, FACE_LEFT, transform.eulerAngles.z);
            else    // When running out of the prison to the left or right, use an intermediate rotation
                transform.eulerAngles =
                    new Vector3(transform.eulerAngles.x, (FACE_LEFT + FACE_FORWARD) / 2, transform.eulerAngles.z);
        }

        else if (direction == dir.right) {
            if (transform.position.z == finalZPos)
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, FACE_RIGHT, transform.eulerAngles.z);
            else
                transform.eulerAngles =
                    new Vector3(transform.eulerAngles.x, (FACE_RIGHT + FACE_FORWARD) / 2, transform.eulerAngles.z);
        }

        else transform.eulerAngles = new Vector3(transform.eulerAngles.x, FACE_FORWARD, transform.eulerAngles.z);
    }

    void UpdateAnimation() {
        if (direction == dir.left || direction == dir.right) anim.SetTrigger("StartRunning");
        // Only wave if we've reached finalZPos
        else if (transform.position.z == finalZPos) anim.SetTrigger("StopRunning");
    }

    float GetXPos() {
        if (direction == dir.left) return transform.position.x - (speed * Time.deltaTime);
        else if (direction == dir.right) return transform.position.x + (speed * Time.deltaTime);
        else return transform.position.x;
    }

    float GetZPos() {
        // Due to the variable framerate, when the prisoner is sufficiently close to the final Z position,
        //  we need to set that final position explicitly
        // FIXME: find a way to do this that doesn't depend on a minimum framerate
        if (transform.position.z == finalZPos) return finalZPos;

        else if ((Mathf.Abs(transform.position.z - finalZPos) < snapDistance) ||
           (Mathf.Abs(transform.position.z + finalZPos) < snapDistance)) {
                timeSinceDirChange = timeToDirChange;   // Force direction/animation change
                return finalZPos;
        }

        else if (transform.position.z > finalZPos)
            return transform.position.z - (speed * zSpeedFactor * Time.deltaTime);

        else return transform.position.z + (speed * zSpeedFactor * Time.deltaTime);
    }

    void OnTriggerEnter(Collider col) {
        if (col.gameObject.tag == "landingPad") {   // On landing pad; run into base
            if (!isRescued) {                       // First call
                isRescued = true;
                SetDirection(dir.right);
                transform.LookAt(baseEntrance.transform);
                finalZPos = baseEntranceZ;
                xDistanceToBase = baseEntranceX - transform.position.x;
                zDistanceToBase = finalZPos - transform.position.z;
                // Changes Z speed so prisoner reaches X and Z destinations simultaneously
                zSpeedFactor = zDistanceToBase / xDistanceToBase;
            }
        }

        // To make the prisoners climb into the cockpit instead of the nose or tail when chopper is facing
        //  left/right, we need to keep track of how many box colliders the prisoner is colliding with
        else if (col.gameObject.tag == "chopper") numChopperColliders += 1;

        else if (col.gameObject.tag == "baseWall") Destroy(gameObject); // Reached base
    }

    void OnTriggerExit(Collider col) {
        if (col.gameObject.tag == "chopper") {
            numChopperColliders -= 1;
        }
    }
}
