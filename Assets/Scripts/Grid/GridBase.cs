using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpecialForces
{
    public class GridBase : MonoBehaviour
    {
        // Grid dimensions
        public int dimX = 32;
        public int dimY = 3;
        public int dimZ = 32;
        public float scaleXZ = 1;
        public float scaleY = 2.3f;

        public bool debugNode; // Todo can create levels in modelling program, in editor or Unity terrain
        public Material debugMaterial;
        private GameObject debugNodeObj;

        public Node[,,] grid;
        public List<Floors> floors;

        private void Start()
        {
            InitStage();
        }

        public void InitStage()
        {
            if (debugNode)
            {
                debugNodeObj = WorldNode();
            }

            InitCheck();
            CreateGrid();
        }

        void InitCheck()
        {
            if (dimX == 0)
            {
                Debug.Log("Dimension x is 0, assigning default value");
                dimX = 16; // Set grid's default x size if inspector set to 0
            }

            if (dimY == 0)
            {
                Debug.Log("Dimension y is 0, assigning default value");
                dimY = 1; // Set grid's default y size if inspector set to 0
            }

            if (dimZ == 0)
            {
                Debug.Log("Dimension z is 0, assigning default default");
                dimZ = 1; // Set grid's default z size if inspector set to 0
            }

            if (scaleXZ == 0)
            {
                Debug.Log("Scale XZ is 0, assigning default value default");
                scaleXZ = 1; // Set grid's default scaleXZ size if inspector set to 0
            }

            if (scaleY == 0)
            {
                Debug.Log("Scale Y is 0, assigning default value default");
                scaleY = 2; // Set grid's default scaleY size if inspector set to 0 
            }
        }

        // Create grid, after doing initialisation checks
        void CreateGrid()
        {
            // Create the grid
            grid = new Node[dimX, dimY, dimZ];
            // Create vertical floors
            for (int y = 0; y < dimY; y++)
            {
                Floors floor = new Floors();
                floor.nodeParent = new GameObject();
                floor.nodeParent.name = "floor " + y.ToString();
                floor.y = y;
                floors.Add(floor); // Todo error - not set to object instance

                // Add collision deteection game object to this floor level
                CreateCollisionDetection(y);

                // Fill the grid with nodes for pathfinding
                for (int x = 0; x < dimX; x++)
                {
                    for (int z = 0; z < dimZ; z++)
                    {
                        Node n = new Node();
                        n.x = x;
                        n.y = y;
                        n.isWalkable = true; // Set all nodes walkable to start with

                        // If we're debugging
                        if (debugNode)
                        {
                            // Place debug object to node position in world
                            Vector3 targetPosition = CoordinatesFromNode(x, y, z);
                            GameObject go = Instantiate(debugNodeObj,
                                targetPosition,
                                Quaternion.identity) as GameObject;
                            go.transform.parent = floor.nodeParent.transform;
                        }

                        grid[x, y, z] = n;
                    }
                }
            }
        }

        // Pass Node to pathfinder
        public Node GetNode(int x, int y, int z)
        {
            // Pathfinder doesn't work with negative numbers
            // Always return a node on the top of the grid
            x = Mathf.Clamp(x, 0, dimX - 1);
            y = Mathf.Clamp(x, 0, dimY - 1);
            z = Mathf.Clamp(z, 0, dimZ - 1);

            return grid[x, y, z];
        }

        // Get the world position for a node
        public Vector3 CoordinatesFromNode(int x, int y, int z)
        {
            // Scale node coordinates to convert to world coordinates
            Vector3 coords = Vector3.zero;
            coords.x = x * scaleXZ;
            coords.y = y * scaleY;
            coords.z = z * scaleXZ;

            // Return world coordinates
            return coords;
        }

        // Visualise the grid in the Unity editor, for debugging
        GameObject WorldNode()
        {
            // Fill the grid with quads so we can see it
            GameObject go = new GameObject();
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(quad.GetComponent<Collider>()); // Deactivate collider
            quad.transform.parent = go.transform;
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localEulerAngles = new Vector3(90, 0, 0);
            quad.transform.localScale = Vector3.one * 0.95f; // Gaps between quads, as gridlines
            quad.GetComponentInChildren<MeshRenderer>().material = debugMaterial;
            return go;
        }

        // Each floor level has its own collision detection
        private void CreateCollisionDetection(int y)
        {
            // Create floor object from passed value
            Floors yFloor = floors[y];
            // Create a game object
            GameObject go = new GameObject();
            // Add box collider to game object
            BoxCollider collisionBox = go.AddComponent<BoxCollider>();
            // The collision box is larger than the floor area to get collisions right at the edges and in corners
            // Starts from centre of grid
            collisionBox.size = new Vector3(dimX * scaleXZ + (scaleXZ * 2), 
                0.2f, dimZ * scaleXZ + (scaleXZ * 2));
            // Add collision box at grid centre
            collisionBox.transform.position = new Vector3((dimX * scaleXZ) * .5f - (scaleXZ * .5f),
                y * dimY,
                (dimZ * scaleXZ) * .5f - (scaleXZ * .5f));
            // Assign the collision detection object to the floor
            yFloor.collisionObj = go;
            go.name = "floor " + y + " collision detection";

        }

        // There will only be one grid objects in each level
        public static GridBase singleton;

        void Awake()
        {
            singleton = this;
        }

        // Store the floor levels
        [System.Serializable]
        public class Floors
        {
            public int y;
            public GameObject nodeParent; // Save the floor nodes
            public GameObject collisionObj; // Collisions on this floor
        }
    }


}

