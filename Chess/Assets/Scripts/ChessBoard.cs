using System;
using UnityEngine;

public class ChessBoard : MonoBehaviour
{
    [Header("Art Stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 0.1f;
    [SerializeField] private float yOffSet = 0.2f;
    [SerializeField] private Vector3 boardCentre = Vector3.zero;

    [Header("Prefab & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    //LOGIC
    private ChessPiece[,] chessPieces;
    private const int tileCount_X = 8;
    private const int tileCount_Y = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;

    private void Awake() {
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
        if(Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile"))){
            //Get the Indexes of the tile hit
            Vector2Int hitPosition = lookUpTileIndex(info.transform.gameObject);
            // Hovering after not hovering a tile
            if(currentHover == -Vector2Int.one){
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
            // Hovering one tile from already hovering a tile
            if(currentHover != -Vector2Int.one){
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
            else{
                if(currentHover != -Vector2Int.one){
                    tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                    currentHover = -Vector2Int.one;
                }
            }
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
        chessPieces[x, y].transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        chessPieces[x, y].transform.Rotate(new Vector3(90f, 0f, 0f), Space.World);
        chessPieces[x, y].transform.position = getTileCenter(x, y);
    }
    private Vector3 getTileCenter(int x, int y){
        return new Vector3(x * tileSize, yOffSet, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }
    
    //Operations
    private Vector2Int lookUpTileIndex(GameObject hitinfo){
        for (int x = 0; x < tileCount_X; x++)
            for (int y = 0; y < tileCount_Y; y++)
                if(tiles[x, y] == hitinfo)
                    return new Vector2Int(x, y);
        return -Vector2Int.one; //Invalid
    }
}
