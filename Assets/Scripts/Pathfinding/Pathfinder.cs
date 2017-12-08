/*
 * Pathfinder.cs
 * Author: Martin Hall
 * 
 * Created: 30/11/17
 * Last Modified: 30/11/17
 * 
 * Description: Finds the optimal path between two nodes
 * 
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SpecialForces;

namespace Pathfinding
{
    public class Pathfinder
    {
        GridBase gridManager;

        // Nodes at the origin and destination of the path
        public Node startPosition;
        public Node endPosition;

        // Flag to indicate thread completion, maybe modified by multiple threads running concurrently
        public volatile bool jobDone = false;
        // Can pass the callback function around
        PathfindMaster.PathfindingJobComplete completeCallback;
        List<Node> foundPath;

        // Constructor
        public Pathfinder(Node start, Node target, 
            PathfindMaster.PathfindingJobComplete callback)
        {
            startPosition = start;
            endPosition = target;
            completeCallback = callback;
            gridManager = GridBase.singleton;
        }

        // The path
        // When we want to find a path, return a list of nodes we can pass through to destination
        public void FindPath()
        {
            foundPath = FindActualPath(startPosition, endPosition);

            jobDone = true;
        }

        // Job done
        public void NotifyComplete()
        {
            if (completeCallback != null)
            {
                completeCallback(foundPath); // Pass the list from thread to thread, bypass Unity thread inaccessibility
            }
        }

        private List<Node> FindActualPath(Node start, Node target)
        {
            // Typical A* alogorithm from this point on
            List<Node> foundPath = new List<Node>();

            // List of nodes to check
            List<Node> checkQueue = new List<Node>();
            // List of nodes already checked
            HashSet<Node> checkedNodes = new HashSet<Node>();

            // Start queueing nodes to check
            checkQueue.Add(start);

            while (checkQueue.Count > 0)
            {
                Node currentNode = checkQueue[0];

                // TODO optimise the node checking function with a heap data structure?
                for (int i = 0; i < checkQueue.Count; i++)
                {
                    // Check the cost of this node
                    // TODO add pathfinding options here as needed
                    if (checkQueue[i].fCost < currentNode.fCost ||
                        (checkQueue[i].fCost == currentNode.fCost &&
                        checkQueue[i].hCost < currentNode.hCost))
                    {
                        // Assign the node just checked as the current node, if the cost is less
                        if (!currentNode.Equals(checkQueue[i]))
                        {
                            currentNode = checkQueue[i];
                        }
                    }

                    // Add the node to the checked list
                    checkQueue.Remove(currentNode);
                    checkedNodes.Add(currentNode);

                    // Destination reached, so start retracing the path
                    if (currentNode.Equals(target))
                    {
                        foundPath = RetracePath(start, currentNode);
                        break;
                    }

                    // If destination hasn't been reached, start looking at neighbouring nodes
                    foreach (Node neighbour in GetNeighbours(currentNode, true))
                    {
                        // Check the neighbour, unless its already been checked
                        if (!checkedNodes.Contains(neighbour))
                        {
                            // Find the movement cost of the neighbouring node
                            float newMovementCostToNeighbour = currentNode.gCost + 
                                GetDistance(currentNode, neighbour);

                            // If it's lower than the neighbour's cost
                            if (newMovementCostToNeighbour < neighbour.gCost || 
                                !checkedNodes.Contains(neighbour))
                            {
                                // Calculate the new costs
                                neighbour.gCost = newMovementCostToNeighbour;
                                neighbour.hCost = GetDistance(neighbour, target);
                                // Assign the parent node
                                neighbour.parentNode = currentNode;
                                // Add the neighbour to the queue to check
                                if (!checkQueue.Contains(neighbour))
                                {
                                    checkQueue.Add(neighbour);
                                }
                            }
                        }
                    }

                }
            }
            
            // Return the optimal path
            return foundPath;
        }

        // Retrace nodes in the path from the origin node to the destination
        private List<Node> RetracePath(Node start, Node destination)
        {
            List<Node> path = new List<Node>();

            Node currentNode = destination;

            while (currentNode != start)
            {
                path.Add(currentNode);
                // Get the next node in the path
                currentNode = currentNode.parentNode;
            }

            // Reverse the list, switch the list round to run from start to destination
            path.Reverse();

            return path;
        }

        // Find neighbouring nodes, flag for 3D or 2D search
        private List<Node> GetNeighbours(Node node, bool getVerticalNeighbours = false) 
        {
            // Start listing the neighbours
            List<Node> returnList = new List<Node>();

            // Check neighbours on each of three axis
            for (int x = -1; x <= 1; x++)
            {
                for (int yIndex = -1; yIndex <= 1; yIndex++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        int y = yIndex;

                        // If it's a 2D search, don't check the y
                        // TODO use 2D for Ludum Dare?
                        if (!getVerticalNeighbours)
                        {
                            y = 0;
                        }

                        if (x == 0 && y == 0 && z == 0)
                        {
                            // 000 means we are on the current node
                        }
                        // If not on the current node, we're on a neighbouring node
                        else
                        {
                            Node searchPos = new Node();

                            // Find the nodes that are forwards/backwards, left/right, up/down from this node
                            searchPos.x = node.x + x;
                            searchPos.y = node.y + y;
                            searchPos.z = node.z + z;

                            Node newNode = GetNeighbourNode(searchPos, true, node);

                            // If there's a node next to this one, add it to the list of neighbours
                            if (newNode != null)
                            {
                                returnList.Add(newNode);
                            }
                        }
                    }
                }
            }
            return returnList;
        }

        // Check if we can move to neighbouring node forward, below or above
        private Node GetNeighbourNode(Node adjPos, bool searchTopDown, Node currentNodePos)
        {
            Node returnVal = null;

            // Take the node from the adjacent positions passed in
            Node node = GetNode(adjPos.x, adjPos.y, adjPos.z);

            // If there's a node forward, and we can move on to it add it to the list
            if (node != null && node.isWalkable)
            {
                // We can use the node in the path
                returnVal = node;
            } // If not and we want a 3D search, check the node below
            else if (searchTopDown)
            {
                // Check the node beneath
                adjPos.y -= 1;
                Node bottomBlock = GetNode(adjPos.x, adjPos.y, adjPos.z);

                // If there's a node below, and we can move to it
                if (bottomBlock != null && bottomBlock.isWalkable)
                {
                    // Add the node beneath to the list of nodes we can add to the path
                    returnVal = bottomBlock;
                }
                // If there isn't a node below that we can move on to, check the node above
                else
                {
                    adjPos.y += 2;
                    Node topBlock = GetNode(adjPos.x, adjPos.y, adjPos.z);
                    // If there's a node above and we can move on to it, add it to the list
                    if (topBlock != null && topBlock.isWalkable)
                    {
                        returnVal = topBlock;
                    }

                    // To move diagonally, there needs to be four walkable nodes adjacent to the current node
                    int originalX = adjPos.x - currentNodePos.x;
                    int originalZ = adjPos.z - currentNodePos.z;

                    if (Mathf.Abs(originalX) == 1 && Mathf.Abs(originalZ) == 1)
                    {
                        // Firs block is originalX + 0(z), second block to check is 0(x) + originalZ
                        Node neighbour1 = GetNode(currentNodePos.x + originalX, 
                            currentNodePos.y, currentNodePos.z);

                        if (neighbour1 == null || !neighbour1.isWalkable)
                        {
                            returnVal = null;
                        }

                        Node neighbour2 = GetNode(currentNodePos.x, currentNodePos.y, 
                            currentNodePos.z + originalZ);

                        if (neighbour2 == null || !neighbour2.isWalkable)
                        {
                            returnVal = null;
                        }

                        // TODO add more diagonal checks if needed
                        /*
                        if (returnVal != null)
                        {
                            // Some more checks
                            // Example don't approach the node from the right
                            
                            if (node.x < currentNodePos.x)
                            {
                                node = null;
                            }
                        }
                        */
                    }
                }
            }

            return returnVal;
        }

        private Node GetNode(int x, int y, int z)
        {
            Node n = null;
            // Prevent changes to node isWalkable until the thread is finished
            lock (gridManager)
            {
                n = gridManager.GetNode(x, y, z);
            }

            return n;
        }

        // Find the distance between two nodes
        private int GetDistance(Node posA, Node posB)
        {
            int distX = Mathf.Abs(posA.x - posB.x);
            int distY = Mathf.Abs(posA.y - posB.y);
            int distZ = Mathf.Abs(posA.z - posB.z);

            if (distX > distZ)
            {
                return 14 * distZ + 10 * (distX - distZ) + 10 * distY;
            }

            return 14 * distX + 10 * (distZ - distX) + 10 * distY;
        }
    }
}


