/// <summary>
/// 
/// GameManager.cs
/// 
///        Author: Martin Hall
///       Created: 08/12/2017
/// Last Modified: 08/12/2017
/// 
///   Description: Manages player movement with available action points. Renders line from unit to mouse, to feedback possible movements to player. 
///   
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

namespace SpecialForces
{
    public class GameManager : MonoBehaviour
    {

        public List<UnitManager> units = new List<UnitManager>();
        public UnitManager curUnit; // Active player unit
        public bool movingPlayer;
        public bool hasPath;

        // Straight move is 2 points per node
        // Diagonal move is 3 points per node

        Node targetNode;
        Node previousNode;

        List<PathInfo> pathInfo; // Temp
        List<PathInfo> redInfo;
        public Material green;
        public Material red;

        LineRenderer pathRed; // Not enough action points to make the move
        LineRenderer pathGreen; // Move possible
        GridBase grid;

        public void Init()
        {
            grid = GridBase.singleton;

            // If move possible draw green line on path from unit to mouse
            GameObject go = new GameObject();
            go.name = "green path line";
            pathGreen = go.AddComponent<LineRenderer>();
            pathGreen.startWidth = .2f;
            pathGreen.endWidth = .2f;
            pathGreen.material = green;

            // If move not possible draw red line on path from unit to mouse
            GameObject go2 = new GameObject();
            go2.name = "red path line";
            pathRed = go2.AddComponent<LineRenderer>();
            pathRed.startWidth = .2f;
            pathRed.endWidth = .2f;
            pathRed.material = red;

            for (int i = 0; i < units.Count; i++)
            {
                units[i].Init();
            }            
        }

        private void Update()
        {
            // Check init or return
            if (grid.isInit == false)
                return;

            FindNode();

            if (Input.GetMouseButton(0))
            {
                // Check if there's a unit in the node player clicked on for unit selection
                UnitManager hasUnit = NodeHasUnit(targetNode);

                if (curUnit != null)
                {
                    if (curUnit.isMoving)
                        return;
                }

                if (hasUnit == null && curUnit != null)
                {
                    if (hasPath && pathInfo != null)
                    {
                        curUnit.AddPath(pathInfo);
                    }
                }
                else
                {
                    curUnit = hasUnit;
                }
            }

            if (curUnit == null)
                return;

            // Stop logic below from running if unit is moving
            if (curUnit.isMoving)
                return;

            if (previousNode != targetNode)
            {
                // Pathfinding
                PathfindMaster.GetInstance().RequestPathFind(curUnit.node, targetNode, PathfinderCallback);
            }

            previousNode = targetNode;

            // Draw line from unit to mouse
            if (hasPath && pathInfo != null)
            {
                if (pathInfo.Count > 0)
                {
                    pathGreen.positionCount = pathInfo.Count;

                    for (int i = 0; i < pathInfo.Count; i++)
                    {
                        pathGreen.SetPosition(i, pathInfo[i].targetPosition);
                    }
                }

                if (redInfo != null)
                {
                    // Draw red line
                    if (redInfo.Count > 1)
                    {
                        pathRed.positionCount = redInfo.Count;

                        pathRed.gameObject.SetActive(true);

                        for (int i = 0; i < redInfo.Count; i++)
                        {
                            pathRed.SetPosition(i, redInfo[i].targetPosition); // Todo error index out of range
                        }
                    }
                    else
                    {
                        pathRed.gameObject.SetActive(false);
                    }
                }
            }
        }

        // Find the node the cursor is over (possible destination of player movement)
        void FindNode()
        {
            // Find which node the cursor is on
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                targetNode = grid.GetNodeFromWorldPosition(hit.point);
            }
            // Render line from unit to mouse to give player feedback on path
        }
        
        // Check if the node the player clicked on has a unit and return the unit so it becomes the selected unit
        UnitManager NodeHasUnit(Node n)
        {
            for (int i = 0; i < units.Count; i++)
            {
                Node un = units[i].node;

                // Unit on this node, return the unit
                if (un.x == n.x && un.y == n.y && un.z == n.z)
                    return units[i];
            }

            // No units here
            return null;
        }
        
        void PathfinderCallback(List<Node>p)
        {
            int curAp = curUnit.actionPoints; // This is going to change based on stats of each unit
            int requiredAp = 0; // action points needed to move

            // Get origin and target position to calculate move cost
            List<PathInfo> tp = new List<PathInfo>();
            PathInfo p1 = new PathInfo();
            p1.ap = 0;
            p1.targetPosition = curUnit.transform.position;
            tp.Add(p1);

            List<PathInfo> red = new List<PathInfo>();

            // Default move cost is 2 (straight move)
            int baseAction = 2;
            int diagonalMove = Mathf.FloorToInt(baseAction / 2); // Add to straight move cost

            for (int i = 0; i < p.Count; i++)
            {
                Node n = p[i];
                Vector3 wp = grid.GetCoordsFromNode(n.x, n.y, n.z); // Current position
                Vector3 dir = Vector3.zero;
                // Get path direction
                if (i == 0) // First node in path
                    dir = GetPathDir(curUnit.node, n);
                else
                    dir = GetPathDir(p[i - 1], p[i]);

                // Moving diagonally
                if (dir.x != 0 && dir.z != 0)
                    baseAction = baseAction + diagonalMove;

                requiredAp += baseAction;

                PathInfo pi = new PathInfo();
                pi.ap = baseAction;
                pi.targetPosition = wp;

                // Not enough action points to move
                if (requiredAp > curAp)
                {
                    if (red.Count == 0)
                    {
                        // First position on red line
                        red.Add(tp[i]);
                    }

                    red.Add(pi);
                }
                else // Can move, add pos to list for green line
                {
                    tp.Add(pi);
                }

            }

            pathInfo = tp;
            redInfo = red;
            hasPath = true;
        }

        // Find direction of path
        Vector3 GetPathDir(Node destination, Node origin)
        {
            Vector3 dir = Vector3.zero;
            dir.x = destination.x - origin.x;
            dir.y = destination.y - origin.y;
            dir.z = destination.z - origin.z;
            return dir;
        }

        public static GameManager singleton;
        private void Awake()
        {
            singleton = this;
        }

        // Store details about the path for action points calculations
        [System.Serializable]
        public class PathInfo
        {
            public int ap; // Action points required to move
            public Vector3 targetPosition;
        }
    }
}


