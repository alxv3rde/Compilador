using Compilador_AAA.Views;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Media3D;

namespace Compilador_AAA.Traductor
{

    public class Parser
    {
        private Dictionary<int, List<Token>> _tokensByLine;
        private int _currentLine;
        private int _currentTokenIndex;
        private List<Token> _currentTokens;

        public Parser(Dictionary<int, List<Token>> tokensByLine)
        {
            _tokensByLine = tokensByLine;
            _currentLine = 1;
            _currentTokenIndex = 0;
            _currentTokens = _tokensByLine[_currentLine];
        }

        public Program Parse()
        {
            var program = new Program();

            while (_currentLine < _tokensByLine.Count)
            {
                var statement = ParseStatement(false);
                if (statement != null)
                {
                    program.children.Add(statement);
                }
                else
                {
                    AdvanceToNextLine();
                }
            }

            return program;
        }

        private Stmt ParseStatement(bool statement)
        {
            IsAtEndOfLine();
            if (Check(TokenType.Keyword, new[] { "class" }))
            {
                Advance();
                return ParseClassDeclaration();
            }
            else if (Check(TokenType.Keyword, new[] { "imprime" }))
            {
                Advance();
                return ParsePrintln(); // Agregar aquí
            }
            else if (Check(TokenType.Keyword, new[] { "hacer" }))
            {
                return ParseDoWhileStatement(); // Agregar aquí
            }
            else if (Check(TokenType.Keyword, new[] { "mientras" }))
            {
                return ParseWhileStatement(); // Agregar aquí
            }
            else if (Check(TokenType.Keyword, new[] { "para" }))
            {
                return ParseForStatement(); // Agregar aquí
            }
            else if (Check(TokenType.Keyword, new[] { "si" }))
            {
                return ParseIfStatement(); // Agregar aquí
            }
            else if (Check(TokenType.Keyword, new[] { "entero", "bool", "decimal", "cadena" }))
            {
                return ParseVarDeclaration(statement);
            }
            else if (Match(TokenType.Identifier))
            {
                if (statement)
                {
                    return ParseIdentifier(true);
                }
                else
                {
                    return ParseIdentifier();
                }

            }
            else
            {
                return null;
            }
        }
        private Println ParsePrintln()
        {
            int currentLineTemp = _currentLine;
            int currentTokenIndexTemp = _currentTokenIndex;
            string expr = null;
            TranslatorView._translation += $"Console.Writeline(";
            Consume(TokenType.OpenParen, "Se esperaba '(' después de 'imprime'", "SIN011");
            if (Consume(TokenType.Identifier, "Se esperaba un identificador", "SIN001"))
            {
                expr = _currentLine > currentLineTemp
                            ? Previous(currentLineTemp, currentTokenIndexTemp).Value
                            : Previous().Value;
            }
            TranslatorView._translation += $"{expr})\n";
            Consume(TokenType.CloseParen, "Se esperaba ')' ", "SIN011");
            Consume(TokenType.Semicolon, "Se esperaba ';' al final.", "SIN007");
            return new Println(currentLineTemp, expr);
        }

        private Identifier ParseIdentifier()
        {

            string identifier = Previous().Value;
            TranslatorView._translation += $"{identifier}";
            AssignmentExpr value = null;
            if (Match(TokenType.Equals))
            {
                TranslatorView._translation += $" = ";
                value = ParseAssignmentExpression(new Identifier(identifier, Previous().StartLine));
            }

            Consume(TokenType.Semicolon, "Se esperaba ';' al final de la declaración de variable.", "SIN007");
            TranslatorView._translation += $"\n";
            if (value != null)
                return new Identifier(identifier, Previous().StartLine, value);
            return null;
        }
        private Identifier ParseIdentifier(bool semicolon)
        {

            string identifier = Previous().Value;
            TranslatorView._translation += $"{identifier}";
            AssignmentExpr value = null;
            if (Match(TokenType.Equals))
            {
                TranslatorView._translation += $" = ";
                value = ParseAssignmentExpression(new Identifier(identifier, Previous().StartLine));
            }

            if (value != null)
                return new Identifier(identifier, Previous().StartLine, value);
            return null;
        }

