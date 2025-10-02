using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace NexoDirectorStudio.Orchestration
{
    public static class HashUtil
    {
        public static string StableHash(object o)
        {
            var json = JsonUtility.ToJson(o ?? new object(), true);
            using var md5 = MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(json));
            var sb = new StringBuilder();
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
