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

public class ControlChopper : MonoBehaviour {
    private const float rotationSpeed = 0.1f;          // Seconds it takes to rotate
    private float       rotationTime = 0.0f;           // Time chopper has been rotating
    private float       rotation;
    private const int   FORWARD_ANGLE = 0;             // Angle rotated about Y when facing forward
    private const int   LEFT_ANGLE = 90;               // Angle rotated about Y when facing left
    private const int   RIGHT_ANGLE = -90;             // Angle rotated about Y when facing right
    private const float MIN_TIME_BETWEEN_TURNS = 0.1f; // Time to disable turns
    private bool        isRotatingLeft;                // Whether chopper is rotating left
    private bool        isRotatingRight;               // Whether chopper is rotating right
    private enum        dir { left, forward, right };  // Possible directions chopper can face
    private dir         direction;                     // Direction chopper is facing

    private const float MIN_PITCH = -30.0f;            // Min degrees to pitch
    private const float MAX_PITCH = 30.0f;             // Max degrees to pitch
    private float       pitchingTime = 0.0f;           // Time chopper has been pitching
    private const float PITCH_ACCELERATION = 2.0f;     // Acceleration for pitching forward
    private const float LEVEL_ACCELERATION = 1.0f;     // Acceleration for leveling out
    private const int   PITCH_DEAD_ZONE = 1;           // Degrees from zero to consider level
    private const float TIME_TO_LEVEL = 2.0f;          // How long it takes to level out
    public float        pitch;                         // Current pitch
    private bool        isLevel;
    private bool        isPitching;
    private bool        isLeveling;

    private const float MAX_HSPEED = 50.0f;            // Max horizontal speed of chopper
    private const float MAX_VSPEED = 25.0f;            // Max vertical speed of chopper
    private const float H_ACCELERATION = 60.0f;        // Horizontal acceleration
    private const float V_ACCELERATION = 40.0f;        // vertical acceleration
    private const float SPEED_CRASH_FACTOR = 0.8f;     // Percentage of horizontal speed that will cause a crash
    public  float       minHeightAboveGround;
    private float       leftBoundary;                  // Left boundary of playfield
    private float       rightBoundary;                 // Right boundary of playfield
    private float       leftRiverBoundary;             // Left edge of river
    private float       rightRiverBoundary;            // Right edge of river
    private float       ceiling;                       // Max height chopper can fly
    private float       ground;                        // Min height chopper can fly
    public  float       hSpeed;                        // Current horizontal speed (jet needs to read this)
    private float       vSpeed;                        // Current vertical speed

    public bool         isOnGround;                    // Rotor blade uses this
    public bool         justLanded;                    // True on the frame that we land
    private GameObject  muzzle;                        // Bullets are instantiated here
    private GameObject  muzzleTail;                    // Used to get the direction the chopper is facing
    public  GameObject  muzzleFlash;
    private ParticleSystem muzzleFlashPS;
    public  GameObject  prisoner;
    private float       prisonerUnloadFreq = 0.5f;     // Frequency that prisoners emerge from chopper
    private float       timeSinceLastUnload = 0.0f;    // Time since last prisoner emerged from chopper

    private const float bulletSpeed = 65.0f;
    public int          capacity = 16;                 // Max prisoners the chopper can hold
    private bool        fixedUpdateOTS = false;

    public GameObject   explosion;                     // Crash explosion
    public bool         isCrashing = false;            // True if chopper is currently crashing
    public float        timeCrashing = 0.0f;           // Time chopper has been crashing
    private const float TIME_TO_CRASH = 7.0f;          // Seconds to wait when crashing before reloading scene
    private const float MAX_CRASH_FORCE = 20.0f;       // Magnitude of forces to apply when crashing

    private ManageUI    uiScript;
    private GameObject  landingPad;
    private Quaternion  startingRotation;              // Initial rotation of chopper

    public GameObject bullet;
    private GameObject bulletClone;
    public GameObject sounds;
    private GameObject soundsClone;

    // Offsets for audio clips attached to this object
    private const int SOUND_FIRE = 0;
    private const int SOUND_PRISONER_BOARD = 1;

    // The following are contained in the Sounds prefab
    private const int SOUND_CHOPPER_EXPLOSION = 1;
    private const int SOUND_PRISONER_SCREAM_MIN = 2;
    private const int SOUND_PRISONER_SCREAM_MAX = 4;

