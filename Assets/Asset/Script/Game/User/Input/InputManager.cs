﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using System.Linq;
using Utility;
using Player;

public class InputManager :  DragHandler
{
    public enum States { Idle, Move, WaitForAction, Attack, Drag };
    public States inputState;
    public Unit moveUnit;

    // private DragHandler mobileInput;

    //Distinguish between click and drag
    private float mouseClickDistance = 0;
    private float isDragDistance = 0.1f;

    private Vector3 startClickPoint;

    private List<Tile> recordTile = new List<Tile>();
    private GameManager gameManager;
    private GameUIManager gameUI;
    public Map _Map;

    User player { get { return transform.FindChild("player").GetComponent<User>(); } }

    // Use this for initialization
    public void SetUp(GameManager p_gamemanager, GameUIManager p_gameUI)
    {
        inputState = States.Idle;
        gameManager = p_gamemanager;
        gameUI = p_gameUI;
        _Map = gameManager.map;

    }


    //========================================================= General Input Command =========================================================

    public void FreePanel()
    {
        inputState = InputManager.States.Idle;
        _Map.gridManager.ResetGrid(_Map.grids);
        _Map.gridManager.ClearPathLine();

        gameUI.actionMenu.SetBool("isOpen", false);
    }

    /// <summary>
    /// Kepp tracking the best path to the point position
    /// </summary>
    public void PathTracking(Unit p_unit)
    {
        int T_layer = GeneralSetting.terrainLayer;

        Vector3 mouseposition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0));
        Collider2D collide = Physics2D.OverlapPoint(new Vector2(mouseposition.x, mouseposition.y), T_layer);

        if (collide == null || collide.tag != "Ground" || collide.GetComponent<GridHolder>().gridStatus != GridHolder.Status.Move) return;

        List<Tile> tiles = FindPath(p_unit.unitPos, collide.transform.position);
        recordTile = tiles;

        List<Vector2> pathList = tiles.Select(x => x.position).ToList();
        pathList.Insert(0, p_unit.unitPos);
        _Map.gridManager.DrawPathLine(pathList);
    }

    /// <summary>
    /// Call A* function
    /// </summary>
    /// <returns>The path.</returns>
    /// <param name="startPos">Start position.</param>
    /// <param name="endPos">End position.</param>
    public List<Tile> FindPath(Vector2 startPos, Vector2 endPos)
    {
        GridHolder startGrid = _Map.FindTileByPos(startPos);
        GridHolder endGrid = _Map.FindTileByPos(endPos);
        return _Map.gridManager.aPathFinding.FindPath(startGrid, endGrid);
    }

    /// <summary>
    /// Moves unit with given paths.
    /// </summary>
    /// <param name="p_moveunit">P moveunit.</param>
    /// <param name="tiles">Tiles.</param>
    /// <param name="callback">Callback.</param>
    public void MoveUnitFromPath(Unit p_moveunit, List<Tile> tiles, System.Action callback = null)
    {
        if (tiles.Count <= 0)
        {
            _Map.gridManager.DrawPathLine(new List<Vector2>());
            if (callback != null) callback();
            return;
        }

        Vector3[] path = tiles.ConvertAll<Vector3>(x => x.position).ToArray();

        p_moveunit.Move(path, delegate ()
        {
            //Clear Path Line
            _Map.gridManager.ClearPathLine();
            if (callback != null) callback();
        });
    }

    public void MoveUnitFromDrop(Unit p_moveunit, Tile p_lastTiles)
    {
        p_moveunit.unitPos = p_lastTiles.position;
        p_moveunit.status = Unit.Status.Moved;
        FreePanel();
    }


    public void DisplayRoute(Unit p_unit)
    {
        _Map.gridManager.FindPossibleRoute(p_unit, gameManager.enemy);
    }

    // ========================================================= Player Input Command =========================================================
    void Update()
    {
        if (gameManager == null) return;

        //Only work in player's turn
        if (gameManager.currentUser.userType == EventFlag.UserType.Player)
        {
            Vector3 mouseposition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0));

            //Mouse CLick
            if (Input.GetMouseButton(0))
            {
                if (startClickPoint == Vector3.zero) startClickPoint = mouseposition;

                //Check if it meet the condition of dragging
                if (inputState != States.Drag)
                {
                    float dis = Vector3.Distance(mouseposition, startClickPoint);
                    if (dis > isDragDistance)
                    {
                        Unit touchedUnit = GetUnitByMousePos(mouseposition);
                        if (touchedUnit != null)
                        {
                            //onDragBegin
                            inputState = States.Drag;
                            OnDragBegin(GetUnitByMousePos(mouseposition).gameObject);
                        }
                    }
                }

                //onDrag
                if (inputState == States.Drag) OnDrag(mouseposition);
            }

            //Drop
            if (Input.GetMouseButtonUp(0) && inputState == States.Drag)
            {
                startClickPoint = Vector3.zero;
                inputState = States.Idle;

                OnDrop(mouseposition);
            }


            //Mouse Position Tracking
            switch (inputState)
            {
                case States.Move:
                    //PathTracking();
                    break;
            }
        }
    }


    private Unit GetUnitByMousePos(Vector3 p_mousePosition)
    {
        int u_layer = GeneralSetting.unitLayer;

        //Get most top sorting collider
        Collider2D mCollide = Physics2D.OverlapPoint(p_mousePosition, u_layer);
        if (mCollide == null) return null;

        //Move Friendly Unit
        if (mCollide.tag == "Player" && mCollide.GetComponent<Unit>().status == Unit.Status.Idle)
        {
            return mCollide.GetComponent<Unit>();
        }

        return null;
    }

    public List<Collider2D> GetAllColliderByMousePos(Vector2 p_mousePoint, Unit p_holdUnit = null)
    {
        int UT_layer = GeneralSetting.unitLayer + GeneralSetting.terrainLayer;
        //Get most top sorting collider
        List<Collider2D> collides = Physics2D.OverlapPointAll(p_mousePoint, UT_layer).ToList();
        collides.Sort((x, y) => x.GetComponent<SpriteRenderer>().sortingOrder.CompareTo(y.GetComponent<SpriteRenderer>().sortingOrder));
        collides.Reverse();

        if (p_holdUnit != null)
        {
            collides.RemoveAll(x=>x.name == p_holdUnit.name);
        }

        if (collides == null || collides.Count <= 0) return null;

        return collides;
    }


    //=========================================== Drag and Drop ======================================
    public override void OnDragBegin(GameObject p_gameobject) {
        if (p_gameobject == null) return;

		moveUnit = p_gameobject.GetComponent<Unit>();
		DisplayRoute(moveUnit);
    }

    public override void OnDrag(Vector3 p_mousePosition) {
		if (moveUnit == null) return; 

		//Move Friendly Unit
		PathTracking( moveUnit );
		moveUnit.transform.position = new Vector2( p_mousePosition.x, p_mousePosition.y);
    }

    public override void OnDrop(Vector3 p_mousePosition) {
		MoveUnitOnDrop(moveUnit , p_mousePosition);
		moveUnit = null;
	}



    public void MoveUnitOnDrop(Unit p_unit, Vector2 point)
    {

        List<Collider2D> collides = GetAllColliderByMousePos(point, p_unit);


        if (collides == null || recordTile.Count == 0) ResumeUnit(p_unit);
        Collider2D mCollide = collides[0];

        if (mCollide.tag == "Ground" && mCollide.GetComponent<GridHolder>().gridStatus == GridHolder.Status.Move)
        {
            Debug.Log("Is Ground");
            //Normal Grid
            inputState = States.WaitForAction;

            MoveUnitFromDrop(p_unit, recordTile[recordTile.Count - 1]);

        }
        else if (mCollide.tag == "Enemy" && collides[1].GetComponent<GridHolder>().gridStatus == GridHolder.Status.Attack)
        {
            Debug.Log("Is Enemy");

            Unit target = mCollide.GetComponent<Unit>();
            GridHolder gridHolder = _Map.FindTileByPos(target.unitPos);
            List<Tile> tiles = FindPath(p_unit.unitPos, gridHolder.attackPos);

            inputState = States.WaitForAction;
            p_unit.Attack(target, gridHolder);

            if (tiles.Count > 0) {
                MoveUnitFromDrop(p_unit, tiles[tiles.Count - 1]);
            } else {
                ResumeUnit(p_unit, false);
            }
        }
        else
        {
            ResumeUnit(p_unit);
        }
    }

    public void ResumeUnit(Unit p_unit, bool isIdle = true)
    {
        p_unit.unitPos = gameManager.map.gridManager.originTile;
        p_unit.status = (isIdle) ? Unit.Status.Idle : Unit.Status.Moved;
        FreePanel();
    }




    // 	void MouseClickHandler(Vector2	point) {
    // 		int UT_layer = GeneralSetting.unitLayer + GeneralSetting.terrainLayer;
    // 		//Get most top sorting collider
    // 		List<Collider2D> collides = Physics2D.OverlapPointAll(point, UT_layer).ToList();
    // 		collides.Sort((x, y) => x.GetComponent<SpriteRenderer>().sortingOrder.CompareTo(y.GetComponent<SpriteRenderer>().sortingOrder));
    // 		collides.Reverse();
    // 		if (collides == null || collides.Count <= 0) return; 
    // 		Collider2D mCollide = collides[0];



    // 		switch (inputState) {
    // 			case States.Idle :
    // 				//Move Friendly Unit
    // 				if (mCollide.tag == "Player" && mCollide.GetComponent<Unit>().status == Unit.Status.Idle ) {
    // 					inputState = States.Move;
    // 					moveUnit = mCollide.GetComponent<Unit>();
    // 					_Map.gridManager.FindPossibleRoute(moveUnit, gameManager.enemy);
    // 				}
    // 			break;

    // 			case States.Move :
    // 				if (mCollide.tag == "Ground") {
    // 					//Normal Grid
    // 					if (mCollide.GetComponent<GridHolder>().gridStatus == GridHolder.Status.Move) {
    // 						inputState = States.WaitForAction;
    // 						MoveUnit(moveUnit, recordTile);
    // 						gameUI.actionMenu.SetBool("isOpen", true);
    // 					}
    // 				} else if (mCollide.tag == "Enemy" && collides[1].GetComponent<GridHolder>().gridStatus == GridHolder.Status.Attack) {
    // 					Unit target = mCollide.GetComponent<Unit>();
    // 					GridHolder gridHolder = _Map.FindTileByPos(target.unitPos);
    // 					List<Tile> tiles = FindPath(moveUnit.transform.position, gridHolder.attackPos);
    // //					Debug.Log("Attack MOve");
    // 					inputState = States.WaitForAction;
    // 					MoveUnit(moveUnit, tiles, delegate() {
    // 						moveUnit.Attack( target, gridHolder);
    // 						moveUnit.status = Unit.Status.Moved;
    // 						FreePanel();						
    // 					});
    // 				}
    // 			break;

    // 			case States.Attack :
    // 				if (mCollide.tag == "Enemy") {
    // 					Unit target = mCollide.GetComponent<Unit>();
    // 					GridHolder gridHolder = _Map.FindTileByPos(target.unitPos);
    // 					moveUnit.Attack( target, gridHolder);
    // 					moveUnit.status = Unit.Status.Moved;
    // 					moveUnit.UnitHighLightControl(false);
    // 					FreePanel();
    // 				}
    // 			break;
    // 		}
    // 	}
}