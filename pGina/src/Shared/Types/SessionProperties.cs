﻿/*
	Copyright (c) 2014, pGina Team
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

namespace pGina.Shared.Types
{
    public class SessionProperties
    {
        private Dictionary<string, object> m_sessionDictionary = new Dictionary<string, object>();
        private Guid m_sessionId = Guid.Empty;
        public Guid Id
        {
            get { return m_sessionId; }
            set { m_sessionId = value; }
        }

        private bool m_credui = false;
        public bool CREDUI
        {
            get { return m_credui; }
            set { m_credui = value; }
        }

        public SessionProperties(Guid sessionId, bool credui)
        {
            m_sessionId = sessionId;
            m_credui = credui;
            AddTrackedObject("SessionId", new Guid(m_sessionId.ToString()));
            AddTrackedObject("CREDUI", m_credui.ToString());
        }

        public SessionProperties(Guid sessionId)
        {
            m_sessionId = sessionId;
            m_credui = false;
            AddTrackedObject("SessionId", new Guid(m_sessionId.ToString()));
            AddTrackedObject("CREDUI", m_credui.ToString());
        }

        public void AddTracked<T>(string name, T value)
        {
            AddTrackedObject(name, value);
        }

        public void AddTrackedSingle<T>(T value)
        {
            AddTrackedObject(typeof(T).ToString(), value);
        }

        public void AddTrackedObject(string name, object value)
        {
            lock(this)
            {
                if (!m_sessionDictionary.ContainsKey(name))
                    m_sessionDictionary.Add(name, value);
                else
                    m_sessionDictionary[name] = value;
            }
        }

        internal T GetTracked<T>(string name)
        {
            return (T)GetTrackedObject(name);
        }

        public T GetTrackedSingle<T>()
        {
            object ret = GetTracked<T>(typeof(T).ToString());
            if (ret == null)
                return (T)Activator.CreateInstance(typeof(T));

            return (T)ret;
        }

        public object GetTrackedObject(string name)
        {
            lock(this)
            {
                if(!m_sessionDictionary.ContainsKey(name))
                    return null;

                return m_sessionDictionary[name];
            }
        }
    }
}
