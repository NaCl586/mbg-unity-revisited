using System;
using System.Collections.Generic;
using UnityEngine;

namespace TS
{
    public class TSObject : ScriptableObject
    {
        public TSObject Parent;
        public List<TSObject> Children = new List<TSObject>();
        public string ClassName = "";
        public string Name = "";
        public Dictionary<string, string> Fields = new Dictionary<string, string>();

        public IEnumerable<TSObject> RecursiveChildren()
        {
            yield return this;

            foreach (var child in Children)
            {
                foreach (var i in child.RecursiveChildren())
                {
                    yield return i;
                }
            }
        }

        public string GetField(string field)
        {
            return Fields.TryGetValue(field.ToLower(), out string value) ? value : "";
        }
    }
}