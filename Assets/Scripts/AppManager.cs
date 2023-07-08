using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppManager : MonoBehaviour
{
    public enum AppState
    {
        MainMenu,
        Playing,
        GameOver,
    }

    public AppState state = AppState.MainMenu;
    

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (state == AppState.Playing)
        {
            if (Input.GetMouseButtonDown(0))
            {
                // raycast, select first block under mouse
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                Debug.Log("click");
                LayerMask mask = LayerMask.GetMask("BoardSpace");
                if (Physics.Raycast(ray, out hit, 100, mask))
                {
                    Debug.Log(hit.collider.name);
                    if (hit.collider.gameObject.TryGetComponent(out BoardSpace space))
                        space.Selected();
                }
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
                        Board.instance.ClearTile(tile);
                }
                else
                {
                    Debug.Log("no hit");
                }
            }
        }
    }
}
