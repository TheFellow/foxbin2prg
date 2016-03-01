using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xx2lib
{
    public static class TreeInfo
    {
        public static List<string> GroupTree(string[] paths)
        {
            // First count how many times each path step occurs in the list
            var pathCount = new Dictionary<string, int>();

            foreach(string path in paths)
            {
                // Think: 1.2.3.4
                int dot = path.IndexOf('.');

                AddPath(path, pathCount);

                while (dot > 0)
                {
                    string currentPath = path.Substring(0, dot);

                    AddPath(currentPath, pathCount);
                    AddPath(currentPath, pathCount); // hack to count all of these

                    // Find the next dot
                    dot = path.IndexOf('.', dot + 1);
                }
            }

            // Now filter out the ones which are leaves
            return new List<string>(pathCount.Keys.Where(x => pathCount[x] > 1).OrderByDescending(x => x.Split('.').Length));
        }

        public static void AddPath(string path, Dictionary<string, int> dict)
        {
            if (!dict.ContainsKey(path))
                dict[path] = 0;

            dict[path]++;
        }
    }
}
