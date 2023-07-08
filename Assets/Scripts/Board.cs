using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;


public class Board : MonoBehaviour
{
    public static Board instance;
    
    public int dimensions = 9;
    public float boardWorldSize = 10;
    [FormerlySerializedAs("padding")] public float tilePadding = .2f;
    
    // Spaces
    [Header("Spaces")]
    public BoardSpace boardSpacePrefab;
    public BoardSpace[,] spaces;
    public List<BoardSpace> boardSpaceList = new List<BoardSpace>();
    public Transform spacesParent;
    
    
    // Tiles
    [Header("Tiles")]
    public Tile tilePrefab;
    public List<Tile> activeTiles = new List<Tile>();
    public Transform tilesParent;
  
    private float tileSize => boardWorldSize / (dimensions + dimensions * tilePadding);
    // between center and next tile
    private float tileSpacing => tileSize * .5f + tilePadding * 2;
    private float spaceSize => boardWorldSize / dimensions;
    private  float minY => -(boardWorldSize * .5f) + spaceSize * .5f;

    private List<BoardSpace> selectedSpaces = new();
    private Color selectedCol;
    
    [Range(0,1)]
    public float populateChance = 1;
    public Color[] cols;
    
    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
        PopulateTiles();
    }


    public bool SelectSpace(BoardSpace space)
    {
        bool selectSpaceSuccess = false;
        if (selectedSpaces.Count == 0)
        {
            selectedSpaces.Add(space);
            selectedCol = space.occupyingTile.col;
            selectSpaceSuccess = true;
        }
        else
        {
            BoardSpace prevSpace = selectedSpaces[^1];
            // Col doesnt match
            if (prevSpace.occupyingTile.col == selectedCol)
            {
                if (Mathf.Abs(prevSpace.yIndex - space.yIndex) == 1
                    || Mathf.Abs(prevSpace.xIndex - space.xIndex) == 1)
                {
                    selectSpaceSuccess = true;
                }
            }
        }
    
        if (!selectSpaceSuccess)
        {
            // clear selection
            foreach (var boardSpace in selectedSpaces)
            {
                boardSpace.Deslected();
            }
            selectedCol = Color.black;
            selectedSpaces.Clear();
        }

        return selectSpaceSuccess;
    }
    
    // Update is called once per frame
    void Update()
    {
        foreach (var tile in activeTiles)
        {
            tile.ManualUpdate(tileSpacing, minY);
        }
    }

    private void OnValidate()
    {
        //GenerateSpaces();
    }

    public void ClearBoardImmediate()
    {
        foreach (BoardSpace space in boardSpaceList)
        {
            space.ClearTile(true);
        }
    }

    public BoardSpace FindBoardSpaceAtPos(Vector3 pos)
    {
        boardSpaceList = boardSpaceList.OrderBy(x => Vector3.Distance(pos, x.transform.position)).ToList();
        return boardSpaceList[0];
    }
    
  
    [ContextMenu("Populate")]
    public void PopulateTiles()
    {
        ClearBoardImmediate();
        activeTiles.Clear();
        
        foreach (var space in boardSpaceList)
        {
            Debug.Log("here");
            if(space.IsOccupied)
                continue;
            
            if(Random.value > populateChance)
                continue;
            
            Debug.Log("here 1");
            Tile newTile = Instantiate(tilePrefab, tilesParent);
            newTile.name = "tile " + activeTiles.Count;
            activeTiles.Add(newTile);
            newTile.SetColor(cols[Random.Range(0, cols.Length - 1)]);
            space.PopulateWithTile(newTile, tileSize);   
        }
    }

  
    [ContextMenu("Generate")]
    void GenerateSpaces()
    {
        if (boardSpaceList.Count > 0)
        {
            boardSpaceList.RemoveAll(item => item == null);
            
            foreach (BoardSpace space in boardSpaceList)
            {
                DestroyImmediate(space.gameObject);
            }
        }
        boardSpaceList.Clear();
        
        float min = boardWorldSize * -.5f;
        spaces = new BoardSpace[dimensions, dimensions];
        for (int x = 0; x < dimensions; x++)
        {
            for (int y = 0; y < dimensions; y++)
            {
                BoardSpace newBoardSpace = Instantiate(boardSpacePrefab, spacesParent);
                newBoardSpace.Init(
                    min + (spaceSize * x) + (spaceSize * .5f),
                    min + (spaceSize * y) + (spaceSize * .5f),
                    x,
                    y,
                    spaceSize);
                spaces[x, y] = newBoardSpace;
                boardSpaceList.Add(newBoardSpace);
            }
        }
    }


    public void ClearTile(Tile tile)
    {
        activeTiles.Remove(tile);
        Destroy(tile.gameObject);
    }
    
    private void OnDrawGizmos()
    {
        return;
        
        if(boardSpaceList.Count == 0 )
            return;
        
        for (int x = 0; x < dimensions; x++)
        {
            for (int y = 0; y < dimensions; y++)
            {
                Gizmos.DrawWireCube(spaces[x,y].transform.position, Vector3.one * spaceSize);
            }
        }
    }
}
