using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AppManager : MonoBehaviour
{
    public enum AppState
    {
        MainMenu,
        Playing,
        GameOver,
    }

    public static AppManager instance;

    public AppState state = AppState.MainMenu;
    public int score;
    private bool selectionStarted = false;

    public float tileMoveSpeed = 6;
    public float tileScaleDuration = 0.15f;
    public float removeDuration = .05f;
    
    public LayerMask tileLayerMask;
    
    
    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
        tileLayerMask  = LayerMask.GetMask("Tile");
        Random.InitState(0);
    }

    public float lerp = 0;
    public void AddScore(int points)
    {
        score += points;
        lerp += .01f * points;
        lerp = Mathf.Clamp01(lerp);
    }

    // Update is called once per frame
    void Update()
    {
        if (state == AppState.Playing)
        {
            if (!Board.instance.CanSelectTiles)
                return;
            
            if (Input.GetMouseButtonDown(0))
            {
                // raycast, select first block under mouse
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                LayerMask mask = LayerMask.GetMask("BoardSpace");
                if (Physics.Raycast(ray, out hit, 100, mask))
                {
                    if (hit.collider.gameObject.TryGetComponent(out BoardSpace space))
                    {
                        if (Board.instance.SpaceIsSelectable(space))
                        {
                            selectionStarted = true;
                            space.SetSelected(true);
                        }
                    }
                }
            }
            else if (Input.GetMouseButton(0) && selectionStarted)
            {
                // raycast, select first block under mouse
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                LayerMask mask = LayerMask.GetMask("BoardSpace");
               
                if (Physics.Raycast(ray, out hit, 100, mask))
                {
                    if (hit.collider.gameObject.TryGetComponent(out BoardSpace space))
                    {
                        if (Board.instance.SpaceIsSelectable(space))
                        {
                            selectionStarted = true;
                            space.SetSelected(true);
                        }
                        else
                        {
                            selectionStarted = false;
                        }
                    }
                }

                if (!selectionStarted)
                    Board.instance.ClearSelection();
            }
            else if(Input.GetMouseButtonUp(0))
            {
                // Finish select
                //Debug.Log("Selection finsihed");
                if(selectionStarted)
                    Board.instance.AssessSelectionSuccess();
                else
                    Board.instance.ClearSelection();
                
                selectionStarted = false;
            }
            

            if (Input.GetMouseButtonDown(1))
            {
                Debug.Log("right mouse");
                // Clear tile
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                LayerMask mask = LayerMask.GetMask("Tile");
                if (Physics.Raycast(ray, out hit, 100, mask))
                {
                    Debug.Log(hit.collider.name);
                    if (hit.collider.gameObject.TryGetComponent(out Tile tile))
                        Board.instance.DestroyTile(tile);
                }
                else
                {
                    Debug.Log("no hit");
                }
            }
        }
    }
}
