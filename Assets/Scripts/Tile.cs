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
    public int2 BoardSpaceIndex => Board.instance.GetIndexAtPos(transform.position);
    
    //-- Rendering
    private MaterialPropertyBlock matPropBlock;
    private MeshRenderer renderer;
    #endregion
    
    // ----------- INITIALIZATION
    //
    public void Init(float timingOffset = 0)
    {
        state = TileState.TransitionIn;
        StartCoroutine(ScaleOverTime(Board.instance.tileScale, AppManager.instance.tileScaleDuration, timingOffset));
    }
    IEnumerator ScaleOverTime(Vector3 target, float duration, float timingOffset)
    {
        yield return new WaitForSeconds(0);
        
        Vector3 originalScale = transform.localScale;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration; // normalized time
            transform.localScale = Vector3.Lerp(originalScale, target, t*t);
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

        if (transform.position.y <= minY)
        {
            state = TileState.InPlace;
            return;
        }

        RaycastHit hit;
        Ray ray = new Ray(transform.position, Vector3.down);
        
        state = TileState.Falling;
        // ray cast down, if it hits a tile within range, set state to in place
        if (Physics.Raycast(ray, out hit, spacing * 1.4f,AppManager.instance.tileLayerMask))
        {
            if (hit.collider.TryGetComponent(out Tile tileBelow))
            {
                if (tileBelow.state == TileState.InPlace)
                {
                    BoardSpace space = Board.instance.FindBoardSpaceAtPos(transform.position);
                    if (Vector3.Distance(space.transform.position, transform.position) < .15f)
                    {
                        state = TileState.InPlace;
                        transform.position = space.transform.position;
                    }
                }
            }
        }

        if (state == TileState.Falling)
        {
            transform.position += Vector3.down * (AppManager.instance.tileMoveSpeed * Time.deltaTime);
            if (transform.position.y < minY)
            {
                transform.position = new Vector3(transform.position.x, minY, transform.position.z);
                state = TileState.InPlace;
            }
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
