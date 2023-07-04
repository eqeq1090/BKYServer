
using System.Reflection;
using System.Runtime.InteropServices;

namespace BKServer;

public static class MiniDump
{

    [DllImport("Dbghelp.dll")]
    static extern bool MiniDumpWriteDump(IntPtr hProcess, uint ProcessId, IntPtr hFile, int DumpType, ref MINIDUMP_EXCEPTION_INFORMATION ExceptionParam, IntPtr UserStreamParam, IntPtr CallbackParam);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll")]
    static extern uint GetCurrentProcessId();

    [DllImport("kernel32.dll")]
    static extern uint GetCurrentThreadId();

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MINIDUMP_EXCEPTION_INFORMATION
    {
        public uint ThreadId;
        public IntPtr ExceptionPointers;
        public int ClientPointers;
    }

    const int MiniDumpNormal = 0x00000000; // 최소한의 스택 정보만 남기는 플래그
    const int MiniDumpWithFullMemory = 0x00000002; // 모든 스택 정보와, 스레드, 메모리 상태 정보를 남기는 플래그

    public static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        // 덤프 파일 이름(각자 원하는대로 변경)
        string dirPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
        string exeName = AppDomain.CurrentDomain.FriendlyName;
        string dateTime = DateTime.Now.ToString("[yyyy-MM-dd][HH-mm-ss-fff]");

        MINIDUMP_EXCEPTION_INFORMATION info = new MINIDUMP_EXCEPTION_INFORMATION();
        info.ClientPointers = 1;
        info.ExceptionPointers = Marshal.GetExceptionPointers();
        info.ThreadId = GetCurrentThreadId();

        {   // 최소한의 스택 정보만 가진 코어 덤프 파일 생성
            string dumpFileFullName = dirPath + "/[" + exeName + "_mini]" + dateTime + ".dmp";
            FileStream file = new FileStream(dumpFileFullName, FileMode.Create);
            MiniDumpWriteDump(GetCurrentProcess(), GetCurrentProcessId(), file.SafeFileHandle.DangerousGetHandle(), MiniDumpNormal, ref info, IntPtr.Zero, IntPtr.Zero);
            file.Close();
        }

        {   // 스택 정보 뿐 아니라 스레드, 메모리 상태 등 남길 수 있는 모든 정보를 가진 코어 덤프 생성
            string dumpFileFullName = dirPath + "/[" + exeName + "]" + dateTime + ".dmp";
            FileStream file = new FileStream(dumpFileFullName, FileMode.Create);
            MiniDumpWriteDump(GetCurrentProcess(), GetCurrentProcessId(), file.SafeFileHandle.DangerousGetHandle(), MiniDumpWithFullMemory, ref info, IntPtr.Zero, IntPtr.Zero);
            file.Close();
        }
    }
}