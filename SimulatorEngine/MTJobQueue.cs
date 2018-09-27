﻿//==============================================================================
// Project:     Trading Simulator
// Name:        MTJobQueue
// Description: multi-threaded job queue to support optimizer
// History:     2018ix20, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

//#define NO_THREADS
// when NO_THREADS is defined, QueueJob translates to a plain function call

//#define SINGLE_THREAD
// with SINGLE_THREAD defined, only one worker thread will be used

#region libraries
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#endregion

namespace FUB_TradingSim
{
    class MTJobQueue
    {
        #region internal data
        private readonly object _queueLock = new object();
        private readonly Queue<Thread> _jobQueue = new Queue<Thread>();
        private int _jobsRunning = 0;
        #endregion

        #region private int MaximumNumberOfThreads
        private int MaximumNumberOfThreads
        {
            get
            {
#if SINGLE_THREAD
                return 1;
#else
                // https://stackoverflow.com/questions/1542213/how-to-find-the-number-of-cpu-cores-via-net-c
                return Environment.ProcessorCount; // number of logical processors
#endif
            }
        }
        #endregion
        #region private void CheckQueue()
        private void CheckQueue()
        {
            lock(_queueLock)
            {
                while (_jobsRunning < MaximumNumberOfThreads
                && _jobQueue.Count > 0)
                {
                    _jobsRunning++;
                    Thread nextThread = _jobQueue.Dequeue();
                    nextThread.Start();
                }
            }
        }
        #endregion
        #region private void JobRunner(Action job)
        private void JobRunner(Action job)
        {
            job();

            lock(_queueLock)
            {
                _jobsRunning--;
            }

            CheckQueue();
        }
        #endregion

        #region public void QueueJob(Action job)
        public void QueueJob(Action job)
        {
#if NO_THREADS
            job();
#else
            lock(_queueLock)
            {
                Thread queuedThread = new Thread(() => JobRunner(job));
                _jobQueue.Enqueue(queuedThread);
            }

            CheckQueue();
#endif
        }
        #endregion
        #region public void WaitForCompletion()
        public void WaitForCompletion()
        {
#if NO_THREADS
            // nothing to do
#else
            int? jobsToDo = null;

            do
            {
                if (jobsToDo != null)
                    Thread.Sleep(500);

                lock (_queueLock)
                {
                    jobsToDo = _jobQueue.Count + _jobsRunning;
                }
            } while (jobsToDo > 0);
#endif
        }
        #endregion
    }
}

//==============================================================================
// end of file