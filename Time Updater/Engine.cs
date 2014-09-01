using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Time_Updater.Collection;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;

namespace Time_Updater
{
    namespace Engines
    {
        abstract class Engine
        {
            internal static Regex rx_shortTags = new Regex("(< *[a-z]{1,4} *([^<=> ]+ *= *('|\")[^<>]+('|\") *)*/? *>)|(< *[a-z]{1,4} */ *>)|(< */ *[a-z]{1,4} *>)");
            abstract protected void Fetch(string word);
            abstract internal string GetResponse(string arg);

            internal static string FetchRaw(string urlStr)
            {
                string raw;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlStr);
                request.Timeout = 6000;
                request.Method = WebRequestMethods.Http.Get;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                long len = response.ContentLength;
                StreamReader sr = new StreamReader(response.GetResponseStream(), new UTF8Encoding());
                raw = sr.ReadToEnd();
                raw = Regex.Replace(raw.Replace(" ", "default_sep1"), "\\s", "default_sep2");
                return raw.Replace("default_sep2", "\n").
                    Replace("default_sep1", " ");
            }

            internal static string RemoveHTMLTag(string rawStr, string tagname)
            {
                string tag0 = "<" + tagname + ">";
                string tag1 = "</" + tagname + ">";
                string res = rawStr;
                while (res.Contains(tag0) && res.Contains(tag1))
                {
                    int tagStart = rawStr.IndexOf(tag0);
                    int tagEnd = rawStr.IndexOf(tag1);
                    if (tagStart > tagEnd)
                        break;
                    res = res.Substring(0, tagStart) + res.Substring(tagEnd + tag1.Length);
                }

                return res;
            }

            internal static string FilterHTMLContent(string rawStr, string tagFilter, int depth, Pair<string> extra_attr)
            {
                Tree<HTML_Element> xtree = ConvertHTMLToXTree(rawStr, tagFilter);
                return PrintXTree(rawStr, xtree, depth, extra_attr);
            }

            internal static string RemoveEmptyLines(string input)
            {
                while (input.Contains("\n\n"))
                    input = input.Replace("\n\n", "\n");
                return input.Trim();
            }

            static void PrintXTree(Tree<HTML_Element> XTree)
            {
                if (XTree.GetValue().IsRoot)
                {
                    XTree = XTree.SubTrees[0]; // root
                }
                else
                {
                    XTree = XTree.Parent.SubTrees[0]; // must be first child
                }
                XTree.PrintTree(0);
            }

            internal static string PrintXTreeNode(string rawStr, Tree<HTML_Element> XTree)
            {
                if (XTree.GetValue().IsRoot)
                {
                    return (PrintXTreeNode(rawStr, XTree.SubTrees[0]));
                }
                return rawStr.Substring(XTree.GetValue().StartIndex,
                       XTree.GetValue().EndIndex - XTree.GetValue().StartIndex); // root
            }
            internal static string PrintXTree(string rawStr, Tree<HTML_Element> XTree, int depth, Pair<string> extra_attr)
            {
                if (XTree.GetValue().IsRoot)
                {
                    return PrintXTree(rawStr, XTree.SubTrees[0], depth, extra_attr); // root
                }

                StringBuilder builder = new StringBuilder();
                if (XTree.Depth == depth || depth < 0)
                {
                    if (XTree.GetValue().HasAttribute(extra_attr))
                    {
                        builder.Append(rawStr.Substring(XTree.GetValue().StartIndex,
                            XTree.GetValue().EndIndex - XTree.GetValue().StartIndex));
                        builder.Append("\n");
                    }
                }
                if (XTree.SubTrees.Count != 0)
                {
                    foreach (Tree<HTML_Element> sbtr in XTree.SubTrees)
                    {
                        builder.Append(PrintXTree(rawStr, sbtr, depth, extra_attr));
                        builder.Append("\n");
                    }
                }
                if (XTree.IsFirstChild)
                {
                    foreach (Tree<HTML_Element> sis in XTree.Sisters)
                    {
                        builder.Append(PrintXTree(rawStr, sis, depth, extra_attr));
                        builder.Append("\n");
                    }
                }
                return builder.ToString();
            }

