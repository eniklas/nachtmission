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

public class ManageBullet : MonoBehaviour {
    public GameObject explosion;                    // Small explosion used when bullet hits ground
    public GameObject bigExplosion;                 // Big explosion used when a major object is hit
    public GameObject smoke;
    public GameObject fire;
    public GameObject sounds;
    private GameObject smokeClone;
    private GameObject fireClone;
    private GameObject soundsClone;
    public GameObject PrisonDamaged;                // Prefab of prison that has been hit
    private GameObject chopper;
    private ManageUI uiScript;
    private ParticleSystem.MainModule psMain;       // Used to modify smoke
    private const int SOUND_GROUND_EXPLOSION = 0;   // Offset in the Sounds prefab
    private const int SOUND_TANK_EXPLOSION = 1;
    private const int SOUND_JET_EXPLOSION = 5;
    private const int SOUND_DRONE_EXPLOSION = 6;
    private const int SOUND_CHOPPER_HIT = 7;

    void Start() {
        chopper = GameObject.Find("Chopper");
        uiScript = GameObject.Find("Canvas").GetComponent<ManageUI>();
    }

    // Plays a particle effect and destroys it when done
    void PlayEffect(GameObject effect, Vector3 position) {
        GameObject effectClone = GameObject.Instantiate(effect, position, Quaternion.identity);
        Destroy(effectClone, effectClone.GetComponent<ParticleSystem>().main.duration);
    }

    // Plays a sound from the Sounds prefab; we can't easily put the sounds on
    //  the bullet or target object, since they're immediately destroyed.
    void PlaySound(int offset) {
        GameObject soundsClone = GameObject.Instantiate(sounds, transform.position, Quaternion.identity);
        soundsClone.GetComponents<AudioSource>()[offset].Play();
        Destroy(soundsClone, soundsClone.GetComponents<AudioSource>()[offset].clip.length);
    }

	void FixedUpdate () {
        // Missiles accelerate downwards once launched
        if (tag == "missile" && transform.parent == null)
            GetComponent<Rigidbody>().AddForce(Vector3.down * 20, ForceMode.Acceleration);
    }

    void OnCollisionEnter(Collision col) {
        if (col.gameObject.tag == "tank") {
            PlayEffect(bigExplosion, col.gameObject.transform.position + new Vector3(0, 2, 0));
            PlaySound(SOUND_TANK_EXPLOSION);
            Destroy(col.gameObject);
        }

        else if (col.gameObject.tag == "jet") {
            PlayEffect(bigExplosion, col.gameObject.transform.position);
            PlaySound(SOUND_JET_EXPLOSION);
            Destroy(col.gameObject);
        }

        else if (col.gameObject.tag == "drone") {
            PlayEffect(bigExplosion, col.gameObject.transform.position);
            PlaySound(SOUND_DRONE_EXPLOSION);
            Destroy(col.gameObject);
        }

        else if (col.gameObject.tag == "chopper") {
            PlayEffect(bigExplosion, col.gameObject.transform.position);
            PlaySound(SOUND_CHOPPER_HIT);
            col.gameObject.GetComponent<ManageChopper>().Crash();
        }

        else if (col.gameObject.tag == "prison") {
            // Prison explosion sound is part of the PrisonDamaged prefab, and played on awake
            PlayEffect(bigExplosion, col.gameObject.transform.position +
                       new Vector3(0, 0, -11));

            // Replace prison with damaged model
            // TODO: can you just rotate the prefab?
            // Have to use Quaternion.Euler below because PrisonDamaged model was rotated when imported
            GameObject.Instantiate(PrisonDamaged, col.gameObject.transform.position, Quaternion.Euler(-90, 180, 0));

            GameObject.Instantiate(fire, col.gameObject.transform.position + new Vector3(0, 0, -9.0f),
                Quaternion.identity);

            smokeClone = GameObject.Instantiate(smoke, col.gameObject.transform.position + new Vector3(0, 0, -9.5f),
                Quaternion.identity);
            psMain = smokeClone.GetComponent<ParticleSystem>().main;
            psMain.startSizeMultiplier = 16; // Thicken smoke

            Destroy(col.gameObject);
        }

        // If we hit a non-targetable object like the ground, just explode where it hit
        else {
            PlayEffect(explosion, transform.position);
            PlaySound(SOUND_GROUND_EXPLOSION);
        }

        Destroy(gameObject);
    }

    // Prisoner prefab has a trigger collider
    void OnTriggerEnter(Collider col) {
        if (col.gameObject.tag == "prisoner" && !col.gameObject.GetComponent<ManagePrisoner>().isRescued) {
            chopper.GetComponent<ManageChopper>().PrisonerScream();
            PlayEffect(explosion, col.gameObject.transform.position);
            Destroy(col.gameObject);
            Destroy(gameObject);
            uiScript.prisonersKilled++;
            uiScript.UpdateScore();
        }

    }
}
