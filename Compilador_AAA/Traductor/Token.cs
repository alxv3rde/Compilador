using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compilador_AAA.Traductor
{
    public class Token
    {
        public TokenType Type { get; }
        public string Value { get; }
        public int Start { get; } // Posición de inicio del token
        public int End { get; }   // Posición final del token
        public int StartLine { get; } // Línea de inicio del token
        public int EndLine { get; }   // Línea de fin del token

        public Token(TokenType type, string value, int start, int end, int startLine, int endLine)
        {
            Type = type;
            Value = value;
            Start = start;
            End = end;
            StartLine = startLine;
            EndLine = endLine;
        }

        public override string ToString()
        {
            return $"{Type}: {Value} (StartColumn: {Start}, EndColumn: {End}, StartLine: {StartLine} EndLine:{EndLine})";
        }
    }

}
