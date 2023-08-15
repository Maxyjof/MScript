

using System.Text;

namespace MScript
{
    internal class Parser
    {
        List<Token> Tokens;
        int Pos = 0;

        Token Current
        {
            get
            {
                if (Tokens[Pos].Value == "\n")
                    Next();
                if (IsEOF())
                    return Tokens[Tokens.Count - 1];
                return Tokens[Pos];
            }
        }

        public Parser(List<Token> t)
        {
            Tokens = t;
        }

        void Throw(string str)
        {
#if DEBUG
            throw new Exception($"第{Current.Row}行：" + str);
#endif
#if RELEASE
            Console.WriteLine($"第{Current.Row}行：" + str);
            Console.ReadKey();
            System.Environment.Exit(0);
#endif
        }

        public Node Scan()
        {
            Node root = new("Root");
            while (Pos < Tokens.Count - 1)
                root.Add(Work());
            return root;
        }

        Node Work()
        {
            switch (Current.Type)
            {
                case TokenType.Keyword:
                    return StartWithKeyword();
                case TokenType.Word:
                    if (Peek(1).Value == "=")
                        return SetVariable();
                    //if (Peek(1).Value == ".")
                    //    //return ClassFunctionCall();
                    //    return GetClassMember();
                    var e = Expression();
                    Skip(";");
                    return e;
                case TokenType.Symbol:
                    return StartWithSymbol();
                case TokenType.BadToken:
                default:
                    Throw("出现了未知的词：" + Current.Value);
                    return null;
            }
        }

        #region 基础方法
        bool Is(string str)
        {
            if (str == "\n")
                return Tokens[Pos].Value == "\n";
            int index = 0;
            while (Tokens[Pos + index].Value == "\n" && Pos + index < Tokens.Count - 1)
                index++;
            if (Tokens.Count > index)
                return Tokens[Pos + index].Value == str;
            return false;
        }

        List<Node> Expr(params Node[] nodes)
        {
            return nodes.ToList();
        }

        Node Read()
        {
            if (Current.Value != "\n")
            {
                var node = new Node(Current);
                Pos++;
                return node;
            }
            else
            {
                int index = 0;
                while (Tokens[Pos + index].Value == "\n")
                    index++;
                if (Tokens.Count > index)
                    return new Node(Tokens[Pos + index]);
                Throw("错误的词元获取");
                return new Node("NullableNode");
            }
        }

        void Next()
        {
            int index = Pos;
            while (Tokens[index].Value == "\n" && index < Tokens.Count - 1)
                index++;
            if (index == Pos && index < Tokens.Count - 1)
                index++;
            Pos = index;
        }

        bool IsEOF() => Pos >= Tokens.Count - 1;

        void Skip(string str)
        {
            if (str != "\n")
                while (Tokens[Pos].Value == "\n")
                    Next();
            if (Tokens[Pos].Value == str)
                Next();
            else
                Throw("缺少符号：" + str);
        }

        void Move(int index)
        {
            for (; index != 0; index--)
            {
                Next();
            }
            if (Pos + index >= Tokens.Count)
                Pos = Tokens.Count - 1;
        }

        Token Peek(int index)
        {
            int p = Pos;
            int index1 = index;
            for (int i = Pos; index1 != 0; index1--)
            {
                while (Tokens[i].Value == "\n" && i < Tokens.Count - 1)
                    i++;
                p = i;
            }
            while (Tokens[p + index].Value == "\n" && p < Tokens.Count - 1)
                p++;
            if (p + index < Tokens.Count)
                return Tokens[p + index];
            return new Token();
        }
        #endregion

        Node StartWithKeyword()
        {
            switch (Current.Value)
            {
                case "def":
                    if (Peek(2).Value == "(")//函数
                    {
                        return Function();
                    }
                    else if (Peek(2).Value is "{" or ":")//类
                    {
                        return Class();
                    }
                    else
                    {
                        //Throw("不能定义函数或类以外的东西");
                        Throw("错误的def使用方式");
                        return null;
                    }
                case "call":
                    return Call();
                case "load":
                    return Load();
                case "if":
                    return If();
                case "while":
                    return While();
                case "for":
                    return For();
                case "return":
                    return Return();
                default:
                    Throw("出现了未知的关键字：" + Current.Value);
                    return null;
            }
        }

