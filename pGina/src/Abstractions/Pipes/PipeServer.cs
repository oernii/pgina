﻿/*
	Copyright (c) 2011, pGina Team
	All rights reserved.

	Redistribution and use in source and binary forms, with or without
	modification, are permitted provided that the following conditions are met:
		* Redistributions of source code must retain the above copyright
		  notice, this list of conditions and the following disclaimer.
		* Redistributions in binary form must reproduce the above copyright
		  notice, this list of conditions and the following disclaimer in the
		  documentation and/or other materials provided with the distribution.
		* Neither the name of the pGina Team nor the names of its contributors 
		  may be used to endorse or promote products derived from this software without 
		  specific prior written permission.

	THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
	ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
	WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
	DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY
	DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
	(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
	LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
	ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
	(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
	SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;

using Abstractions.Logging;

namespace Abstractions.Pipes
{
    public class PipeServer : Pipe
    {
        public int MaxClients { get; private set; }
        
        private Thread[] m_serverThreads = null;
        private bool m_running = false;
        private bool Running
        {
            get { lock (this) { return m_running; } }
            set { lock (this) { m_running = value; } }
        }
        
        public PipeServer(string name, int maxClients, Func<BinaryReader, BinaryWriter, bool> action) 
            : base(name, action)
        {
            MaxClients = maxClients;
        }

        public PipeServer(string name, int maxClients, Func<dynamic, dynamic> action)
            : base(name, action) 
        {
            MaxClients = maxClients;
        }

        public void Start()
        {
            StartServerThreads();
        }

        public void Stop()
        {
            StopServerThreads();
        }

        private void StartServerThreads()
        {
            if (Running)
                return;

            lock (this)
            {
                Running = true;

                m_serverThreads = new Thread[MaxClients];
                for (int x = 0; x < MaxClients; x++)
                {
                    m_serverThreads[x] = new Thread(new ThreadStart(ServerThread));
                    m_serverThreads[x].Start();
                }
            }
        }

        private void StopServerThreads()
        {
            if (!Running)
                return;

            Running = false;
            
            // Some or all of our threads may be blocked waiting for connections,
            // this is a bit nasty, but since I can't seem to get the async 
            // wait working, we do this - poke em! 
            for (int x = 0; x < MaxClients; x++)
            {
                FakeClientToWakeEmAndShakem();
            }

            for (int x = 0; x < MaxClients; x++)
            {
                m_serverThreads[x].Join();
            }

            m_serverThreads = null;            
        }
        
        private void ServerThread()
        {                        
            PipeSecurity security = new PipeSecurity();                
            // Anyone can talk to us
            security.AddAccessRule(new PipeAccessRule("Users", PipeAccessRights.ReadWrite, AccessControlType.Allow)); 
            // But only we have full control (including the 'create' right, which allows us to be the server side of this equation)
            security.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().Owner, PipeAccessRights.FullControl, AccessControlType.Allow));

            while (Running)
            {
                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(Name, PipeDirection.InOut, MaxClients,
                        PipeTransmissionMode.Byte, PipeOptions.WriteThrough, 0, 0, security, HandleInheritability.None))
                {
                    try
                    {
                        pipeServer.WaitForConnection();
                    }
                    catch (Exception e)
                    {
                        LibraryLogging.Error("Error in server connection handler: {0}", e);
                        continue;
                    }

                    // Handle this connection, note that we always expect client to initiate the
                    //  flow of messages, so we do not include an initial message
                    HandlePipeConnection(pipeServer, null);
                }
            }
        }

        private void FakeClientToWakeEmAndShakem()
        {
            try
            {
                PipeClient client = new PipeClient(Name);
                client.Start(((r, w) => { return false; }), null, 100);                
            }
            catch { /* intentionally ignored */ }
        }
    }
}