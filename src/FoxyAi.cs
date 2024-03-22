using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TextCore.Text;

namespace ExampleEnemy;

public class FoxyAi : EnemyAI
{
    public AudioSource footStepAudio;
    public AudioSource howlingAudioSRC;
    public AudioSource engine;
    public ParticleSystem footSpeed;
    public Light eyesLight;
    public AudioClip portalSFX;
    public AudioClip idle1;
    public AudioClip idle2;
    public AudioClip deactivate;
    public AudioClip activate;
    public BoxCollider foxyCollider;
    
    
    public Material foxEyes;

    public Transform killZone;
    
    
    
    private int generatedNumber;
    private bool justSwitchedBehaviour;
    private float timer;
    private bool startedHowling = false;
    
    private float duration = 3f; // Total duration of speed reduction in seconds
    private float currentDuration = 0f; // Current duration of the reduction process
    private float oldspeed;
    enum State {
        Down,
        Standing,
        ChargePose,
        Running,
        Seen,
        Jumping
    }

    public override void Start()
    {
        base.Start();
        foxEyes.SetFloat("_Strenght",0);
        agent.angularSpeed = 10000f;
        justSwitchedBehaviour = false;
        timer = 0;
        foxyCollider.size = new Vector3(4.531483f, 21.24057f, 5.783448f);
    }

