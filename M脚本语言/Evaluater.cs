
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace MScript
{
    internal class Evaluater
    {
        [DllImport("kernel32.dll ")]
        static extern bool QueryPerformanceCounter(ref long lpPerformanceCount);

        Environment Env;
        Node Root;

        public Evaluater(Node root)
        {
            Root = root;
            Env = new Environment();
        }

        public Evaluater(Node root, Environment e)
        {
            Root = root;
            Env = e;
        }

        void Throw(Node node, string str)
        {
#if DEBUG
            throw new Exception($"第{node.Token.Row}行：" + str);
#endif
#if RELEASE
            if (node.Token.Row != -1)
                Console.WriteLine($"第{node.Token.Row}行：" + str);
            else
                Console.WriteLine($"> " + str);
            Console.ReadKey();
            System.Environment.Exit(0);
#endif
        }

        public void Eval()
        {
            List<Node> firstNodes = TakeFirstRunNodes(Root.Children);
            foreach (var item in firstNodes)//优先解析的节点，一些定义和加载
            {
                if (Eval(item, Env) is not null)
                    break;
            }
            foreach (var item in Root.Children)
            {
                if (item.Name == "NullableNode")
                    continue;
                if (Eval(item, Env) is not null)
                    break;
            }
        }

        List<Node> TakeFirstRunNodes(List<Node> nodes)
        {
            List<Node> result = new();

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Name is "functionDefine" or "classDefine" or "load")
                {
                    result.Add(nodes[i]);
                    nodes.Remove(nodes[i]);
                    i--;
                }
            }

            return result;
        }

        Node Eval(Node node, Environment env)
        {
            switch (node.Name)
            {
                case "toType":
                case ".":
                case "+=":
                case "-=":
                case "*=":
                case "/=":
                    ExprEval(node, env);
                    break;
                case "=":
                    //带点的不是变量，是成员
                    if (node.Left.Name == ".")
                    {
                        //var classMembers = ExprEval(node.Left.Left, env);
                        //var members = ClassInstanceVarToEnv(GetClassInstanceVar(node.Left.Left, env));
                        var memberName = node.Left.Right;
                        while (memberName.Name == ".")
                            memberName = memberName.Right;

                        var members = GetClassInstanceStmt(node.Left.Left, env);
                        foreach (var item in members.Children)
                        {
                            if (item.Name == memberName.Name)
                            {
                                if (item.Left.Type is TokenType.Integer or TokenType.Float or TokenType.String or TokenType.Bool)
                                {
                                    item.Children[0].SetValue(ExprEval(node.Right, env).Value);
                                    break;
                                }
                                item.Children[0] = ExprEval(node.Right, env);
                                break;
                            }
                        }
                    }
                    else
                        env.Set(node.Left.Left.Name, ExprEval(node.Left.Right, env));
                    break;
                case "set":
                    if (env.Has(node.Left.Left.Name))
                        //env.Get(node.Left.Left.Name).SetValue(ExprEval(node.Left.Right, env).Value);
                        env.Set(node.Left.Left.Name, ExprEval(node.Left.Right, env));
                    else
                        env.Set(node.Left.Left.Name, ExprEval(node.Left.Right, env));
                    break;
                case "call":
                    Call(node, env);
                    break;
                case "load":
                    Load(node);
                    break;
                case "classFunction":
                case "functionDefine":
                    node.Env = env;
                    env.Set(node.Left.Name, node);
                    break;
                case "functionCall":
                    FunctionCall(node, env);
                    break;
                case "if":
                    return If(node, env);
                case "while":
                    return While(node, env);
                case "for":
                    return For(node, env);
                case "classDefine":
                    env.Set(node.Left.Name, node);
                    break;
                case "stmt":
                    foreach (var item in node.Children)
                    {
                        if (Eval(item, env) is not null)
                            return new Node("");
                    }
                    break;
                case "comment":
                    break;
                case "return":
                    return ExprEval(node.Left, env);
                case "break":
                    return new Node("break");
                case "continue":
                    return new Node("continue");
                default:
                    if (node.Type is TokenType.Word)//单纯一个词所构成的语句，说明是一个没有值的set语句
                    {
                        env.Set(node.Value, new Node("0"));
                        return null;
                    }
                    Throw(node, "未知的语法结构");
                    break;
            }
            return null;
        }

        Node ToType(Node node, Environment env)
        {
            if (Env.Has(node.Left.Name))//转换目标是一个类
            {
                //将目标类的不同的成员复制过来，自身的不同的成员删掉
            }
            else//转换目标是基础类型
            {
                //基础类型之间可互相转换的只能是int跟float
                if (node.Left.Name is "int")
                {
                    if (BigInteger.TryParse(ExprEval(node.Right, env).Value, out BigInteger BigIntegerResult))
                    {
                        var n = new Node(int.Parse(ExprEval(node.Right, env).Value).ToString());
                        n.Token.Type = TokenType.Integer;
                        return n;
                    }
                }
                else
                {
                    if (float.TryParse(ExprEval(node.Right, env).Value, out float floatResult))
                    {
                        var n = new Node(float.Parse(ExprEval(node.Right, env).Value).ToString());
                        n.Token.Type = TokenType.Float;
                        return n;
                    }
                }
            }
            Throw(node, "不允许的类型转换");
            return null;
        }

        Node FunctionCall(Node node, Environment env)
        {
            Node ChildfunctionCall = null;
            if (node.Left.Name == "functionCall")
            {
                ChildfunctionCall = FunctionCall(node.Left, env);
                if (ChildfunctionCall is null)
                    Throw(node, "函数没有返回值，因此不能对其返回值执行函数调用");
                if (ChildfunctionCall.Name != "functionDefine")
                    Throw(node, "函数返回值不是函数，因此不能对其返回值执行函数调用");
            }
            Environment e = null;
            Node RealFunction = null;
            if (ChildfunctionCall is not null)//说明是一个闭包函数
                RealFunction = ChildfunctionCall;
            else
                RealFunction = env.Get(node.Left.Name);

            if (Env.Has(RealFunction.Left.Name))//全局存在该函数，说明它是全局函数
            {
                e = new Environment(Env);
            }
            else if (env.SelfHas(RealFunction.Left.Name))//目前独立的函数支持全局函数，也支持内部函数(闭包函数)了
            {
                e = new Environment(env);
            }
            else if (node.Name == "classFunction")//类实例函数
            {
                e = new Environment(env);
            }
            else//其他地方来的闭包函数
            {
                if (ChildfunctionCall is not null)
                    e = new Environment(ChildfunctionCall.Env);
                else
                    e = new Environment(env.Get(node.Left.Name).Env);
            }
            //函数参数序列本体
            Node funcValues = null;
            if (ChildfunctionCall is not null)
                funcValues = ChildfunctionCall.Right;
            else
                funcValues = RealFunction.Right;
            for (int i = 1; i < node.ChildrenCount; i++)//遍历函数的实参
            {
                //逐个添加进e环境
                //将形参名+实参值加入e环境
                e.SetCurrent(funcValues.Children[i - 1].Name, ExprEval(node.Children[i], env));
                //这一块没写完
                //123123这块函数方面的记录和调用都有点毛病
            }
            //执行
            Node realFunc = null;
            Node stmt = null;
            if (ChildfunctionCall is not null)
                stmt = ChildfunctionCall.Children[2];
            else
            {
                realFunc = env.Get(node.Left.Name);
                stmt = realFunc.Children[2];
            }
            Node result = null;
            for (int i = 0; i < stmt.ChildrenCount; i++)
            {
                result = Eval(stmt.Children[i], e);
                if (result is not null)
                    return result;
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instanceNameNode">类实例名称，可带点嵌套</param>
        /// <param name="env"></param>
        /// <returns></returns>
        Node GetClassInstanceStmt(Node instanceNameNode, Environment env)
        {
            if (instanceNameNode.Name == ".")
            {
                //左类实例的集合中寻找右类实例的集合
                var stmt = env.Get(instanceNameNode.Left.Name);
                Node result = null;
                if (instanceNameNode.Right.Name != ".")
                {
                    foreach (var item in stmt.Children)
                    {
                        if (item.Name == instanceNameNode.Right.Name)
                        {
                            result = item.Left;
                            break;
                        }
                    }
                    if (result is not null)
                        return result;
                    else
                    {
                        Throw(instanceNameNode, instanceNameNode + "类中不存在" + instanceNameNode.Right + "成员");
                        return new Node("NullableNode");
                    }
                }
                else
                {
                    Environment e = new();
                    foreach (var item in stmt.Children)
                    {
                        e.SetCurrent(item.Name, item.Left);
                    }
                    return GetClassInstanceStmt(instanceNameNode.Right, e);
                }
            }
            return env.Get(instanceNameNode.Name);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instanceNameNode">类实例名称</param>
        /// <param name="memberName"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        Node GetClassInstanceMember(Node instanceNameNode, string memberName, Environment env)
        {
            var stmt = GetClassInstanceStmt(instanceNameNode, env);
            foreach (var item in stmt.Children)
            {
                if (item.Name == memberName)
                    return item;
            }
            Throw(instanceNameNode, instanceNameNode.Name + "类中不存在该成员：" + memberName);
            return new Node("NullableNode");
        }

        /// <summary>
        /// 传入类实例Node，stmt
        /// </summary>
        /// <param name="instanceNode"></param>
        /// <returns></returns>
        Environment ClassInstanceToEnv(Node instanceNode)
        {
            Environment e = new(Env);
            foreach (var item in instanceNode.Children)
            {
                //只将类内容中的变量定义和函数定义存入
                e.SetCurrent(item.Name, item.Left);
            }
            return e;
        }

        Node ClassFunctionCall(Node classInstance, Node funcCall, Environment currentEnv)
        {
            Environment classInstanceEnv = new Environment(Env);
            Environment classFuncEnv = new Environment(classInstanceEnv);
            var members = currentEnv.Get(classInstance.Name).Children;
            foreach (var item in members)
            {
                classInstanceEnv.SetCurrent(item.Name, item.Left);
            }
            var funcName = funcCall.Left.Name;
            var memberFunc = GetClassInstanceMember(classInstance, funcName, currentEnv);
            for (int i = 1; i < funcCall.ChildrenCount; i++)//遍历函数的实参
            {
                Node formatFuncValues = memberFunc.Left.Right;
                classFuncEnv.SetCurrent(formatFuncValues.Children[i - 1].Name, ExprEval(funcCall.Children[i], currentEnv));
            }
            //获取函数stmt
            Node realFunc = memberFunc.Left;
            Node stmt = realFunc.Children[2];
            //执行
            for (int i = 0; i < stmt.ChildrenCount; i++)
            {
                var result = Eval(stmt.Children[i], classFuncEnv);
                if (result is not null)
                    return result;
            }
            return new Node("NoReturnResult");
        }

        Node If(Node node, Environment env)
        {
            Environment ifEnv = new Environment(env);
            if (ExprEval(node.Left, env).Value == "true")
            {
                var ifstmt = node.Right;
                for (int i = 0; i < ifstmt.ChildrenCount; i++)
                {
                    var result = Eval(ifstmt.Children[i], ifEnv);
                    if (result is not null)
                        return result;
                }
            }
            else
            {
                if (node.ChildrenCount < 3)
                    return null;
                var elsestmt = node.Children[2];
                for (int i = 0; i < elsestmt.ChildrenCount; i++)
                {
                    var result = Eval(elsestmt.Children[i], ifEnv);
                    if (result is not null)
                        return result;
                }
            }
            return null;
        }

        Node While(Node node, Environment env)
        {
            Environment whileEnv = new Environment(env);
            var whilestmt = node.Right;
            while (ExprEval(node.Left, env).Value == "true")
            {
                for (int i = 0; i < whilestmt.ChildrenCount; i++)
                {
                    var stmtResult = Eval(whilestmt.Children[i], whileEnv);
                    if (stmtResult.Value is "break")
                        return null;
                    if (stmtResult.Value is "continue")
                        break;
                    if (stmtResult is not null)
                        return new Node("");
                }
            }
            return null;
        }

        Node For(Node node, Environment env)
        {
            Environment forEnv = new Environment(env);

            Node start = null, condition = null, end = null, forStmt = null;
            foreach (var item in node.Children)
            {
                if (item.Name == "start")
                    start = item.Left;
                else if (item.Name == "condition")
                    condition = item.Left;
                else if (item.Name == "end")
                    end = item.Left;
                else if (item.Name == "stmt")
                    forStmt = item;
            }
            if (condition is null)
                Throw(start, "for循环缺少条件");

            if (start is not null)
            {
                ExprEval(start, forEnv);
            }

            if (forStmt is not null)
                while (ExprEval(condition, forEnv).Value == "true")
                {
                    for (int i = 0; i < forStmt.ChildrenCount; i++)
                    {
                        var stmtResult = Eval(forStmt.Children[i], forEnv);
                        if (stmtResult is null)
                            continue;
                        if (stmtResult.Value is "break")
                            return null;
                        if (stmtResult.Value is "continue")
                            break;
                        if (stmtResult is not null)
                            return new Node("");
                    }
                    if (end is not null)
                        //Eval(end, forEnv);
                        ExprEval(end, forEnv);
                }
            return null;
        }

        Node ExprEval(Node op, Environment env)
        {
            if (op.Type != TokenType.String && op.Type != TokenType.Word)
                switch (op.Name)
                {
                    case "toType":
                        return ToType(op, env);
                    case "+":
                        return ExprEval(op.Left, env) + ExprEval(op.Right, env);
                    case "-":
                        return ExprEval(op.Left, env) - ExprEval(op.Right, env);
                    case "*":
                        return ExprEval(op.Left, env) * ExprEval(op.Right, env);
                    case "/":
                        //return ExprEval(op.Left, env) / ExprEval(op.Right, env);
                        var l = ExprEval(op.Left, env);
                        var r = ExprEval(op.Right, env);
                        return l / r;
                    case "==":
                        return ExprEval(op.Left, env) == ExprEval(op.Right, env);
                    case "!=":
                        return ExprEval(op.Left, env) != ExprEval(op.Right, env);
                    case ">=":
                        return ExprEval(op.Left, env) >= ExprEval(op.Right, env);
                    case "<=":
                        return ExprEval(op.Left, env) <= ExprEval(op.Right, env);
                    case ">":
                        return ExprEval(op.Left, env) > ExprEval(op.Right, env);
                    case "<":
                        return ExprEval(op.Left, env) < ExprEval(op.Right, env);
                    case "!":
                        return !env.Get(op.Left.Name);
                    case "&&":
                        var condition = bool.Parse(ExprEval(op.Left, env).Value) && bool.Parse(ExprEval(op.Right, env).Value);
                        var result = new Node(condition ? "true" : "false");
                        result.SetValue(condition.ToString().ToLower());
                        return result;
                    case "||":
                        var condition1 = bool.Parse(ExprEval(op.Left, env).Value) || bool.Parse(ExprEval(op.Right, env).Value);
                        var result1 = new Node(condition1 ? "true" : "false");
                        result1.SetValue(condition1.ToString().ToLower());
                        return result1;
                    case "+=":
                        env.Get(op.Left.Name).SetValue((ExprEval(op.Left, env) + ExprEval(op.Right, env)).Value);
                        return env.Get(op.Left.Name);
                    case "-=":
                        env.Get(op.Left.Name).SetValue((ExprEval(op.Left, env) - ExprEval(op.Right, env)).Value);
                        return env.Get(op.Left.Name);
                    case "*=":
                        env.Get(op.Left.Name).SetValue((ExprEval(op.Left, env) * ExprEval(op.Right, env)).Value);
                        return env.Get(op.Left.Name);
                    case "/=":
                        env.Get(op.Left.Name).SetValue((ExprEval(op.Left, env) / ExprEval(op.Right, env)).Value);
                        return env.Get(op.Left.Name);
                    case "=":
                        env.Set(op.Left.Name, ExprEval(op.Right, env));
                        return op.Right;
                    case "set":
                        var setResult = ExprEval(op.Left, env);
                        return setResult;
                    case "functionCall":
                        return FunctionCall(op, env);
                    case "call":
                        return Call(op, env);
                    case "new":
                        //通过类名获取类本体
                        var classSelf = Env.Get(op.Left.Name);
                        Node classStmt = null;
                        Environment envInstance = new(Env);
                        if (classSelf.ChildrenCount == 2)
                            classStmt = classSelf.Right;
                        else
                            classStmt = classSelf.Children[2];
                        Eval(classStmt, envInstance);
                        //新建一个环境，将类本体内容执行，结果中的所有变量（成员）存储到新环境
                        //向当前环境添加
                        //name:a
                        //将新环境中的全部变量的引用作为当前环境中名称a的值："stmt"的子节点们
                        Node stmt = new Node("stmt");//stmt底下全都是变量名->值
                        foreach (var i in envInstance.Variables)
                        {
                            var item = new Node(i.Key);
                            item.Add(i.Value);
                            stmt.Add(item);
                        }
                        return stmt;
                    case "."://类成员
                        var instanceNameNode = op.Left;
                        var member = op.Right;
                        var memberName = member.Name;
                        if (memberName == ".")
                            memberName = member.Left.Name;

                        bool isFunction = false;
                        if (memberName == "functionCall")
                        {
                            isFunction = true;
                            memberName = op.Right.Left.Name;
                        }
                        var memberNode = GetClassInstanceMember(instanceNameNode, memberName, env);
                        Node searchResult = null;
                        if (isFunction)//如果以函数形式执行 a.b()
                        {
                            searchResult = ClassFunctionCall(instanceNameNode, op.Right, env);
                        }
                        else//如果是变量形式执行 a.b
                        {
                            if (member.Name == ".")//说明当前获取到的只是a.b.c中的b类，还需要再获取c
                            {
                                //此时，"b"是item.Name，b类的本体在item.Left
                                //return ExprEval(c,b的实例环境)
                                return ExprEval(member.Right, ClassInstanceToEnv(memberNode.Left));
                            }
                            else if (memberNode.Left.Name == "functionDefine")//如果a.b是一个函数
                            {//则应当返回{类实例，函数体}
                                Node classFunctionNode = new Node("classFunction");
                                Node classIns = env.Get(instanceNameNode.Name);
                                classFunctionNode.Add(classIns);
                                classFunctionNode.Add(memberNode.Left);
                                return classFunctionNode;
                            }
                            searchResult = memberNode.Left;
                        }
                        if (searchResult is null)
                            Throw(op, $"该类中不存在此成员");
                        return searchResult;
                    default:
                        //可能是数字
                        //也可能是字符串
                        if (op.Type is TokenType.Integer or TokenType.Float or TokenType.String or TokenType.Bool)
                            return op;
                        Throw(op, "未知的运算符：" + op.Name);
                        break;
                }
            else
            {
                //也可能是字符串
                if (op.Type is TokenType.String)
                    return op;
                if (op.Type is TokenType.Word)
                {
                    return env.Get(op.Name);
                }
                Throw(op, "未知的运算符：" + op.Name);
            }
            return new Node("NullableNode");
        }

        Node Call(Node node, Environment env)
        {
            string funcName = node.Left.Name;
            List<Token> values = new();
            for (int i = 1; i < node.ChildrenCount; i++)
            {
                values.Add(node.Children[i].Token);
            }
            switch (funcName)
            {
                case "Write":
                    var writeValue = node.Right;
                    if (writeValue.Token.Type is TokenType.Integer or TokenType.Float or TokenType.String)
                        Console.WriteLine(writeValue.Name);
                    else if (writeValue.Token.Type is TokenType.Word)
                    {
                        Console.WriteLine(env.Get(writeValue.Name).Value);
                    }
                    else
                    {
                        Throw(writeValue, "无法输出的对象");
                    }
                    break;
                case "Read":
                    string str = "";
                    if (node.ChildrenCount > 1)
                        str = env.Get(node.Right.Name).Value;
                    if (str != "")
                        Console.Write(str);
                    var n = new Node(Console.ReadLine());
                    n.SetValue(n.Name);
                    return n;
                case "Pause":
                    Console.ReadKey();
                    break;
                case "QueryPerformanceCounter":
                    long time = 0;
                    QueryPerformanceCounter(ref time);
                    return new Node(new Token(time.ToString(), TokenType.Integer, node.Token.Row));
                case "QueryPerformanceFrequency":
                    return new Node(new Token(Program.Count.ToString(), TokenType.Integer, node.Token.Row));
                case "TickCount":
                    return new Node(new Token(System.Environment.TickCount.ToString(), TokenType.Integer, node.Token.Row));
                default:
                    Throw(node, "未知的内置函数");
                    break;
            }
            return null;
        }

        void Load(Node node)
        {
            string path = "";
            if (node.Value.StartsWith("..\\"))//相对
            {
                path = System.AppDomain.CurrentDomain.BaseDirectory + "\\Lib" + node.Value.Remove(0, 2);
            }
            else//绝对
            {
                path = node.Value;
            }
            path += ".ms";
            if (!File.Exists(path))
            {
                Throw(node, node.Value + "模块不存在！");
            }
            StreamReader sr = new(path, Encoding.GetEncoding("GB2312"));

            Lexer lexer = new Lexer(path, sr.ReadToEnd());
            var tokens = lexer.GetAllTokens();
            Parser parser = new Parser(tokens);
            var root = parser.Scan();
            Environment env = new Environment();
            env.SetCurrent("__name__", new Node(new Token("module", TokenType.String, -1)));
            Evaluater evaluater = new Evaluater(root, env);
            evaluater.Eval();
            Env.Parent = evaluater.Env;
        }
    }
}
