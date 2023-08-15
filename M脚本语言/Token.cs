
namespace MScript
{
    public enum TokenType
    {
        BadToken,

        Bool,
        Integer,
        Float,
        String,

        Word,
        Keyword,
        Symbol,
    }

    internal class Token
    {
        public string Value = "";
        public TokenType Type = TokenType.BadToken;
        public int Row = -1;
        public Token()
        {
            
        }
        public Token(string value, TokenType type, int row)
        {
            if (value != null)
                Value = value;
            Type = type;
            Row = row;
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
