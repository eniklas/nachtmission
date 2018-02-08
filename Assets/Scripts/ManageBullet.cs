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

// TODO: add camera shake (see perlin noise)
public class ManageBullet : MonoBehaviour {
    public GameObject explosion;                // Small explosion used when bullet hits ground/non-targetable object
    public GameObject bigExplosion;             // Big explosion used when a major object is hit
    public GameObject smoke;
    public GameObject fire;
    public GameObject sounds;
    private GameObject smokeClone;
    private GameObject fireClone;
    private GameObject soundsClone;
    public GameObject PrisonDamaged;            // Prefab of prison that has been hit
    private GameObject chopper;
    private ManageUI uiScript;
    private ParticleSystem.MainModule psMain;   // Used to modify smoke
    private const int SOUND_GROUND_EXPLOSION = 0;   // Offset in the Sounds prefab
    private const int SOUND_TANK_EXPLOSION = 1;
    private const int SOUND_JET_EXPLOSION = 5;
    private const int SOUND_DRONE_EXPLOSION = 6;
    private const int SOUND_CHOPPER_HIT = 7;

    void Start() {
        chopper = GameObject.Find("Chopper");
        uiScript = GameObject.Find("Canvas").GetComponent<ManageUI>();
    }

    // Plays a sound from the Sounds prefab; we can't easily put the sounds on
    //  the bullet or target object, since they're immediately destroyed.
    void PlaySound(int offset) {
        GameObject soundsClone = GameObject.Instantiate(sounds, transform.position, Quaternion.identity);
        soundsClone.GetComponents<AudioSource>()[offset].Play();
        Destroy(soundsClone, soundsClone.GetComponents<AudioSource>()[offset].clip.length);
    }

    void OnCollisionEnter(Collision col) {
        if (col.gameObject.tag == "tank" && gameObject.tag != "source:tank") {
            PlaySound(SOUND_TANK_EXPLOSION);

            GameObject expClone = GameObject.Instantiate(bigExplosion, col.gameObject.transform.position + new Vector3(0, 2, 0), Quaternion.identity);
            Destroy(expClone, 3);   // Explosion lasts 3 secs
            Destroy(col.gameObject);
        }

        else if (col.gameObject.tag == "jet") {
            if (gameObject.tag != "missile") {  // Don't allow missile to collide with jet
                PlaySound(SOUND_JET_EXPLOSION);

                GameObject expClone =
                    GameObject.Instantiate(bigExplosion, col.gameObject.transform.position, Quaternion.identity);
                Destroy(expClone, 3);
                Destroy(col.gameObject);
            }
        }

        else if (col.gameObject.tag == "drone") {
            if (gameObject.tag != "source:drone") {
                PlaySound(SOUND_DRONE_EXPLOSION);

                GameObject expClone =
                    GameObject.Instantiate(bigExplosion, col.gameObject.transform.position, Quaternion.identity);
                Destroy(expClone, 3);
                Destroy(col.gameObject);
            }
        }

        else if (col.gameObject.tag == "chopper" && gameObject.tag != "source:chopper") {
            PlaySound(SOUND_CHOPPER_HIT);

            GameObject expClone =
                GameObject.Instantiate(bigExplosion, col.gameObject.transform.position, Quaternion.identity);
            Destroy(expClone, 3);
            col.gameObject.GetComponent<ControlChopper>().Crash();
        }

        else if (col.gameObject.tag == "prison") {
            // Prison explosion sound is part of the PrisonDamaged prefab, and played on awake
            GameObject expClone = GameObject.Instantiate(bigExplosion, col.gameObject.transform.position +
                new Vector3(0, 0, -11), Quaternion.identity);
            Destroy(expClone, 3);

            // Replace prison with damaged model
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
            PlaySound(SOUND_GROUND_EXPLOSION);

            GameObject soundsClone = GameObject.Instantiate(sounds, transform.position, Quaternion.identity);
            soundsClone.GetComponents<AudioSource>()[SOUND_GROUND_EXPLOSION].Play();
            Destroy(soundsClone, 5);

            GameObject expClone = GameObject.Instantiate(explosion, transform.position, Quaternion.identity);
            // FIXME: this causes the bullet to flicker on when explosion is destroyed
            Destroy(expClone, 2);
        }

        // Destroy bullet/missile, but don't allow missile to collide with jet
        if (!(col.gameObject.tag == "jet" && gameObject.tag == "missile")) Destroy(gameObject);
    }

    // Prisoner prefab has a trigger collider
    void OnTriggerEnter(Collider col) {
        // Can't shoot a prisoner when he's running into base; it would be difficult to do that anyway since he
        //  immediately leaves Z=0, but possible, in which case the counts would get screwed up. If you want to
        //  allow shooting prisoners as they run into base, you need a new state when they're doing so (not
        //  onboard and not rescued)
        if (col.gameObject.tag == "prisoner" && !col.gameObject.GetComponent<ManagePrisoner>().isRescued) {
            // We could play the prisoner scream directly with PlaySound(), but the chopper needs to
            //  do it for landing on prisoners, so might as well use the same method
            chopper.GetComponent<ControlChopper>().PrisonerScream();

            GameObject expClone = GameObject.Instantiate(explosion,
                col.gameObject.transform.position + new Vector3(0, 1, 0), Quaternion.identity);
            Destroy(col.gameObject);
            Destroy(expClone, 2);

            uiScript.prisonersKilled++;
            uiScript.UpdateScore();
        }

    }
}
