﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MeshColliderImporter : AssetPostprocessor
{
    public void ProcessMesh(GameObject obj)
    {
        // Make object static if the name starts with SM ("Static Mesh").
        if (obj.name.StartsWith("SM"))
            obj.isStatic = true;

        if (obj.name.EndsWith("_Collider"))
        {
            // Add mesh collider to the imported object.
            obj.AddComponent<MeshCollider>();
            MeshCollider newCollider = obj.GetComponent<MeshCollider>();

            // Set mesh collider mesh to the object's mesh.
            newCollider.sharedMesh = obj.GetComponent<MeshFilter>().sharedMesh;
            newCollider.convex = true; // Ensure convex collision detection.

            // Disable rendering, as this mesh shouldn't be visible.
            obj.GetComponent<MeshRenderer>().enabled = false;

            Debug.Log("Mesh: " + obj.name + " detected with '_Collider' suffix. Adding mesh collider...");
        }
    }

    void OnPostprocessModel(GameObject obj)
    {
        // Check meshes for each child.
        for(int i = 0; i < obj.transform.childCount; ++i)
        {
            Transform childTransform = obj.transform.GetChild(i);
            GameObject child = childTransform.gameObject;

            ProcessMesh(child);
        }
    }
}
