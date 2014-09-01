using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace Time_Updater
{
    namespace Collection
    {
        class Dictionary<T> : SortedDictionary<T, T>
        {
            public void Add(Pair<T> keyValPair)
            {
                this.Add(keyValPair[0], keyValPair[1]);
            }
        }
        class Pair<T> // Pair wraps two items of the same type
        {
            T[] content;
            internal Pair(T val1, T val2)
            {
                content = new T[2] { val1, val2 };
            }
            internal T this[int index]
            {
                get { return content[index]; }
                set { content[index] = value; }
            }

            public override string ToString()
            {
                return content[0] + " : " + content[1];
            }
        }

        class HTML_Element
        {
            string _name;
            string[] _attr, _val;

            // the start & end positions of an element,
            // e.g. abc<div>abc</div>abcabc
            //      ^                ^
            Pair<int> _rawStrPosition;
            public HTML_Element(Pair<int> rawStrPosition, string data)
            {
                _rawStrPosition = rawStrPosition;
                if (data == "ROOT")
                {
                    _name = "ROOT";
                    return;
                }

                _name = Regex.Match(data, "< *[^<=> ]+ *").Value.Replace("<", "").Trim();
                MatchCollection matches = Regex.Matches(data, " +([^<=> ]+ *= *('|\")[^<=>]+('|\"))");
                _attr = new string[matches.Count];
                _val = new string[matches.Count];

                for (int i = 0; i < matches.Count; i++)
                {
                    string[] attrVal = matches[i].Value.Split('=');

                    _attr[i] = attrVal[0].Trim(); _val[i] = attrVal[1].Trim();
                }
            }

            internal void CloseTag(int position)
            {
                _rawStrPosition[1] = position;
            }

            internal bool HasAttribute(Pair<string> attrVal)
            {
                if (attrVal == null)
                    return true;

                for (int i = 0; i < _attr.Length; i++)
                {
                    if (_attr[i] == null || _val[i] == null)
                        continue;
                    else if (_attr[i] == attrVal[0] && _val[i] == attrVal[1])
                        return true;
                }
                return false;
            }

            public bool IsRoot
            {
                get
                {
                    return _rawStrPosition[0] == int.MinValue
                        && _rawStrPosition[1] == int.MaxValue
                        && _name == "ROOT";
                }
            }

            public override string ToString()
            {
                return ":" + _name + ", " + FlattenArray(CombineArrays<string>(_attr, _val, "="), "; ");
            }

            private string[] CombineArrays<T>(T[] _attr, T[] _val, string sep)
            {
                string[] res = new string[_attr.Length];
                for (int i = 0; i < _attr.Length; i++)
                {
                    res[i] = _attr[i].ToString() + sep + _val[i].ToString();
                }
                return res;
            }

            internal static string FlattenArray(object[] _attr, string sep)
            {
                if (_attr.Length == 0)
                    return "";

                StringBuilder builder = new StringBuilder();
                foreach (object value in _attr)
                {
                    builder.Append(sep);
                    if (value != null)
                        builder.Append(value.ToString());
                }

                return builder.ToString().Substring(sep.Length);
            }

            public int StartIndex { get { return _rawStrPosition[0]; } }

            public int EndIndex { get { return _rawStrPosition[1]; } }
        }

        // A class that implements trees
        // any tree node has Sisters and Subtrees, and _subtrees is the same as Subtrees property
        // however, a tree node may lack _sisters, since all possible sisters are stored in the
        // FIRST child of their common parent
        class Tree<T>
        {
            T _value;
            Tree<T> _parent;
            List<Tree<T>> _subtrees = new List<Tree<T>>();
            List<Tree<T>> _sistrees;
            int _depth;

            internal Tree(T value, int depth)
            {
                _value = value;
                _depth = depth;
            }

            internal void AddSub(Tree<T> tree)
            {
                _subtrees.Add(tree);
                _subtrees[_subtrees.Count - 1].Parent = this;
                if (SubTrees.Count == 1 && _subtrees[0]._sistrees == null)
                {
                    _subtrees[0]._sistrees = new List<Tree<T>>();
                    // only the first child's sistrees can be initialized
                }
            }

            internal void AddSis(Tree<T> tree)
            {
                this.Parent._subtrees[0]._sistrees.Add(tree); // ALWAYS link sisters to the first child
                tree.Parent = this.Parent; // the parent needs to endorse the new child
            }

            public Tree<T> Parent { get { return _parent; } set { _parent = value; } }

            public List<Tree<T>> SubTrees { get { return _subtrees; } }


            internal T GetValue()
            {
                return _value;
            }

            public List<Tree<T>> Sisters
            {
                get // can only get from the first child
                {
                    return this.Parent._subtrees[0]._sistrees;
                }
            }

            public bool IsFirstChild { get { return _sistrees != null; } }



            static string Spaces(int n)
            {
                return new String(' ', n);
            }

            internal void PrintTree(int offset)
            {
                string offset_str = Spaces(offset);
                System.Diagnostics.Debug.WriteLine(offset_str + this._value.ToString());
                //"starts at " + this.GetValue()[0] + ", ends at " + this.GetValue()[1]);
                if (this.SubTrees.Count != 0)
                {
                    foreach (Tree<T> sbtr in this._subtrees)
                    {
                        sbtr.PrintTree(offset + 2);
                    }
                }

                if (this.IsFirstChild)
                {
                    foreach (Tree<T> sis in this._sistrees)
                    {
                        sis.PrintTree(offset);
                    }
                }

            }


            public int Depth { get { return _depth; } }
        }
    }

}
