using Compilador_AAA.Models;
using Compilador_AAA.Views;
using Microsoft.Windows.Themes;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Compilador_AAA.Traductor
{
    public class SemanticAnalyzer : IVisitor
    {
        private readonly Dictionary<string, Variable> _variables = new Dictionary<string, Variable>();
        private readonly HashSet<string> _functions = new HashSet<string>();
        private readonly HashSet<string> _classes = new HashSet<string>();

        public void Visit(Program program)
        {
            foreach (var stmt in program.children)
            {
                stmt.Accept(this);
            }
        }
        public void Visit(Println println)
        {
            if (println.Content != null && _variables.ContainsKey(println.Content))
            {
                if (_variables[println.Content].Value != null)
                    TranslatorView._print.Add(EvaluateExpression(_variables[println.Content].Value).ToString());
                else
                    TranslatorView.HandleError($"La variable '{println.Content}' no está inicializada.", println.StartLine, "SEM014");
            }
            else
            {
                TranslatorView.HandleError($"La variable '{println.Content}' no existe.", println.StartLine, "SEM014");
            }
        }

        public void Visit(ClassDeclaration classDeclaration)
        {
            // Añadir la clase al conjunto de clases
            if (!_classes.Add(classDeclaration.Name))
            {
                // Error: clase ya definida
                TranslatorView.HandleError("La clase '" + classDeclaration.Name + "' ya está definida.", classDeclaration.StartLine, "SEM001");
            }

            foreach (var child in classDeclaration.Children)
            {
                child.Accept(this);
            }
        }
        public bool? Visit(BooleanLiteral booleanLiteral) { return booleanLiteral.Value; }
        public int Visit(IntegerLiteral integerLiteral) { return integerLiteral.Value; }
        public void Visit(StringLiteral stringLiteral) { /* Implementar según sea necesario */ }
        public double Visit(DoubleLiteral doubleLiteral) { return doubleLiteral.Value; }




        public void Visit(FunctionDeclaration functionDeclaration)
        {
            // Verificar si la función ya está definida
            if (!_functions.Add(functionDeclaration.Name))
            {
                // Error: función ya definida
                Console.WriteLine($"La función '{functionDeclaration.Name}' ya está definida.");
            }

            foreach (var param in functionDeclaration.Parameters)
            {
                //// Se asume que los parámetros son identificadores
                //if (param is Identifier identifier)
                //{
                //    if (!_variables.Add(identifier.ID,n))
                //    {
                //        // Error: parámetro ya definido
                //        Console.WriteLine($"El parámetro '{identifier.ID}' ya está definido en la función '{functionDeclaration.Name}'.");
                //    }
                //}
            }

            foreach (var stmt in functionDeclaration.Children)
            {
                stmt.Accept(this);
            }
        }

        public void Visit(AssignmentExpr assignmentExpr)
        {
            // Verificar que la variable asignada esté declarada
            if (assignmentExpr.Identifier is Identifier identifier)
            {
                if (!_variables.ContainsKey(identifier.ID))
                {
                    Console.WriteLine($"La variable '{identifier.ID}' no está declarada.");
                }
            }

            // Aquí puedes agregar más lógica para verificar el valor asignado
            assignmentExpr.Value.Accept(this);
        }

        public void Visit(Identifier identifier)
        {
            if (!_variables.ContainsKey(identifier.ID))
            {
                TranslatorView.HandleError($"El nombre '{identifier.ID}' no está definido.", identifier.StartLine, "SEM003");
            }
            else
            {

                if (identifier.Assignment != null && identifier.Assignment.Value != null)
                {
                    Variable tempVar = _variables[identifier.ID];
                    if (identifier.Assignment.Value.Kind == NodeType.BinaryExpr)
                    {
                        object result = Visit((BinaryExpr)identifier.Assignment.Value);
                        if (tempVar.VarType == "entero")
                        {
                            if (exprGotDouble || varGotDouble)
                            {
                                TranslatorView.HandleError("No se puede convertir el tipo 'decimal' en 'entero'.", identifier.StartLine, "SEM014");
                                offVars();

                            }
                            else if (result is var res)
                                _variables[identifier.ID].Value = new IntegerLiteral(Convert.ToInt32(res), identifier.StartLine);
                        }
                        else if (tempVar.VarType == "decimal")
                        {
                            if (varGotBool)
                            {
                                TranslatorView.HandleError("No se puede convertir el tipo 'bool' en 'double'.", identifier.StartLine, "SEM014");
                                offVars();
                            }
                            else if (result is double res)
                            {
                                _variables[identifier.ID].Value = new DoubleLiteral(Convert.ToDouble(res), identifier.StartLine);
                                offVars();
                            }
                            else if (result is int ress)
                            {
                                _variables[identifier.ID].Value = new DoubleLiteral(Convert.ToDouble(ress), identifier.StartLine);
                                offVars();
                            }

                        }
                        else if (tempVar.VarType == "bool")
                        {
                            if (result is bool res)
                            {
                                _variables[identifier.ID].Value = new BooleanLiteral(Convert.ToBoolean(res), identifier.StartLine);
                                offVars();
                            }
                            else if (!varGotDouble)
                            {
                                TranslatorView.HandleError($"No se puede convertir el tipo 'entero' en 'bool'.", identifier.StartLine, "SEM014");
                                offVars();
                            }
                            else
                            {
                                TranslatorView.HandleError($"No se puede convertir el tipo 'decimal' en 'bool'.", identifier.StartLine, "SEM014");
                                offVars();
                            }
                            offVars();
                        }
                        offVars();
                    }
                    else
                    {
                        EvaluateExpression(identifier.Assignment.Value);
                        if (_variables[identifier.ID].VarType == "entero" && identifier.Assignment.Value.Kind != NodeType.IntegerLiteral)
                        {
                            TranslatorView.HandleError($"No se puede convertir el tipo '{identifier.Assignment.Value.Kind}' en 'int'.", identifier.StartLine, "SEM014");
                            offVars();
                        }
                        else if (_variables[identifier.ID].VarType == "decimal" && (identifier.Assignment.Value.Kind != NodeType.IntegerLiteral || identifier.Assignment.Value.Kind != NodeType.DoubleLiteral))
                        {
                            TranslatorView.HandleError($"No se puede convertir el tipo '{identifier.Assignment.Value.Kind}' en 'double'.", identifier.StartLine, "SEM020");
                            offVars();
                        }
                        else if (_variables[identifier.ID].VarType == "bool" && identifier.Assignment.Value.Kind != NodeType.BooleanLiteral)
                        {
                            TranslatorView.HandleError($"Tipo incorrecto para la variable 'bool''{identifier.Assignment.Value.Kind}' en 'double'.", identifier.StartLine, "SEM020");
                            offVars();
                        }
                        else if (_variables[identifier.ID].VarType == "cadena" && identifier.Assignment.Value.Kind != NodeType.StringLiteral)
                        {
                            TranslatorView.HandleError($"Tipo incorrecto para la variable cadena: '{identifier.Assignment.Value.Kind}'.", identifier.StartLine, "SEM020");
                            offVars();
                        }
                        _variables[identifier.ID].Value = identifier.Assignment.Value;
                        offVars();
                    }

                }

            }
        }
        public void Visit(VarDeclaration varDeclaration)
        {
            // Verificar si la variable ya está declarada
            if (!_variables.ContainsKey(varDeclaration.Identifier.ID))
            {
                _variables.Add(varDeclaration.Identifier.ID, new Variable(varDeclaration.VarType, null));
                if (varDeclaration.Assignment != null && varDeclaration.Assignment.Value != null)
                {
                    if (varDeclaration.Assignment.Value.Kind == NodeType.BinaryExpr)
                    {
                        object result = Visit((BinaryExpr)varDeclaration.Assignment.Value);
                        if (varDeclaration.VarType == "entero")
                        {
                            if (exprGotDouble || varGotDouble)
                            {
                                TranslatorView.HandleError("No se puede convertir el tipo 'double' en 'entero'.", varDeclaration.StartLine, "SEM014");
                                offVars();

                            }
                            else if (varGotBool)
                            {
                                TranslatorView.HandleError("No se puede convertir el tipo 'bool' en 'entero'.", varDeclaration.StartLine, "SEM014");
                                offVars();
                            }
                            else if (result is var res)
                                _variables[varDeclaration.Identifier.ID].Value = new IntegerLiteral(Convert.ToInt32(res), varDeclaration.StartLine);

                        }
                        else if (varDeclaration.VarType == "decimal")
                        {
                            if (varGotBool)
                            {
                                TranslatorView.HandleError("No se puede convertir el tipo 'bool' en 'decimal'.", varDeclaration.StartLine, "SEM014");
                                offVars();
                            }
                            else if (result is double res)
                            {
                                _variables[varDeclaration.Identifier.ID].Value = new DoubleLiteral(Convert.ToDouble(res), varDeclaration.StartLine);
                                offVars();
                            }
                            else if (result is int ress)
                            {
                                _variables[varDeclaration.Identifier.ID].Value = new DoubleLiteral(Convert.ToDouble(ress), varDeclaration.StartLine);
                                offVars();
                            }

                        }
                        else if (varDeclaration.VarType == "bool")
                        {
                            if (result is bool)
                            {
                                _variables[varDeclaration.Identifier.ID].Value = new BooleanLiteral(Convert.ToBoolean(result), varDeclaration.StartLine);
                                offVars();

                            }
                            else
                            {
                                TranslatorView.HandleError("No se puede convertir el tipo 'bool' en 'decimal'.", varDeclaration.StartLine, "SEM014");
                                offVars();

                            }
                            offVars();
                        }
                        offVars();
                    }
                    else
                    {
                        EvaluateExpression(varDeclaration.Assignment.Value);
                        if (_variables[varDeclaration.Identifier.ID].VarType == "entero")
                        {
                            if (!varGotInt)
                            {
                                TranslatorView.HandleError($"Tipo incorrecto, se esperaba un entero", varDeclaration.StartLine, "SEM014");
                                offVars();
                            }

                        }
                        else if (_variables[varDeclaration.Identifier.ID].VarType == "decimal")
                        {
                            if (!varGotDouble)
                            {
                                TranslatorView.HandleError($"Tipo incorrecto, se esperaba un decimal", varDeclaration.StartLine, "SEM014");
                                offVars();
                            }
                        }
                        else if (_variables[varDeclaration.Identifier.ID].VarType == "bool")
                        {
                            if (!varGotBool)
                            {
                                TranslatorView.HandleError($"Tipo incorrecto, se esperaba un bool", varDeclaration.StartLine, "SEM014");
                                offVars();
                            }
                        }
                        else if (_variables[varDeclaration.Identifier.ID].VarType == "cadena")
                        {
                            if (!varGotString)
                            {
                                TranslatorView.HandleError($"Tipo incorrecto, se esperaba una cadena", varDeclaration.StartLine, "SEM014");
                                offVars();
                            }
                        }
                        _variables[varDeclaration.Identifier.ID].Value = varDeclaration.Assignment.Value;
                        offVars();
                    }
                }
            }
            else
            {
                // Error: variable ya definida
                TranslatorView.HandleError("La variable '" + varDeclaration.Identifier + "' ya está definida.", varDeclaration.StartLine, "SEM002");
            }


        }
        private void offVars()
        {
            varGotDouble = false;
            varGotBool = false;
            varGotInt = false;
            varGotString = false;

        }
        private object HandleStringOperation(object leftValue, object rightValue, string operatorSymbol, int line)
        {
            // Manejo específico para operaciones con strings
            if (leftValue is not string || rightValue is not string)
            {
                TranslatorView.HandleError("Expresion invalida ", line, "SEM019");
                return null;
            }
            if (operatorSymbol == "+")
            {
                string pattern = "^\"(.+)\"$";
                return "\"" + Regex.Replace(leftValue.ToString(), pattern, "$1") + Regex.Replace(rightValue.ToString(), pattern, "$1") + "\""; // Concatenar
            }
            TranslatorView.HandleError("Operador no soportado para cadenas: ", line, "SEM017");
            throw new InvalidOperationException("Operador no soportado para cadenas: " + operatorSymbol);
        }
        bool varGotString = false;
        bool exprGotDouble = false;
        public object Visit(BinaryExpr binaryExpr)
        {
            // Primero, evaluamos la parte izquierda de la expresión
            var leftValue = EvaluateExpression(binaryExpr.Left);

            // Luego, evaluamos la parte derecha de la expresión
            var rightValue = EvaluateExpression(binaryExpr.Right);

            if (leftValue is string || rightValue is string)
            {
                return HandleStringOperation(leftValue, rightValue, binaryExpr.Operator, binaryExpr.StartLine);
            }

            if (rightValue == null)
                return leftValue;

            // Convertimos a double para operaciones aritméticas
            double leftDouble = Convert.ToDouble(leftValue);
            double rightDouble = Convert.ToDouble(rightValue);

            // Realizamos la operación y determinamos el tipo de retorno
            switch (binaryExpr.Operator)
            {
                case "+":
                    var sumResult = leftDouble + rightDouble;
                    // Si ambos son enteros y su suma es un entero, devolvemos int
                    if (IsInteger(leftValue) && IsInteger(rightValue) && IsInteger(sumResult))
                    {
                        return (int)sumResult;
                    }
                    return sumResult; // Devolvemos double si hay decimales.
                case "-":
                    return leftDouble - rightDouble; // Siempre devuelve double
                case "*":
                    return leftDouble * rightDouble; // Siempre devuelve double
                case "%":
                    return leftDouble % rightDouble; // Siempre devuelve double
                case "/":
                    if (rightDouble == 0)
                    {
                        TranslatorView.HandleError("No se puede dividir por cero.", binaryExpr.StartLine, "SEM016");
                        return null;
                    }
                    return leftDouble / rightDouble; // Siempre devuelve double
                default:
                    TranslatorView.HandleError("Operador no soportado: " + binaryExpr.Operator, binaryExpr.StartLine, "SEM016");
                    return null;
            }
        }
        public void Visit(WhileStatement whileStatement)
        {

            bool? done = null;
            // Lógica para manejar el if statement
            if (whileStatement.Condition != null)
            {
                if (whileStatement.Condition is ConditionExpr)
                {

                    done = Visit((ConditionExpr)whileStatement.Condition);
                    while (done != null && done == true)
                    {
                        foreach (var stmt in whileStatement.ThenBranch)
                        {
                            stmt.Accept(this);
                        }
                        done = Visit((ConditionExpr)whileStatement.Condition);
                    }
                }

                else if (whileStatement.Condition.Kind == NodeType.Identifier)
                {
                    var temp = EvaluateExpression(whileStatement.Condition);
                    if (temp is bool)
                    {
                        done = (bool)temp;
                        while (done != null && done == true)
                        {
                            foreach (var stmt in whileStatement.ThenBranch)
                            {
                                stmt.Accept(this);
                            }
                            done = Visit((ConditionExpr)whileStatement.Condition);
                        }
                    }
                    else
                    {
                        TranslatorView.HandleError("Expresion invalida: " + temp, whileStatement.Condition.StartLine, "SEM016");
                    }
                }

            }


        }
        public void Visit(DoWhileStatement doWhileStatement)
        {

            bool? done = null;
            // Lógica para manejar el if statement
            if (doWhileStatement.Condition != null)
            {
                if (doWhileStatement.Condition is ConditionExpr)
                {

                    done = Visit((ConditionExpr)doWhileStatement.Condition);
                    do
                    {
                        foreach (var stmt in doWhileStatement.ThenBranch)
                        {
                            stmt.Accept(this);
                        }
                        done = Visit((ConditionExpr)doWhileStatement.Condition);
                    } while (done != null && done == true);
                }

                else if (doWhileStatement.Condition.Kind == NodeType.Identifier)
                {
                    var temp = EvaluateExpression(doWhileStatement.Condition);
                    if (temp is bool)
                    {
                        done = (bool)temp;
                        do
                        {
                            foreach (var stmt in doWhileStatement.ThenBranch)
                            {
                                stmt.Accept(this);
                            }
                            done = Visit((ConditionExpr)doWhileStatement.Condition);
                        } while (done != null && done == true) ;
                    }
                    else
                    {
                        TranslatorView.HandleError("Expresion invalida: " + temp, doWhileStatement.Condition.StartLine, "SEM016");
                    }
                }

            }


        }
        public void Visit(ForStatement forStatement)
        {

            if (forStatement.Initialization != null)
            {
                forStatement.Initialization.Accept(this);
            }
            bool? done = null;
            // Lógica para manejar el if statement
            if (forStatement.Condition != null)
            {
                if (forStatement.Condition is ConditionExpr)
                {

                    done = Visit((ConditionExpr)forStatement.Condition);
                    if (forStatement.Expresion != null)
                    {
                        while (done != null && done == true)
                        {
                            foreach (var stmt in forStatement.Body)
                            {
                                stmt.Accept(this);
                            }
                            if (forStatement.Expresion != null)
                            {
                                forStatement.Expresion.Accept(this);
                            }
                            done = Visit((ConditionExpr)forStatement.Condition);
                        }
                    }
                    else
                    {
                        TranslatorView.HandleError("Se esperaba una expresion de incremento de decremento en el 'para'.", forStatement.StartLine, "SEM027");
                    }

                }

                else if (forStatement.Condition.Kind == NodeType.Identifier)
                {
                    var temp = EvaluateExpression(forStatement.Condition);
                    if (temp is bool)
                    {
                        done = (bool)temp;
                        if (forStatement.Expresion != null)
                        {
                            while (done != null && done == true)
                            {
                                foreach (var stmt in forStatement.Body)
                                {
                                    stmt.Accept(this);
                                }
                                if (forStatement.Expresion != null)
                                {
                                    forStatement.Expresion.Accept(this);
                                }
                                done = Visit((ConditionExpr)forStatement.Condition);
                            }
                        }
                        else
                        {
                            TranslatorView.HandleError("Se esperaba una expresion de incremento de decremento en el 'para'.", forStatement.StartLine, "SEM027");
                        }
                    }
                    else
                    {
                        TranslatorView.HandleError("Expresion invalida: " + temp, forStatement.Condition.StartLine, "SEM016");
                    }
                }

            }


        }
        public void Visit(IfStatement ifStatement)
        {
            bool? done = null;
            // Lógica para manejar el if statement
            if (ifStatement.Condition != null)
            {
                if (ifStatement.Condition is ConditionExpr)
                    done = Visit((ConditionExpr)ifStatement.Condition);
                else if (ifStatement.Condition.Kind == NodeType.Identifier)
                {
                    var temp = EvaluateExpression(ifStatement.Condition);
                    if (temp is bool)
                    {
                        done = (bool)temp;
                    }
                    else
                    {
                        TranslatorView.HandleError("Expresion invalida: " + temp, ifStatement.Condition.StartLine, "SEM016");
                    }
                }
            }

            if (done != null && done == true)
            {
                foreach (var stmt in ifStatement.ThenBranch)
                {
                    stmt.Accept(this);
                }
            }
            else
            {
                foreach (var stmt in ifStatement.ElseBranch)
                {
                    stmt.Accept(this);
                }
            }
        }
        public bool? Visit(ConditionExpr conditionExpr)
        {
            var leftValue = EvaluateExpression(conditionExpr.Left);
            var rightValue = EvaluateExpression(conditionExpr.Right);
            if (leftValue == null)
            {
                return null;
            }
            switch (conditionExpr.Operator)
            {

                case "==":
                    return leftValue.Equals(rightValue);
                case "!=":
                    return !leftValue.Equals(rightValue);
                case ">":
                    return Convert.ToDouble(leftValue) > Convert.ToDouble(rightValue);
                case "<":
                    return Convert.ToDouble(leftValue) < Convert.ToDouble(rightValue);
                case ">=":
                    return Convert.ToDouble(leftValue) >= Convert.ToDouble(rightValue);
                case "<=":
                    return Convert.ToDouble(leftValue) <= Convert.ToDouble(rightValue);
                case "||":
                    return Convert.ToBoolean(leftValue) || Convert.ToBoolean(rightValue);
                case "&&":
                    return Convert.ToBoolean(leftValue) && Convert.ToBoolean(rightValue);
                default:
                    TranslatorView.HandleError("Operador no soportado: " + conditionExpr.Operator, conditionExpr.StartLine, "SEM014");
                    return null;
            }
        }
        private bool IsInteger(object value)
        {
            return value is int || (value is double d && d % 1 == 0);
        }


        bool varGotDouble = false;
        bool varGotBool = false;
        bool varGotInt = false;
        private object EvaluateExpression(Expr expression)
        {
            if (expression != null)
            {
                switch (expression.Kind)
                {
                    case NodeType.IntegerLiteral:
                        if (expression is IntegerLiteral intLiteral && intLiteral.Value is int valInt)
                        {
                            varGotInt = true;
                            return valInt;
                        }
                        break;

                    case NodeType.DoubleLiteral:
                        if (expression is DoubleLiteral doubleLiteral && doubleLiteral.Value is double valDouble)
                        {
                            varGotDouble = true;
                            return valDouble;
                        }
                        break;

                    case NodeType.StringLiteral:
                        if (expression is StringLiteral stringLiteral && stringLiteral.Value is string valString)
                        {
                            varGotString = true;
                            return valString;
                        }
                        break;

                    case NodeType.BooleanLiteral:
                        if (expression is BooleanLiteral boolLiteral && boolLiteral.Value is bool valBool)
                        {
                            varGotBool = true;
                            return valBool;
                        }
                        break;
                    case NodeType.Identifier:
                        // Aquí deberías manejar la recuperación del valor de la variable
                        var identifier = (Identifier)expression;
                        if (_variables.ContainsKey(identifier.ID))
                        {
                            if (_variables[identifier.ID].VarType == "decimal")
                            {
                                varGotDouble = true;
                            }
                            else if (_variables[identifier.ID].VarType == "entero")
                            {
                                varGotInt = true;
                            }
                            else if (_variables[identifier.ID].VarType == "bool")
                            {
                                varGotBool = true;
                            }
                        }
                        if (!_variables.ContainsKey(identifier.ID))
                        {
                            TranslatorView.HandleError($"La variable '{identifier.ID}' no existe.", expression.StartLine, "SEM014");
                            return null;
                        }
                        else if (_variables[identifier.ID].Value == null)
                        {
                            TranslatorView.HandleError($"La variable '{identifier.ID}' no está inicializada.", expression.StartLine, "SEM014");
                            return null;
                        }

                        return EvaluateExpression(GetVariableValue(identifier.ID));
                    case NodeType.BinaryExpr:
                        return Visit((BinaryExpr)expression);
                    case NodeType.ConditionExpr:
                        return Visit((ConditionExpr)expression);
                    default:
                        TranslatorView.HandleError($"Expresion no implementada", expression.StartLine, "SEM014");
                        return null;
                }
            }
            return null;

        }

        private Expr GetVariableValue(string variableName)
        {

            return _variables[variableName].Value;
        }

        public void Visit(CallExpr callExpr)
        {
            // Verificar que la función llamada esté definida
            if (callExpr.Caller is Identifier caller)
            {
                if (!_functions.Contains(caller.ID))
                {
                    Console.WriteLine($"La función '{caller.ID}' no está definida.");
                }
            }

            // Verificar argumentos
            foreach (var arg in callExpr.Arguments)
            {
                arg.Accept(this);
            }
        }

        // Implementar otros métodos de visita según sea necesario
        public void Visit(MemberExpr memberExpr) { /* Implementar según sea necesario */ }


        public void Visit(Property property) { /* Implementar según sea necesario */ }
        public void Visit(ObjectLiteral objectLiteral) { /* Implementar según sea necesario */ }


    }
}