    void Awake() {
        // ChopperMuzzle and ChopperMuzzleTail are zero-size objects; bullets are instantiated on muzzle, and muzzleTail
        //  is used to get a normalized vector of the direction the chopper is pointing so that bullets move in the same
        //  direction (i.e., taking pitch into account) when facing left or right
        muzzle = transform.Find("ChopperMuzzle").gameObject;
        muzzleTail = transform.Find("ChopperMuzzleTail").gameObject;
        muzzleFlashPS = transform.Find("ChopperMuzzleFlash").gameObject.GetComponent<ParticleSystem>();
        startingRotation = transform.rotation;
    }

    void Start () {
        uiScript = GameObject.Find("Canvas").GetComponent<ManageUI>();
        landingPad = GameObject.Find("LandingPad");
        // Empty objects are used to delimit boundaries
        leftBoundary = GameObject.Find("LeftBoundary").gameObject.transform.position.x;
        rightBoundary = GameObject.Find("RightBoundary").gameObject.transform.position.x;
        leftRiverBoundary = GameObject.Find("LeftRiverBoundary").gameObject.transform.position.x;
        rightRiverBoundary = GameObject.Find("RightRiverBoundary").gameObject.transform.position.x;
        ceiling = GameObject.Find("Ceiling").gameObject.transform.position.y;
        ground = GameObject.Find("Terrain").gameObject.transform.position.y + minHeightAboveGround;
        initChopper();
    }

	void FixedUpdate () {
        fixedUpdateOTS = true;     // Required to prevent multiple calls to OnTriggerStay per frame
    }

	void Update () {
        // Landing; this should only happen on the frame when it touches the ground
        if (!isOnGround &&
            transform.position.y <= ground &&
            !(transform.position.x >= leftRiverBoundary &&
            transform.position.x <= rightRiverBoundary)) {
                // Crash if player hits ground when going too fast
                if (Mathf.Abs(hSpeed) >= SPEED_CRASH_FACTOR * MAX_HSPEED) Crash();
                isOnGround = true;
                justLanded = true;  // Will only be true for this frame; used to detect if we've landed on a prisoner
                hSpeed = 0;
        }
        else if (justLanded) justLanded = false;

        if (isCrashing) {
            if (justLanded) {   // Just crashed into the ground; explode
                GameObject soundsClone = GameObject.Instantiate(sounds, transform.position, Quaternion.identity);
                soundsClone.GetComponents<AudioSource>()[SOUND_CHOPPER_EXPLOSION].Play();
                Destroy(soundsClone, soundsClone.GetComponents<AudioSource>()[SOUND_CHOPPER_EXPLOSION].clip.length);
                
                GameObject expClone = GameObject.Instantiate(explosion, transform.position, Quaternion.identity);
                Destroy(expClone, 3.5f);
            }

            Crash();
            return;             // Don't do anything else if we're currently crashing
        }

		if ((Input.GetAxis("LeftTrigger") > 0 || Input.GetKey("q")) &&
            !isRotatingLeft && !isRotatingRight &&
            !isOnGround &&
            direction != dir.left) {
                rotationTime = 0.0f;
                isRotatingLeft = true;

                if (direction == dir.right) direction = dir.forward;
                else direction = dir.left;
        }

		else if ((Input.GetAxis("RightTrigger") > 0 || Input.GetKey("e")) &&
            !isRotatingLeft &&
            !isRotatingRight &&
            !isOnGround &&
            direction != dir.right) {
                rotationTime = 0.0f;
                isRotatingRight = true;

                if (direction == dir.left) direction = dir.forward;
                else direction = dir.right;
        }

		if (Input.GetAxis("Horizontal") != 0) {
            hSpeed += (Input.GetAxis("Horizontal") * Time.deltaTime * H_ACCELERATION);
            if ((transform.position.x >= rightBoundary && hSpeed > 0) || isOnGround) hSpeed = 0;
            else if (transform.position.x <= leftBoundary && hSpeed < 0) hSpeed = 0;
            else if (hSpeed >= MAX_HSPEED) hSpeed = MAX_HSPEED;
            else if (hSpeed <= -MAX_HSPEED) hSpeed = -MAX_HSPEED;

            if (!isPitching) {          // We've just started pitching
                pitchingTime = 0.0f;
                isPitching = true;
                isLevel = false;
                isLeveling = false;
            }
        }

        else {  // No horizontal input; slow down and level out
            if (hSpeed > 0) hSpeed -= Time.deltaTime * 0.5f * H_ACCELERATION;
            else if (hSpeed < 0) hSpeed += Time.deltaTime * 0.5f * H_ACCELERATION;

            if (!isLevel && !isLeveling) {
                pitchingTime = 0.0f;
                isLeveling = true;
                isPitching = false;
            }
        }

		if (Input.GetAxis("Vertical") != 0) {
            vSpeed += (Input.GetAxis("Vertical") * Time.deltaTime * V_ACCELERATION);

            if (vSpeed >= MAX_VSPEED) vSpeed = MAX_VSPEED;
            else if (vSpeed <= -MAX_VSPEED) vSpeed = -MAX_VSPEED;

            // Taking off
            if (vSpeed > 0 && isOnGround) isOnGround = false;
        }

        else {
            if (vSpeed > 0) vSpeed -= Time.deltaTime * 0.5f * V_ACCELERATION;
            else if (vSpeed < 0) vSpeed += Time.deltaTime * 0.5f * V_ACCELERATION;
        }

        // Don't fire if the menu has just cleared; prevents the GUI button click from causing an unwanted fire.
        //  For some reason, the timeSinceMenuClear method only works for the joystick, and the timeScale method only
        //  works for the keyboard and mouse, so we need both
        if (Input.GetButtonDown("Fire1") && !isOnGround && uiScript.timeSinceMenuClear > 0.2f && Time.timeScale == 1)
            Fire();
        if (isLeveling || isPitching) Pitch();
        if (isRotatingLeft || isRotatingRight) Rotate();

        // Keep chopper inside boundaries
        if (transform.position.x > rightBoundary) {
            transform.position = new Vector3(rightBoundary, transform.position.y, 0);
            hSpeed = 0;
        }
        else if (transform.position.x < leftBoundary) {
            transform.position = new Vector3(leftBoundary, transform.position.y, 0);
            hSpeed = 0;
        }

        if (transform.position.y > ceiling) {
            transform.position = new Vector3(transform.position.x, ceiling, 0);
            vSpeed = 0;
        }
        else if (transform.position.y < ground) {
            transform.position = new Vector3(transform.position.x, ground, 0);
            vSpeed = 0;
        }

        transform.Translate(new Vector3(hSpeed, vSpeed, 0) * Time.deltaTime, Space.World);
	}

