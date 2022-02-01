using System;
using System.Collections.Generic;
using UnityEngine;

public enum SpecialMove{
    none =  0,
    Enpassant,
    Castling,
    Promotion
}

public class ChessBoard : MonoBehaviour
{
    [Header("Art Stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 0.1f;
    [SerializeField] private float yOffSet = 0.2f;
    [SerializeField] private Vector3 boardCentre = Vector3.zero;
    [SerializeField] private float deadSize = 0.006f;
    [SerializeField] private float deathSpacing = 0.006f;
    [SerializeField] private float dragOffset = 1.5f;
    [SerializeField] private GameObject victoryScreen;

    [Header("Prefab & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;
    

    //LOGIC
    private ChessPiece[,] chessPieces;
    private ChessPiece currentlyDragging;
    private List<ChessPiece> deadWhites = new List<ChessPiece>();
    private List<ChessPiece> deadBlacks = new List<ChessPiece>();
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private const int tileCount_X = 8;
    private const int tileCount_Y = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;
    private bool isWhiteTurn;
    private SpecialMove specialMove;
    private List<Vector2Int[]> moveList = new List<Vector2Int[]>();

    private void Awake() {
        isWhiteTurn = true;

        generateTiles(tileSize, tileCount_X, tileCount_Y);

        spawnAllPieces();
        positionAllPieces();
    }
    private void Update() {
        if(!currentCamera){
            currentCamera = Camera.main;
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile","Hover", "Highlight"))){
            //Get the Indexes of the tile hit
            Vector2Int hitPosition = lookUpTileIndex(info.transform.gameObject);
            // Hovering after not hovering a tile
            if(currentHover == -Vector2Int.one){
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
            // Hovering one tile from already hovering a tile
            if(currentHover != hitPosition){
                tiles[currentHover.x, currentHover.y].layer = (containsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            // If we press down on mouse
            if(Input.GetMouseButtonDown(0)){
                if(chessPieces[hitPosition.x, hitPosition.y] != null){
                    //is it our turn
                    if((chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn) || (chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn)){
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];

                        //get a list of all possible moves and highlight them
                        availableMoves = currentlyDragging.getAvailableMoves(ref chessPieces, tileCount_X, tileCount_Y);
                        //get a list of Special Moves
                        specialMove = currentlyDragging.getSpecialMove(ref chessPieces, ref moveList, ref availableMoves);
                        highlightTiles();
                    }
                }
            }
            //If we realease the mouse
            if(currentlyDragging != null && Input.GetMouseButtonUp(0)){
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

                bool validMove = moveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                if(!validMove)
                    currentlyDragging.setPosition(getTileCenter(previousPosition.x, previousPosition.y));
                currentlyDragging = null;
                removeHighlightTiles();
            }
        }
        else{
            if(currentHover != -Vector2Int.one){
                tiles[currentHover.x, currentHover.y].layer = (containsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }
            if(currentlyDragging && Input.GetMouseButtonUp(0)){
                currentlyDragging.setPosition(getTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                currentlyDragging = null;
                removeHighlightTiles();
            }
        }
        //if we're dragging a piece
        if(currentlyDragging){
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffSet);
            float distance = 0.0f;
            if(horizontalPlane.Raycast(ray, out distance))
                currentlyDragging.setPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
        }
    }

    //Generate Board
    private void generateTiles(float tileSize, int tileCountX, int tileCountY){
        yOffSet += transform.position.y;
        bounds = new Vector3((tileCount_X / 2) * tileSize, 0, (tileCount_Y / 2) * tileSize) + boardCentre;
        tiles = new GameObject[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++)
            for (int y = 0; y < tileCountY; y++)
                tiles[x,y] = generateSingleTile(tileSize, x, y);
    }
    private GameObject generateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffSet, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffSet, (y+1) * tileSize) - bounds;
        vertices[2] = new Vector3((x+1) * tileSize, yOffSet, y * tileSize) - bounds;
        vertices[3] = new Vector3((x+1) * tileSize, yOffSet, (y+1) * tileSize) - bounds;

        int[] tris = new int[] {0, 1, 2, 1, 3, 2};

        mesh.vertices = vertices;
        mesh.triangles = tris;

        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }

    //Spawning of Pieces
    private void spawnAllPieces(){
        chessPieces = new ChessPiece[tileCount_X, tileCount_Y];

        int whiteTeam = 0, blackTeam = 1;
        
        //White Team
        chessPieces[0, 0] = spawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        chessPieces[1, 0] = spawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[2, 0] = spawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[3, 0] = spawnSinglePiece(ChessPieceType.Queen, whiteTeam);
        chessPieces[4, 0] = spawnSinglePiece(ChessPieceType.King, whiteTeam);
        chessPieces[5, 0] = spawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[6, 0] = spawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[7, 0] = spawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        for (int i = 0; i < tileCount_X; i++)
            chessPieces[i, 1] = spawnSinglePiece(ChessPieceType.Pawn, whiteTeam);
        
        //Black Team
        chessPieces[0, 7] = spawnSinglePiece(ChessPieceType.Rook, blackTeam);
        chessPieces[1, 7] = spawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[2, 7] = spawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[3, 7] = spawnSinglePiece(ChessPieceType.Queen, blackTeam);
        chessPieces[4, 7] = spawnSinglePiece(ChessPieceType.King, blackTeam);
        chessPieces[5, 7] = spawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[6, 7] = spawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[7, 7] = spawnSinglePiece(ChessPieceType.Rook, blackTeam);
        for (int i = 0; i < tileCount_X; i++)
            chessPieces[i, 6] = spawnSinglePiece(ChessPieceType.Pawn, blackTeam);
        
    }
    private ChessPiece spawnSinglePiece(ChessPieceType type, int team){
        ChessPiece cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPiece>();

        cp.type = type;
        cp.team = team;
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team];

        return cp;
    }

    //Positioning
    private void positionAllPieces(){
        for (int x = 0; x < tileCount_X; x++)
            for (int y = 0; y < tileCount_Y; y++)
                if(chessPieces[x, y] != null)
                    positionSinglePiece(x, y, true);
    }
    private void positionSinglePiece(int x, int y, bool force = false){
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x, y].setPosition(getTileCenter(x, y), force);
    }
    private Vector3 getTileCenter(int x, int y){
        return new Vector3(x * tileSize, yOffSet, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }
    
    //Highlight Tiles
    private void highlightTiles(){
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
    }
    private void removeHighlightTiles(){
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");

        availableMoves.Clear();
    }

    //Checkmate
    private void checkMate(int team){
        displayVictory(team);
    }
    private void displayVictory(int winningTeam){
        victoryScreen.SetActive(true);
        victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true);
    }
    public void onResetButton(){
        //UI
        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.SetActive(false);

        //Fields Reset
        currentlyDragging = null;
        availableMoves.Clear();
        moveList.Clear();

        //Clean Up
        for (int x = 0; x < tileCount_X; x++)
        {
            for (int y = 0; y < tileCount_Y; y++)
            {
                if(chessPieces[x, y] != null)
                    Destroy(chessPieces[x, y].gameObject);

                chessPieces[x, y] = null;
            }
        }
        for (int i = 0; i < deadWhites.Count; i++)
            Destroy(deadWhites[i].gameObject);
        for (int i = 0; i < deadBlacks.Count; i++)
            Destroy(deadBlacks[i].gameObject);

        deadWhites.Clear();
        deadBlacks.Clear();

        spawnAllPieces();
        positionAllPieces();
        isWhiteTurn = true;

    }
    public void onExitButton(){
        Application.Quit();
    }
    
    //special Moves
    private void processSpecialMove(){
        if(specialMove == SpecialMove.Enpassant){
            var newMove = moveList[moveList.Count - 1];
            ChessPiece myPawn = chessPieces[newMove[1].x, newMove[1].y];
            var targetPawnPosition = moveList[moveList.Count - 2];
            ChessPiece enemyPawn = chessPieces[targetPawnPosition[1].x, targetPawnPosition[1].y];

            if(myPawn.currentX == enemyPawn.currentX){
                if(myPawn.currentY == enemyPawn.currentY - 1 || myPawn.currentY == enemyPawn.currentY + 1){
                    if(enemyPawn.team == 0){
                        deadWhites.Add(enemyPawn);
                        enemyPawn.setScale(Vector3.one * deadSize);
                        enemyPawn.setPosition(new Vector3(8 * tileSize, yOffSet, -1 * tileSize) 
                                - bounds 
                                + new Vector3(tileSize / 2, 0, tileSize / 2) 
                                + (Vector3.forward * deathSpacing) * deadWhites.Count);
                    }else{
                        deadBlacks.Add(enemyPawn);
                        enemyPawn.setScale(Vector3.one * deadSize);
                        enemyPawn.setPosition(new Vector3(-1 * tileSize, yOffSet, 8 * tileSize) 
                                - bounds 
                                + new Vector3(tileSize / 2, 0, tileSize / 2) 
                                + (Vector3.forward * deathSpacing) * deadBlacks.Count);
                    }
                    chessPieces[enemyPawn.currentX, enemyPawn.currentY] = null;
                }
            }
        }
    }

    //Operations
    private bool containsValidMove(ref List<Vector2Int> moves, Vector2Int pos){
        for (int i = 0; i < moves.Count; i++)
            if(moves[i].x == pos.x && moves[i].y == pos.y)
                return true;

        return false;
    }
    private bool moveTo(ChessPiece cp, int x, int y){
        if(!containsValidMove(ref availableMoves, new Vector2Int(x,y)))
            return false;

        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        //is there another piece in target position
        if(chessPieces[x, y] != null){
            ChessPiece ocp = chessPieces[x, y];

            if(cp.team == ocp.team)
                return false;
            
            //if it's the enemy team
            if(ocp.team == 0){
                if(ocp.type == ChessPieceType.King)
                    checkMate(1);

                deadWhites.Add(ocp);
                ocp.setScale(Vector3.one * deadSize);
                ocp.setPosition(new Vector3(8 * tileSize, yOffSet, -1 * tileSize) 
                                - bounds 
                                + new Vector3(tileSize / 2, 0, tileSize / 2) 
                                + (Vector3.forward * deathSpacing) * deadWhites.Count);
            }
            else{
                if(ocp.type == ChessPieceType.King)
                    checkMate(0);

                deadBlacks.Add(ocp);
                ocp.setScale(Vector3.one * deadSize);
                ocp.setPosition(new Vector3(-1 * tileSize, yOffSet, 8 * tileSize) 
                                - bounds 
                                + new Vector3(tileSize / 2, 0, tileSize / 2) 
                                + (Vector3.back * deathSpacing) * deadBlacks.Count);
            }
        }

        chessPieces[x, y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        positionSinglePiece(x, y);

        isWhiteTurn = !isWhiteTurn;
        moveList.Add(new Vector2Int[]{previousPosition, new Vector2Int(x, y)});

        processSpecialMove();

        return true;
    }
    private Vector2Int lookUpTileIndex(GameObject hitinfo){
        for (int x = 0; x < tileCount_X; x++)
            for (int y = 0; y < tileCount_Y; y++)
                if(tiles[x, y] == hitinfo)
                    return new Vector2Int(x, y);
        return -Vector2Int.one; //Invalid
    }
}