        private Expr ParseNumericLiteral()
        {
            string numericL = Previous().Value;
            return new Identifier(numericL, _currentLine);
        }

        private ClassDeclaration ParseClassDeclaration()
        {
            var accessModifier = TokenType.Public; // Almacena el modificador de acceso

            int currentLineTemp = _currentLine;
            int currentTokenIndexTemp = _currentTokenIndex;
            // Ahora, consumimos el identificador del nombre de la clase
            string className = null;

            if (Consume(TokenType.Identifier, "Se esperaba un nombre de clase después de la palabra clave 'class'.", "SIN001"))
            {
                className = _currentLine > currentLineTemp
                            ? Previous(currentLineTemp, currentTokenIndexTemp).Value
                            : Previous().Value;
            }


            Consume(TokenType.OpenBrace, "Se esperaba '{' ", "SIN004");

            List<Stmt> children = new List<Stmt>();
            while (!Check(TokenType.CloseBrace) && !IsAtEndOfFile())
            {
                var statement = ParseStatement(false);
                if (statement != null)
                {
                    children.Add(statement);
                }
                else
                {
                    if (!IsAtEndOfLine()) _currentTokenIndex++;
                }

            }

            Consume(TokenType.CloseBrace, "Se esperaba '}' al final de la declaración de la clase", "SIN005");
            return new ClassDeclaration(className, new List<string>(), children, accessModifier, currentLineTemp);
        }
        private Expr ParseCondition()
        {
            Expr left = ParseAddExpr(); // Analiza la expresión a la izquierda

            while (true) // Permite múltiples condiciones de comparación
            {
                Token token = AdvancePeek(); // Obtiene el siguiente token
                if (token.Type == TokenType.Operator && (token.Value == "==" || token.Value == "!=" || token.Value == ">" || token.Value == "<" || token.Value == ">=" || token.Value == "<="))
                {
                    TranslatorView._translation += $"{token.Value} ";
                    Advance(); // Consume el operador
                    Expr right = ParseAddExpr(); // Analiza la expresión a la derecha
                    left = new ConditionExpr(left, token.Value, right, _currentLine); // Crea una nueva condición
                }
                else
                {
                    break; // Sale del bucle si no hay más operadores de comparación
                }
            }

            return left; // Si no hay operador, retorna la expresión izquierda
        }
        private Expr ParseLogicAND()
        {
            Expr left = ParseCondition(); // Analiza la expresión a la izquierda

            Token token = AdvancePeek(); // Obtiene el siguiente token
            if (token.Type == TokenType.Operator && (token.Value == "&&"))
            {
                TranslatorView._translation += $"{token.Value} ";
                Advance(); // Consume el operador
                Expr right = ParseCondition(); // Analiza la expresión a la derecha
                left = new ConditionExpr(left, token.Value, right, _currentLine); // Crea una nueva condición
            }

            return left; // Si no hay operador, retorna la expresión izquierda
        }
        private Expr ParseLogicOR()
        {
            Expr left = ParseLogicAND(); // Analiza la expresión a la izquierda

            Token token = AdvancePeek(); // Obtiene el siguiente token
            if (token.Type == TokenType.Operator && (token.Value == "||"))
            {
                TranslatorView._translation += $"{token.Value} ";
                Advance(); // Consume el operador
                Expr right = ParseLogicAND(); // Analiza la expresión a la derecha
                left = new ConditionExpr(left, token.Value, right, _currentLine); // Crea una nueva condición
            }

            return left; // Si no hay operador, retorna la expresión izquierda
        }
        private DoWhileStatement ParseDoWhileStatement()
        {
            Consume(TokenType.Keyword, "Se esperaba 'hacer'", "SIN010");
            
            Consume(TokenType.OpenBrace, "Se esperaba '{' después de la condición", "SIN013");
            TranslatorView._translation += new string('\t', indentLevel);
            TranslatorView._translation += $"Do\n";
            indentLevel++;
            List<Stmt> thenBranch = new List<Stmt>();
            while (!Check(TokenType.CloseBrace) && !IsAtEndOfFile())
            {
                TranslatorView._translation += new string('\t', indentLevel);
                var statement = ParseStatement(false);
                if (statement != null)
                {
                    thenBranch.Add(statement);
                }
                else
                {
                    AdvanceToNextLine();
                }
            }
            indentLevel--;
            Consume(TokenType.CloseBrace, "Se esperaba '}' al final del bloque 'mientras'", "SIN014");
            Consume(TokenType.Keyword, "Se esperaba 'mientras'", "SIN010");
            TranslatorView._translation += new string('\t', indentLevel);
            TranslatorView._translation += $"Loop While ";
            Consume(TokenType.OpenParen, "Se esperaba '(' después de 'mientras'", "SIN011");
            
            Expr condition = (Expr)ParseLogicOR(); // Analiza la condición

            Consume(TokenType.CloseParen, "Se esperaba ')' después de la condición", "SIN012");
            Consume(TokenType.Semicolon, "Se esperaba ';'", "SIN007");
            TranslatorView._translation += $"\n";
            return new DoWhileStatement(condition, thenBranch, _currentLine); // Retorna el if statement
        }
        private WhileStatement ParseWhileStatement()
        {
            Consume(TokenType.Keyword, "Se esperaba 'mientras'", "SIN010");
            Consume(TokenType.OpenParen, "Se esperaba '(' después de 'mientras'", "SIN011");
            TranslatorView._translation += new string('\t', indentLevel);
            TranslatorView._translation += $"While ";

            Expr condition = (Expr)ParseLogicOR(); // Analiza la condición

            Consume(TokenType.CloseParen, "Se esperaba ')' después de la condición", "SIN012");
            Consume(TokenType.OpenBrace, "Se esperaba '{' después de la condición", "SIN013");
            TranslatorView._translation += $"\n";

            indentLevel++;

            List<Stmt> thenBranch = new List<Stmt>();
            
            while (!Check(TokenType.CloseBrace) && !IsAtEndOfFile())
            {
                // Agregar tabulación al inicio de cada línea según el nivel actual
                TranslatorView._translation += new string('\t', indentLevel);

                var statement = ParseStatement(false);
                if (statement != null)
                {
                    thenBranch.Add(statement);
                }
                else
                {
                    AdvanceToNextLine();
                }
            }

            // Salida de bloque: reducir nivel si el bloque está cerrado
            indentLevel--;
            TranslatorView._translation += new string('\t', indentLevel);
            Consume(TokenType.CloseBrace, "Se esperaba '}' al final del bloque 'mientras'", "SIN014");
            TranslatorView._translation += $"\n";

            return new WhileStatement(condition, thenBranch, _currentLine); // Retorna el if statement
        }
        int indentLevel = 0; // Nivel inicial de indentación
        private ForStatement ParseForStatement()
        {
            Consume(TokenType.Keyword, "Se esperaba 'para'", "SIN021");
            Consume(TokenType.OpenParen, "Se esperaba '(' después de 'para'", "SIN011");
            TranslatorView._translation += new string('\t', indentLevel);
            TranslatorView._translation += $"For";
            int currentLineTemp = _currentLine;
            int currentTokenIndexTemp = _currentTokenIndex;
            Stmt initialization = null;
            if (!Check(TokenType.Semicolon) && !IsAtEndOfFile())
            {
                initialization = ParseStatement(true);
            }
            Expr condition = null;
            if (!Check(TokenType.Semicolon) && !IsAtEndOfFile())
            {
                condition = ParseLogicOR(); // Analiza la condición
            }
            Consume(TokenType.Semicolon, "Se esperaba ';'", "SIN007");

            Stmt expression = null;
            if (!Check(TokenType.Semicolon) && !IsAtEndOfFile())
            {
                expression = ParseStatement(true); // Analiza la condición
            }
            TranslatorView._translation += new string('\t', indentLevel);
            TranslatorView._translation += $"\n";

            Consume(TokenType.CloseParen, "Se esperaba ')' después de la condición", "SIN012");
            Consume(TokenType.OpenBrace, "Se esperaba '{' después de la condición", "SIN013");
            indentLevel ++;
            List<Stmt> body = new List<Stmt>();
            while (!Check(TokenType.CloseBrace) && !IsAtEndOfFile())
            {
                TranslatorView._translation += new string('\t', indentLevel);
                var statement = ParseStatement(false);
                if (statement != null)
                {
                    body.Add(statement);
                }
                else
                {
                    AdvanceToNextLine();
                }
            }
            Consume(TokenType.CloseBrace, "Se esperaba '}' al final del bloque 'para'", "SIN014");
            indentLevel--;
            TranslatorView._translation += new string('\t', indentLevel);
            TranslatorView._translation += $"End For \n";

            return new ForStatement(initialization, condition, expression, body, _currentLine); // Retorna el if statement
        }
        private IfStatement ParseIfStatement()
        {
            Consume(TokenType.Keyword, "Se esperaba 'si'", "SIN010");
            Consume(TokenType.OpenParen, "Se esperaba '(' después de 'si'", "SIN011");
            TranslatorView._translation += $"If ";
            Expr condition = (Expr)ParseLogicOR(); // Analiza la condición
            TranslatorView._translation += $"Then \n";
            Consume(TokenType.CloseParen, "Se esperaba ')' después de la condición", "SIN012");
            Consume(TokenType.OpenBrace, "Se esperaba '{' después de la condición", "SIN013");
            indentLevel++;
            List<Stmt> thenBranch = new List<Stmt>();
            while (!Check(TokenType.CloseBrace) && !IsAtEndOfFile())
            {
                TranslatorView._translation += new string('\t', indentLevel);
                var statement = ParseStatement(false);
                if (statement != null)
                {
                    thenBranch.Add(statement);
                }
                else
                {
                    AdvanceToNextLine();
                }
            }
            indentLevel--;
            Consume(TokenType.CloseBrace, "Se esperaba '}' al final del bloque 'si'", "SIN014");
            Token el = AdvancePeek();
            List<Stmt> elseBranch = new List<Stmt>();
            if (el.Type == TokenType.Keyword && (el.Value == "sino"))
            {
                TranslatorView._translation += new string('\t', indentLevel);
                TranslatorView._translation += $"Else \n";
                Advance();  // Consume el operador
                Consume(TokenType.OpenBrace, "Se esperaba '{' después del sino", "SIN013");
                indentLevel++;
                while (!Check(TokenType.CloseBrace) && !IsAtEndOfFile())
                {
                    TranslatorView._translation += new string('\t', indentLevel);
                    var statement = ParseStatement(false);
                    if (statement != null)
                    {
                        elseBranch.Add(statement);
                    }
                    else
                    {
                        AdvanceToNextLine();
                    }
                }
                indentLevel--;
                
                Consume(TokenType.CloseBrace, "Se esperaba '}' al final del bloque 'si'", "SIN014");
            }
            TranslatorView._translation += new string('\t', indentLevel);
            TranslatorView._translation += $"End If \n";
            return new IfStatement(condition, thenBranch, elseBranch, _currentLine); // Retorna el if statement
        }