    public void initChopper() {
        pitch = 0.0f;
        hSpeed = 0.0f;
        vSpeed = 0.0f;
        direction = dir.forward;
        isRotatingLeft = false;
        isRotatingRight = false;
        isLevel = true;
        isPitching = false;
        isLeveling = false;
        isOnGround = true;
        justLanded = false;

        gameObject.GetComponent<Rigidbody>().useGravity = false;
        gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;

        transform.rotation = startingRotation;
        transform.position = new Vector3(landingPad.transform.position.x, ground, 0);
    }

    private void Fire() {
        if (direction == dir.forward) {
            bulletClone = GameObject.Instantiate(bullet, transform.position + new Vector3(0, -2, 0),
                Quaternion.identity);
            bulletClone.GetComponent<Rigidbody>().AddForce(Vector3.down * 0.5f * bulletSpeed, ForceMode.VelocityChange);
        }

        else {
            bulletClone = GameObject.Instantiate(bullet, muzzle.transform.position, Quaternion.identity);
            bulletClone.GetComponent<Rigidbody>().useGravity = false;    // Disable gravity for left and right firing

            bulletClone.GetComponent<Rigidbody>().AddForce(
                (muzzle.transform.position - muzzleTail.transform.position) * bulletSpeed, ForceMode.VelocityChange);

            // Adding an additional horizontal force prevents the appearance of bullet lag when moving fast
            bulletClone.GetComponent<Rigidbody>().AddForce(Vector3.right * hSpeed * 0.75f, ForceMode.VelocityChange);
        }

        muzzleFlashPS.Play();
        GetComponents<AudioSource>()[SOUND_FIRE].Play();
        bulletClone.tag = "source:chopper";
    }