            internal static Tree<HTML_Element> ConvertHTMLToXTree(string rawStr, string tagFilter)
            {
                string tagStart = "< *" + tagFilter + "( +[^<=> ]+ *= *('|\")[^<>]+('|\"))* *>";
                string tagEnd = "(< *" + tagFilter + " */ *>)|(< */ *" + tagFilter + " *>)";
                MatchCollection matches = Regex.Matches(rawStr, "(" + tagStart + ")|" + tagEnd);

                List<int> positions = new List<int>();
                List<bool> elementTyps = new List<bool>(); // 0 (false) = begin, 1 (true) = end
                List<string> data = new List<string>();

                foreach (Match m in matches)
                {
                    if (Regex.IsMatch(m.Value, tagEnd))
                    {
                        positions.Add(m.Index + m.Length);
                        elementTyps.Add(true);
                    }
                    else
                    {
                        positions.Add(m.Index);
                        elementTyps.Add(false);
                    }

                    data.Add(m.Value);
                }

                return ConvertListToXTree(positions, elementTyps, data);
            }

            static Tree<HTML_Element> ConvertListToXTree(List<int> positions, List<bool> typs, List<string> data)
            {
                // positions stores all tag positions, together with typs, 
                // we know where are the starts and ends
                // e.g. an HTML node starts with <X> (type0, posX) and ends with </X> (type1, pos/X)

                Tree<HTML_Element> document = new Tree<HTML_Element>
                    (new HTML_Element(new Pair<int>(int.MinValue, int.MaxValue), "ROOT"), 0);

                document.AddSub(new Tree<HTML_Element>(
                     new HTML_Element(new Pair<int>(positions[0], int.MaxValue), data[0]), 1));

                Tree<HTML_Element> current_node = document.SubTrees[0];
                int current_depth = 1;

                for (int i = 1; i < positions.Count; i++)
                {
                    if (typs[i] == typs[i - 1])
                    {
                        if (typs[i] == false) // starting new subtree
                        {
                            current_depth += 1;
                            current_node.AddSub(new Tree<HTML_Element>(
                                new HTML_Element(new Pair<int>(positions[i], int.MaxValue), data[i]), current_depth));
                            current_node = current_node.SubTrees[0]; // NEW ENTRY POINT = SUB.0
                        }
                        else // ending the parent tree
                        {
                            current_depth -= 1;
                            current_node.Parent.GetValue().CloseTag(positions[i]);
                            current_node = current_node.Parent;
                        }
                    }
                    else
                    {
                        if (typs[i] == false) // starting new sistree
                        {
                            current_node.AddSis(new Tree<HTML_Element>(
                                new HTML_Element(new Pair<int>(positions[i], int.MaxValue), data[i]), current_depth)); // adds to the first child only
                            current_node = current_node.Sisters[current_node.Sisters.Count - 1];
                        }
                        else // ending current node
                        {
                            current_node.GetValue().CloseTag(positions[i]);
                        }
                    }
                }

                return document; // the ending node should be the root
            }
        }

        class Worldtimeserver : Engine
        {
            string _rawStr;
            protected override void Fetch(string arg)
            {
                _rawStr = Engine.FetchRaw("http://www.worldtimeserver.com/current_time_in_" + arg + ".aspx");
            }

            internal override string GetResponse(string arg)
            {
                try
                {
                    Fetch(arg);
                    string temp = Engine.FilterHTMLContent(_rawStr, "span", -1, new Pair<string>("class", "\"font7\""));
                    //temp = Engine.FilterHTMLContent(temp, "div", 5, null);
                    //temp = Engine.FilterHTMLContent(temp, "p", 1, null);
                    //temp = Engine.rx_shortTags.Replace(temp, "");
                    //Regex rx2 = new Regex("\\[\\d+\\]");
                    //temp = rx2.Replace(temp, "");
                    temp = Engine.RemoveEmptyLines(temp);
                    return Regex.Split(temp, "\n")[1].Trim();
                }
                catch
                {
                    return "Not found!";
                }
            }
        }

    }
}
