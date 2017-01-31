using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace pcbAgentLib.gamePatchCheck
{
    /// <summary>
    /// 대상 game의 exe 들을 찾는 기능, 성능을 위해서 여러 파일을 동시에 찾는다.
    /// </summary>
    public class FileFinder
    {
        private string _fileExt;
        private List<string> _targetFiles;
        private List<string> _foundFiles;

        public FileFinder(string fileExt,  List<string> targetFiles)
        {
            this._fileExt = fileExt;
            this._targetFiles = targetFiles;
            this._foundFiles = new List<string>();
        }

        /// <summary>
        /// 지정 디텍토리로부터 하위 디렉토리까지 모두 뒤지면서 검색하는 공용 함수
        /// </summary>
        public static string findOneFilePath(string path, string fileName)
        {
            foreach (string dir in Directory.GetDirectories(path))
            {
                try
                {
                    foreach (string file in Directory.GetFiles(dir, fileName))
                    {
                        Console.WriteLine("[findOneFilePath] found file '{0}'", file);
                        return file;
                    }
                }
                catch (System.Exception excpt)
                {
                    Console.WriteLine("[findOneFilePath] exception {0}:{1}", excpt.GetType(), excpt.Message);
                    //exception 발생하면 다음 dir 로 skip 한다.
                    continue;
                }

                findOneFilePath(dir, fileName);
            }

            return null;
        }

        private bool isFoundAllFiles()
        {
            if (_foundFiles.Count == _targetFiles.Count) return true;
            else return false;
        }

        private bool filterFile(string file)
        {
            foreach(string target in _targetFiles)
            {
                if (file.EndsWith(target)) return true;
            }

            return false;
        }

        private void DirSearch(string sDir)
        {
            //game이 설치되어 있지 않을 디렉토리는 top 레벨에서 제거한다.  LINQ 구문 굿!!!
            var dirs = Directory.GetDirectories(sDir).Where(s => !(s.StartsWith("C:\\Windows") || s.StartsWith("C:\\ProgramData") || s.StartsWith("C:\\Users")));

            foreach (string dir in dirs)
            {
//                Console.WriteLine("[FileFinder] dir:{0}", dir);

                try
                {
                    //대상 exe 파일들을 모두 대조해본다.  LINQ 구문 굿!!!
                    var files = Directory.GetFiles(dir, _fileExt).Where(s => filterFile(s));

                    foreach (string file in files)
                    {
                        //Console.WriteLine("[FileFinder] found file '{0}'", file);
                        _foundFiles.Add(file);
                    }

                    //모두 찾으면 리턴
                    if (isFoundAllFiles())
                    {
                        return;
                    }
                }
                catch (System.Exception excpt)
                {
                    Console.WriteLine("[FileFinder] exception {0}:{1}", excpt.GetType(), excpt.Message);
                    //exception 발생하면 다음 dir 로 skip 한다.
                    continue;
                }

                DirSearch(dir);
            }
        }

        public List<string> findFilePath()
        {
            if (_targetFiles.Count == 0 ) return _foundFiles;

            //local disk 정보를 가져와서 반복 수행
            foreach (string localDrive in Directory.GetLogicalDrives())
            {
                Console.WriteLine("[FileFinder] local drive:{0}", localDrive);

                try
                {
                    //skip c: drive
                    if (localDrive.Contains("c:\\") || localDrive.Contains("C:\\")) continue;

                    DirSearch(localDrive);
                }
                catch (System.Exception excpt)
                {
                    Console.WriteLine("[FileFinder] exception {0}:{1}", excpt.GetType(), excpt.Message);
                    //exception 발생하면 다음 drive 로 skip 한다.
                    continue;
                }

                if (isFoundAllFiles()) break;
            }

            return _foundFiles;
        }
    }
}
