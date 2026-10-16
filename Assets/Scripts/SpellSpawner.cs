using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellSpawner : MonoBehaviour
{
    public List<GameObject> objects;

    public void Spawn(string objectName)
    {
        Debug.Log("SpellSpawner received: " + objectName);

        foreach (var item in objects)
        {
            Debug.Log("Checking object: " + item.name);

            item.SetActive(objectName == item.name);
        }
    }
}