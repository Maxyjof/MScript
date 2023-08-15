using System.ComponentModel.Design;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace MScript
{
    internal class Node
    {
        public Token Token = new Token("", TokenType.BadToken, -1);
        public string Name = "";
        public string Value => Token.Value;
        public TokenType Type => Token.Type;
        public List<Node> Children = new();
        public int ChildrenCount => Children.Count;
        public Node Left => Children[0];
        public Node Right => Children[1];
        public Environment Env = null;

        public Node(string name)
        {
            Name = name;
            Token.Value = name;
            Token.Type = TokenType.Keyword;
        }

        public Node(Token t)
        {
            if (t != null)
            {
                Token = t;
                Name = t.Value;
            }
        }

        public void Add(Node node)
        {
            Children.Add(node);
        }

        public void AddRange(List<Node> nodes)
        {
            Children.AddRange(nodes);
        }

        public void Remove(Node node)
        {
            Children.Remove(node);
        }

        public void SetName(string name)
        {
            Name = name;
            if (Type is TokenType.Integer or TokenType.Float or TokenType.String)
            {
                Token.Value = name;
            }
        }

        public void SetValue(string value)
        {
            Token.Value = value;
            if (Regex.IsMatch(value, "^\\\"([^\\\"]*)\\\"$") || Regex.IsMatch(value, "^'([^']*)'$"))
            {
                Token.Type = TokenType.String;
                Name = value;
            }
            else if (Regex.IsMatch(value, @"^((=)|(\+=)|(-=)|(\*=)|(/=))$") || Regex.IsMatch(value, @"^[\.,!(){};=+\-*/]$"))
            {
                Token.Type = TokenType.String;
                Name = value;
            }
            else if (Regex.IsMatch(value, @"^[+-]?[\d]+$"))
            {
                Token.Type = TokenType.Integer;
                Name = value;
            }
            else if (Regex.IsMatch(value, @"^[+-]?[\d]+\.[\d]+$"))
            {
                Token.Type = TokenType.Float;
                Name = value;
            }
            else if (Regex.IsMatch(value, @"^true$|^false$"))
            {
                Token.Type = TokenType.Bool;
                Name = value;
            }
            else if (Regex.IsMatch(value, @"^[_a-zA-Z][_a-zA-Z0-9]*$"))
            {
                Token.Type = TokenType.Word;
                Name = value;
            }
            else
                Token.Type = TokenType.Word;
        }

#if DEBUG
        public override string ToString()
        {
            if (Type != TokenType.Word && Type != TokenType.String)
            {
                switch (Name)
                {
                    case "functionDefine":
                        return "def " + Left.ToString() + "(" + Right.ToString() + ")" + Children[2].ToString();
                    case "functionCall":
                        string text = Left.ToString() + "(";
                        foreach (var item in Children.GetRange(1, ChildrenCount - 1))
                        {
                            text += item.ToString();
                            text += ", ";
                        }
                        if (text.EndsWith(", "))
                            text = text.Remove(text.Length - 2, 2);
                        text += ")";
                        return text;
                    case "classDefine":
                        return "class " + Left.ToString();
                    case "if":
                        return "if(" + Left.ToString() + ")";
                }
            }
            if (ChildrenCount == 0)
                if (Type == TokenType.String)
                    return "\"" + Name + "\"";
                else
                    return Name;
            if (ChildrenCount == 1)
                return Left.ToString();
            else if (ChildrenCount == 2)
                if (Type == TokenType.Symbol)
                    return Left.ToString() + Name + Right.ToString();
                else
                    return Left.ToString() + Right.ToString();
            else
            {
                //return Name;// + ":" + Left.ToString() + Right.ToString() + "...";
                StringBuilder sb = new();
                foreach (Node item in Children)
                {
                    sb.Append(item.ToString() + ' ');
                }
                return sb.ToString();
            }
        }
#endif

        static void Throw(Node node, string str)
        {
            throw new Exception(node.Token.Row + str);
        }

        public static Node operator +(Node left, Node right)
        {
            if (BigInteger.TryParse(left.Token.Value, out BigInteger BigInteger1))
            {
                if (BigInteger.TryParse(right.Token.Value, out BigInteger BigInteger2))
                    return new Node(new Token($"{BigInteger1 + BigInteger2}", TokenType.Integer, left.Token.Row));
            }
            if (float.TryParse(left.Token.Value, out float float1))
                if (right.Type is TokenType.String)
                    return new Node(new Token(left.Value + right.Value, TokenType.String, left.Token.Row));
                else
                    return new Node(new Token($"{float.Parse(left.Token.Value) + float.Parse(right.Token.Value)}", TokenType.Float, left.Token.Row));
            if (left.Type is TokenType.String)
            {
                return new Node(new Token(left.Value + right.Value, TokenType.String, left.Token.Row));
            }
            Throw(left, "错误的运算对象：" + left.Value);
            return new Node("NullableNode");
        }

        public static Node operator -(Node left, Node right)
        {
            if (BigInteger.TryParse(left.Token.Value, out BigInteger BigInteger1))
            {
                if (BigInteger.TryParse(right.Token.Value, out BigInteger BigInteger2))
                    return new Node(new Token($"{BigInteger1 - BigInteger2}", TokenType.Integer, left.Token.Row));
            }
            if (float.TryParse(left.Token.Value, out float float1))
                return new Node(new Token($"{float.Parse(left.Token.Value) - float.Parse(right.Token.Value)}", TokenType.Float, left.Token.Row));
            if (left.Type is TokenType.String)
            {
                return new Node(new Token(left.Value.Replace(right.Value, ""), TokenType.String, left.Token.Row));
            }
            Throw(left, "错误的运算对象：" + left.Value);
            return new Node("NullableNode");
        }

        public static Node operator *(Node left, Node right)
        {
            if (BigInteger.TryParse(left.Token.Value, out BigInteger BigInteger1))
            {
                if (BigInteger.TryParse(right.Token.Value, out BigInteger BigInteger2))
                    return new Node(new Token($"{BigInteger1 * BigInteger2}", TokenType.Integer, left.Token.Row));
            }
            return new Node(new Token($"{float.Parse(left.Token.Value) * float.Parse(right.Token.Value)}", TokenType.Float, left.Token.Row));
        }

        public static Node operator /(Node left, Node right)
        {
            if (BigInteger.TryParse(left.Token.Value, out BigInteger BigInteger1))
            {
                if (BigInteger.TryParse(right.Token.Value, out BigInteger BigInteger2))
                    return new Node(new Token($"{(float)BigInteger1 / (float)BigInteger2}", TokenType.Integer, left.Token.Row));
            }
            return new Node(new Token($"{float.Parse(left.Token.Value) / float.Parse(right.Token.Value)}", TokenType.Float, left.Token.Row));
        }

        public static Node operator ==(Node left, Node right)
        {
            if (left.Type is TokenType.Bool)
            {
                if (right.Type is TokenType.Bool)
                    return new Node(new Token((bool.Parse(left.Value) == bool.Parse(right.Value)).ToString().ToLower(), TokenType.Bool, left.Token.Row));
                if (BigInteger.TryParse(right.Value, out BigInteger BigInteger11))
                    if (bool.Parse(left.Value))
                        return new Node(new Token((BigInteger11 != 0).ToString().ToLower(), TokenType.Bool, left.Token.Row));
                    else
                        return new Node(new Token((BigInteger11 == 0).ToString().ToLower(), TokenType.Bool, left.Token.Row));
                if (float.TryParse(right.Value, out float float11))
                    if (bool.Parse(left.Value))
                        return new Node(new Token((float11 != 0).ToString().ToLower(), TokenType.Bool, left.Token.Row));
                    else
                        return new Node(new Token((float11 == 0).ToString().ToLower(), TokenType.Bool, left.Token.Row));
            }

            if (BigInteger.TryParse(left.Token.Value, out BigInteger BigInteger1))
            {
                if (BigInteger.TryParse(right.Token.Value, out BigInteger BigInteger2))
                    return new Node(new Token($"{(BigInteger1 == BigInteger2).ToString().ToLower()}", TokenType.Integer, left.Token.Row));
                else if (right.Type is TokenType.Bool)
                    if (bool.Parse(right.Value))
                        return new Node(new Token($"{(BigInteger1 != 0).ToString().ToLower()}", TokenType.Integer, left.Token.Row));
                    else
                        return new Node(new Token($"{(BigInteger1 == 0).ToString().ToLower()}", TokenType.Integer, left.Token.Row));
            }
            if (float.TryParse(left.Value, out float float1))
                if (float.TryParse(right.Value, out float float2))
                    return new Node(new Token($"{(float.Parse(left.Token.Value) == float.Parse(right.Token.Value)).ToString().ToLower()}", TokenType.Float, left.Token.Row));
                else if (right.Type is TokenType.Bool)
                    if (bool.Parse(right.Value))
                        return new Node(new Token($"{(float1 != 0).ToString().ToLower()}", TokenType.Integer, left.Token.Row));
                    else
                        return new Node(new Token($"{(float1 == 0).ToString().ToLower()}", TokenType.Integer, left.Token.Row));
            if (left.Type is TokenType.String)
            {
                return new Node(new Token((left.Value == right.Value).ToString().ToLower(), TokenType.String, left.Token.Row));
            }
            if (left.Name == "functionDefine")//函数
            {
                if (right.Name == "functionDefine")//也是函数
                    return left == right;
                else//不是函数
                    return new Node("false");
            }
            if (right.Name == "functionDefine")//到了这里，就说明左边肯定不是函数，但右边却是
                return new Node("false");

            if (left.Name == "stmt")//类实例
            {
                if (right.Name == "stmt")//也是类实例
                    return left == right;
                else//不是类实例
                    return new Node("false");
            }
            if (right.Name == "stmt")//到了这里，就说明左边肯定不是类实例，但右边却是
                return new Node("false");
            Throw(left, "错误的运算对象：" + left.Value);
            return new Node("NullableNode");
        }

        public static Node operator !=(Node left, Node right)
        {
            if (left.Type is TokenType.Bool)
            {
                if (right.Type is TokenType.Bool)
                    return new Node(new Token((bool.Parse(left.Value) != bool.Parse(right.Value)).ToString().ToLower(), TokenType.Bool, left.Token.Row));
                if (BigInteger.TryParse(right.Value, out BigInteger BigInteger11))
                    if (bool.Parse(left.Value))
                        return new Node(new Token((BigInteger11 == 0).ToString().ToLower(), TokenType.Bool, left.Token.Row));
                    else
                        return new Node(new Token((BigInteger11 != 0).ToString().ToLower(), TokenType.Bool, left.Token.Row));
                if (float.TryParse(right.Value, out float float11))
                    if (bool.Parse(left.Value))
                        return new Node(new Token((float11 == 0).ToString().ToLower(), TokenType.Bool, left.Token.Row));
                    else
                        return new Node(new Token((float11 != 0).ToString().ToLower(), TokenType.Bool, left.Token.Row));
            }

            if (BigInteger.TryParse(left.Token.Value, out BigInteger BigInteger1))
            {
                if (BigInteger.TryParse(right.Token.Value, out BigInteger BigInteger2))
                    return new Node(new Token($"{(BigInteger1 != BigInteger2).ToString().ToLower()}", TokenType.Integer, left.Token.Row));
                else if (right.Type is TokenType.Bool)
                    if (bool.Parse(right.Value))
                        return new Node(new Token($"{(BigInteger1 == 0).ToString().ToLower()}", TokenType.Integer, left.Token.Row));
                    else
                        return new Node(new Token($"{(BigInteger1 != 0).ToString().ToLower()}", TokenType.Integer, left.Token.Row));
            }
            if (float.TryParse(left.Value, out float float1))
                if (float.TryParse(right.Value, out float float2))
                    return new Node(new Token($"{(float.Parse(left.Token.Value) != float.Parse(right.Token.Value)).ToString().ToLower()}", TokenType.Float, left.Token.Row));
                else if (right.Type is TokenType.Bool)
                    if (bool.Parse(right.Value))
                        return new Node(new Token($"{(float1 == 0).ToString().ToLower()}", TokenType.Integer, left.Token.Row));
                    else
                        return new Node(new Token($"{(float1 != 0).ToString().ToLower()}", TokenType.Integer, left.Token.Row));
            if (left.Type is TokenType.String)
            {
                return new Node(new Token((left.Value != right.Value).ToString().ToLower(), TokenType.String, left.Token.Row));
            }
            if (left.Name == "functionDefine")//函数
            {
                if (right.Name == "functionDefine")//也是函数
                    return left == right;
                else//不是函数
                    return new Node("true");
            }
            if (right.Name == "functionDefine")//到了这里，就说明左边肯定不是函数，但右边却是
                return new Node("true");

            if (left.Name == "stmt")//类实例
            {
                if (right.Name == "stmt")//也是类实例
                    return left == right;
                else//不是类实例
                    return new Node("true");
            }
            if (right.Name == "stmt")//到了这里，就说明左边肯定不是类实例，但右边却是
                return new Node("true");
            Throw(left, "错误的运算对象：" + left.Value);
            return new Node("NullableNode");
        }

        public static Node operator >=(Node left, Node right)
        {
            if (BigInteger.TryParse(left.Token.Value, out BigInteger BigInteger1))
            {
                if (BigInteger.TryParse(right.Token.Value, out BigInteger BigInteger2))
                    return new Node(new Token($"{BigInteger1 >= BigInteger2}", TokenType.Integer, left.Token.Row));
            }
            if (float.TryParse(left.Token.Value, out float float1))
                return new Node(new Token($"{(float.Parse(left.Token.Value) >= float.Parse(right.Token.Value)).ToString().ToLower()}", TokenType.Float, left.Token.Row));
            if (left.Type is TokenType.String)
            {
                return new Node(new Token((left.Value.Length >= right.Value.Length).ToString().ToLower(), TokenType.String, left.Token.Row));
            }
            Throw(left, "错误的运算对象：" + left.Value);
            return new Node("NullableNode");
        }

        public static Node operator <=(Node left, Node right)
        {
            if (BigInteger.TryParse(left.Token.Value, out BigInteger BigInteger1))
            {
                if (BigInteger.TryParse(right.Token.Value, out BigInteger BigInteger2))
                    return new Node(new Token($"{(BigInteger1 <= BigInteger2).ToString().ToLower()}", TokenType.Integer, left.Token.Row));
            }
            if (float.TryParse(left.Token.Value, out float float1))
                return new Node(new Token($"{(float.Parse(left.Token.Value) <= float.Parse(right.Token.Value)).ToString().ToLower()}", TokenType.Float, left.Token.Row));
            if (left.Type is TokenType.String)
            {
                return new Node(new Token((left.Value.Length <= right.Value.Length).ToString().ToLower(), TokenType.String, left.Token.Row));
            }
            Throw(left, "错误的运算对象：" + left.Value);
            return new Node("NullableNode");
        }

        public static Node operator >(Node left, Node right)
        {
            if (BigInteger.TryParse(left.Token.Value, out BigInteger BigInteger1))
            {
                if (BigInteger.TryParse(right.Token.Value, out BigInteger BigInteger2))
                    return new Node(new Token($"{BigInteger1 > BigInteger2}", TokenType.Integer, left.Token.Row));
            }
            if (float.TryParse(left.Token.Value, out float float1))
                return new Node(new Token($"{(float.Parse(left.Token.Value) > float.Parse(right.Token.Value)).ToString().ToLower()}", TokenType.Float, left.Token.Row));
            if (left.Type is TokenType.String)
            {
                return new Node(new Token((left.Value.Length > right.Value.Length).ToString().ToLower(), TokenType.String, left.Token.Row));
            }
            Throw(left, "错误的运算对象：" + left.Value);
            return new Node("NullableNode");
        }

        public static Node operator <(Node left, Node right)
        {
            if (BigInteger.TryParse(left.Token.Value, out BigInteger BigInteger1))
            {
                if (BigInteger.TryParse(right.Token.Value, out BigInteger BigInteger2))
                    return new Node(new Token($"{(BigInteger1 < BigInteger2).ToString().ToLower()}", TokenType.Integer, left.Token.Row));
            }
            if (float.TryParse(left.Token.Value, out float float1))
                return new Node(new Token($"{(float.Parse(left.Token.Value) < float.Parse(right.Token.Value)).ToString().ToLower()}", TokenType.Float, left.Token.Row));
            if (left.Type is TokenType.String)
            {
                return new Node(new Token((left.Value.Length < right.Value.Length).ToString().ToLower(), TokenType.String, left.Token.Row));
            }
            Throw(left, "错误的运算对象：" + left.Value);
            return new Node("NullableNode");
        }

        public static Node operator !(Node left)
        {
            if (BigInteger.TryParse(left.Token.Value, out BigInteger BigInteger1))
            {
                return new Node(new Token($"{(BigInteger1 == 0).ToString().ToLower()}", TokenType.Integer, left.Token.Row));
            }
            if (float.TryParse(left.Token.Value, out float float1))
                return new Node(new Token($"{(float.Parse(left.Token.Value) == 0).ToString().ToLower()}", TokenType.Float, left.Token.Row));
            if (left.Type is TokenType.String)
            {
                return new Node(new Token((left.Value.Length == 0).ToString().ToLower(), TokenType.String, left.Token.Row));
            }
            Throw(left, "错误的运算对象：" + left.Value);
            return new Node("NullableNode");
        }
    }
}
