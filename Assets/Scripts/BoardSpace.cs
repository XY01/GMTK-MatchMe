using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class BoardSpace : MonoBehaviour
{
    public Tile occupyingTile;
    public bool IsOccupied => occupyingTile != null;

    public Color selectedCol = Color.yellow;
    private Color col;
    public int2 index;

    public List<BoardSpace> adjascentSpaces = new();

    private void Start()
    {
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

    public void Init( float xPos, float yPos, float spaceSize)
    {
        transform.position = new Vector3(xPos, yPos, 0);
        transform.localScale = new Vector3(spaceSize*.95f, spaceSize*.95f, .3f);
        Board board = FindObjectOfType<Board>();
        this.index = board.WorldPosXYIndex(transform.position);
        name = $"Space {index.x} {index.y}";
    }

    public bool IsAdjascentSpace(BoardSpace space)
    {
        return adjascentSpaces.Contains(space);
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

    public void ClearOccupied()
    {
        occupyingTile = null;
    }

    public void SetTile(Tile tile)
    {
        occupyingTile = tile;
    }

    public void PopulateWithTile(Tile tile, float tileSize)
    {
        occupyingTile = tile;
        tile.transform.position = transform.position;
        tile.transform.localScale = Vector3.one * tileSize;
    }

    private MaterialPropertyBlock matPropBlock;
    private MeshRenderer renderer;
    public void SetColor(Color col)
    {
        if (matPropBlock == null)
            matPropBlock = new MaterialPropertyBlock();
        
        renderer = GetComponent<MeshRenderer>();
        matPropBlock.SetColor("_Col", col);
        this.col = col;
        
        GetComponent<Renderer>().SetPropertyBlock(matPropBlock);
    }

    public void Selected()
    {
        if (Board.instance.SpaceIsSelectable(this))
        {
            SetColor(selectedCol);
            Debug.Log($"Selected {index}");
        }
        else
        {
            Debug.Log($"Selected unsuccessful {index}");
        }
    }
    
    public void Deslected()
    {
        SetColor(Color.white);
        //Debug.Log($"Deselected {index}");
    }
}