        private VarDeclaration ParseVarDeclaration(bool constant)
        {
            
            bool isConstant = constant;
            int currentLineTemp = _currentLine;
            int currentTokenIndexTemp = _currentTokenIndex;
            Token tokenIdentifier = null;
            Token tokenKeyword = null;
            if (Consume(TokenType.Keyword, "Se esperaba un tipo de dato.", "SIN007"))
            {
                tokenKeyword = _currentLine > currentLineTemp
                            ? Previous(currentLineTemp, currentTokenIndexTemp)
                            : Previous();
            }
            if (Consume(TokenType.Identifier, "Se esperaba un identificador para la variable.", "SIN006"))
            {
                tokenIdentifier = _currentLine > currentLineTemp
                            ? Previous(currentLineTemp, currentTokenIndexTemp)
                            : Previous();
            }

            switch (tokenKeyword.Value)
            {
                case "entero":
                    TranslatorView._translation += $"{(constant ? "" : "Dim")} {tokenIdentifier.Value} As Integer";
                    break;
                case "decimal":
                    TranslatorView._translation += $"{(constant ? "" : "Dim")} {tokenIdentifier.Value} As Double";
                    break;
                case "cadena":
                    TranslatorView._translation += $"{(constant ? "" : "Dim")} {tokenIdentifier.Value} As String";
                    break;
                case "bool":
                    TranslatorView._translation += $"{(constant ? "" : "Dim")} {tokenIdentifier.Value} As Boolean";
                    break;

            }
            AssignmentExpr value = null;
            if (Match(TokenType.Equals))
            {
                TranslatorView._translation += $" = ";
                value = ParseAssignmentExpression(new Identifier(tokenIdentifier.Value, tokenIdentifier.StartLine));
            }

            Consume(TokenType.Semicolon, "Se esperaba ';' al final de la declaración de variable.", "SIN007");
            if (!constant)
            TranslatorView._translation += $"\n";

            if (tokenIdentifier != null)
            {
                
                return new VarDeclaration(tokenKeyword.Value, tokenKeyword.StartLine, isConstant, new Identifier(tokenIdentifier.Value, tokenIdentifier.StartLine), value);
            }
                
            return null;
        }