        Node StartWithSymbol()
        {
            switch (Current.Value)
            {
                case " ":
                case "\n":
                    break;
                case "/":
                    if (Peek(1).Value == "/")
                        return Comment();
                    break;
                default:
                    Throw("未知的符号开头语句");
                    break;
            }
            return new Node("NullableNode");
        }

        Node Comment()
        {
            Skip("/");
            Skip("/");
            while (!Is("\n") && !IsEOF())
                Next();
            if (!IsEOF())
                Skip("\n");
            return new Node("comment");
        }

        Node Function()
        {
            Node function = new Node("functionDefine");
            Next();
            if (Current.Type != TokenType.Word)
            {
                Throw("未提供定义的名称");
            }
            function.Add(new Node(Current.Value));
            function.Add(new Node("values"));
            var valueList = function.Children[1];
            Next();
            if (Current.Value != "(")
            {
                Throw("缺少符号：(");
            }
            Next();
            while (Current.Value != ")")//有参数
            {
                if (Current.Type != TokenType.Word)
                {
                    Throw("函数定义缺少参数名，此处：\'" + Current.Value + "\'" + "，词素类型：" + Current.Type + "，词素下标：" + Pos);
                }
                valueList.Add(new Node(Current));
                Next();
                if (Current.Value != "," && Current.Value != ")")
                {
                    Throw("缺少符号：,");
                }
                if (Current.Value != ")")
                    Next();
            }
            Next();
            if (Current.Value != "{")
            {
                Throw("缺少符号：{");
            }
            Next();
            function.Add(new Node("stmt"));
            var stmt = function.Children[2];
            while (Current.Value != "}")
            {
                stmt.Add(Work());
            }
            Next();
            return function;
        }

        Node FunctionCall()
        {
            Node functionCall = new Node("functionCall");
            Node functionName = Read();
            functionCall.Add(functionName);
            Next();
            while (Current.Value != ")")//有参数
            {
                if (Current.Type != TokenType.Word && Current.Type != TokenType.String
                    && Current.Type != TokenType.Integer && Current.Type != TokenType.Float)
                {
                    Throw("函数调用缺少参数名，此处：\'" + Current.Value + "\'" + "，词素类型：" + Current.Type + "，词素下标：" + Pos);
                }
                functionCall.Add(Expression());
                if (Current.Value != "," && Current.Value != ")")
                {
                    Throw("缺少符号：,");
                }
                if (Current.Value != ")")
                    Next();
            }//此处是)
            Node node = functionCall;
            if (Peek(1).Value == "(")//嵌套调用语法
            {
                node = FunctionCall();
                node.Children[0] = functionCall;
            }
            else
            {
                Next();
            }
            return node;
        }

        //Node GetClassMember()
        //{

        //}

        Node ClassFunctionCall()
        {
            Node classFunctionCall = new Node("classFunctionCall");
            Node functionName = Point();
            classFunctionCall.Add(functionName);
            Next();
            while (Current.Value != ")")//有参数
            {
                if (Current.Type != TokenType.Word && Current.Type != TokenType.String
                    && Current.Type != TokenType.Integer && Current.Type != TokenType.Float)
                {
                    Throw("类函数调用缺少参数名，此处：\'" + Current.Value + "\'" + "，词素类型：" + Current.Type + "，词素下标：" + Pos);
                }
                classFunctionCall.Add(Expression());
                if (Current.Value != "," && Current.Value != ")")
                {
                    Throw("缺少符号：,");
                }
                if (Current.Value != ")")
                    Next();
            }
            Next();
            Skip(";");
            return classFunctionCall;
        }

