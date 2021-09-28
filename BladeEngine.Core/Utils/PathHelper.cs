﻿namespace BladeEngine.Core.Utils
{
    public static class PathHelper
    {
        public static string Refine(string path)
        {
            path = path.Replace("\\", "/");

            var arr = path.Split(new char[] { '/' });
            var s = new OpenStack<string>();
            var i = 0;

            foreach (var p in arr)
            {
                if (p == "..")
                {
                    if (s.Count == 0 || s.Peek() == "..")
                    {
                        s.Push(p);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(s.Peek()))
                        {
                            s.Pop();
                            s.Push(p);
                        }
                        else
                        {
                            s.Pop();
                        }
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(p))
                    {
                        if (i == 0)
                        {
                            s.Push(p);
                        }
                    }
                    else if (p.Trim() != ".")
                    {
                        s.Push(p);
                    }
                }

                i++;
            }

            var result = "";

            foreach (var p in s)
            {
                result = p + (string.IsNullOrEmpty(result) ? "" : "/") + result;
            }

            return result;
        }
    }
}
