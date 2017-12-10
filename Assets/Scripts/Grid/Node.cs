/*
 * Author: Martin Hall
 * 
 * Created: 30/11/17
 * Last Modified: 01/12/17
 * 
 * Description: The node object
 * 
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpecialForces
{
    public class Node
    {
        // This node's positions in the grid
        public int x;
        public int y;
        public int z;

        // Cost of moving to this node
        public float hCost; // TODO are floats hCost and gCost the horizontal and vertical, if yes rename
        public float gCost;

        public float fCost
        {
            get
            {
                return gCost + hCost;
            }
        }

        // Next node
        public Node parentNode;
        public bool isWalkable = true;

        // Reference to the world object so we can get the world position of the node
        public GameObject worldObject;

        public void ChangeNodeStatus(bool walkable)
        {

            if (worldObject != null)
            {
                GridBase grid = GridBase.singleton;

                worldObject.GetComponentInChildren<Renderer>().material =
                    (walkable == true) ? grid.debugMaterial : grid.unwalkableMaterial;
            }

            isWalkable = walkable;
        }

        // Different types of terrain 
        public NodeType nodeType;
        public enum NodeType
        {
            ground,
            air
        }
    }
}