    private void Pitch() {
        // Since we're not using Time in this function, the chopper can pitch while the game is paused
        if (Time.timeScale != 1) return;

        // Level out faster when we're on the ground
        if (isOnGround) pitchingTime += Time.deltaTime * 3;
        else pitchingTime += Time.deltaTime;

        if (isOnGround) {
            isPitching = false;
            isLeveling = true;
        }

        if (!isRotatingLeft && !isRotatingRight) {
            if (isPitching) {
                pitch += Input.GetAxis("Horizontal") * PITCH_ACCELERATION * pitchingTime;

                if (pitch < MIN_PITCH) pitch = MIN_PITCH;
                else if (pitch > MAX_PITCH) pitch = MAX_PITCH;

                if (pitch == MIN_PITCH || pitch == MAX_PITCH) isPitching = false;
            }

            else if (isLeveling) {
                if (pitch > 0) pitch -= LEVEL_ACCELERATION * pitchingTime;
                else pitch += LEVEL_ACCELERATION * pitchingTime;

                if (Mathf.Abs(pitch) < PITCH_DEAD_ZONE) {
                    SnapRotate();
                    isLevel = true;
                    isLeveling = false;
                    pitch = 0;
                }
            }
        }

        if (isPitching || isLeveling || isRotatingLeft || isRotatingRight) {
            // Blender model has X and Z swapped
            if (direction == dir.right) transform.eulerAngles = new Vector3(-pitch, transform.localEulerAngles.y, 0);
            else if (direction == dir.left) transform.eulerAngles = new Vector3(pitch, transform.localEulerAngles.y, 0);
            else transform.eulerAngles = new Vector3(0, transform.localEulerAngles.y, -pitch);
        }
    }

    // +90 is left, -90 is right
    private void Rotate() {
        rotationTime += Time.deltaTime;
        rotation = Time.deltaTime / rotationSpeed * 90;
        pitch -= Time.deltaTime / rotationSpeed * pitch;

        if (rotationTime >= rotationSpeed) {
            SnapRotate();
            isPitching = false;
        }
        else if (isRotatingLeft) transform.Rotate(Vector3.up, rotation);
        else if (isRotatingRight) transform.Rotate(Vector3.up, -rotation);
        Pitch();

        // Inputs that give a constant input, like triggers, can cause the chopper to turn twice before the user
        //   releases it. Wait a short time after the turn is complete before allowing another turn to prevent this
        if (rotationTime >= (rotationSpeed + MIN_TIME_BETWEEN_TURNS)) {
            isRotatingLeft = false;
            isRotatingRight = false;
        }
    }

    // Since Update() takes a variable amount of time, for the last frame we need to explicitly set the final angle
    private void SnapRotate() {
        if (direction == dir.left)         transform.eulerAngles = new Vector3(0, LEFT_ANGLE, 0);
        else if (direction == dir.forward) transform.eulerAngles = new Vector3(0, FORWARD_ANGLE, 0);
        else                               transform.eulerAngles = new Vector3(0, RIGHT_ANGLE, 0);
    }

    public void Crash() {
        timeCrashing += Time.deltaTime;

        if (!isCrashing) {          // First time through; do one-time tasks
            isCrashing = true;
            timeCrashing = 0.0f;

            // Stop tanks from firing
            foreach (GameObject turret in GameObject.FindGameObjectsWithTag("turret"))
                turret.GetComponent<ControlTurret>().enabled = false;

            gameObject.GetComponent<Rigidbody>().useGravity = true;
            gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;

            // Add random forces in all directions for an interesting crash
            gameObject.GetComponent<Rigidbody>().AddForce(
//                Vector3.down * Random.Range(-MAX_CRASH_FORCE, MAX_CRASH_FORCE) + 
                Vector3.left * Random.Range(-MAX_CRASH_FORCE, MAX_CRASH_FORCE) + 
                Vector3.forward * Random.Range(-MAX_CRASH_FORCE, MAX_CRASH_FORCE),
                ForceMode.VelocityChange);

            // FIXME: this doesn't work right; want a consistent tailspin
            // Induce spin
            if (direction == dir.forward)
                gameObject.GetComponent<Rigidbody>().AddForceAtPosition(
                    Vector3.right * 10f, transform.position + new Vector3(0.0f, 0.0f, 0.5f),
                    ForceMode.VelocityChange);
            else if (direction == dir.left)
                gameObject.GetComponent<Rigidbody>().AddForceAtPosition(
                    Vector3.forward * -10f, transform.position + new Vector3(0.0f, 0.0f, 0.5f),
                    ForceMode.VelocityChange);
            else
                gameObject.GetComponent<Rigidbody>().AddForceAtPosition(
                    Vector3.forward * -10f, transform.position + new Vector3(0.0f, 0.0f, 0.5f),
                    ForceMode.VelocityChange);
//            Vector3 forceDir = gameObject.GetComponent<Rigidbody>().transform.position - transform.position;
//            gameObject.GetComponent<Rigidbody>().AddForceAtPosition(forceDir.normalized, transform.position);
        }

        if (timeCrashing >= TIME_TO_CRASH) {
            // Don't do this if it's the last chopper crashing
            if (uiScript.numChoppersLeft > 1) {
                isCrashing = false;

                // Crash scene is done; set tanks back to active
                foreach (GameObject turret in GameObject.FindGameObjectsWithTag("turret"))
                    turret.GetComponent<ControlTurret>().enabled = true;
            }

            uiScript.chopperDestroyed();
        }
    }

