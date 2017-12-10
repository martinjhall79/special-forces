using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpecialForces
{
    public class UnitManager : MonoBehaviour
    {
        // List of nodes on path
        List<GameManager.PathInfo> path; // Todo access PathInfo
        int pathIndex; // Track current node unit is on
        float moveT; // Move unit towards target node
        float rotateT; // Rotate to target node
        float speed;
        Vector3 startPosition;
        Vector3 endPosition; // Target node
        bool initLerp; // Start Lerp for moving
        public bool isMoving; // Is the unit moving

        public int actionPoints = 20; // Movement cost
        public float walkSpeed = 2;
        public float rotationSpeed = 8;

        Animator anim; // Unit animations

        // Memory optimisation, check node with a pointer to save memory
        public Node node
        {
            get
            {
                return GridBase.singleton.GetNodeFromWorldPosition(transform.position);
            }
        }

        // Previous node on path
        Node prevNode;

        // Start positions
        public int xStart, zStart;

        public void Init()
        {
            Vector3 startPos = GridBase.singleton.GetCoordsFromNode(xStart, 0, zStart); // Start player at arbitary tile 5,6 for debugging
            transform.position = startPos;
            node.ChangeNodeStatus(false);

            anim = GetComponentInChildren<Animator>();
            anim.applyRootMotion = false;
        }

        private void Update()
        {
            // Update
            if (isMoving)
            {
                Moving();
                // Animate unit move
                anim.SetFloat("Vertical", 1, 0.2f, Time.deltaTime);
            }
            else
            {
                anim.SetFloat("Vertical", 0, 0.4f, Time.deltaTime);
            }
        }

        // Movement
        void Moving()
        {
            
            if (!initLerp)
            {
                // Reached the destination, stop moving
                if (pathIndex == path.Count)
                {
                    isMoving = false;
                    return;
                }

                // Moving, make the current node walkable
                node.ChangeNodeStatus(true);
                // Reset movement vars
                moveT = 0;
                rotateT = 0;
                startPosition = this.transform.position;
                endPosition = path[pathIndex].targetPosition;
                // Calculate distance to keep speed constant for diagonal / straight moves
                float distance = Vector3.Distance(startPosition, endPosition);
                speed = walkSpeed / distance; // Todo add running speed
                initLerp = true;
            }

            moveT += Time.deltaTime * speed;

            if (moveT > 1)
            {
                moveT = 1;
                initLerp = false;
                DecreaseAP(path[pathIndex]);
                // If unit hasn't reached destination node, increment path index
                if (pathIndex < path.Count - 1)
                {
                    pathIndex++;
                }
                else // Destination reached
                {
                    isMoving = false; // Stop moving
                }
                // Move unit
                Vector3 newPosition = Vector3.Lerp(startPosition, endPosition, moveT);
                transform.position = newPosition;
                // Rotate unit, ready for the next move
                rotateT += Time.deltaTime * rotationSpeed;

                Vector3 lookDirection = endPosition - startPosition;
                lookDirection.y = 0;
                if (lookDirection == Vector3.zero)
                    lookDirection = transform.forward;
                Quaternion targetRot = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateT);
            }
        }

        void DecreaseAP(GameManager.PathInfo p)
        {
            // Todo enemy AI and interuption checks
            actionPoints -= p.ap;
            node.ChangeNodeStatus(false);
        }

        // Populate the path
        public void AddPath(List<GameManager.PathInfo> p)
        {
            pathIndex = 1;
            path = p;
            isMoving = true;
        }
    }
}


