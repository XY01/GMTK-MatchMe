using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class BoardSpace : MonoBehaviour
{
    // PROPERTIES
    public bool IsOccupied => occupyingTile != null;

    #region VARIABLES
    // VARS
    public int2 index;
    public Tile occupyingTile;
    public List<BoardSpace> adjascentSpaces = new();
    //- Rendering
    private MaterialPropertyBlock matPropBlock;
    private MeshRenderer renderer;
    private Color col;
    #endregion
   

    // ----------- INITIALIZATION
    //
    private void Start()
    {
        FindAdjascentSpaces();
    }
    private void FindAdjascentSpaces()
    {
        adjascentSpaces.Clear();
        
        if (index.x == 0)
            adjascentSpaces.Add(Board.instance.GetSpaceAtIndex(1, index.y));
        else if (index.x == Board.instance.dimensions-1)
            adjascentSpaces.Add(Board.instance.GetSpaceAtIndex(index.x-1, index.y));
        else
        {
            adjascentSpaces.Add(Board.instance.GetSpaceAtIndex(index.x-1, index.y));
            adjascentSpaces.Add(Board.instance.GetSpaceAtIndex(index.x+1, index.y));
        }
        
        
        if (index.y == 0)
            adjascentSpaces.Add(Board.instance.GetSpaceAtIndex(index.x, 1));
        else if (index.y == Board.instance.dimensions-1)
            adjascentSpaces.Add(Board.instance.GetSpaceAtIndex(index.x, index.y-1));
        else
        {
            adjascentSpaces.Add(Board.instance.GetSpaceAtIndex(index.x, index.y-1));
            adjascentSpaces.Add(Board.instance.GetSpaceAtIndex(index.x, index.y+1));
        }
    }
    public void Init(float xPos, float yPos, float spaceSize)
    {
        transform.position = new Vector3(xPos, yPos, 0);
        transform.localScale = new Vector3(spaceSize*.95f, spaceSize*.95f, .3f);
        Board board = FindObjectOfType<Board>();
        this.index = board.GetIndexAtPos(transform.position);
        name = $"Space {index.x} {index.y}";
    }

    
    // ----------- SELECTION & TILES
    //
    public void SetSelected(bool selected)
    {
        if (matPropBlock == null)
            matPropBlock = new MaterialPropertyBlock();
        
        renderer = GetComponent<MeshRenderer>();
        matPropBlock.SetColor("_Col", selected ? Color.yellow : Color.white);
        renderer.SetPropertyBlock(matPropBlock);
    }
    public void SetTile(Tile tile)
    {
        occupyingTile = tile;
        tile.transform.position = transform.position;
    }
    public void DestroyTile(bool immediate = false)
    {
        if (IsOccupied)
        {
            if(immediate)
                DestroyImmediate(occupyingTile.gameObject);
            else
            {
                Destroy(occupyingTile.gameObject);
            }
        }
    }
    public void ClearOccupiedTileReference()
    {
        occupyingTile = null;
    }
    
    
    // ----------- HELPER METHODS
    //
    public bool IsAdjascentToSpace(BoardSpace space)
    {
        return adjascentSpaces.Contains(space);
    }
}