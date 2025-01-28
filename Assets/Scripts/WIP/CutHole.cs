using System.Collections.Generic;
using UnityEngine;
using MyCsg;

public class CutHole : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        List<Collider> collisions = new List<Collider>(Physics.OverlapBox(transform.position, transform.localScale, Quaternion.identity, 6));
            for(int j = 0; j < collisions.Count; j++ )
            {
                    
                var resultModel = CSG.Perform(CSG.BooleanOp.Subtraction, gameObject, collisions[j].gameObject);
        
                GameObject Quarto = new GameObject();
                Quarto.transform.position = transform.position;
                Quarto.AddComponent<MeshFilter>().sharedMesh = resultModel.mesh;
                Quarto.AddComponent<MeshRenderer>().sharedMaterials = resultModel.materials.ToArray();
                Quarto.transform.localScale = Vector3.one;

                    
            }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    }
