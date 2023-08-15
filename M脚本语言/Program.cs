

using System.Runtime.InteropServices;
using System.Text;

namespace MScript
{
    internal class Program
    {
        [DllImport("kernel32")]
        static extern bool QueryPerformanceFrequency(ref long PerformanceFrequency);

        public static long Count = 0;
        static void Main(string[] args)
        {
            QueryPerformanceFrequency(ref Count);
            string path = "";
            if (args.Length != 0)
            {
                path = args[0];
            }
            else if (File.Exists(System.AppDomain.CurrentDomain.BaseDirectory + "\\config.cfg"))
            {
                StreamReader srr = new StreamReader(System.AppDomain.CurrentDomain.BaseDirectory + "\\config.cfg");
                string commands = srr.ReadToEnd();
                if (commands.StartsWith("start "))
                    commands = commands.Substring(6);
                path = commands;
            }
            else
            {
#if RELEASE
                Console.Write("> ");
                path = Console.ReadLine();
#endif
#if DEBUG
                path = "F:\\VS_WorkSpace\\M脚本语言\\M脚本语言\\代码.ms";
#endif
            }

            if (!path.Contains(":"))
            {
                if (path.StartsWith(".\\"))
                    path = path.Substring(2);
                path = System.AppDomain.CurrentDomain.BaseDirectory + path;
            }
            if (!path.EndsWith(".ms"))
                path = path + ".ms";
            //Console.WriteLine(path);


            if (!File.Exists(path))
            {
                Console.WriteLine("路径不存在");
                Console.ReadKey();
                System.Environment.Exit(0);
            }
            Console.WriteLine($"Run \'{path}\'.");
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            StreamReader sr = new(path, Encoding.GetEncoding("GB2312"));
            //StreamReader sr = new("F:\\VS_WorkSpace\\M脚本语言\\M脚本语言\\代码.ms", Encoding.GetEncoding("GB2312"));
            Lexer lexer = new Lexer(path, sr.ReadToEnd());
            //Console.WriteLine(lexer.Text);
            var tokens = lexer.GetAllTokens();
            Parser parser = new Parser(tokens);
            var root = parser.Scan();


            Environment env = new Environment();
            env.SetCurrent("__name__", new Node(new Token("main", TokenType.String, -1)));
            Evaluater evaluater = new Evaluater(root, env);
            evaluater.Eval();
        }
    }
}