        Node Class()
        {
            Node classNode = new Node("classDefine");
            Next();
            if (Current.Type != TokenType.Word)
            {
                Throw("未提供定义的名称");
            }
            classNode.Add(new Node(Current.Value));
            classNode.Add(new Node("parents"));
            var parents = classNode.Children[1];
            Next();
            if (Current.Value is not "{" or ":")
            {
                Throw("缺少符号：{");//提示这个就已经够了捏
            }
            if (Current.Value == ":")//有父类
            {
                Next();
                while (Current.Value != "{")
                {
                    if (Current.Type != TokenType.Word)
                    {
                        Throw("缺少类名");
                    }
                    parents.Add(new Node(Current));
                    Next();
                    if (Current.Value != "," && Current.Value != "{")
                    {
                        Throw("缺少符号：, 或 {");
                    }
                    if (Current.Value == ",")
                        Next();
                }
            }
            Next();
            classNode.Add(new Node("stmt"));
            var stmt = classNode.Children[2];
            while (Current.Value != "}")
            {
                stmt.Add(Work());
                if (stmt.Children[stmt.ChildrenCount - 1].Name == "functionDefine")
                    stmt.Children[stmt.ChildrenCount - 1].Name = "classFunction";
            }
            Next();
            return classNode;
        }

        Node Call()
        {
            Node call = new("call");
            Next();
            if (Current.Type != TokenType.Word)
            {
                Throw("缺少函数名");
            }
            call.Add(new Node(Current.Value));
            Next();
            if (Current.Value != "(")
            {
                Throw("缺少符号：(");
            }
            Next();
            while (Current.Value != ")")
            {
                if (!(Current.Type is TokenType.Word or TokenType.String or TokenType.Integer or TokenType.Float))
                {
                    Throw("内置函数调用缺少参数名，此处：\'" + Current.Value + "\'" + "，词素类型：" + Current.Type + "，词素下标：" + Pos);
                }
                call.Add(new Node(Current));
                Next();
                if (Current.Value != "," && Current.Value != ")")
                {
                    Throw("缺少符号：,");
                }
                if (Current.Value != ")")
                    Next();
            }
            Next();
            if (Current.Value != ";")
            {
                Throw("缺少符号：;");
            }
            Next();
            return call;
        }

        Node Load()
        {
            Node load = new Node("load");
            Next();
            StringBuilder path = new("");
            while (Current.Value != ";")
            {
                if (Current.Type != TokenType.Word)
                {
                    Throw("缺少词");
                }
                path.Append(Current.Value);
                Next();
                if (!(Current.Value is "." or ";"))
                {
                    Throw("缺少符号：.");
                }
                if (Current.Value is ".")
                    Next();
            }
            if (!path.ToString().Contains(':'))
            {
                path = new("..\\" + path);
            }
            Next();
            load.SetValue(path.ToString());
            return load;
        }

        Node If()
        {
            Node ifNode = Read();
            Node stmt = new Node("stmt");
            Skip("(");
            Node condition = Expression();
            ifNode.Add(condition);
            ifNode.Add(stmt);
            Skip(")");
            if (Current.Value == "{")
            {
                Next();
                while (Current.Value != "}")
                {
                    stmt.Add(Work());
                }
                Next();
            }
            else
            {
                stmt.Add(Work());
            }
            if (Current.Value == "else")
            {
                Node elseStmt = Read();
                ifNode.Add(elseStmt);
                if (Current.Value == "{")
                {
                    Next();
                    while (Current.Value != "}")
                    {
                        elseStmt.Add(Work());
                    }
                    Next();
                }
                else
                {
                    elseStmt.Add(Work());
                }
            }
            return ifNode;
        }

        Node While()
        {
            Node whileNode = Read();
            Node stmt = new Node("stmt");
            Skip("(");
            Node condition = Expression();
            whileNode.Add(condition);
            whileNode.Add(stmt);
            Skip(")");
            if (Current.Value == "{")
            {
                Next();
                while (Current.Value != "}")
                {
                    stmt.Add(Work());
                }
                Next();
            }
            else
            {
                stmt.Add(Work());
            }
            return whileNode;
        }

