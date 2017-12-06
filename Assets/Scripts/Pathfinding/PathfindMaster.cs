using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SpecialForces;
using System.Threading;

namespace Pathfinding
{
    // This class controls the threads
    public class PathfindMaster : MonoBehaviour
    {
        //Singleton
        private static PathfindMaster instance;
        void Awake()
        {
            instance = this;
        }
        public static PathfindMaster GetInstance()
        {
            return instance;
        }

        // The maximum simultaneous threads we allow to open
        public int MaxJobs = 3;

        public delegate void PathfindingJobComplete(List<Node> path);

        private List<Pathfinder> currentJobs;
        private List<Pathfinder> toDoJobs;

        void Start()
        {
            currentJobs = new List<Pathfinder>();
            toDoJobs = new List<Pathfinder>();
        }

        void Update()
        {
            // We could just create a new thread to keep track of the threads we're running, but Unity's Update works fine

            int i = 0;

            // Check if the current job is complete
            while (i < currentJobs.Count)
            {
                if (currentJobs[i].jobDone)
                {
                    currentJobs[i].NotifyComplete();
                    currentJobs.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            if (toDoJobs.Count > 0 && currentJobs.Count < MaxJobs)
            {
                Pathfinder job = toDoJobs[0];
                toDoJobs.RemoveAt(0);
                currentJobs.Add(job);

                //Start a new thread

                Thread jobThread = new Thread(job.FindPath);
                jobThread.Start();

                // The C# built-in garbage collector clears the threads when they've finished
                //As per the doc
                //https://msdn.microsoft.com/en-us/library/system.threading.thread(v=vs.110).aspx
                //It is not necessary to retain a reference to a Thread object once you have started the thread. 
                //The thread continues to execute until the thread procedure is complete.				
            }
        }

        public void RequestPathFind(Node start, Node target, PathfindingJobComplete completeCallback)
        {
            Pathfinder newJob = new Pathfinder(start, target, completeCallback);
            toDoJobs.Add(newJob);
        }
    }
}