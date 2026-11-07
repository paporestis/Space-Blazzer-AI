using System.Collections.Generic;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [SerializeField] private Transform minPos;
    [SerializeField] private Transform maxPos;

    [SerializeField] private int waveNumber;
    [SerializeField] private List<Wave> waves;

    [System.Serializable]
    public class Wave
    {
        public GameObject prefab;
        public float spawnTimer;
        public float spawnInterval;
        public int objectsPerWave;
        public int spawnedObjectsCount;
    }

    void Update()
    {
        waves[waveNumber].spawnTimer += Time.deltaTime * PlayerMovement.instance.boost;
        if (waves[waveNumber].spawnTimer >= waves[waveNumber].spawnInterval)
        {
            waves[waveNumber].spawnTimer = 0f;
            SpawnObject();
        }
        if (waves[waveNumber].spawnedObjectsCount >= waves[waveNumber].objectsPerWave)
        {
            waves[waveNumber].spawnedObjectsCount = 0;
            waveNumber++;
            if (waveNumber >= waves.Count)
            {
                waveNumber = 0;
            }
        }
    }

    private void SpawnObject()
    {
        Instantiate(waves[waveNumber].prefab, RandomSpawnPoint(), transform.rotation);
        waves[waveNumber].spawnedObjectsCount++;
    }

    private Vector2 RandomSpawnPoint()
    {
        Vector2 spawnPoint;

        spawnPoint.y = minPos.position.y;
        spawnPoint.x = Random.Range(minPos.position.x, maxPos.position.x);

        return spawnPoint;
    }
}
