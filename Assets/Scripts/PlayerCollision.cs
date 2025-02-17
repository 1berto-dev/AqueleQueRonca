using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    private Inventory playerInventory;

    void Start()
    {
        playerInventory = GetComponent<Inventory>();
    }

    private void OnCollisionEnter(Collision other) 
    {
        if(other.gameObject.CompareTag("Key"))
        {
            playerInventory.HasKey = true;
            Destroy(other.gameObject);
        }

        if(other.gameObject.CompareTag("Door") && playerInventory.HasKey)
        {
            Destroy(other.gameObject);
        }
    }
}
