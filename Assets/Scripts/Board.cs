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
    public enum BoardState
    {
        Static,
        RemovingTiles,
        Moving,
        Filling,
    }

    public static Board instance;
    public BoardState state = BoardState.Static;

    // BOARD
    //
    public int dimensions = 9;
    public float boardWorldSize = 10;

    public float tilePadding = .2f;

    // - Properties
    private float BoardSpaceSize => boardWorldSize / dimensions;
    private float MinY => -(boardWorldSize * .5f) + BoardSpaceSize * .5f;

    // SPACES
    //
    [Header("Spaces")] public BoardSpace boardSpacePrefab;
    private Dictionary<int2, BoardSpace> _boardSpacesDict = new();
    public Transform spacesParent;

    // TILES
    //
    [Header("Tiles")] public Tile tilePrefab;
    public List<Tile> activeTiles = new List<Tile>();

    public Transform tilesParent;

    // - Properties
    public float tileSize => boardWorldSize / (dimensions + dimensions * tilePadding);

    public Vector3 tileScale => new Vector3(tileSize, tileSize, .8f);

    // between center and next tile
    private float tileSpacing => tileSize * .5f + tilePadding * 2;
    private int InPlaceTiles => activeTiles.Count(x => x.state == Tile.TileState.InPlace);

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

        _boardSpacesDict = new Dictionary<int2, BoardSpace>();
        BoardSpace[] boardSpaced = FindObjectsOfType<BoardSpace>();
        foreach (var space in boardSpaced)
            _boardSpacesDict.Add(space.index, space);

        PopulateSpaces(true);
    }

    void Update()
    {
        /////////////
        // Assess board state
        int movingTiles = activeTiles.Count - InPlaceTiles;
        int populatedSpaces = _boardSpacesDict.Count(x => x.Value.IsOccupied);
        if (state == BoardState.Static)
        {
            if (movingTiles > 0 || populatedSpaces != _boardSpacesDict.Count)
                SetState(BoardState.Moving);
        }
        else if (state == BoardState.RemovingTiles)
        {
            removeTimer -= Time.deltaTime;
            if (removeTimer < 0)
            {
                removeTimer += removeDuration;
                BoardSpace space = selectedSpaces[0];
                DestroyTile(space.occupyingTile);
                selectedSpaces.Remove(space);

                if (selectedSpaces.Count == 0)
                {
                    SetState(BoardState.Moving);
                    ClearSelection();
                }
            }
        }
        else if (state == BoardState.Moving)
        {
            //////////////////
            // Update tile
            foreach (var tile in activeTiles)
                tile.UpdateTileMovement(tileSpacing, MinY);

            if (InPlaceTiles == activeTiles.Count)
                SetState(BoardState.Filling);
        }
        else if (state == BoardState.Filling)
        {
            foreach (var space in _boardSpacesDict)
            {
                space.Value.ClearOccupiedTileReference();
                Tile tile = FindTileAtSpace(space.Key);
                if (tile != null)
                    space.Value.SetTile(FindTileAtSpace(space.Key));
            }

            populatedSpaces = _boardSpacesDict.Count(x => x.Value.IsOccupied);
            //Debug.Log($"populatedSpaces  {populatedSpaces}");
            if (populatedSpaces == _boardSpacesDict.Count)
                SetState(BoardState.Static);
            else
            {
                PopulateSpaces(false);
            }
        }
    }

    void SetState(BoardState newState)
    {
        if (state == newState)
            return;

        state = newState;

        Debug.Log(newState);
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
        foreach (var space in _boardSpacesDict)
            space.Value.SetSelected(false);

        selectedSpaces.Clear();
    }

    [ContextMenu("Populate")]
    public void PopulateSpaces(bool clearAllTiles)
    {
        if (clearAllTiles)
        {
            DestroyAllTiles();
            activeTiles.Clear();
        }

        foreach (var space in _boardSpacesDict)
        {
            if (space.Value.IsOccupied)
                continue;

            if (activeTiles.Count >= _boardSpacesDict.Count)
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

    public void AssessSelectionSuccess()
    {
        if (selectedSpaces.Count >= 3)
        {
            //selectedSpaces = selectedSpaces.OrderBy(x => x.index.y).ToList();
            SetState(BoardState.RemovingTiles);
            AppManager.instance.AddScore(selectedSpaces.Count);
        }
        else
        {
            ClearSelection();
        }
    }

    public void DestroyTile(Tile tile)
    {
        activeTiles.Remove(tile);
        Destroy(tile.gameObject);
    }

    public void DestroyAllTiles()
    {
        foreach (var space in _boardSpacesDict)
        {
            space.Value.DestroyTile(true);
        }
    }


    // HELPER METHODS
    //
    public Tile FindTileAtSpace(int2 index) =>
        activeTiles.FirstOrDefault(x => x.boardSpaceIndex.x == index.x && x.boardSpaceIndex.y == index.y);

    public BoardSpace GetSpaceAtIndex(int x, int y) => _boardSpacesDict[new int2(x, y)];
    public BoardSpace FindBoardSpaceAtPos(Vector3 pos) => _boardSpacesDict[GetIndexAtPos(pos)];
    public Vector3 FindQuantizedSpacePos(Vector3 pos) => _boardSpacesDict[GetIndexAtPos(pos)].transform.position;

    public int2 GetIndexAtPos(Vector3 pos)
    {
        int2 index;
        float min = boardWorldSize * -.5f;
        index.x = Mathf.FloorToInt((pos.x - min) / BoardSpaceSize);
        index.y = Mathf.FloorToInt((pos.y - min) / BoardSpaceSize);
        return index;
    }

    public Vector3 GetPosAtIndex(int2 index)
    {
        float min = boardWorldSize * -.5f;
        Vector3 pos = new Vector3();
        pos.x = min + (BoardSpaceSize * index.x) + (BoardSpaceSize * .5f);
        pos.y = min + (BoardSpaceSize * index.y) + (BoardSpaceSize * .5f);
        return pos;
    }


    // GENERATION
    //
    [ContextMenu("Generate")]
    void GenerateSpaces()
    {
        if (_boardSpacesDict.Count > 0)
        {
            // Find keys of all elements that meet the condition
            var keysToRemove = _boardSpacesDict.Where(pair => pair.Value == null).Select(pair => pair.Key).ToList();
            foreach (var key in keysToRemove)
                _boardSpacesDict.Remove(key);


            foreach (var space in _boardSpacesDict)
                DestroyImmediate(space.Value.gameObject);
        }

        _boardSpacesDict.Clear();

        float min = boardWorldSize * -.5f;
        _boardSpacesDict = new Dictionary<int2, BoardSpace>();
        for (int x = 0; x < dimensions; x++)
        {
            for (int y = 0; y < dimensions; y++)
            {
                BoardSpace newBoardSpace = Instantiate(boardSpacePrefab, spacesParent);
                newBoardSpace.Init(
                    min + (BoardSpaceSize * x) + (BoardSpaceSize * .5f),
                    min + (BoardSpaceSize * y) + (BoardSpaceSize * .5f),
                    BoardSpaceSize);

                _boardSpacesDict.Add(new int2(x, y), newBoardSpace);
            }
        }
    }

    // GIZMOS AND DEBUG
    //
    private void OnDrawGizmos()
    {
        return;

        if (_boardSpacesDict.Count == 0)
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