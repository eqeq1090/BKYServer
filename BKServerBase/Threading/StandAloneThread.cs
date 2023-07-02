using System.Threading;

namespace BKServerBase.Threading
{
    public class StandAloneThread
    {
        public readonly int ThreadID;
        private volatile bool m_Running;
        private Command? m_Command;
        private Thread? ThreadHandle = null;
        public StandAloneThread(int threadID, Command command)
        {
            ThreadID = threadID;
            m_Running = false;
            m_Command = command;
        }

        public void Start()
        {
            if (!m_Running)
            {
                m_Running = true;

                ThreadHandle = new Thread(new ThreadStart(ProcCommandPumping));
                ThreadHandle.Start();
            }
            else
            {
            }
        }

        public void Stop(bool join = false)
        {
            if (m_Running == true)
            {
                m_Running = false;
                if (join)
                {
                    ThreadHandle?.Join();
                }
                else
                {

                }
            }
        }

        public void ProcCommandPumping()
        {
            while (true)
            {
                if (m_Command != null)
                {
                    m_Command.Invoke();
                }

                if (!m_Running)
                {
                    break;
                }
            }
        }
    }
}
