using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;


public class Board : MonoBehaviour
{
    public enum  BoardState
    {
        Static,
        RemovingTiles,
        Moving,
        Filling,
    }

    public BoardState state = BoardState.Moving;

    
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
  
    public float tileSize => boardWorldSize / (dimensions + dimensions * tilePadding);

    public Vector3 tileScale => new Vector3(tileSize, tileSize, .8f);
    // between center and next tile
    private float tileSpacing => tileSize * .5f + tilePadding * 2;
    private float spaceSize => boardWorldSize / dimensions;
    private  float minY => -(boardWorldSize * .5f) + spaceSize * .5f;

    private List<BoardSpace> selectedSpaces = new();
    private Tile.TileType selectedTileType => selectedSpaces[0].occupyingTile.type;
    private BoardSpace prevSelectedSpave => selectedSpaces[^1];
    public bool IsSelecting => selectedSpaces.Count != 0;

    public bool CanSelectTiles => state == BoardState.Static;
    
    [Range(0,1)]
    public float populateChance = 1;
    public Color[] cols;

    private float removeDuration = .1f;
    private float removeTimer = 0;
    
    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
        spaces = new BoardSpace[dimensions, dimensions];
        foreach (var space in boardSpaceList)
        {
            spaces[space.index.x, space.index.y] = space;
        }
        PopulateSpaces(true);
    }

    // Update is called once per frame
    void Update()
    {
        /////////////
        // Assess board state
        int inPlaceTiles = activeTiles.Count(x => x.state == Tile.TileState.InPlace);
        int movingTiles = activeTiles.Count - inPlaceTiles;
        int populatedSpaces = boardSpaceList.Count(x => x.IsOccupied);
        if (state == BoardState.Static)
        {
            if (movingTiles > 0 || populatedSpaces != boardSpaceList.Count)
                state = BoardState.Moving;
        }
        else if(state == BoardState.RemovingTiles)
        {
            removeTimer -= Time.deltaTime;
            if (removeTimer < 0)
            {
                removeTimer += removeDuration;
                BoardSpace space = selectedSpaces[0];
                ClearTile(space.occupyingTile);
                selectedSpaces.Remove(space);

                if (selectedSpaces.Count == 0)
                {
                    state = BoardState.Moving;
                    ClearSelection();
                }
            }
        }
        else if (state == BoardState.Moving)
        {
            //////////////////
            // Update tile
            foreach (var tile in activeTiles)
            {
                tile.UpdateTileMovement(tileSpacing, minY);
            }
            
            if(inPlaceTiles == activeTiles.Count)
                state = BoardState.Filling;
        }
        else if(state == BoardState.Filling)
        {
            foreach (var space in boardSpaceList)
            {
                space.ClearOccupiedTileReference();
                Tile tile = FindOverlappingTile(space.index);
                if(tile!=null)
                    space.SetTile(FindOverlappingTile(space.index));
            }
            
            populatedSpaces = boardSpaceList.Count(x => x.IsOccupied);
            //Debug.Log($"populatedSpaces  {populatedSpaces}");
            if (populatedSpaces == boardSpaceList.Count)
                state = BoardState.Static;
            else
            {
                PopulateSpaces(false);
            }
        }
    }

    public bool SpaceIsSelectable(BoardSpace occupiedSpace)
    {
        //Debug.Log($"Trying to select {occupiedSpace.index} {occupiedSpace.occupyingTile.col}");
        bool selectSpaceSuccess = false;
        
        if (selectedSpaces.Count == 0)
            selectSpaceSuccess = true;
        else if (occupiedSpace == prevSelectedSpave)
            return true;
        else if (selectedTileType != occupiedSpace.occupyingTile.type)
        {
            Debug.Log($"Not same type as prev {prevSelectedSpave.occupyingTile.type}");
            selectSpaceSuccess = false;
        }
        else if (prevSelectedSpave.IsAdjascentToSpace(occupiedSpace))
            selectSpaceSuccess = true;
       
    
        if (selectSpaceSuccess)
        {
            selectedSpaces.Add(occupiedSpace);
        }

        return selectSpaceSuccess;
    }

    public void ClearSelection()
    {
        // clear selection
        foreach (var boardSpace in boardSpaceList)
            boardSpace.SetSelected(false);
            
        selectedSpaces.Clear();
    }

    public BoardSpace GetSpaceAtIndex(int x, int y)
    {
        return spaces[x, y];
    }

    public void DestroyAllTiles()
    {
        foreach (BoardSpace space in boardSpaceList)
        {
            space.DestroyTile(true);
        }
    }

    public BoardSpace FindBoardSpaceAtPos(Vector3 pos)
    {
        boardSpaceList = boardSpaceList.OrderBy(x => Vector3.Distance(pos, x.transform.position)).ToList();
        return boardSpaceList[0];
    }
    
    public Tile FindOverlappingTile(int2 index)
    {
        return activeTiles.FirstOrDefault(x => x.boardSpaceIndex.x == index.x && x.boardSpaceIndex.y == index.y);
    }

    public int2 WorldPosXYIndex(Vector3 worldPos)
    {
        int2 index;
        index.x = Mathf.FloorToInt((worldPos.x + (boardWorldSize * .5f))/dimensions);
        index.y = Mathf.FloorToInt((worldPos.y + (boardWorldSize * .5f))/dimensions);
        return index;
    }
  
    [ContextMenu("Populate")]
    public void PopulateSpaces(bool clearAllTiles)
    {
        if (clearAllTiles)
        {
            DestroyAllTiles();
            activeTiles.Clear();
        }

        foreach (var space in boardSpaceList)
        {
            if(space.IsOccupied)
                continue;
           
            if(activeTiles.Count >= Board.instance.boardSpaceList.Count)
                return;
            
            Debug.Log("Populating " +space.index);
           
            /////////////////
            /// Spawn new tile
            Tile newTile = Instantiate(tilePrefab, tilesParent);
            newTile.name = "tile " + activeTiles.Count;
            activeTiles.Add(newTile);
            newTile.SetType((Tile.TileType)Random.Range(0, cols.Length - 1));
            space.SetTile(newTile);   
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
                    spaceSize);
                spaces[x, y] = newBoardSpace;
                boardSpaceList.Add(newBoardSpace);
            }
        }
    }

    public void AssessSelectionSuccess()
    {
        if (selectedSpaces.Count >= 3)
        {
            //selectedSpaces = selectedSpaces.OrderBy(x => x.index.y).ToList();
            state = BoardState.RemovingTiles;
            AppManager.instance.AddScore(selectedSpaces.Count);
        }
        else
        {
            ClearSelection();
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