        Node For()
        {
            Node forNode = Read();
            Node stmt = new Node("stmt");
            Skip("(");
            Node start = new Node("start");
            if (Current.Value == ";")
                Skip(";");
            else
            {
                start.Add(SetVariable());
            }
            Node condition = new Node("condition");
            condition.Add(Expression());
            Skip(";");
            Node end = new Node("end");
            if (Current.Value != ")")
                end.Add(Expression());
            if (start.ChildrenCount != 0)
                forNode.Add(start);
            forNode.Add(condition);
            if (end.ChildrenCount != 0)
                forNode.Add(end);
            forNode.Add(stmt);
            Skip(")");
            if (Current.Value == "{")
            {
                Next();
                while (Current.Value != "}")
                {
                    stmt.Add(Work());
                }
                Next();
            }
            else
            {
                stmt.Add(Work());
            }
            return forNode;
        }

        Node Return()
        {
            Node returnNode = Read();
            returnNode.Add(Expression());
            Next();
            return returnNode;
        }

        Node SetVariable()
        {
            Node Set = new("set");
            Set.Add(Expression());
            Next();
            return Set;
        }

        #region 表达式
        Node Expression()
        {
            Node left = Expression_1();
            Node op = new Node(""), right = null;
            Node root = op, pos = op;
            if (IsEOF())
                return left;
            if (Is("=") || Is("+=") || Is("-=")
                || Is("*=") || Is("/="))
            {
                op = Read();
                right = Expression_1();
                op.AddRange(Expr(left, right));
                if (pos.ChildrenCount > 1)
                    pos.Remove(pos.Children[1]);
                pos.Add(op);
                pos = pos.Children[0];
                left = pos.Children[1];
            }
            while (Is("=") || Is("+=") || Is("-=")
                || Is("*=") || Is("/="))
            {
                op = Read();
                right = Expression_1();
                op.AddRange(Expr(left, right));
                if (pos.ChildrenCount > 1)
                    pos.Remove(pos.Children[1]);
                pos.Add(op);
                left = pos.Children[1];
            }
            if (right is not null)
                return root.Children[0];
            return left;
        }

        Node Expression_1()
        {
            Node left = Expression_2();
            Node op = new Node(""), right = null;
            Node root = op, pos = op;
            if (IsEOF())
                return left;
            if (Is("&&") || Is("||"))
            {
                op = Read();
                right = Expression_2();
                op.AddRange(Expr(left, right));
                if (pos.ChildrenCount > 1)
                    pos.Remove(pos.Children[1]);
                pos.Add(op);
                pos = pos.Children[0];
                left = pos.Children[1];
            }
            while (Is("&&") || Is("||"))
            {
                op = Read();
                right = Expression_2();
                op.AddRange(Expr(left, right));
                if (pos.ChildrenCount > 1)
                    pos.Remove(pos.Children[1]);
                pos.Add(op);
                left = pos.Children[1];
            }
            if (right is not null)
                return root.Children[0];
            return left;
        }

        Node Expression_2()
        {
            Node left = Expression_3();
            Node op = new Node(""), right = null;
            Node root = op, pos = op;
            if (IsEOF())
                return left;
            if (Is("==") || Is("!=")
                || Is(">") || Is("<")
                || Is(">=") || Is("<="))
            {
                op = Read();
                right = Expression_3();
                op.AddRange(Expr(left, right));
                if (pos.ChildrenCount > 1)
                    pos.Remove(pos.Children[1]);
                pos.Add(op);
                pos = pos.Children[0];
                left = pos.Children[1];
            }
            while (Is("==") || Is("!=")
                || Is(">") || Is("<")
                || Is(">=") || Is("<="))
            {
                op = Read();
                right = Expression_3();
                op.AddRange(Expr(left, right));
                if (pos.ChildrenCount > 1)
                    pos.Remove(pos.Children[1]);
                pos.Add(op);
                left = pos.Children[1];
            }
            if (right is not null)
                return root.Children[0];
            return left;
        }

