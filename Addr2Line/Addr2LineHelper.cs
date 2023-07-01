using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Addr2Line
{
    internal static class Addr2LineHelper
    {
        public static void Main(string[] args)
        {
            var _curPath = Directory.GetCurrentDirectory();
            var _configPath = Path.Combine(_curPath, Addr2LineConst.CONFIG_NAME);
            if (!File.Exists(_configPath))
                throw new NullReferenceException($"Can't Find Config File, Please Make Config File :: {_configPath} :: Config.txt");

            var _result = ReadConfig(_configPath, out var _addr2LinePath);
            if (!_result)
                throw new Exception(_addr2LinePath);
            
            using var _sr = new StreamReader(Console.OpenStandardInput());
            var _sb = new StringBuilder();
            while (true)
            {
                if (_sr.EndOfStream)
                    break;
                
                var _input = _sr.ReadLine();
                if (string.IsNullOrEmpty(_input))
                    break;

                var _inputToArr = _input.Split();
                var _stackIdx = _inputToArr[Addr2LineConst.STACK_INDEX];
                var _platform = _inputToArr[Addr2LineConst.PLATFORM_INDEX];
                var _address = _inputToArr[Addr2LineConst.ADDRESS_INDEX];
                if (_inputToArr.Length <= 3)
                {
                    _sb.AppendLine(StringHelper.Get(" ",_stackIdx, _platform, Addr2LineConst.LESS_IDX_TEXT));
                    continue;
                }
                
                var _so = _inputToArr[Addr2LineConst.SO_NAME_INDEX];
                
                var _soFullPath = GetSoPath(_so, _curPath);
                if (!File.Exists(_soFullPath))
                {
                    _sb.AppendLine(StringHelper.Get(" ",_stackIdx, _platform, Addr2LineConst.NO_SO_TEXT));
                    continue;
                }
                var _stack = GetStack(Path.Combine(_curPath, _soFullPath), _address, _addr2LinePath);
                if (string.IsNullOrEmpty(_stack))
                {
                    _sb.AppendLine(StringHelper.Get(" ", _stackIdx, _platform, Addr2LineConst.NO_SO_TEXT));
                    continue;
                }

                _stack = FilterLog(_stack);
                var _fullStack = StringHelper.Get(" ", _stackIdx, _platform, _stack);
                if (string.IsNullOrEmpty(_fullStack))
                {
                    _sb.AppendLine(StringHelper.Get(" ", _stackIdx, _platform, Addr2LineConst.NO_SO_TEXT));
                    continue;
                }
                
                _sb.AppendLine(_fullStack);
            }

            var _deskTop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var _resultTextName = Path.Combine(_deskTop, $"{DateTime.Now:yy-mm-dd}-{Addr2LineConst.RESULT_TEXT_NAME}");
            File.WriteAllText(_resultTextName, _sb.ToString());
        }

        private static string GetSoPath(string so, string curPath)
        {
            var _soToArr = so?.Split('.');
            return _soToArr == null ? string.Empty : Path.Combine(curPath,$"{_soToArr[0]}.sym.so");
        }
        
        private static string GetStack(string so, string address, string addr2Path)
        {
            var _pi = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                FileName = Addr2LineConst.CMD_EXE,
            };
            
            using var _proc = new Process();
            _proc.StartInfo = _pi;
            _proc.Start();
            
            _proc.StandardInput.WriteLine($"cd {GetAddrPathNonExe(addr2Path)}");
            _proc.StandardInput.WriteLine($"{GetCmdArg(so)}");
            _proc.StandardInput.WriteLine($"{address}");
            _proc.StandardInput.Flush();
            _proc.StandardInput.Close();
            var _output = _proc.StandardOutput.ReadToEnd();
            _proc.WaitForExit();
            _proc.Close();
            return _output;
        }

        private static string GetAddrPathNonExe(string addrFullPath) => addrFullPath.Replace($"\\{Addr2LineConst.ADDR2_NAME}", string.Empty);
        private static string FilterLog(string log)
        {
            var _arr = log?.Split('\r', '\n').Where(x => !x.Equals(string.Empty)).ToArray();
            if (_arr == null)
                return string.Empty;

            var _len = _arr.Length;
            return StringHelper.Get("  ",_arr[_len - 3], _arr[_len - 2]);
        }

        private static string GetCmdArg(string soPath) =>
            StringHelper.Get(" ", Addr2LineConst.ADDR2_NAME, "-f", "-C", "-e", $"{soPath}");

        private static bool ReadConfig(string path, out string addr2LinePath)
        {
            try
            {
                addr2LinePath = File.ReadAllText(path);
                return true;
            }
            catch (Exception e)
            {
                addr2LinePath = e.ToString();
                return false;
            }
        }

        private static class StringHelper
        {
            private static StringBuilder m_Sb;

            public static string Get(string join, params string[] arg)
            {
                var _lenOrNull = arg?.Length;
                if (!_lenOrNull.HasValue)
                {
                    throw new ArgumentException("The Arg Is Null");
                }

                if (m_Sb == null)
                    m_Sb = new StringBuilder();
                else
                    Clear();

                var _len = _lenOrNull.Value;
                for (var i = 0; i < _len; i++)
                {
                    m_Sb.Append(i == _len - 1 ? 
                        $"{arg[i]}" : 
                        $"{arg[i]}{join}");
                }

                return m_Sb.ToString();
            }

            private static void Clear() => m_Sb?.Clear();
        }
    }
}