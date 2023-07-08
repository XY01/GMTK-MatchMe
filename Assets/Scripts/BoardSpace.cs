using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardSpace : MonoBehaviour
{
    public int xIndex;
    public int yIndex;
    public Tile occupyingTile;
    public bool IsOccupied => occupyingTile != null;

    public Color selectedCol = Color.yellow;
    private Color col;
    
    public void Init(float xPos, float yPos, int x, int y, float spaceSize)
    {
        name = $"Space {x} {y}";
        transform.position = new Vector3(xPos, yPos, 0);
        transform.localScale = new Vector3(spaceSize*.95f, spaceSize*.95f, .3f);
        xIndex = x;
        yIndex = y;
    }
    
    public void ClearTile(bool immediate = false)
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

    public void SetTile(Tile tile)
    {
        occupyingTile = tile;
    }

    public void PopulateWithTile(Tile tile, float tileSize)
    {
        occupyingTile = tile;
        tile.boardIndexX = xIndex;
        tile.boardIndexY = yIndex;
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
        if (Board.instance.SelectSpace(this))
        {
            SetColor(selectedCol);
            Debug.Log($"Selected {xIndex} {yIndex}");
        }
        else
        {
            
            Debug.Log($"Selected unsuccessful {xIndex} {yIndex}");
        }
    }
    
    public void Deslected()
    {
        SetColor(Color.white);
        Debug.Log($"Deselected {xIndex} {yIndex}");
    }
}