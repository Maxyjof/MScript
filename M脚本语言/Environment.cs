

using System.Data;
using System.Xml.Linq;

namespace MScript
{
    internal class Environment
    {
        public Environment Parent = null;
        public Dictionary<string, Node> Variables = new();

        public Environment()
        {

        }

        public Environment(Environment parent)
        {
            Parent = parent;
        }

        public bool Has(string name)
        {
            if (Parent != null)
            {
                if (Parent.Has(name)) return true;
            }
            return Variables.ContainsKey(name);
        }

        public bool SelfHas(string name)
        {
            return Variables.ContainsKey(name);
        }

        /// <summary>
        /// 只在当前环境Set，不对上层环境寻找再Set
        /// </summary>
        /// <param name="name"></param>
        /// <param name="node"></param>
        public Node SetCurrent(string name, Node node)
        {
            if (Variables.ContainsKey(name))
            {
                if (node.Type is TokenType.Word)//Word说明这个值也是一个变量，所以要在环境里找到这个变量的值
                {
                    Variables[name].SetValue(GetCurrent(node.Name).Token.Value);
                    return Variables[name];
                }
                Variables[name] = node;
                return node;
            }
            else
            {
                Variables.Add(name, node);
                return node;
            }
        }

        public Node Set(string name, Node node)
        {
            if (Variables.ContainsKey(name))
            {
                if (node.Type is TokenType.Word)//Word说明这个值也是一个变量，所以要在环境里找到这个变量的值
                {
                    Variables[name].SetValue(Get(node.Name).Token.Value);
                    return Variables[name];
                }
                Variables[name].SetValue(node.Value);
                Variables[name].Children = node.Children;
                return node;
            }
            else
            {
                if (Parent != null && Parent.Has(name))
                {
                    return Parent.Set(name, node);
                }
                else
                {
                    if (node.Type is TokenType.Word)
                    {
                        Variables.Add(name, new Node(Get(node.Name).Token));
                        return Variables[name];
                    }
                    Variables.Add(name, node);
                    return node;
                }
            }
        }

        //public Node Set(string name, string value)
        //{
        //    return Set(name, new Node(value));
        //}

        public Node Get(string name)
        {
            if (Variables.ContainsKey(name))
            {
                return Variables[name];
            }
            else
            {
                if (Parent != null && Parent.Has(name))
                    return Parent.Get(name);
                else
                    throw new Exception("未定义该变量：" + name);
            }
        }

        public Node GetCurrent(string name)
        {
            if (Variables.ContainsKey(name))
            {
                return Variables[name];
            }
            else
            {
                throw new Exception("未定义该变量：" + name);
            }
        }

        //public void Add(Environment e)
        //{
        //    foreach (var item in e.Variables)
        //    {
        //        Variables.Append(item);
        //    }
        //}
    }
}
