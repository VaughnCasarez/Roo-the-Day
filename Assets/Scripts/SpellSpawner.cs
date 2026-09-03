using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellSpawner : MonoBehaviour
{
    public List<GameObject> objects;

    public void Spawn(string objectName)
    {
        foreach (var item in objects)
        {
            item.SetActive(objectName == item.name);
        }
    }
}
