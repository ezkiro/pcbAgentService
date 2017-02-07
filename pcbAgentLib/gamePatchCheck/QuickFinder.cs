using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace pcbAgentLib.gamePatchCheck
{
    /// <summary>
    /// directory 목록을 받아서 그 directory 목록에서만 file을 검색한다.
    /// </summary>
    public class QuickFinder
    {
        private string _targetFile;
        private List<string> _targetPaths; //drive 정보가 없다.

        public QuickFinder(string targetFile, List<string> targetPaths)
        {
            this._targetFile = targetFile;
            this._targetPaths = targetPaths;
        }

        public List<string> buildFullPath(string localDrive)
        {
            List<string> fullPaths = new List<string>();

            foreach (string filePath in _targetPaths)
            {
                fullPaths.Add(localDrive + filePath);
            }

            return fullPaths;
        }

        public string searchFile(string localDrive)
        {
            List<string> fullPaths = buildFullPath(localDrive);

            foreach (string filePath in fullPaths)
            {
                Console.WriteLine("[searchFile] filePath:'{0}'", filePath);
                try
                {
                    if (!Directory.Exists(filePath)) continue;

                    foreach (string file in Directory.GetFiles(filePath, _targetFile))
                    {
                        Console.WriteLine("[searchFile] found file '{0}'", file);
                        return file;
                    }
                }
                catch (System.Exception excpt)
                {
                    Console.WriteLine("[searchFile] exception {0}:{1}", excpt.GetType(), excpt.Message);
                    //exception 발생하면 다음 drive 로 skip 한다.
                    continue;
                }
            }

            return null;
        }

        public string searchRFile(string localDrive)
        {
            List<string> fullPaths = buildFullPath(localDrive);

            foreach (string filePath in fullPaths)
            {
                Console.WriteLine("[searchRFile] filePath:'{0}'", filePath);
                try
                {
                    if (!Directory.Exists(filePath))
                    {
                        Console.WriteLine("[searchRFile] not exist file path:'{0}'", filePath);
                        continue;
                    }

                    //filePath에서 한번 찾아보고 없으면 sub 디렉토리로 검색한다.
                    foreach (string file in Directory.GetFiles(filePath, _targetFile))
                    {
                        Console.WriteLine("[searchRFile] found file '{0}'", file);
                        return file;
                    }

                    return FileFinder.findInSubDir(filePath, _targetFile);

                }
                catch (System.Exception excpt)
                {
                    Console.WriteLine("[searchRFile] exception {0}:{1}", excpt.GetType(), excpt.Message);
                    //exception 발생하면 다음 drive 로 skip 한다.
                    continue;
                }
            }

            return null;
        }


        public string findInAllDrive()
        {
            if (_targetPaths.Count == 0) return null;

            //local disk 정보를 가져와서 반복 수행
            foreach (string localDrive in Directory.GetLogicalDrives())
            {
                Console.WriteLine("[findInAllDrive] local drive:{0}", localDrive);

                try
                {
                    //skip c: drive
                    if (localDrive.Contains("c:\\") || localDrive.Contains("C:\\")) continue;

                    string foundFile = searchFile(localDrive);

                    //하나라도 찾으면 바로 빠져나온다.
                    if (foundFile != null) return foundFile;
                }
                catch (System.Exception excpt)
                {
                    Console.WriteLine("[findInAllDrive] exception {0}:{1}", excpt.GetType(), excpt.Message);
                    //exception 발생하면 다음 drive 로 skip 한다.
                    continue;
                }
            }

            return null;
        }

        public string findRInAllDrive()
        {
            if (_targetPaths.Count == 0) return null;

            //local disk 정보를 가져와서 반복 수행
            foreach (string localDrive in Directory.GetLogicalDrives())
            {
                Console.WriteLine("[findRInAllDrive] local drive:{0}", localDrive);

                try
                {
                    //skip c: drive
                    if (localDrive.Contains("c:\\") || localDrive.Contains("C:\\")) continue;

                    string foundFile = searchRFile(localDrive);

                    //하나라도 찾으면 바로 빠져나온다.
                    if (foundFile != null) return foundFile;
                }
                catch (System.Exception excpt)
                {
                    Console.WriteLine("[findRInAllDrive] exception {0}:{1}", excpt.GetType(), excpt.Message);
                    //exception 발생하면 다음 drive 로 skip 한다.
                    continue;
                }
            }

            return null;
        }
    }
}
