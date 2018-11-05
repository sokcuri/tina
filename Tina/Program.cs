// Program.cs
// Adapted from code by Justin Stenning
// Licensed under the terms of the MIT License1

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ManyConsole;

public struct PROCESS_INFORMATION
{
    public IntPtr hProcess;
    public IntPtr hThread;
    public int dwProcessId;
    public int dwThreadId;
}

public struct STARTUPINFO
{
    public uint cb;
    public string lpReserved;
    public string lpDesktop;
    public string lpTitle;
    public uint dwX;
    public uint dwY;
    public uint dwXSize;
    public uint dwYSize;
    public uint dwXCountChars;
    public uint dwYCountChars;
    public uint dwFillAttribute;
    public uint dwFlags;
    public short wShowWindow;
    public short cbReserved2;
    public IntPtr lpReserved2;
    public IntPtr hStdInput;
    public IntPtr hStdOutput;
    public IntPtr hStdError;
}

public struct SECURITY_ATTRIBUTES
{
    public int length;
    public IntPtr lpSecurityDescriptor;
    public bool bInheritHandle;
}

namespace Tina
{
    public class PrintFileCommand : ConsoleCommand
    {
        private const int Success = 0;
        private const int Failure = 2;

        public string FileLocation { get; set; }
        public string WatchFileLocation { get; set; }
        public bool StripCommaCharacter { get; set; }

        public PrintFileCommand()
        {
            // Register the actual command with a simple (optional) description.
            IsCommand("Watch", "Watch file utility");
            HasLongDescription("Watch the file and print it to the console.");

            // Required options/flags, append '=' to obtain the required value.
            HasRequiredOption("f|file=", "The full path of the file to run.", p => Program.ExecutePathArgs = p);
            HasRequiredOption("w|watch=", "Specifies the file to watch for output to the console.", p => Program.WatchFilePath = p);
        }
        public override int Run(string[] remainingArguments)
        {
            return 0;
        }
    }
    class Program
    {
        static public string WatchFilePath { get; set; }
        static public string ExecutePathArgs { get; set; }

        [DllImport("Kernel32.dll")]
        private static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, CreateProcessFlags dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        enum CreateProcessFlags : uint
        {
            CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
            CREATE_DEFAULT_ERROR_MODE = 0x04000000,
            CREATE_NEW_CONSOLE = 0x00000010,
            CREATE_NEW_PROCESS_GROUP = 0x00000200,
            CREATE_NO_WINDOW = 0x08000000,
            CREATE_PROTECTED_PROCESS = 0x00040000,
            CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
            CREATE_SEPARATE_WOW_VDM = 0x00000800,
            CREATE_SHARED_WOW_VDM = 0x00001000,
            CREATE_SUSPENDED = 0x00000004,
            CREATE_UNICODE_ENVIRONMENT = 0x00000400,
            DEBUG_ONLY_THIS_PROCESS = 0x00000002,
            DEBUG_PROCESS = 0x00000001,
            DETACHED_PROCESS = 0x00000008,
            EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
            INHERIT_PARENT_AFFINITY = 0x00010000
        }

        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

        const UInt32 INFINITE = 0xFFFFFFFF;
        const UInt32 WAIT_ABANDONED = 0x00000080;
        const UInt32 WAIT_OBJECT_0 = 0x00000000;
        const UInt32 WAIT_TIMEOUT = 0x00000102;

        public static IEnumerable<ConsoleCommand> GetCommands()
        {
            return ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof(Program));
        }

        static int Main(string[] args)
        {
            var commands = GetCommands();
            ConsoleCommandDispatcher.DispatchCommand(commands, args, System.IO.TextWriter.Null, true);
            if (WatchFilePath == null || ExecutePathArgs == null)
            {
                Console.WriteLine(@"Expected usage: Tina Watch <options>
<options> available:
  -f, --file=VALUE           The full path of the file to run.
  -w, --watch=VALUE          Specifies the file to watch for output to the
                               console.");
                return -1;
            }

            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            STARTUPINFO si = new STARTUPINFO();

            if (!CreateProcess(null, ExecutePathArgs, IntPtr.Zero, IntPtr.Zero, false, CreateProcessFlags.CREATE_SUSPENDED, IntPtr.Zero, null, ref si, out pi))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("There was an error CreateProcess");
                Console.ResetColor();
                return -2;
            }
            
            Console.WriteLine("Tina Starting! Attempting to create and inject process.");
            
            // Will contain the name of the IPC server channel
            string channelName = null;

            // Create the IPC server using the FileMonitorIPC.ServiceInterface class as a singleton
            TinaHook.ServerInterface.WatchFile = WatchFilePath;
            EasyHook.RemoteHooking.IpcCreateServer<TinaHook.ServerInterface>(ref channelName, System.Runtime.Remoting.WellKnownObjectMode.Singleton);
            
            // Get the full path to the assembly we want to inject into the target process
            string injectionLibrary = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "TinaHook.dll");

            // start and inject into a new process
            try
            {
                EasyHook.RemoteHooking.Inject(
                    pi.dwProcessId,
                    EasyHook.InjectionOptions.DoNotRequireStrongName, // allow injectionLibrary to be unsigned
                    injectionLibrary,   // 32-bit library to inject (if target is 32-bit)
                    injectionLibrary,   // 64-bit library to inject (if target is 64-bit)
                    channelName         // the parameters to pass into injected library
                                        // ...
                );
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("There was an error while injecting into target:");
                Console.ResetColor();
                Console.WriteLine(e.ToString());
                return -3;
            }

            ResumeThread(pi.hThread);
            WaitForSingleObject(pi.hProcess, INFINITE);

            uint exit_code;
            GetExitCodeProcess(pi.hProcess, out exit_code);
            return (int)exit_code;
        }
    }
}