        Node Expression_3()
        {
            Node left = Term();
            Node op = new Node(""), right = null;
            Node root = op, pos = op;
            if (IsEOF())
                return left;
            if (Is("+") || Is("-"))
            {
                op = Read();
                right = Term();
                op.AddRange(Expr(left, right));
                if (pos.ChildrenCount > 1)
                    pos.Remove(pos.Children[1]);
                pos.Add(op);
                pos = pos.Children[0];
                left = pos.Children[1];
            }
            while (Is("+") || Is("-"))
            {
                op = Read();
                right = Term();
                op.AddRange(Expr(left, right));
                if (pos.ChildrenCount > 1)
                    pos.Remove(pos.Children[1]);
                pos.Add(op);
                left = pos.Children[1];
            }
            if (right is not null)
                return root.Children[0];
            return left;
        }

        Node Term()
        {
            //Term:    Factor { ( "*" | "/" ) Factor }
            Node left = Point();
            Node op = new Node(""), right = null;
            Node root = op, pos = op;
            if (IsEOF())
                return left;
            if (Is("*") || Is("/"))
            {
                op = Read();
                right = Point();
                op.AddRange(Expr(left, right));
                if (pos.ChildrenCount > 1)
                    pos.Remove(pos.Children[1]);
                pos.Add(op);
                pos = pos.Children[0];
                left = pos.Children[1];
            }
            while (Is("*") || Is("/"))
            {
                op = Read();
                right = Point();
                op.AddRange(Expr(left, right));
                if (pos.ChildrenCount > 1)
                    pos.Remove(pos.Children[1]);
                pos.Add(op);
                left = pos.Children[1];
            }
            if (right is not null)
                return root.Children[0];
            return left;
        }

        Node Point()
        {
            Node left = Factor();
            Node op = new Node(""), right = null;
            Node root = op, pos = op;
            if (IsEOF())
                return left;
            if (Is("."))
            {
                op = Read();
                right = Factor();
                op.AddRange(Expr(left, right));
                if (pos.ChildrenCount > 1)
                    pos.Remove(pos.Children[1]);
                pos.Add(op);
                pos = pos.Children[0];
                left = pos.Children[1];
            }
            while (Is("."))
            {
                op = Read();
                right = Factor();
                op.AddRange(Expr(left, right));
                if (pos.ChildrenCount > 1)
                    pos.Remove(pos.Children[1]);
                pos.Add(op);
                left = pos.Children[1];
            }
            if (right is not null)
                return root.Children[0];
            return left;
        }

        Node Factor()
        {
            //Factor:    FunctionCall | Word | Number | "(" Expression ")"

            //function 或 Word
            if (Current.Value == "call")
            {
                return Call();
            }
            else if (Current.Type == TokenType.Word)
            {
                if (Peek(1).Value is "(")
                    return FunctionCall();
                else
                    return Read();
            }
            else if (Current.Value == "new")
            {
                Next();
                if (Current.Type != TokenType.Word)
                {
                    Throw("new缺少类名");
                }
                var word = Read();
                Skip("(");
                Skip(")");
                Node newNode = new Node("new");
                newNode.Add(word);
                return newNode;
            }
            else if (Is("("))
            {
                if (Peek(1).Type is TokenType.Word && Peek(2).Value is ")"
                        && (Peek(3).Type is TokenType.Word or TokenType.Integer or TokenType.Float) || Peek(3).Value is "(")//类型转换语法
                {
                    Node typeTransform = new Node("toType");
                    Next();
                    typeTransform.Add(Read());
                    Next();
                    if (Current.Value is "(")
                    {
                        Next();
                        typeTransform.Add(Expression());
                        Next();
                    }
                    else
                        typeTransform.Add(Read());
                    return typeTransform;
                }
                Next();
                Node e = Expression();
                Skip(")");
                return e;
            }
            else if (Current.Type is TokenType.Integer or TokenType.Float)
                return Read();
            else if (Current.Type is TokenType.String)
                return Read();
            else if (Current.Value == "!")
            {
                Node reverse = Read();
                reverse.Add(Factor());
                return reverse;
            }
            else if (Current.Type == TokenType.Bool)
                return Read();
            //else
            //    Throw("因子；若是空表达式，请无视该报错");
            return new Node("NullableNode");
        }
        #endregion
    }
}
