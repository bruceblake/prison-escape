using UnityEngine;
using System.Collections;
using Riptide;

public class Gun : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float fireRange = 100f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float fireRate = 0.5f; 
    
    [Header("Ammo")]
    public int bulletCapacity = 30; // Changed to Int
    public int bulletsLeft = 20;    // REMOVED 'static', Changed to Int

    public float reloadTime = 2f;
    private bool isReloading = false;

    private float nextTimeToFire = 0f;

    public RecoilController recoilScript; // Drag your Camera's script here
    
    [Header("Audio")]
    public AudioSource gunAudioSource;
    public AudioClip shootSound;
    public AudioClip emptyMagSound;

    public GameObject bulletHolePrefab;

    public enum FireMode
    {
        Single,
        Automatic
    }

    public FireMode fireMode = FireMode.Single;

    public string gunName; // Renamed from 'name' to avoid hiding Unity's Object.name
    public GameObject modelPrefab;

    public ParticleSystem muzzleFlash;
    public AudioClip reloadSound;      // Optional reload sound

    [Range(0f, 0.2f)]
    public float pitchRandomization = 0.05f; // Adds variety

    public void Shoot(ushort tick, Player player)
    {
       // 1. Check Fire Rate FIRST
        if (Time.time < nextTimeToFire || isReloading)
            return;

        // 2. Check Ammo SECOND
        if (bulletsLeft <= 0)
        {
            Debug.Log("Out of bullets!");
            if (gunAudioSource != null && emptyMagSound != null)
            {
                // Optional: Randomize pitch for dry fire too
                gunAudioSource.pitch = Random.Range(0.9f, 1.1f); 
                gunAudioSource.PlayOneShot(emptyMagSound);
            }

            // --- THIS IS THE MISSING LINE ---
            // We pretend we fired a shot so the gun has to "cycle" before clicking again
            nextTimeToFire = Time.time + (1f / fireRate); 
            // -------------------------------

            return; 
        }

        // 3. SUCCESS - Apply Costs and Cooldowns

        // 1. SUCCESS: Play the sound
        if (gunAudioSource != null && shootSound != null)
        {
            
            float minPitch = 0.95f; 
            float maxPitch = 1.05f;
            gunAudioSource.pitch = Random.Range(minPitch, maxPitch);
            gunAudioSource.PlayOneShot(shootSound);
        }
        Debug.Log($"Player {player.Id} fired {gunName} at tick {tick}");
        bulletsLeft--;
        nextTimeToFire = Time.time + (1f / fireRate); // Correct cooldown math
        if (muzzleFlash != null)
            muzzleFlash.Play();

        Transform firePoint = player.shootOrigin;
        if(recoilScript != null) recoilScript.ApplyRecoil();

        // 4. Visual Raycast (Local Only)
        // We act on the hit locally for effects, but the Server is the authority for damage usually
        if (Physics.Raycast(firePoint.position, firePoint.forward, out RaycastHit hit, fireRange))
        {
            Debug.DrawRay(firePoint.position, firePoint.forward * hit.distance, Color.red, 1f);
            Debug.Log($"Player {player.Id} hit {hit.collider.name} at distance {hit.distance}");
            if (hit.collider.CompareTag("Player"))
            {
                // We hit another player
                Debug.Log($"Player {player.Id} hit Player with ID: {hit.collider.GetComponent<Player>().Id}");
                StartCoroutine(PlayHitmarker());
            }
            else
            {
                //SpawnBulletHole(hit);
            }
        }
        else
        {
            Debug.DrawRay(firePoint.position, firePoint.forward * fireRange, Color.green, 1f);
        }

        // 5. Network Message
        SendShootMessage(tick, firePoint);
    }

     IEnumerator PlayHitmarker()
{
    // Check if references exist to prevent red errors if you disconnect/change scenes
    if (GameClient.Singleton != null && GameClient.Singleton.hitmarker != null)
    {
        GameClient.Singleton.hitmarker.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        
        // Check again (Singleton might have been destroyed during the wait)
        if (GameClient.Singleton != null && GameClient.Singleton.hitmarker != null)
            GameClient.Singleton.hitmarker.SetActive(false);
    }
}


    public void SpawnBulletHole(RaycastHit hit)
    {
        if (bulletHolePrefab == null) return;

        // 1. Calculate Rotation: Look away from the wall
        Quaternion lookRotation = Quaternion.LookRotation(hit.normal);

        // 2. Calculate Position: Move slightly away from wall to stop flickering
        Vector3 spawnPosition = hit.point + (hit.normal * 0.001f);

        // 3. Spawn It
        GameObject hole = Instantiate(bulletHolePrefab, spawnPosition, lookRotation);

        // 4. Random Rotation (Optional)
        // Spin the texture so every hole doesn't look identical
        //hole.transform.Rotate(Vector3.forward, Random.Range(0f, 360f));

        // 5. Parent it (CRITICAL)
        // If you shoot a moving door, the hole should stick to the door
        hole.transform.SetParent(hit.collider.transform);

        // 6. Clean up
        Destroy(hole, 20f); // Disappear after 20 seconds
    }

    private void SendShootMessage(ushort tick, Transform firePoint)
    {
        Message msg = Message.Create(MessageSendMode.Reliable, ClientToServerId.PlayerShoot);
        
        // Note: You don't need to send Client.Id. The server knows who sent the message automatically.
        msg.AddUShort(tick);
        msg.AddVector3(firePoint.position);
        msg.AddVector3(firePoint.forward);
        
        // It is better to have the server look up range/damage based on the gun ID, 
        // rather than trusting the client to send it.
        // But for now, we keep your structure:
        msg.AddFloat(fireRange);
        msg.AddFloat(damage); 

        NetworkManager.Singleton.Client.Send(msg);
    }

    public void Update()
    {
        // IMPORTANT: Only reload if this is OUR gun (Local Player check usually happens before calling Update)
        if(Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(Reload());
        }
    }

    IEnumerator Reload()
    {
        Debug.Log("Reloading...");
        isReloading = true;
        if (gunAudioSource != null && reloadSound != null)
        {
            gunAudioSource.pitch = 1f; // Reset pitch for reloading
            gunAudioSource.PlayOneShot(reloadSound);
        }
        yield return new WaitForSeconds(reloadTime);
        bulletsLeft = bulletCapacity;
        Debug.Log("Reloaded!");
        isReloading = false;
    }
}