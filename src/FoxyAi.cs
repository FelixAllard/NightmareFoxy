using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using DunGen;
using GameNetcodeStuff;
using MonoMod.Utils;
using NightmareFoxyLC.Configurations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TextCore.Text;

namespace NightmareFoxyLC;

public class FoxyAi : EnemyAI
{
    //TODO flashlight slows faster
    //TODO Long staring for hard mod 
    //TODO Instead of creating a configuration for flashlight usage, why not leave it as it is currently by default but make the flashlight accelerate the monster's deceleration?
    //Another idea would be to stop the monster when a Flashbang or Stun grenade explodes in front of him ?
    
    public AudioSource howlingAudioSRC;
    
    public ParticleSystem footSpeed;
    public Light eyesLight;
    public AudioClip portalSFX;
    
    public AudioClip deactivate;
    public AudioClip activate;
    public BoxCollider foxyCollider;
    [Header("Engine Stuff")]
    public AudioSource engine;
    public AudioClip idle1;
    public AudioClip idle2;
    [Header("Whenseen")]
    public AudioClip fallOnKnee;
    public AudioClip fallOnBody;
    public AudioClip fallOnKneeVariant;
    
    
    public AudioClip destroyDoor;
    public AudioClip train;
    [Header("Footstep")]
    public AudioSource footStepAudio;
    public AudioClip footStep1;
    public AudioClip footStep2;
    public AudioClip footStep3;
    public AudioClip footStep4;

    [Header("Kill Animation")] 
    public AudioClip[] killAnimationSound;

    public AudioClip[] howlAudioSounds;
    
    
    
    public Transform killZone;
    public Material foxEyes;

    
    
    
    
    private int generatedNumber;
    private bool justSwitchedBehaviour;
    private float timer;
    private bool startedHowling = false;
    
