using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public enum TileState
    {
        InPlace,
        Falling,
    }

    public TileState state = TileState.InPlace;
    
    public Color col;
    public int boardIndexX;
    public int boardIndexY;
    private MeshRenderer renderer;

    private MaterialPropertyBlock matPropBlock;
    private LayerMask mask;
    private BoardSpace boardSpace;
    
    // Start is called before the first frame update
    void Awake()
    {
        mask  = LayerMask.GetMask("Tile");
    }

    // Update is called once per frame
    public void ManualUpdate(float spacing, float minY)
    {
        if (transform.position.y <= minY)
            return;
        
        RaycastHit hit;
        Ray ray = new Ray(transform.position, Vector3.down);
        
        // ray cast down, if it hits a tile wihtin range, set state to in place
        if (Physics.Raycast(ray, out hit, spacing * 2,mask))
        {
            // If close to correct spacing then lock in place
            if (Mathf.Abs(hit.distance - spacing) < .2f)
            {
                boardSpace = Board.instance.FindBoardSpaceAtPos(transform.position);
                boardSpace.SetTile(this);
                transform.position = new Vector3(boardSpace.transform.position.x, boardSpace.transform.position.y, transform.position.z);
                //Debug.Log("Found space");
            }
            else
            {
                state = TileState.Falling;
            }
        }
        else
        {
            state = TileState.Falling;
        }
        

        if (state == TileState.Falling)
            transform.position += Vector3.down * 3 * Time.deltaTime;
        

        if (transform.position.y < minY)
        {
            transform.position = new Vector3(transform.position.x, minY, transform.position.z);
        }
    }
   

    public void SetColor(Color col)
    {
        if (matPropBlock == null)
            matPropBlock = new MaterialPropertyBlock();
        
        renderer = GetComponent<MeshRenderer>();
        matPropBlock.SetColor("_Col", col);
        this.col = col;
        
        renderer.SetPropertyBlock(matPropBlock);
    }

}
