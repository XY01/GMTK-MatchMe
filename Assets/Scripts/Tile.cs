using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class Tile : MonoBehaviour
{
    public enum TileState
    {
        TransitionIn,
        InPlace,
        Falling,
        TransitionOut,
    }
    
    public enum TileType
    {
        Type0,
        Type1,
        Type2,
        Type3,
    }
    
    #region VARIABLES
    public TileState state = TileState.TransitionIn;
    public TileType type = TileType.Type0;
    public int2 boardSpaceIndex;
    
    //-- Rendering
    private MaterialPropertyBlock matPropBlock;
    private MeshRenderer renderer;
    public float scaleDuration = 0.3f;
    #endregion
    
    // ----------- INITIALIZATION
    //
    void Start()
    {
        state = TileState.TransitionIn;
        StartCoroutine(ScaleOverTime(Board.instance.tileScale, scaleDuration));
    }
    IEnumerator ScaleOverTime(Vector3 target, float duration)
    {
        Vector3 originalScale = transform.localScale;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration; // normalized time
            transform.localScale = Vector3.Lerp(originalScale, target, t);
            yield return null;
        }

        // Ensure the transformation is exact
        transform.localScale = target;
        state = TileState.InPlace;
    }
    
    
    // ----------- BEHAVIOR
    //
    public void UpdateTileMovement(float spacing, float minY)
    {
        if(state == TileState.TransitionIn || state == TileState.TransitionOut)
            return;
        
        boardSpaceIndex = Board.instance.WorldPosXYIndex(transform.position);
        
        if (transform.position.y <= minY)
            return;
        
        RaycastHit hit;
        Ray ray = new Ray(transform.position, Vector3.down);
        
        // ray cast down, if it hits a tile wihtin range, set state to in place
        if (Physics.Raycast(ray, out hit, spacing * 2,AppManager.instance.tileLayerMask))
        {
            // If close to correct spacing then lock in place
            if (Mathf.Abs(hit.distance - spacing) < .1f)
            {
                // replace using position got lookup index and board space
                
                // BoardSpace boardSpace = Board.instance.FindBoardSpaceAtPos(transform.position);
                // transform.position = new Vector3(boardSpace.transform.position.x, boardSpace.transform.position.y, transform.position.z);
                // state = TileState.InPlace;
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
            transform.position += Vector3.down * 6 * Time.deltaTime;
        

        if (transform.position.y < minY)
        {
            transform.position = new Vector3(transform.position.x, minY, transform.position.z);
            state = TileState.InPlace;
        }
    }
    public void SetType(TileType type)
    {
        if (matPropBlock == null)
        {
            matPropBlock = new MaterialPropertyBlock();
            renderer = GetComponent<MeshRenderer>();
        }
        
        this.type = type;
        switch (type)
        {  
            case TileType.Type0:
                matPropBlock.SetColor("_Col", Color.red);
                break;
            case TileType.Type1:
                matPropBlock.SetColor("_Col", Color.blue);
                break;
        }
        
        renderer.SetPropertyBlock(matPropBlock);
    }
}
