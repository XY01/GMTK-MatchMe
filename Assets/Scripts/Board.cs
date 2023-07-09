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
    
    public static Board instance;
    public BoardState state = BoardState.Moving;

    // BOARD
    //
    public int dimensions = 9;
    public float boardWorldSize = 10;
    public float tilePadding = .2f;
    // - Properties
    private float boardSpaceSize => boardWorldSize / dimensions;
    private  float minY => -(boardWorldSize * .5f) + boardSpaceSize * .5f;
    
    // SPACES
    //
    [Header("Spaces")]
    public BoardSpace boardSpacePrefab;
    public Dictionary<int2, BoardSpace> boardSpacesDict = new();
    public Transform spacesParent;
    
    // TILES
    //
    [Header("Tiles")]
    public Tile tilePrefab;
    public List<Tile> activeTiles = new List<Tile>();
    public Transform tilesParent;
    // - Properties
    public float tileSize => boardWorldSize / (dimensions + dimensions * tilePadding);
    public Vector3 tileScale => new Vector3(tileSize, tileSize, .8f);
    // between center and next tile
    private float tileSpacing => tileSize * .5f + tilePadding * 2;
    
    // SELECTION
    //
    private List<BoardSpace> selectedSpaces = new();
    private Tile.TileType selectedTileType => selectedSpaces[0].occupyingTile.type;
    private BoardSpace prevSelectedSpave => selectedSpaces[^1];
    public bool IsSelecting => selectedSpaces.Count != 0;
    public bool CanSelectTiles => Board.instance.state == Board.BoardState.Static;
 
    
   
    public Color[] cols;
    private float removeDuration = .1f;
    private float removeTimer = 0;
    
   
    void Awake()
    {
        instance = this;
       
        boardSpacesDict = new Dictionary<int2, BoardSpace>();
        BoardSpace[] boardSpaced = FindObjectsOfType<BoardSpace>();
        foreach (var space in boardSpaced)
            boardSpacesDict.Add(space.index, space);
        
        PopulateSpaces(true);
    }
    void Update()
    {
        /////////////
        // Assess board state
        int inPlaceTiles = activeTiles.Count(x => x.state == Tile.TileState.InPlace);
        int movingTiles = activeTiles.Count - inPlaceTiles;
        int populatedSpaces = boardSpacesDict.Count(x => x.Value.IsOccupied);
        if (state == BoardState.Static)
        {
            if (movingTiles > 0 || populatedSpaces != boardSpacesDict.Count)
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
            foreach (var space in boardSpacesDict)
            {
                space.Value.ClearOccupiedTileReference();
                Tile tile = FindOverlappingTile(space.Key);
                if(tile!=null)
                    space.Value.SetTile(FindOverlappingTile(space.Key));
            }
            
            populatedSpaces = boardSpacesDict.Count(x => x.Value.IsOccupied);
            //Debug.Log($"populatedSpaces  {populatedSpaces}");
            if (populatedSpaces == boardSpacesDict.Count)
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
        foreach (var space in boardSpacesDict)
            space.Value.SetSelected(false);
            
        selectedSpaces.Clear();
    }

    public BoardSpace GetSpaceAtIndex(int x, int y) => boardSpacesDict[new int2(x, y)];
  

    public void DestroyAllTiles()
    {
        foreach (var space in boardSpacesDict)
        {
            space.Value.DestroyTile(true);
        }
    }
    

    // public BoardSpace FindBoardSpaceAtPos(Vector3 pos)
    // {
    //     boardSpaceList = boardSpacesDict.OrderBy(x => Vector3.Distance(pos, x.Value.transform.position)).ToList();
    //     return boardSpaceList[0];
    // }
    
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

        foreach (var space in boardSpacesDict)
        {
            if(space.Value.IsOccupied)
                continue;
           
            if(activeTiles.Count >= boardSpacesDict.Count)
                return;
            
            Debug.Log("Populating " + space.Value.index);
           
            /////////////////
            /// Spawn new tile
            Tile newTile = Instantiate(tilePrefab, tilesParent);
            newTile.name = "tile " + activeTiles.Count;
            activeTiles.Add(newTile);
            newTile.SetType((Tile.TileType)Random.Range(0, cols.Length - 1));
            space.Value.SetTile(newTile);   
        }
    }

  
    [ContextMenu("Generate")]
    void GenerateSpaces()
    {
        if (boardSpacesDict.Count > 0)
        {
            // Find keys of all elements that meet the condition
            var keysToRemove = boardSpacesDict.Where(pair => pair.Value == null).Select(pair => pair.Key).ToList();
            foreach (var key in keysToRemove)
                boardSpacesDict.Remove(key);
            
            
            foreach (var space in boardSpacesDict)
                DestroyImmediate(space.Value.gameObject);
        }
        boardSpacesDict.Clear();
        
        float min = boardWorldSize * -.5f;
        boardSpacesDict = new Dictionary<int2, BoardSpace>();
        for (int x = 0; x < dimensions; x++)
        {
            for (int y = 0; y < dimensions; y++)
            {
                BoardSpace newBoardSpace = Instantiate(boardSpacePrefab, spacesParent);
                newBoardSpace.Init( 
                    min + (boardSpaceSize * x) + (boardSpaceSize * .5f),
                    min + (boardSpaceSize * y) + (boardSpaceSize * .5f),
                    boardSpaceSize);
                
                boardSpacesDict.Add(new int2(x,y), newBoardSpace);
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
        
        if(boardSpacesDict.Count == 0 )
            return;
        
        // for (int x = 0; x < dimensions; x++)
        // {
        //     for (int y = 0; y < dimensions; y++)
        //     {
        //         Gizmos.DrawWireCube(spaces[x,y].transform.position, Vector3.one * boardSpaceSize);
        //     }
        // }
    }
}
