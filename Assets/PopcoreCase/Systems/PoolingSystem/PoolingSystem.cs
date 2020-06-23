using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
public class SourceObjects
{
    public string ID;
    
    public GameObject SourcePrefab;
    //If 0 will use the global object count
    public int MinNumberOfObject = 0;
    public bool AllowGrow = true;

    [ReadOnly]
    public List<GameObject> clones;
}

public class PoolingSystem : Singleton<PoolingSystem>
{
    public List<SourceObjects> SourceObjects = new List<SourceObjects>();

    private List<AudioSource> pooledAudioSources = new List<AudioSource>();


    public int DefaultCount = 10;

    private void Start()
    {
        InitilizePool();
    }

    public void InitilizePool()
    {
        InitilizeGameObjects();
        InitilizeAudioSources();
    }

    private void InitilizeGameObjects()
    {
        for (int i = 0; i < SourceObjects.Count; i++)
        {
            int copyNumber = DefaultCount;
            if (SourceObjects[i].MinNumberOfObject != 0)
                copyNumber = SourceObjects[i].MinNumberOfObject;

            for (int j = 0; j < copyNumber; j++)
            {
                GameObject go = Instantiate(SourceObjects[i].SourcePrefab, transform);
                go.SetActive(false);
                SourceObjects[i].clones.Add(go);
            }
        }
    }

    private void InitilizeAudioSources()
    {
        GameObject audioHolder = new GameObject();
        audioHolder.name = "AudioHolder";
        audioHolder.transform.SetParent(transform);
        audioHolder.transform.position = Vector3.zero;

        for (int i = 0; i < 20; i++)
        {
            GameObject go = new GameObject();
            go.name = "PooledSource";
            go.transform.position = Vector3.zero;
            go.transform.SetParent(audioHolder.transform);
            AudioSource audioSource = go.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            pooledAudioSources.Add(audioSource);
        }
    }

    public GameObject InstantiateAPS(string iD)
    {
        for (int i = 0; i < SourceObjects.Count; i++)
        {
            if(string.Equals(SourceObjects[i].ID, iD))
            {
                for (int j = 0; j < SourceObjects[i].clones.Count; j++)
                {
                    if (!SourceObjects[i].clones[j].activeInHierarchy)
                    {
                        SourceObjects[i].clones[j].SetActive(true);
                        return SourceObjects[i].clones[j];
                    }
                }

                if (SourceObjects[i].AllowGrow)
                {
                    GameObject go = Instantiate(SourceObjects[i].SourcePrefab, transform);
                    SourceObjects[i].clones.Add(go);
                    return go;
                }

            }
        }
        return null;
    }
    public GameObject InstantiateAPS(string iD, Transform parent)
    {
        GameObject go = InstantiateAPS(iD);
        if (go)
        {
            go.transform.SetParent(parent);
            return go;
        }
        else
            return null;
    }



    public GameObject InstantiateAPS(string iD, Vector3 position)
    {
        GameObject go = InstantiateAPS(iD);
        if(go)
        {
            go.transform.position = position;
            return go;
        }
        else
            return null;
    }

    public GameObject InstantiateAPS(string iD, Vector3 position, Transform parent)
    {
        GameObject go = InstantiateAPS(iD);
        if (go)
        {
            go.transform.position = position;
            go.transform.SetParent(parent);
            return go;
        }
        else
            return null;
    }

    public GameObject InstantiateAPS(string iD, Vector3 position, Quaternion rotation)
    {
        GameObject go = InstantiateAPS(iD);
        if (go)
        {
            go.transform.position = position;
            go.transform.rotation = rotation;
            return go;
        }
        else
            return null;
    }

    public AudioSource GetAudioSource()
    {
        for (int i = 0; i < pooledAudioSources.Count; i++)
        {
            if (!pooledAudioSources[i].isPlaying)
                return pooledAudioSources[i];
        }

        Transform audioHolder = transform.Find("AudioHolder");
        GameObject go = new GameObject();
        go.name = "PooledSource";
        go.transform.position = Vector3.zero;
        go.transform.SetParent(audioHolder);
        AudioSource audioSource = go.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        pooledAudioSources.Add(audioSource);
        return audioSource;
    }

    [Button]
    public void ClearClones()
    {
        for (int i = 0; i < SourceObjects.Count; i++)
        {
            for (int j = 0; j < SourceObjects[i].clones.Count; j++)
            {
                if(Application.isPlaying)
                    Destroy(SourceObjects[i].clones[j].gameObject);
                else
                    DestroyImmediate(SourceObjects[i].clones[j].gameObject);
            }
            SourceObjects[i].clones.Clear();
        }
    }


    public void DestroyAPS(GameObject clone)
    {
        clone.transform.SetParent(transform);
        clone.SetActive(false);
    }
}
