

using System.Text.RegularExpressions;

namespace MScript
{
    internal class Lexer
    {
        public string FileName = "";
        public string Text = "";

        int Pos = 0;
        int Row = 1;
        List<Token> Tokens = new List<Token>();
        Token Token = new();
        bool hasToken = false;

        Regex Space = new(@"^[\t\x20]+");
        Regex Return = new(@"^[\r]?\n");
        Regex Word = new(@"^[_a-zA-Z][_a-zA-Z0-9]*");
        Regex String1 = new("^\\\"([^\\\"]*)\\\"");
        Regex String2 = new("^'([^']*)'");
        Regex Condition = new(@"^((==)|(!=)|(>=)|(<=)|(>)|(<)|(&&)|(\|\|))");
        Regex CompoundSymbol = new(@"^((=)|(\+=)|(-=)|(\*=)|(/=))");
        Regex Symbol = new(@"^[\.,!(){};=+\-*/]");
        Regex Integer = new(@"^[+-]?[\d]+");
        Regex Float = new(@"^[+-]?[\d]+\.[\d]+");
        Regex Bool = new(@"^true|^false");

        public Lexer(string fileName, string text)
        {
            FileName = fileName;
            Text = text;
        }

        public List<Token> GetAllTokens()
        {
            while (Pos < Text.Length)
                Read();
            return Tokens;
        }

        void Read()
        {
            hasToken = false;
            Token = new Token("", TokenType.BadToken, Row);

            while (Return.IsMatch(Text[Pos..]))
            {
                Row++;
                hasToken = true;
                Token = new Token("\n", TokenType.Symbol, Row);
                Tokens.Add(Token);
                Pos += Return.Match(Text[Pos..]).Length;
            }
            Check(Space, TokenType.BadToken);
            Check(String1, TokenType.String);
            Check(String2, TokenType.String);
            Check(Condition, TokenType.Symbol);
            Check(Bool, TokenType.Bool);
            Check(Float, TokenType.Float);
            Check(Integer, TokenType.Integer);
            Check(CompoundSymbol, TokenType.Symbol);
            Check(Symbol, TokenType.Symbol);

            foreach (var item in GetKeywords())
            {
                if (Regex.IsMatch(Text[Pos..], item))
                {
                    hasToken = true;
                    string name = Regex.Match(Text[Pos..], item).Value;
                    Token = new Token(name, TokenType.Keyword, Row);
                    Tokens.Add(Token);
                    Pos += name.Length;
                }
            }

            Check(Word, TokenType.Word);
            if (!hasToken)
            {
                Tokens.Add(Token);
                Pos++;
            }
        }

        void Check(Regex regex, TokenType type)
        {
            if (regex.IsMatch(Text[Pos..]))
            {
                hasToken = true;
                string name = regex.Match(Text[Pos..]).Value;
                Pos += name.Length;
                if (type == TokenType.String)
                {
                    name = name[1..^1];
                }
                if (type != TokenType.BadToken)
                {
                    Token = new Token(name, type, Row);
                    Tokens.Add(Token);
                }
            }
        }

        List<string> GetKeywords()
        {
            List<string> result = new List<string>()
            {
                "^def",
                "^call",
                "^load",
                "^if",
                "^while",
                "^for",
                "^new",
                "^return",
            };
            return result;
        }
    }
}
