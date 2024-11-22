using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compilador_AAA.Traductor
{
    public enum NodeType
    {
        // PROGRAM STRUCTURE
        Program,

        // DECLARATIONS
        ClassDeclaration,
        VarDeclaration,
        FunctionDeclaration,

        // STATEMENTS
        ExpressionStatement,
        IfStatement,
        ForStatement,
        WhileStatement,
        DoWhileStatement,
        ReturnStatement,
        Println,

        // EXPRESSIONS
        AssignmentExpr,
        MemberExpr,
        CallExpr,
        BinaryExpr,
        ConditionExpr,

        // LITERALS
        Property,
        NumericLiteral,
        ObjectLiteral,
        BooleanLiteral,
        StringLiteral,
        IntegerLiteral,
        DoubleLiteral,
        Identifier
    }

    // STATEMENT BASE CLASS
    public abstract class Stmt
    {
        private NodeType program;

        public NodeType Kind { get; protected set; }
        public int StartLine { get; set; }

        protected Stmt(NodeType kind, int startLine)
        {
            Kind = kind;
            StartLine = startLine;
        }

        protected Stmt(NodeType program)
        {
            this.program = program;
        }

        public abstract void Accept(IVisitor visitor);
    }

    // PROGRAM NODE
    public class Program : Stmt
    {
        public List<Stmt> children { get; set; }

        public Program() : base(NodeType.Program)
        {

            children = new List<Stmt>();
        }
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    public class ConditionExpr : Expr
    {
        public Expr Left { get; set; }
        public string Operator { get; set; }
        public Expr Right { get; set; }

        public ConditionExpr(Expr left, string operatorSymbol, Expr right, int startLine)
            : base(NodeType.ConditionExpr, startLine)
        {
            Left = left;
            Operator = operatorSymbol;
            Right = right;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    public class ClassDeclaration : Stmt
    {
        public TokenType AccessMod { get; set; }
        public List<string> Parameters { get; set; }
        public string Name { get; set; }
        public List<Stmt> Children { get; set; }

        public ClassDeclaration(string name, List<string> parameters, List<Stmt> children, TokenType accessMod, int startLine)
                    : base(NodeType.ClassDeclaration, startLine)
        {
            Name = name;
            Parameters = parameters;
            Children = children;
            AccessMod = accessMod;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class VarDeclaration : Stmt
    {
        public string VarType { get; set; }
        public bool Constant { get; set; }
        public Identifier Identifier { get; set; }
        public AssignmentExpr Assignment { get; set; }

        public VarDeclaration(string varType, int startLine, bool constant, Identifier identifier, AssignmentExpr assignment = null)
            : base(NodeType.VarDeclaration, startLine)
        {
            Constant = constant;
            Identifier = identifier;
            Assignment = assignment;
            VarType = varType;
        }
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    public class IfStatement : Stmt
    {
        public Expr Condition { get; set; }
        public List<Stmt> ThenBranch { get; set; }
        public List<Stmt> ElseBranch { get; set; } // Opcional, si deseas manejar 'else'

        public IfStatement(Expr condition, List<Stmt> thenBranch, List<Stmt> elseBranch, int startLine)
            : base(NodeType.IfStatement, startLine)
        {
            Condition = condition;
            ThenBranch = thenBranch;
            ElseBranch = elseBranch;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    public class ForStatement : Stmt
    {
        public Stmt Initialization { get; set; }
        public Expr Condition { get; set; }
        public Stmt Expresion { get; set; }
        public List<Stmt> Body { get; set; }

        public ForStatement(Stmt initialization, Expr condition, Stmt expresion, List<Stmt> body, int startLine)
            : base(NodeType.IfStatement, startLine)
        {
            Initialization = initialization;
            Condition = condition;
            Expresion = expresion;
            Body = body;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    public class WhileStatement : Stmt
    {
        public Expr Condition { get; set; }
        public List<Stmt> ThenBranch { get; set; }

        public WhileStatement(Expr condition, List<Stmt> thenBranch, int startLine)
            : base(NodeType.WhileStatement, startLine)
        {
            Condition = condition;
            ThenBranch = thenBranch;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    public class DoWhileStatement : Stmt
    {
        public Expr Condition { get; set; }
        public List<Stmt> ThenBranch { get; set; }

        public DoWhileStatement(Expr condition, List<Stmt> thenBranch, int startLine)
            : base(NodeType.WhileStatement, startLine)
        {
            Condition = condition;
            ThenBranch = thenBranch;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    public class FunctionDeclaration : Stmt
    {
        public List<Stmt> Parameters { get; set; }
        public string Name { get; set; }
        public List<Stmt> Children { get; set; }

        public FunctionDeclaration(string name, List<Stmt> parameters, List<Stmt> children)
            : base(NodeType.FunctionDeclaration)
        {
            Name = name;
            Parameters = parameters;
            Children = children;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }


    // EXPRESSION BASE CLASS
    public abstract class Expr : Stmt
    {
        protected Expr(NodeType kind, int startLine) : base(kind, startLine) { }
    }

    public class AssignmentExpr : Expr
    {
        public Identifier? Identifier { get; set; }
        public Expr? Value { get; set; }

        public AssignmentExpr(Identifier? identifier, Expr? value, int startLine)
            : base(NodeType.AssignmentExpr, startLine)
        {
            Identifier = identifier;
            Value = value;
        }
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }


    public class BinaryExpr : Expr
    {
        public Expr Left { get; set; }
        public Expr Right { get; set; }
        public string Operator { get; set; } // Consider defining an enum for specific operators

        public BinaryExpr(Expr left, Expr right, string operatorSymbol, int startLine)
            : base(NodeType.BinaryExpr, startLine)
        {
            Left = left;
            Right = right;
            Operator = operatorSymbol;
        }
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class CallExpr : Expr
    {
        public List<Expr> Arguments { get; set; }
        public Expr Caller { get; set; }

        public CallExpr(Expr caller, List<Expr> arguments, int startLine)
            : base(NodeType.CallExpr, startLine)
        {
            Caller = caller;
            Arguments = arguments;
        }
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class MemberExpr : Expr
    {
        public Expr Object { get; set; }
        public Expr Property { get; set; }
        public bool Computed { get; set; }

        public MemberExpr(Expr obj, Expr property, bool computed, int startLine)
            : base(NodeType.MemberExpr, startLine)
        {
            Object = obj;
            Property = property;
            Computed = computed;
        }
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    // LITERALS
    public class Identifier : Expr
    {
        public string ID { get; set; }
        public AssignmentExpr Assignment { get; set; }
        public Identifier(string iD, int startLine) : base(NodeType.Identifier, startLine)
        {
            ID = iD;
        }
        public Identifier(string iD, int startLine, AssignmentExpr assignment) : base(NodeType.Identifier, startLine)
        {
            ID = iD;
            Assignment = assignment;
        }
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    public class Println : Stmt
    {
        public string? Content { get; set; }
        public Println(int startLine, string? content) : base(NodeType.Println, startLine)
        {
            Content = content;
        }

        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class IntegerLiteral : Expr
    {
        public string type { get; set; }
        public int Value { get; set; }

        public IntegerLiteral(int value, int startLine) : base(NodeType.IntegerLiteral, startLine)
        {
            Value = value;
        }
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    public class DoubleLiteral : Expr
    {
        public double Value { get; set; }

        public DoubleLiteral(double value, int startLine) : base(NodeType.DoubleLiteral, startLine)
        {
            Value = value;
        }
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    public class StringLiteral : Expr
    {
        public string Value { get; set; }

        public StringLiteral(string value, int startLine) : base(NodeType.StringLiteral, startLine)
        {
            Value = value;
        }
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    public class BooleanLiteral : Expr
    {
        public bool Value { get; set; }

        public BooleanLiteral(bool value, int startLine) : base(NodeType.BooleanLiteral, startLine)
        {
            Value = value;
        }
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class Property : Expr
    {
        public string Key { get; set; }
        public Expr Value { get; set; }

        public Property(int startLine, string key, Expr value = null) : base(NodeType.Property, startLine)
        {
            Key = key;
            Value = value;
        }
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);

        }
    }

    public class ObjectLiteral : Expr
    {
        public List<Property> Properties { get; set; }

        public ObjectLiteral(List<Property> properties, int startLine) : base(NodeType.ObjectLiteral, startLine)
        {
            Properties = properties;
        }
        public override void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

}