    // Produces a random prisoner scream sound
    public void PrisonerScream() {
        int offset = Random.Range(SOUND_PRISONER_SCREAM_MIN, SOUND_PRISONER_SCREAM_MAX + 1);

        GameObject soundsClone = GameObject.Instantiate(sounds, transform.position, Quaternion.identity);
        soundsClone.GetComponents<AudioSource>()[offset].Play();
        Destroy(soundsClone, soundsClone.GetComponents<AudioSource>()[offset].clip.length);
    }

    void OnTriggerStay(Collider col) {
        // Required to prevent multiple executions of this per frame, despite what the docs say
        if (fixedUpdateOTS) {
            fixedUpdateOTS = false;

            if (isOnGround) {
                if (col.gameObject.tag == "prisoner" && !col.GetComponent<ManagePrisoner>().isRescued) {
                    // We collided with a prisoner
                    if (justLanded) {
                        // Landed on prisoner
                        Destroy(col.gameObject);
                        uiScript.prisonersKilled++;
                        uiScript.UpdateScore();
                        PrisonerScream();
                    }

                    // There are 2 colliders on the chopper: one on the whole body, and a partially overlapping one
                    //  on the cockpit. When facing forward, colliding with either of these is sufficient to climb
                    //  into the chopper; when facing left or right, the prisoner has to collide with both
                    else if (uiScript.prisonersOnboard < capacity &&
                             !isCrashing &&
                             (col.gameObject.GetComponent<ManagePrisoner>().numChopperColliders == 1 &&
                                direction == dir.forward ||
                              col.gameObject.GetComponent<ManagePrisoner>().numChopperColliders > 1 &&
                                direction != dir.forward))
                    {
                        // Pick up prisoner
                        GetComponents<AudioSource>()[SOUND_PRISONER_BOARD].Play();
                        // Climb into chopper
                        col.gameObject.GetComponent<ManagePrisoner>().anim.SetTrigger("EnterChopper");
                        // Need to disable collider since we're not destroying the object immediately
                        col.gameObject.GetComponent<BoxCollider>().enabled = false;
                        Destroy(col.gameObject, 0.5f);
                        uiScript.prisonersOnboard++;
                        uiScript.UpdateScore();
                    }
                }

                else if ((col.gameObject.tag == "landingPad") &&
                    uiScript.prisonersOnboard > 0) {
                    // On landing pad; unload prisoners
                    timeSinceLastUnload += Time.deltaTime;

                    // FIXME: prisoners unload slower if facing left/right vs. forward
                    if (timeSinceLastUnload >= prisonerUnloadFreq) {
                        GetComponents<AudioSource>()[SOUND_PRISONER_BOARD].Play();
                        GameObject.Instantiate(prisoner, transform.position + new Vector3(2, -1.65f, 0),
                            Quaternion.Euler(0, 180, 0));
                        uiScript.prisonersOnboard--;
                        uiScript.prisonersRescued++;
                        uiScript.UpdateScore();
                        timeSinceLastUnload = 0.0f;
                    }
                }
            }
        }
    }

    /* TODO: chopper should crash into terrain this way; avoids chopper clipping into terrain
        Crash works with the below, but no explosion effect or sound happens
    void OnCollisionEnter(Collision col) {
        if (!isCrashing && col.gameObject.tag == "terrain" && Mathf.Abs(hSpeed) >= SPEED_CRASH_FACTOR * MAX_HSPEED) {
            Crash();
            isOnGround = true;
            justLanded = true;
            Debug.Log("Crashing in OnCollisionEnter");
        }
    }
    */
}
