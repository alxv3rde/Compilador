using Compilador_AAA.Views;
using ICSharpCode.AvalonEdit.Document;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Compilador_AAA.Traductor
{
    public enum TokenType
    {
        Public,
        Private,
        Protected,
        Internal,
        Constant,
        Keyword,
        Identifier,
        Literal,
        IntegerLiteral,
        DoubleLiteral,
        StringLiteral,
        Operator,
        BinaryOperator,
        Equals,
        OpenParen,
        CloseParen,
        OpenBrace,
        CloseBrace,
        OpenBracket,
        CloseBracket,
        Semicolon,
        Comma,
        SingleLineComment,
        MultiLineComment,
        EOF,
        UnrecognizedCharacter,
        BooleanLiteral,
    }

    public class Lexer
    {
        private TextDocument _code; // El código fuente
        private int _position; // Posición actual en el código
        private int _currentLine;

        private Dictionary<TokenType, string> _tokenPatterns = new Dictionary<TokenType, string>()
    {
        { TokenType.SingleLineComment, @"//.*" },
        { TokenType.Constant, @"\bconst\b" },
        { TokenType.MultiLineComment, @"/\*[\s\S]*?\*/" },
        { TokenType.Public, @"\bpublic\b" },
        { TokenType.Private, @"\bprivate\b" },
        { TokenType.Protected, @"\bprotected\b" },
        { TokenType.Internal, @"\binternal\b" },
        { TokenType.Keyword, @"\b(entero|si|para|mientras|return|cadena|char|decimal|bool|class|func|imprime|sino|hacer)\b" },
        { TokenType.DoubleLiteral,  @"(?<!\w)(\.\d+|\d+\.\d+(\.\d+)*)(e[+-]?\d+)?(?!\w)" },
        { TokenType.IntegerLiteral, @"(?<!\w)\d+(?!\w)" },
        { TokenType.BooleanLiteral, @"\b(true|false)\b" },
        { TokenType.Identifier, @"\b[a-zA-Z0-9_]+\b" },
        { TokenType.Operator, @"&&|\|\||==|[+\-*/%&|^!<>]=?" },
        { TokenType.Equals, @"=" },
        { TokenType.BinaryOperator, @"[+\-*/%&|^!=<>]" },
        { TokenType.OpenParen, @"\(" },
        { TokenType.CloseParen, @"\)" },
        { TokenType.OpenBrace, @"\{" },
        { TokenType.CloseBrace, @"\}" },
        { TokenType.OpenBracket, @"\[" },
        { TokenType.CloseBracket, @"\]" },
        { TokenType.Comma, @"," },
        { TokenType.Semicolon, @";" },


    };

        public Lexer(TextDocument code)
        {
            _code = code;
            _position = 0;
        }

        public Dictionary<int, List<Token>> Tokenize()
        {
            Dictionary<int, List<Token>> tokensByLine = new Dictionary<int, List<Token>>();
            _currentLine = 1;
            List<Token> currentTokens = new List<Token>();

            for (int lineNumber = 1; lineNumber <= _code.LineCount; lineNumber++)
            {
                // Obtener la línea específica por su número
                DocumentLine line = _code.GetLineByNumber(lineNumber);

                // Obtener el texto de la línea
                string lineText = _code.GetText(line);
                _position = 0; // Reiniciar la posición para cada línea

                while (_position < lineText.Length)
                {
                    // Ignorar espacios en blanco
                    if (char.IsWhiteSpace(lineText[_position]))
                    {
                        _position++;
                        continue;
                    }

                    // Obtener el siguiente token
                    Token token = GetNextToken(lineText);
                    if (token != null)
                    {
                        // Verificar identificadores que comienzan con un número
                        if (token.Type == TokenType.Identifier && char.IsDigit(token.Value[0]))
                        {
                            TranslatorView.HandleError($"Identificador no puede comenzar con un número en la posición {_position}: '{token.Value}'", token.EndLine, "LEX001");
                        }

                        // Verificar números mal formateados con múltiples puntos decimales
                        if (token.Type == TokenType.DoubleLiteral && token.Value.Count(c => c == '.') > 1)
                        {
                            TranslatorView.HandleError($"Número mal formateado en la posición {_position}: '{token.Value}'", token.EndLine, "LEX002");
                        }

                        currentTokens.Add(token);
                    }
                    else
                    {
                        // Manejo de errores
                        TranslatorView.HandleError($"Token no reconocido en la posición {_position}: '{lineText[_position]}'", _currentLine, "LEX003");
                        _position++;
                    }
                }

                // Agregar los tokens de la última línea si hay alguno
                if (currentTokens.Count > 0)
                {
                    tokensByLine[_currentLine] = currentTokens.ToList();
                    _currentLine++;
                    currentTokens.Clear();
                }
            }

            // Agregar el token EOF al final
            currentTokens.Add(new Token(TokenType.EOF, "", _position, _position, _currentLine, _currentLine));
            tokensByLine[_currentLine] = currentTokens;

            return tokensByLine;
        }

        private Token GetNextToken(string lineText)
        {
            // Contadores para la línea y la posición en la línea
            int startLine = _currentLine;
            int startColumn = 1;
            // Verificar manualmente los literales de cadena
            if (lineText[_position] == '\"' || lineText[_position] == '\'')
            {
                char quoteChar = lineText[_position];
                int start = _position++;
                while (_position < lineText.Length && lineText[_position] != quoteChar)
                {
                    if (lineText[_position] == '\n') // Incrementar la línea si encontramos un salto de línea
                    {
                        startLine++;
                        startColumn = 1;
                    }
                    else
                    {
                        startColumn++;
                    }
                    _position++;
                }

                // Si alcanzamos el final sin encontrar la comilla de cierre
                if (_position >= lineText.Length || lineText[_position] != quoteChar)
                {
                    TranslatorView.HandleError($"Literal de cadena sin cerrar que comienza en la posición {start}.", startLine, "LEX004");
                }
                else
                {
                    // Avanzar la posición para incluir la comilla de cierre
                    _position++;
                }

                string value = lineText.Substring(start, _position - start);
                return new Token(TokenType.StringLiteral, value, start, _position - 1, startLine, startLine); // Fin es _position - 1
            }

            // Buscar otros tokens mediante patrones
            foreach (var pattern in _tokenPatterns)
            {
                Match match = Regex.Match(lineText.Substring(_position), pattern.Value);
                if (match.Success && match.Index == 0) // El token debe comenzar en la posición actual
                {
                    int start = _position;
                    int end = _position + match.Length - 1; // Fin del token
                    _position += match.Length; // Avanzamos la posición

                    // Calcular la línea de fin
                    int endLine = startLine;
                    int endColumn = startColumn + match.Length - 1;

                    return new Token(pattern.Key, match.Value, start, end, startLine, endLine); // Fin es _position - 1
                }
            }

            return null; // Si no se encuentra un token válido
        }
    }

}