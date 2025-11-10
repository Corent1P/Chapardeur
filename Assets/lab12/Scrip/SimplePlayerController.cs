using Unity.Netcode;
using UnityEngine;

public class SimplePlayerController : NetworkBehaviour
{
    public float moveSpeed = 5f;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        if (!IsOwner) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 moveInput = new Vector3(h,0, v);

        transform.Translate(moveInput*moveSpeed*Time.deltaTime);
    }
}