        private AssignmentExpr ParseAssignmentExpression(Identifier id)
        {
            Expr expresion = ParseAddExpr();
            if (expresion != null)
                return new AssignmentExpr(id, expresion, _currentLine);
            return null;


        }
        private Expr PrimaryExpr()
        {
            int currentLineTemp = _currentLine;
            int currentTokenIndexTemp = _currentTokenIndex;
            Token token = null;
            if (Consume([TokenType.IntegerLiteral, TokenType.StringLiteral, TokenType.DoubleLiteral, TokenType.OpenParen, TokenType.Identifier, TokenType.BooleanLiteral], "Se esperaba una expresión ", "SIN008"))
            {
                token = _currentLine > currentLineTemp
                            ? Previous(currentLineTemp, currentTokenIndexTemp)
                            : Previous();
            }
            else
            {
                _currentTokenIndex = currentTokenIndexTemp;
                _currentLine = currentLineTemp;
            }
            if (token != null)
            {
                TranslatorView._translation += $"{token.Value} ";
                switch (token.Type)
                {
                    case TokenType.Identifier:
                        return new Identifier(token.Value, currentLineTemp);
                    case TokenType.IntegerLiteral:
                        return new IntegerLiteral(Convert.ToInt32(token.Value), currentLineTemp);
                    case TokenType.DoubleLiteral:
                        return new DoubleLiteral(Convert.ToDouble(token.Value), currentLineTemp);
                    case TokenType.StringLiteral:
                        return new StringLiteral(token.Value, currentLineTemp);
                    case TokenType.BooleanLiteral:
                        return new BooleanLiteral(Convert.ToBoolean(token.Value), currentLineTemp);
                    case TokenType.OpenParen:
                        var value = ParseAddExpr();
                        if(Consume(TokenType.CloseParen, "se esperaba el cierre del parentesis", "sin007"));
                            TranslatorView._translation += $") ";
                        return value;
                }
            }

            return null;
        }
        private Expr ParseAddExpr()
        {
            int currentLineTemp = _currentLine;
            int currentTokenIndexTemp = _currentTokenIndex;
            Expr left = ParseMultExpr();  // Comienza con una expresión de multiplicación

            while (true)
            {
                Token token = AdvancePeek();
                if (token.Type == TokenType.Operator && (token.Value == "+" || token.Value == "-"))
                {
                    Advance();  // Consume el operador
                    TranslatorView._translation += $"{token.Value} ";
                    Expr right = ParseMultExpr();  // Obtiene la siguiente expresión de multiplicación
                    left = new BinaryExpr(left, right, token.Value, currentLineTemp);  // Crea una nueva expresión binaria
                }
                else
                {
                    break;  // Sale del bucle si no hay más operadores de suma o resta
                }
            }
            return left;
        }
        private Expr ParseMultExpr()
        {
            int currentLineTemp = _currentLine;
            int currentTokenIndexTemp = _currentTokenIndex;
            Expr left = PrimaryExpr();  // Comienza con una expresión primaria

            while (true)
            {
                Token token = AdvancePeek();
                
                if (token.Type == TokenType.Operator && (token.Value == "*" || token.Value == "/" || token.Value == "%"))
                {
                    TranslatorView._translation += $"{token.Value} ";
                    Advance();  // Consume el operador
                    Expr right = PrimaryExpr();  // Obtiene la siguiente expresión primaria
                    left = new BinaryExpr(left, right, token.Value, currentLineTemp);  // Crea una nueva expresión binaria
                }
                else
                {
                    break;  // Sale del bucle si no hay más operadores de multiplicación o división
                }
            }
            return left;
        }
        


