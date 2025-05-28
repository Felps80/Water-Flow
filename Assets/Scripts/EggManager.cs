using System.Collections.Generic;
using UnityEngine;

public class EggManager : MonoBehaviour
{
    public static EggManager instance;

    // Reference to your egg prefab (assign via Inspector)
    public GameObject eggPrefab;
    // List of spawn points (assign your empty GameObjects here)
    public List<Transform> spawnPoints;

    // (Optional) Track eggs currently in the scene.
    private List<GameObject> eggsInScene = new List<GameObject>();

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        RespawnEggs();
    }

    public void RespawnEggs()
    {
        // Clean up any remaining egg objects.
        foreach (GameObject egg in eggsInScene)
        {
            if (egg != null)
                Destroy(egg);
        }
        eggsInScene.Clear();

        // Instantiate new eggs at each spawn point.
        if (spawnPoints != null && eggPrefab != null)
        {
            foreach (Transform sp in spawnPoints)
            {
                GameObject newEgg = Instantiate(eggPrefab, sp.position, sp.rotation);
                eggsInScene.Add(newEgg);
            }
        }
        else
        {
            Debug.LogWarning("EggManager: SpawnPoints and/or EggPrefab are not assigned.");
        }
    }
}