    public override void Update()
    {
        base.Update();
        if (justSwitchedBehaviour)
        {
            timer +=Time.deltaTime;
            if (timer >= 5)
            {
                timer = 0;
                justSwitchedBehaviour = false;
                
            }
        }
    }
    
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (IsHost)
        {
            if (!justSwitchedBehaviour)
            {
                generatedNumber= RandomNumberGenerator.GetInt32(1, 100);
            }
            else
            {
                generatedNumber = 100;
            }
            
        }
        switch (currentBehaviourStateIndex)
        {
            case (int)State.Down:
                movingTowardsTargetPlayer = false;
                agent.isStopped = true;
                agent.ResetPath();
                targetPlayer = null;
                creatureAnimator.speed = 1;
                footSpeed.Stop();
                if (IsHost)
                {
                    if (generatedNumber <= 1)
                    {
                        SwitchToBehaviourClientRpc(1);
                    }
                }
                break;
            case (int)State.Standing:
                foxyCollider.size = new Vector3(4.531483f, 21.24057f, 5.783448f);
                creatureAnimator.speed = 1;
                agent.isStopped = true;
                agent.ResetPath();
                if (generatedNumber <= 1)
                {
                    SwitchToBehaviourClientRpc(2);
                    StartCoroutine(EyesManager(true));

                }
                break;
            case (int)State.ChargePose:
                foxyCollider.size = new Vector3(4.531483f, 21.24057f, 5.783448f);
                creatureAnimator.speed = 1;
                agent.isStopped = true;
                agent.ResetPath();
                if (generatedNumber <= 3 && startedHowling != true)
                {
                    
                    //Animator do the running
                    creatureAnimator.speed = 0;
                    agent.speed = 0;
                    if (IsHost)
                    {
                        startedHowling = true;
                        StartCoroutine(CloseHunt(RandomNumberGenerator.GetInt32(3, 7)));
                    }
                }
                break;
            case (int)State.Running:
                foxyCollider.size = new Vector3(4.531483f, 21.24057f, 29.73968f);
                if (targetPlayer == null || targetPlayer.isPlayerDead)
                {
                    FetchTarget();
                }
                if (
                    targetPlayer.HasLineOfSightToPosition(transform.position) 
                    || !targetPlayer.isInsideFactory 
                    || !targetPlayer.isPlayerControlled 
                    || targetPlayer.isPlayerDead
                )
                {
                    //Seen
                    SwitchToBehaviourClientRpc(4);
                    StartCoroutine(EyesManager(false));
                    break;
                }
                movingTowardsTargetPlayer = true;
                SetDestinationToPosition(targetPlayer.transform.position);
                agent.speed += 0.1f;
                creatureAnimator.speed += 0.02f;
                BreakDoorServerRpc();
                if (!footSpeed.isPlaying)
                {
                    footSpeed.Play();
                }
                break;
            case (int)State.Seen:
                if (currentDuration < duration)
                {
                    // Calculate the interpolation parameter based on the current duration
                    float t = currentDuration / duration;
                    // Calculate the new speed values using Mathf.Lerp
                    float newAgentSpeed = Mathf.Lerp(agent.speed, 0f, t);
                    float newAnimatorSpeed = Mathf.Lerp(creatureAnimator.speed, 0f, t);
                    // Apply the new speed values
                    agent.speed = newAgentSpeed;
                    creatureAnimator.speed = newAnimatorSpeed;
                    // Increment the current duration
                    currentDuration += Time.deltaTime;
                }
                else
                {
                    // If the duration is exceeded, set both speeds to 0
                    agent.speed = 0f;
                    creatureAnimator.speed = 0f;
                }

                if (agent.speed <= 0.5f)
                {
                    creatureAnimator.speed = 0.8F;
                    DoAnimationClientRpc("GotSeen");
                    StartCoroutine(EyesManager(false));
                    SwitchToBehaviourClientRpc(5);
                    StartCoroutine(WaitAndGoBackUp(RandomNumberGenerator.GetInt32(10, 30)));
                }
                
                break;
            case (int)State.Jumping:
                
                
                break;
            default:
                if (IsHost)
                {
                    SwitchToBehaviourClientRpc(1);
                }

                break;
        }
    }

    IEnumerator CloseHunt(int howling)
    {
        
        int howlingStore = howling;
        yield return new WaitForSeconds(4);
        while (howlingStore>0)
        {
            HowlClientRpc();
            yield return new WaitForSeconds(3);
            howlingStore -= 1;
        }

        startedHowling = false;
        creatureAnimator.speed = 0;
        agent.speed = 0;
        SwitchToBehaviourClientRpc(3);
    }

    IEnumerator WaitAndGoBackUp(int x)
    {
        yield return new WaitForSeconds(x);
        SwitchToBehaviourClientRpc(0);
    }

    IEnumerator EyesManager(bool opening)
    {
        float objective = opening ?  1.0f : 0.0f;
        float currentState = opening ?  0.0f: 1.0f;
        if (opening)
        {
            engine.PlayOneShot(activate);

        }
        else
        {
            engine.PlayOneShot(deactivate);
        }
        
        bool fixing = true;
        float elapsedTime = 0;
        while (fixing)
        {
            foxEyes.SetFloat("_Strenght",Mathf.Lerp(currentState,objective,elapsedTime/5));
            elapsedTime += Time.deltaTime;
            if (foxEyes.GetFloat("_Strenght") == objective)
            {
                fixing = false;
                yield break;
            }
        }
    }

    public override void OnCollideWithPlayer(Collider other)
    {
        Debug.Log("Yeah, collider works!");
        PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
        //TODO detect if it is targetPlayer or other player!
        if ((int)State.Running == currentBehaviourStateIndex)
        {
            Debug.Log("Agent has reached the destination!");
            SwitchToBehaviourClientRpc(5);
            StartCoroutine(FoxyKills(targetPlayer));
            playerControllerB.DamagePlayer(1);
        }
    }

    public void FetchTarget()
    {
        List<String> chosableTarget = new List<string>();
        foreach (var player in RoundManager.Instance.playersManager.allPlayerScripts)
        {
            if (player.isInsideFactory)
            {
                chosableTarget.Add(player.name);
            }
        }
        SetTargetPlayerClientRpc(chosableTarget[RandomNumberGenerator.GetInt32(0,chosableTarget.Count)]);
        agent.speed = 0;
        creatureAnimator.speed = 0;
    }

    public void FootStepHandler()
    {
        footStepAudio.Play();
    }

    IEnumerator FoxyKills(PlayerControllerB player)
    {
        //Switch to jumping
        SwitchToBehaviourClientRpc(5);
        agent.speed = 0;
        creatureAnimator.speed = 2.0f;
        movingTowardsTargetPlayer = false;
        agent.isStopped = true;
        agent.ResetPath();
        oldspeed =  player.movementSpeed;
        player.movementSpeed = 0;
        StartCoroutine(RotatePlayerToMe(player));
        transform.LookAt(player.transform.position, Vector3.up);
        DoAnimationClientRpc("Kill");
        yield return new WaitForSeconds(5);
        player.movementSpeed = oldspeed;
    }
    IEnumerator RotatePlayerToMe(PlayerControllerB PCB)
    {
        if (PCB)
        {
            Vector3 Position = transform.position - PCB.gameObject.transform.position;
            while (PCB.health != 0)
            {
                PlayerSmoothLookAt(Position,PCB);
                yield return null;
            }
        }
    }
    void PlayerSmoothLookAt(Vector3 newDirection, PlayerControllerB PCB)
    {
        PCB.gameObject.transform.rotation = Quaternion.Lerp(PCB.gameObject.transform.rotation, Quaternion.LookRotation(newDirection), Time.deltaTime * 5);
    }
    
    [ClientRpc]
    public void SwingAttackHitClientRpc() {
        if (currentBehaviourStateIndex == (int)State.Running)
        {
            int playerLayer = 1 << 3; // This can be found from the game's Asset Ripper output in Unity
            Collider[] hitColliders = Physics.OverlapBox(killZone.position, killZone.localScale, Quaternion.identity, playerLayer);
            if (hitColliders.Length > 0)
            {
                foreach (var player in hitColliders)
                {
                    Debug.Log("Player In punishable zone!");
                    PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(player);
                    if (playerControllerB != null)
                    {
                        if (playerControllerB == targetPlayer)
                        {
                            agent.speed = 0;
                            agent.ResetPath();
                            creatureAnimator.speed = 1.5f;
                            SwitchToBehaviourClientRpc(5);
                            StartCoroutine(FoxyKills(targetPlayer));
                        }
                    }
                }
            }
        }
    }
    
    [ClientRpc]
    public void KillTargetPlayerClientRpc()
    {
        targetPlayer.KillPlayer(Vector3.back,true,CauseOfDeath.Bludgeoning,2);
    }
    
    
    [ClientRpc]
    public void DoAnimationClientRpc(String x)
    {
        creatureAnimator.SetTrigger(x);
    }

    [ClientRpc]
    public void SetTargetPlayerClientRpc(String name)
    {
        foreach (var player in RoundManager.Instance.playersManager.allPlayerScripts)
        {
            if (player.name == name)
            {
                targetPlayer = player;
            }
        }
    }

    
    
    [ClientRpc]
    public void HowlClientRpc()
    {
        howlingAudioSRC.Play();
    }
    [ClientRpc]
    public void PlaySoundClientRpc(String x)
    {
        
    }
    [ServerRpc]
    public void BreakDoorServerRpc()
    {
        foreach (DoorLock Door in FindObjectsOfType(typeof(DoorLock)) as DoorLock[])
        {
            var ThisDoor = Door.transform.parent.transform.parent.transform.parent.gameObject;
            if (!ThisDoor.GetComponent<Rigidbody>())
            {
                if (Vector3.Distance(transform.position, ThisDoor.transform.position) <= 4f)
                {
                    BashDoorClientRpc(ThisDoor, (targetPlayer.transform.position - transform.position).normalized * 20);
                }
            }
        }
    }
    [ClientRpc]
    public void BashDoorClientRpc(NetworkObjectReference netObjRef, Vector3 Position)
    {
        if (netObjRef.TryGet(out NetworkObject netObj))
        {
            var ThisDoor = netObj.gameObject;
            var rig = ThisDoor.AddComponent<Rigidbody>();
            var newAS = ThisDoor.AddComponent<AudioSource>();
            newAS.spatialBlend = 1;
            newAS.maxDistance = 60;
            newAS.rolloffMode = AudioRolloffMode.Linear;
            newAS.volume = 3;
            StartCoroutine(TurnOffC(rig, .12f));
            rig.AddForce(Position, ForceMode.Impulse);
            //newAS.PlayOneShot(audioClips[3]);
        }
    }
    
    
    IEnumerator TurnOffC(Rigidbody rigidbody,float time)
    {
        rigidbody.detectCollisions = false;
        yield return new WaitForSeconds(time);
        rigidbody.detectCollisions = true;
        Destroy(rigidbody.gameObject, 5);
    }

}