        private StringLiteral ParseStringLiteral()
        {
            Token stringLiteral = Previous();
            return new StringLiteral(stringLiteral.Value, stringLiteral.StartLine);
        }
        private IntegerLiteral ParseIntegerLiteral()
        {
            Token integerLiteral = Previous();
            return new IntegerLiteral(Convert.ToInt32(integerLiteral.Value), integerLiteral.StartLine);
        }
        private DoubleLiteral ParseDoubleLiteral()
        {
            Token doubleLiteral = Previous();
            return new DoubleLiteral(Convert.ToDouble(doubleLiteral.Value), doubleLiteral.StartLine);
        }
        private Token Previous(int previousLine, int previousIndexToken)
        {
            List<Token> tempTokens;
            tempTokens = _tokensByLine[previousLine];
            return tempTokens[previousLine - 1];
        }
        private void AdvanceToNextLine()
        {
            _currentLine++;
            _currentTokenIndex = 0;

            if (_currentLine <= _tokensByLine.Count)
            {
                _currentTokens = _tokensByLine[_currentLine];
            }
        }

        private bool Match(params TokenType[] tokenTypes)
        {
            foreach (var type in tokenTypes)
            {
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            }
            return false;
        }
        private bool Check(TokenType type)
        {
            IsAtEndOfLine();
            return Peek().Type == type;
        }
        private bool Check(TokenType token, string[] keywords)
        {
            if (Check(token))
            {
                foreach (var k in keywords)
                {
                    if (Peek().Value == k)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        private Token AdvancePeek()
        {
            IsAtEndOfLine();
            return Peek();
        }
        private Token Advance()
        {
            if (!IsAtEndOfLine()) _currentTokenIndex++;
            return Previous();
        }

        private Token Peek()
        {
            return _currentTokens[_currentTokenIndex];
        }

        private Token Previous()
        {
            return _currentTokens[_currentTokenIndex - 1];
        }

        private bool IsAtEndOfFile()
        {
            return Peek().Type == TokenType.EOF;
        }
        private bool IsAtEndOfLine()
        {
            if (_currentTokenIndex >= _currentTokens.Count)
            {
                AdvanceToNextLine();
                return true;
            }

            return false;
        }
        private bool Consume(TokenType expectedType, string errorMessage, string errorCode)
        {
            string errorMsg;
            if (Check(expectedType))
            {
                Advance();
                return true;
            }
            else if (!IsAtEndOfLine())
            {
                if (errorCode != "")
                    TranslatorView.HandleError(errorMessage + $" '{Peek().Value}' no es válido", expectedType == TokenType.OpenBrace ? Peek().StartLine - 1 : Peek().StartLine, errorCode);
                Advance();
                return false;
            }

            errorMsg = "Error de sintaxis: " + errorMessage;
            if (errorCode != "")
                TranslatorView.HandleError(errorMsg, Previous().StartLine, errorCode);
            Advance();
            return false;

        }


        private bool Consume(TokenType expectedType, string keyword, string errorMessage, string errorCode)
        {
            string errorMsg;
            if (Check(expectedType) && Peek().Value == keyword)
            {
                Advance(); // Avanza al siguiente token si coincide
                return true;
            }
            else if (!IsAtEndOfLine())
            {
                if (errorCode != "")
                    TranslatorView.HandleError(errorMessage + $" '{Peek().Value}' no es válido", Peek().StartLine, errorCode);
                Advance();
                return false;
            }

            errorMsg = errorMessage;
            if (errorCode != "")
                TranslatorView.HandleError(errorMsg, Previous().StartLine, errorCode);
            Advance();
            return false;
        }
        private bool Consume(TokenType[] expectedType, string errorMessage, string errorCode)
        {
            foreach (var type in expectedType)
            {
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            }

            int errorLine = IsAtEndOfLine() ? Previous().StartLine : Peek().StartLine;

            // Manejar el error
            if (errorCode != "")
                TranslatorView.HandleError(errorMessage + $" '{Peek().Value}' no es válido", errorLine, errorCode);
            Advance();
            return false;
        }
    }
}