    private float duration = 3f; // Total duration of speed reduction in seconds
    private float currentDuration = 0f; // Current duration of the reduction process
    private float oldspeed;
    private PlayerControllerB localPlayer;
    public List<DoorLock> doorLocked;
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
        duration = Config.Instance.TIME_TO_SLOW_DOWN.Value;
        foxEyes.SetFloat("_Strenght",0);
        agent.angularSpeed = 10000f;
        justSwitchedBehaviour = false;
        timer = 0;
        foxyCollider.size = new Vector3(4.531483f, 21.24057f, 5.783448f);
        agent.updateRotation = true;
        localPlayer = RoundManager.Instance.playersManager.localPlayerController;
        howlingAudioSRC.volume = Config.Instance.HOWLING_STRENGHT.Value;
    }

    /*public void LateUpdate()
    {
        transform.rotation = Quaternion.LookRotation(agent.velocity.normalized);

    }*/

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
                generatedNumber = RandomNumberGenerator.GetInt32(1, Config.Instance.CHANCE_NEXT_PHASE.Value);
            }
            else
            {
                generatedNumber = 100;
            }

        }

        switch (currentBehaviourStateIndex)
        {
            case (int)State.Down:
                footSpeed.Stop();
                engine.Stop();
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

                        engine.PlayOneShot(portalSFX);
                    }
                }

                break;
            case (int)State.Standing:

                footSpeed.Stop();
                foxyCollider.size = new Vector3(foxyCollider.size.x, foxyCollider.size.y, 6.619311f);
                foxyCollider.center = new Vector3(foxyCollider.center.x, foxyCollider.center.y, 0.8878318f);
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
                if (!engine.isPlaying)
                {
                    engine.clip = idle1;
                    engine.Play(3);
                    engine.loop = true;
                }

                creatureAnimator.speed = 1;
                agent.isStopped = true;
                agent.ResetPath();
                if (generatedNumber <= 2 && startedHowling != true)
                {
                    engine.clip = idle2;
                    //Animator do the running
                    creatureAnimator.speed = 0;
                    agent.speed = 0;
                    engine.Play();
                    startedHowling = true;
                    if (IsHost)
                    {
                        StartCoroutine(CloseHunt(RandomNumberGenerator.GetInt32(
                                Config.Instance.MIN_AMOUNT_HOWL.Value,
                                Config.Instance.MAX_AMOUNT_HOWL.Value
                            )
                        ));
                    }

                }

                break;
            case (int)State.Running:
                foxyCollider.size = new Vector3(foxyCollider.size.x, foxyCollider.size.y, 30);
                foxyCollider.center = new Vector3(foxyCollider.center.x, foxyCollider.center.y, 12.56404f);

                if (targetPlayer == null || targetPlayer.isPlayerDead || !targetPlayer.isInsideFactory) {
                    if (IsHost)
                    {
                        FetchTarget();
                    }
                    if (targetPlayer == null)
                    {
                        SwitchToBehaviourClientRpc(4);
                        StartCoroutine(EyesManager(false));
                        break;
                    }
                }
                if (!targetPlayer.isInsideFactory)
                {
                    SwitchToBehaviourClientRpc(0);
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
                if (agent.speed <=Config.Instance.MAX_SPEED.Value)
                {
                    agent.speed += 0.1f*Config.Instance.SPEED_MULTIPLIER.Value;
                    creatureAnimator.speed += 0.02f*Config.Instance.SPEED_MULTIPLIER.Value;
                }
                else
                {
                    creatureVoice.PlayOneShot(train);
                }
                BreakDoorServerRpc();
                if (!footSpeed.isPlaying)
                {
                    footSpeed.Play();
                }
                
                currentDuration = 0f;
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
                    
                    transform.rotation = Quaternion.LookRotation(agent.velocity.normalized);
                    if (CheckIfFlashedAt())
                    {
                        //If FlashedAt
                        currentDuration += Time.deltaTime * Config.Instance.FLASHLIGHT_SLOW_DOWN_MODIFIER.Value;
                        if (agent.speed >= 0.3)
                        {
                            agent.speed -= 0.1f *Config.Instance.FLASHLIGHT_SLOW_DOWN_MODIFIER.Value;
                        }
                    }
                    else
                    {
                        if (targetPlayer != null)
                        {
                            SetDestinationToPosition(targetPlayer.transform.position);
                            if (Config.Instance.ACTIVATE_CONSTANT_LOOK.Value)
                            {
                                if(!((targetPlayer.HasLineOfSightToPosition(transform.position)
                                      || !targetPlayer.isInsideFactory
                                      || !targetPlayer.isPlayerControlled
                                      || targetPlayer.isPlayerDead
                                       )))
                                {
                                    SwitchToBehaviourWithoutEnterStateClientRpc((int)State.Running);
                                    StartCoroutine(EyesManager(true));
                                    break;
                                }
                                else
                                {
                                    currentDuration += Time.deltaTime;
                                }
                            }
                            else
                            {
                                currentDuration += Time.deltaTime;
                            }
                        }
                        else
                        {
                            currentDuration += Time.deltaTime; 
                        }
                    }
                }
                else
                {
                    // If the duration is exceeded, set both speeds to 0
                    agent.speed = 0.0000f;
                    creatureAnimator.speed = 0f;
                }

                if (agent.speed <= 0.1f)
                {
                    transform.rotation = Quaternion.LookRotation(agent.velocity.normalized);
                    agent.ResetPath();
                    agent.speed = 0f;
                    creatureAnimator.speed = 0.8F;
                    DoAnimationClientRpc("GotSeen");
                    StartCoroutine(EyesManager(false));
                    SwitchToBehaviourClientRpc(5);
                    footSpeed.Stop();
                    if (IsHost)
                    {
                        StartCoroutine(WaitAndGoBackUp(RandomNumberGenerator.GetInt32(10, 30)));
                    }
                    
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
        UnlockAllDoorClientRpc();
    }

    IEnumerator WaitAndGoBackUp(int x)
    {
        movingTowardsTargetPlayer = false;
        agent.isStopped = true;
        agent.ResetPath();
        agent.speed = 0;
        yield return new WaitForSeconds(x);
        DoAnimationClientRpc("FallDown");
        SwitchToBehaviourClientRpc(0);
    }

    IEnumerator EyesManager(bool opening)
    {
        float objective = opening ?  2.0f : 0.0f;
        float currentState = opening ?  0.0f: 2.0f;
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
            foxEyes.SetFloat("_Strenght",Mathf.Lerp(currentState,objective,elapsedTime/10));
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
        PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
        if (playerControllerB == null || !playerControllerB.isPlayerControlled)
        {
            return;
        }
        if (playerControllerB == targetPlayer)
        {
            if ((int)State.Running == currentBehaviourStateIndex)
            {
                Debug.Log("Agent has reached the destination!");
                SwitchToBehaviourClientRpc(5);
                agent.speed = 0;
                StartCoroutine(FoxyKills(targetPlayer));
            }
            if ((int)State.Seen == currentBehaviourStateIndex)
            {
                if (agent.speed >= Config.Instance.MAX_SPEED.Value / (100/ Config.Instance.SPEED_FOXY_KILLS.Value))
                {
                    Debug.Log("Agent has reached the destination. Current Speed = " + agent.speed );
                    SwitchToBehaviourClientRpc(5);
                    agent.speed = 0;
                    StartCoroutine(FoxyKills(targetPlayer));
                }
                else if(agent.speed >= Config.Instance.MAX_SPEED.Value/ (100/ Config.Instance.SPEED_FOXY_DAMAGES.Value))
                {
                    Debug.Log("Agent has reached the destination. Current speed to low = " + agent.speed);
                    targetPlayer.DamagePlayer(Config.Instance.FOXY_DAMAGES.Value);
                    agent.speed = 0;
                }
            }
        }
        else
        {
            if ((int)State.Running == currentBehaviourStateIndex)
            {
                if (agent.speed > 5)
                {
                    playerControllerB.DamagePlayer(2);
                }
            }
            else if((int)State.Seen == currentBehaviourStateIndex)
            {
                if (agent.speed > 2)
                {
                    playerControllerB.DamagePlayer(1);
                }
            }
            else
            {
                
            }
        }
        
    }

    public void FetchTarget()
    {
        List<String> chosableTarget = new List<string>();
        foreach (var player in RoundManager.Instance.playersManager.allPlayerScripts)
        {
            if (player.isInsideFactory)
            {
                NavMeshPath path = new NavMeshPath();


                if (NavMesh.CalculatePath(agent.transform.position, player.transform.position, NavMesh.AllAreas, path))
                {
                    chosableTarget.Add(player.name);
                }
            }
        }

        if (chosableTarget.Count > 0)
        {
            SetTargetPlayerClientRpc(chosableTarget[RandomNumberGenerator.GetInt32(0,chosableTarget.Count)]);
            agent.speed = 0;
            creatureAnimator.speed = 0;
        }
        else
        {
            targetPlayer = null;
            SetTargetPlayerClientRpc("NoneCode1234567890");
        }
        
    }

    public void FootStepHandler()
    {
        if (localPlayer.isInsideFactory)
        {
            switch (RandomNumberGenerator.GetInt32(0,4))
            {
                case 0 :
                    footStepAudio.PlayOneShot(footStep1);
                    break;
                case 1:
                    footStepAudio.PlayOneShot(footStep2);
                    break;
                case 2:
                    footStepAudio.PlayOneShot(footStep3);
                    break;
                case 3:
                    footStepAudio.PlayOneShot(footStep4);
                    break;
            }
        }
    }

    /*
     *<summary>
     *  This function must be called bu the animation when the knee hit the ground
     * </summary>
     * 
     */
    public void FallOnKnee()
    {
        if (RandomNumberGenerator.GetInt32(0, 101)!=0)
        {
            creatureVoice.PlayOneShot(fallOnKnee);
        }
        else
        {
            creatureVoice.PlayOneShot(fallOnKneeVariant);
        }
    }
    public void FallOnBody()
    {
        creatureVoice.PlayOneShot(fallOnBody);
    }

    IEnumerator FoxyKills(PlayerControllerB player)
    {
        //Switch to jumping
        SwitchToBehaviourClientRpc(5);
        agent.speed = 0;
        creatureAnimator.speed = 1.0f;
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
        engine.Stop();
        footSpeed.Stop();
    }

    private bool stopLooking;
    IEnumerator RotatePlayerToMe(PlayerControllerB PCB)
    {
        stopLooking = false;
        StartCoroutine(StopAfterThreeSeconds(PCB));
        if (PCB)
        {
            Vector3 Position = transform.position - PCB.gameObject.transform.position;
            while (PCB.health != 0 && !PCB.isPlayerDead && !stopLooking)
            {
                PlayerSmoothLookAt(Position,PCB);
                yield return null;
            }
        }
    }

    IEnumerator StopAfterThreeSeconds(PlayerControllerB PCB)
    {
        yield return new WaitForSeconds(3f);
        stopLooking = true;
        yield return new WaitForSeconds(1f);
        stopLooking = false;
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
        SwitchToBehaviourClientRpc(1);
        targetPlayer.KillPlayer(Vector3.back,true,CauseOfDeath.Bludgeoning,2);
        targetPlayer = null;
    }
    [ClientRpc]
    public void DoAnimationClientRpc(String x)
    {
        creatureAnimator.SetTrigger(x);
    }

    public void PlayKillSound(int x)
    {
        creatureVoice.PlayOneShot(killAnimationSound[x]);
    }
    [ClientRpc]
    public void SetTargetPlayerClientRpc(String name)
    {
        if (name == "NoneCode1234567890")
        {
            targetPlayer = null;
        }
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
        int rndnbr = 200;
        //int rndnbr = RandomNumberGenerator.GetInt32(0, 201);
        /*if (rndnbr == 0)
        {
            howlingAudioSRC.PlayOneShot(howlAudioSounds[2]);
        }
        else if(rndnbr <=120)
        {
            howlingAudioSRC.PlayOneShot(howlAudioSounds[1]);         
        }
        else
        {
            howlingAudioSRC.PlayOneShot(howlAudioSounds[0]);         
        }*/
        howlingAudioSRC.Play();
        
    }

    [ClientRpc]
    public void UnlockAllDoorClientRpc()
    {
        foreach (DoorLock Door in FindObjectsOfType(typeof(DoorLock)) as DoorLock[])
        {
            if (Door.isLocked)
            {
                doorLocked.Add(Door);
                Door.UnlockDoorClientRpc();
                Door.isLocked = false;
            }
        }
    }
    [ServerRpc]
    public void BreakDoorServerRpc()
    {
        
        foreach (DoorLock Door in FindObjectsOfType(typeof(DoorLock)) as DoorLock[])
        {
            try
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
            catch (NullReferenceException e)
            {
                try
                {
                    Door.OpenDoorAsEnemyClientRpc();
                }
                catch (Exception y)
                {
                    Debug.Log("The doors are not formatted the right way and as such foxy may seems really stupid hitting doors " + y);
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
            newAS.volume = 1;
            StartCoroutine(TurnOffC(rig, .12f));
            rig.AddForce(Position, ForceMode.Impulse);
            newAS.PlayOneShot(destroyDoor);
        }
    }

    private bool CheckIfFlashedAt()
    {
        foreach (var player in RoundManager.Instance.playersManager.allPlayerScripts)
        {
            if (!player.HasLineOfSightToPosition(transform.position)) continue;
            foreach (var item in player.ItemSlots)
            {
                if (item != null)
                    if (item.gameObject.GetComponent<FlashlightItem>() != null)
                        if (item.gameObject.GetComponent<FlashlightItem>().isBeingUsed)
                            if (Vector3.Distance(player.transform.position, transform.position) < 6f)
                            {
                                Debug.Log("FlashedAt");
                                return true;
                            }
                                
            }
        }
        return false;
    }
    IEnumerator TurnOffC(Rigidbody rigidbody,float time)
    {
        rigidbody.detectCollisions = false;
        yield return new WaitForSeconds(time);
        rigidbody.detectCollisions = true;
        Destroy(rigidbody.gameObject, 5);
    }
    [ClientRpc]
    public void SwitchToBehaviourWithoutEnterStateClientRpc(int x)
    {
        this.previousBehaviourStateIndex = this.currentBehaviourStateIndex;
        this.currentBehaviourStateIndex = x;
        this.currentBehaviourState = this.enemyBehaviourStates[x];
    }
}