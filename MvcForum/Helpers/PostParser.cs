using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using System.Text.RegularExpressions;

namespace MvcForum.Helpers
{
    public static class PostParser
    {
        static Dictionary<string, BBCodeTag> BBDict = new Dictionary<string, BBCodeTag>();

        const string URLProtocolRegex = @"^(https?)|(ftp)://";

        public static void InitBBCodes()
        {
            BBCodeTag.Init("b", "<b>", "</b>");
            BBCodeTag.Init("u", "<span class=\"u\">", "</span>");
            BBCodeTag.Init("i", "<i>", "</i>");
            BBCodeTag.Init("s", "<span class=\"s\">", "</span>");
            BBCodeTag.Init("url", "<a href=\"{0}\">", "</a>", AcceptParameters:true, Parser: UrlEncodeTagParser);
            BBCodeTag.Init("img", "<img src=\"{0}\" alt=\"\">", "</img>", TopMost: true, Parser: UrlEncodeTagParser, SupressChildren: true);
            BBCodeTag.Init("center", "<div class=\"Center\">", "</div>");
            BBCodeTag.Init("right", "<div class=\"Right\">", "</div>");
            BBCodeTag.Init("left", "<div class=\"Left\">", "</div>");
            BBCodeTag.Init("justify", "<div class=\"Justify\">", "</div>");
            BBCodeTag.Init("quote", "<fieldset class=\"quote\"><legend>{0}</legend>", "</fieldset>", AcceptParameters: true, AllowNesting: true, Parser: QuoteParser);
            BBCodeTag.Init("youtube", @"<div class=""video""><img src=""/Content/Images/video-aspect.png"" alt="""" /><!--[if !IE]>--><object data=""http://www.youtube.com/embed/{0}"" type=""text/html"" class=""youtube""></object>
<!--<![endif]--><!--[if IE]><iframe class=""youtube"" type=""text/html"" src=""http://www.youtube.com/embed/{0}"" frameborder=""0""></iframe><![endif]--></div>", "", SupressChildren: true, Parser: UrlEncodePartParser);
            BBCodeTag.Init("spoiler", "<div class=\"spoiler\"><a href=\"#\"\">Spoiler:</a><div>", "</div></div>");
            BBCodeTag.Init("code", "<div class=\"code\"><ol><li>{0}", "</li></ol></div>", TopMost:true, SupressChildren: true, Parser: CodeParser);
        }

        static string CodeParser(BBCodeTag ToEncode, BBTreeNode NodeToParse)
        {
            StringBuilder Test = new StringBuilder();
            foreach (var Child in NodeToParse.Children)
                Test.Append(HttpUtility.HtmlEncode(Child.Data));
            var data = Test.Replace("\n", "</li><li>").Replace("\r", "").ToString();
            return String.Format(ToEncode.HTML, data);
        }

        static string QuoteParser(BBCodeTag ToEncode, BBTreeNode NodeToParse)
        {
            string Name = "Quote";
            if (!String.IsNullOrWhiteSpace(NodeToParse.Data)) Name = HttpUtility.HtmlAttributeEncode(NodeToParse.Data);
            return String.Format(ToEncode.HTML, Name); 
        }

        static string UrlEncodePartParser(BBCodeTag ToEncode, BBTreeNode NodeToParse)
        {
            string URL = NodeToParse.Data;
            if (String.IsNullOrEmpty(URL) && NodeToParse.Children.Count > 0)
                URL = NodeToParse.Children[0].Data;
            return String.Format(ToEncode.HTML, HttpUtility.HtmlAttributeEncode(HttpUtility.UrlPathEncode(URL)));
        }

        static string UrlEncodeTagParser(BBCodeTag ToEncode, BBTreeNode NodeToParse)
        {
            string URL = NodeToParse.Data;
            if (String.IsNullOrEmpty(URL) && NodeToParse.Children.Count > 0)
            {
                URL = NodeToParse.Children[0].Data;
            }
            Regex Reg = new Regex(URLProtocolRegex);
            
            if (!Reg.IsMatch(URL, 0))
            {
                URL = "http://" + URL;
            }
            return String.Format(ToEncode.HTML, HttpUtility.HtmlAttributeEncode(HttpUtility.UrlPathEncode(URL)));
        }

        static string HTMLEncodeTagParser(BBCodeTag ToEncode, BBTreeNode NodeToParse)
        {
            if (!ToEncode.AcceptParameters) return ToEncode.HTML;
            return String.Format(ToEncode.HTML, HttpUtility.HtmlAttributeEncode(NodeToParse.Data)); 
        }

        public class BBCodeTag
        {
            public Func<BBCodeTag, BBTreeNode, string> Parser;
            public string Tag;
            public string HTML;
            public string HTMLClosure;
            public bool AcceptParameters;
            public bool TopMost;
            public bool SupressChildren;
            public bool AllowNesting;

            public static void Init(string Tag, string HTML, string HTMLClosure, bool AcceptParameters = false, bool TopMost = false, Func<BBCodeTag, BBTreeNode, string> Parser = null, bool SupressChildren = false, bool AllowNesting = false)
            {
                BBCodeTag New = new BBCodeTag()
                {
                    Tag = Tag,
                    HTML = HTML,
                    HTMLClosure = HTMLClosure,
                    AcceptParameters = AcceptParameters,
                    Parser = Parser ?? HTMLEncodeTagParser,
                    TopMost = TopMost,
                    SupressChildren = SupressChildren,
                    AllowNesting = AllowNesting
                };
                BBDict.Add(New.Tag.ToLowerInvariant(), New);
            }

            BBCodeTag()
            {}    
        }

        public class BBTreeNode
        {
            public BBTreeNode Parent {get; private set;}
            public List<BBTreeNode> Children { get; private set; }

            public string Data { get; private set; }
            public BBCodeTag TagType { get; private set; }

            public void AddChild(BBTreeNode ChildNode)
            {
                Children.Add(ChildNode);
                ChildNode.Parent = this;
            }

            public BBTreeNode(string Data, BBCodeTag TagType)
            {
                Children = new List<BBTreeNode>();
                this.Data = Data;
                this.TagType = TagType;
            }

            public void WriteNode(StringBuilder ToWriteTo)
            {
                if (TagType != null)
                    ToWriteTo.Append(TagType.Parser(TagType, this));
                else
                    ToWriteTo.Append(HttpUtility.HtmlEncode(Data));

                if (TagType == null || !TagType.SupressChildren)
                {
                    foreach (BBTreeNode Child in Children)
                    {
                        Child.WriteNode(ToWriteTo);
                    }
                }
                if (TagType != null)
                    ToWriteTo.Append(TagType.HTMLClosure);
            }
        }

        static BBTreeNode ParseNode(string sTag)
        {
            int SplitPosition = sTag.IndexOf("=");
            string TagName = sTag;
            string Param = "";
            if (SplitPosition >= 0)
            {
                TagName = sTag.Substring(0, SplitPosition);
                if (sTag.Length >= SplitPosition)
                    Param = sTag.Substring(SplitPosition + 1);
            }
            BBCodeTag CodeTag;
            if (!BBDict.TryGetValue(TagName.ToLowerInvariant(), out CodeTag) || (!CodeTag.AcceptParameters && SplitPosition >= 0))
            {
                return new BBTreeNode(String.Format("[{0}]", sTag), null);    
            }
            return new BBTreeNode(Param, CodeTag);
        }

        public static MvcHtmlString Parse(string PostText)
        {
            if (String.IsNullOrEmpty(PostText)) return MvcHtmlString.Empty;
            int TagStartPosition = 0;
            int TagEndPosition = 0;
            int ParsePosition = 0;
            BBTreeNode RootNode = new BBTreeNode("", null);
            BBTreeNode CurrentNode = RootNode;
            while ((TagStartPosition = PostText.IndexOf("[", TagEndPosition)) >= 0 && (TagEndPosition = PostText.IndexOf("]", TagStartPosition)) >= 0)
            {
                TagStartPosition = PostText.LastIndexOf("[", TagEndPosition);
                
                if (TagStartPosition > ParsePosition) // Current data.
                {
                    CurrentNode.AddChild(new BBTreeNode(PostText.Substring(ParsePosition, TagStartPosition - ParsePosition), null));
                }
                ParsePosition = TagEndPosition + 1;
                string Tag = PostText.Substring(TagStartPosition + 1, TagEndPosition - TagStartPosition - 1);
                if (Tag.Length > 1 && Tag.Substring(0, 1) == "/") // End tag
                {
                    BBTreeNode SearchNode = CurrentNode;
                    string TagName = Tag.Substring(1);
                    while (SearchNode != RootNode)
                    {
                        if (String.Equals(SearchNode.TagType.Tag, TagName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            CurrentNode = SearchNode.Parent;
                            break;
                        }
                        SearchNode = SearchNode.Parent;
                    }
                    if (SearchNode == RootNode && CurrentNode != RootNode && CurrentNode.TagType.TopMost)
                    {
                        CurrentNode.AddChild(new BBTreeNode(String.Format("[{0}]", Tag), null));
                    } 
                    continue;
                }
                BBTreeNode NewNode;
                if (CurrentNode.TagType != null && CurrentNode.TagType.TopMost)
                {
                    NewNode = new BBTreeNode(String.Format("[{0}]", Tag), null);
                }
                else
                {
                    NewNode = ParseNode(Tag);
                    if (NewNode.TagType != null && !NewNode.TagType.AllowNesting)
                    {
                        BBTreeNode SearchNode = CurrentNode;
                        while (SearchNode != RootNode)
                        {
                            if (SearchNode.TagType == NewNode.TagType)
                            {
                                NewNode = new BBTreeNode(String.Format("[{0}]", Tag), null);
                                break;
                            }
                            SearchNode = RootNode;
                        }
                    }
                }

                CurrentNode.AddChild(NewNode);
                if (NewNode.TagType != null)
                    CurrentNode = NewNode;
            }
            
            if (PostText.Length > ParsePosition) // Current data.
            {
                CurrentNode.AddChild(new BBTreeNode(PostText.Substring(ParsePosition), null));
            }

            StringBuilder OutputBuilder = new StringBuilder(PostText.Length + 10);
            RootNode.WriteNode(OutputBuilder);
            return MvcHtmlString.Create(OutputBuilder.ToString());
        }
    }
}